using LeadAnalytics.Api.Data;
using LeadAnalytics.Api.DTOs.Cloudia;
using LeadAnalytics.Api.DTOs.Response;
using LeadAnalytics.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading.Channels;

namespace LeadAnalytics.Api.Service;

public class LeadService(
    AppDbContext db,
    ILogger<LeadService> logger,
    UnitService unitService,
    AttendantService attendantService,
    LeadAttributionService attributionService)
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<LeadService> _logger = logger;
    private readonly UnitService _unitService = unitService;
    private readonly AttendantService _attendantService = attendantService;
    private readonly LeadAttributionService _attributionService = attributionService;

    public async Task<LeadProcessResponseDto> SaveLeadAsync(CloudiaWebhookDto dto)
    {
        return dto.Type switch
        {
            "CUSTOMER_CREATED" => await CreateLeadAsync(dto.Data),
            "CUSTOMER_UPDATED" => await UpdateLeadAsync(dto.Data),
            "CUSTOMER_TAGS_UPDATED" => await UpdateUserTagAsync(dto),
            "USER_ASSIGNED_TO_CUSTOMER" => await GetProcessAssignment(dto),
            _ => new LeadProcessResponseDto
            {
                Message = $"Tipo de evento desconhecido: {dto.Type}",
                Result = ProcessResult.Ignored,
            }
        };
     }

    public async Task<List<Lead>> GetAllLeadsAsync()
    {
        return await _db.Leads
            .AsNoTracking()
            .ToListAsync();
    }

    private async Task<LeadProcessResponseDto> CreateLeadAsync(CloudiaLeadDataDto dto)
    {
        var externalId = dto.Id;
        var tenantId = dto.ClinicId;

        var existingLead = await _db.Leads
            .Include(l => l.StageHistory)
            .FirstOrDefaultAsync(l =>
                l.ExternalId == externalId &&
                l.TenantId == tenantId);

        if (existingLead is not null)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Lead já existe e será ignorado: {ExternalId} / Tenant {TenantId}",
                externalId, tenantId);
            }

            return new LeadProcessResponseDto {
                LeadId = existingLead.Id,
                Message = "Lead já existe, processo de criação ignorado",
                Result = ProcessResult.Ignored,
                Source = existingLead.Source,
                TrackingConfidence = existingLead.TrackingConfidence
            };
        }

        var phone = dto.Phone ?? throw new ArgumentException("Telefone obrigatório");
        var normalizedPhone = LeadAttributionService.NormalizePhone(phone);

        var unit = await _unitService.GetOrCreateAsync(dto.ClinicId);
        var stageLabel = dto.Stage;
        var stageId = dto.IdStage;

        var originEvent = await _attributionService.FindBestOriginEventAsync(normalizedPhone, tenantId);

        string source, campaign, confidence;
        string? ad;

        if (originEvent is not null)
        {
            var attribution = _attributionService.ExtractAttributionData(originEvent);
            source = attribution.Source;
            campaign = attribution.Campaign;
            ad = attribution.Ad;
            confidence = attribution.Confidence;

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                "🎯 INTERCEPTAÇÃO: Lead {Phone} terá origem da Meta: {Source} / {Campaign}",
                phone, source, campaign);
            }
        }
        else
        {
            (source, campaign, ad, confidence) = ResolverTracking(dto);

            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "⚠️ FALLBACK: Lead {Phone} sem evento da Meta, usando origem Cloudia (baixa confiança)",
                    phone);
            }
        }

        var channel = ResolverChannel(dto);
        var conversationState = dto.ConversationState ?? "bot";

        var newLead = new Lead
        {
            ExternalId = externalId,
            TenantId = tenantId,

            Name = dto.Name ?? "Sem nome",
            Phone = phone,
            Email = dto.Email,
            Cpf = dto.Cpf,
            Gender = dto.Gender,
            Observations = dto.Observations,

            IdFacebookApp = dto.IdFacebookApp,
            HasHealthInsurancePlan = dto.HasHealthInsurancePlan,
            IdChannelIntegration = dto.IdChannelIntegration,
            ConversationState = dto.ConversationState,

            // 🔥 ATRIBUIÇÃO (da Meta ou Cloudia)
            Source = source,
            Channel = ResolverChannel(dto),
            Campaign = campaign,
            Ad = ad,
            TrackingConfidence = confidence,

            CurrentStage = stageLabel ?? "SEM_ETAPA",
            CurrentStageId = stageId,

            Status = "new",
            HasAppointment = GetAppointmentAvailable(stageLabel),
            HasPayment = GetHasPayment(stageLabel),

            Tags = dto.Tags is not null
                ? JsonSerializer.Serialize(dto.Tags)
                : null,

            UnitId = unit.Id,

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ConvertedAt = GetHasPayment(stageLabel) ? DateTime.UtcNow : null,

            StageHistory =
            [
                new LeadStageHistory
                {
                    StageId = stageId ?? 0,
                    StageLabel = stageLabel ?? "SEM_ETAPA",
                    ChangedAt = DateTime.UtcNow
                }
            ],
            Conversations = [
                new LeadConversation
                {
                    Channel = channel,
                    Source = source,
                    ConversationState = conversationState,
                    StartedAt = DateTime.UtcNow,
            
                    Interactions = [
                        new LeadInteraction
                        {
                            Type = "LEAD_CREATED",
                            Content = $"Lead criado via {source}",
                            CreatedAt = DateTime.UtcNow
                        }
                    ]
                }
            ]
        };

        _db.Leads.Add(newLead);
        await _db.SaveChangesAsync();

        if (originEvent is not null)
        {
            await _attributionService.CreateAttributionAsync(
                newLead.Id,
                originEvent,
                normalizedPhone,
                tenantId);
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Lead criado: {ExternalId} / Tenant {TenantId} / Source: {Source}",
            externalId, tenantId, source);
        }

        return new LeadProcessResponseDto {
            LeadId = newLead.Id,
            Message = "Lead criado",
            Result = ProcessResult.Created,
            Source = newLead.Source,
            TrackingConfidence = newLead.TrackingConfidence
        };
    }

    private async Task<LeadProcessResponseDto> UpdateLeadAsync(CloudiaLeadDataDto dto)
    {
        var externalId = dto.Id;
        var tenantId = dto.ClinicId;

        var lead = await _db.Leads
            .Include(l => l.StageHistory)
            .Include(l => l.Conversations)
                .ThenInclude(c => c.Interactions)
            .Include(l => l.Payments)
            .FirstOrDefaultAsync(l =>
                l.ExternalId == externalId &&
                l.TenantId == tenantId);

        if (lead is null)
        {
            _logger.LogWarning("Lead não encontrado para atualizar: {ExternalId} / Tenant {TenantId}",
                externalId, tenantId);
            return new LeadProcessResponseDto
            {
                LeadId = externalId,
                Message = "Lead não encontrado para atualização",
            };
        }

        if (_attributionService.ShouldTryImproveAttribution(lead))
        {
            var normalizedPhone = LeadAttributionService.NormalizePhone(lead.Phone);
            var originEvent = await _attributionService.FindBestOriginEventAsync(normalizedPhone, tenantId);

            if (originEvent is not null && _attributionService.IsEventBetter(originEvent, lead))
            {
                var attribution = _attributionService.ExtractAttributionData(originEvent);

                lead.Source = attribution.Source;
                lead.Campaign = attribution.Campaign;
                lead.Ad = attribution.Ad;
                lead.TrackingConfidence = attribution.Confidence;

                await _attributionService.CreateAttributionAsync(
                    lead.Id,
                    originEvent,
                    normalizedPhone,
                    tenantId);

                _logger.LogInformation(
                    "🔄 MELHORIA: Lead {Phone} teve origem atualizada para {Source}",
                    lead.Phone, lead.Source);
            }
        }

        if (dto.Name is not null) lead.Name = dto.Name;
        if (dto.Phone is not null) lead.Phone = dto.Phone;
        if (dto.Email is not null) lead.Email = dto.Email;
        if (dto.Cpf is not null) lead.Cpf = dto.Cpf;
        if (dto.Gender is not null) lead.Gender = dto.Gender;
        if (dto.Observations is not null) lead.Observations = dto.Observations;
        if (dto.IdFacebookApp is not null) lead.IdFacebookApp = dto.IdFacebookApp;
        if (dto.HasHealthInsurancePlan.HasValue) lead.HasHealthInsurancePlan = dto.HasHealthInsurancePlan;
        if (dto.IdChannelIntegration.HasValue) lead.IdChannelIntegration = dto.IdChannelIntegration;
        if (dto.LastAdId is not null) lead.LastAdId = dto.LastAdId;
        if (dto.ConversationState is not null) lead.ConversationState = dto.ConversationState;

        if (dto.Tags is not null)
            lead.Tags = JsonSerializer.Serialize(dto.Tags);

        lead.Channel = ResolverChannel(dto);

        // ── Conversa / ConversationState ──────────────────────────────
        if (dto.ConversationState is not null &&
            dto.ConversationState != lead.ConversationState)
        {
            var conversaAberta = lead.Conversations
                .FirstOrDefault(c => c.EndedAt is null);

            if (conversaAberta is not null)
            {
                conversaAberta.EndedAt = DateTime.UtcNow;
                conversaAberta.Interactions.Add(new LeadInteraction
                {
                    Type = "STATE_CHANGED",
                    Content = $"{lead.ConversationState} → {dto.ConversationState}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            var novaConversa = new LeadConversation
            {
                LeadId = lead.Id,
                Channel = lead.Channel,
                Source = lead.Source,
                ConversationState = dto.ConversationState,
                StartedAt = DateTime.UtcNow,
                Interactions =
                [
                    new LeadInteraction
                    {
                        Type = "STATE_CHANGED",
                        Content = dto.ConversationState,
                        CreatedAt = DateTime.UtcNow
                    }
                ]
            };

            lead.Conversations.Add(novaConversa);
            lead.ConversationState = dto.ConversationState;
        }

        // ── Stage ─────────────────────────────────────────────────────
        if (dto.Stage is not null)
        {
            var novoStage = dto.Stage;
            var novoStageId = dto.IdStage;

            if (lead.CurrentStage != novoStage || lead.CurrentStageId != novoStageId)
            {
                lead.StageHistory.Add(new LeadStageHistory
                {
                    LeadId = lead.Id,
                    StageId = novoStageId ?? 0,
                    StageLabel = novoStage,
                    ChangedAt = DateTime.UtcNow
                });

                var conversaAtiva = lead.Conversations.FirstOrDefault(c => c.EndedAt is null);
                conversaAtiva?.Interactions.Add(new LeadInteraction
                {
                    Type = "STAGE_CHANGED",
                    Content = $"{lead.CurrentStage} → {novoStage}",
                    CreatedAt = DateTime.UtcNow
                });

                lead.CurrentStage = novoStage;
                lead.CurrentStageId = novoStageId;
            }

            var tinhaPayment = lead.HasPayment;
            lead.HasAppointment = GetAppointmentAvailable(novoStage);
            lead.HasPayment = GetHasPayment(novoStage);

            // ── Pagamento ─────────────────────────────────────────────
            if (!tinhaPayment && lead.HasPayment)
            {
                lead.ConvertedAt = DateTime.UtcNow;

                lead.Payments.Add(new Payment
                {
                    LeadId = lead.Id,
                    Amount = 0,
                    PaidAt = DateTime.UtcNow
                });

                var conversaAtiva = lead.Conversations.FirstOrDefault(c => c.EndedAt is null);
                conversaAtiva?.Interactions.Add(new LeadInteraction
                {
                    Type = "PAYMENT",
                    Content = novoStage,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        lead.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Lead atualizado: {ExternalId} / Tenant {TenantId}", externalId, tenantId);
        }

        return new LeadProcessResponseDto {
            LeadId = lead.Id,
            Message = "Lead atualizado",
            Result = ProcessResult.Updated,
            Source = lead.Source,
            TrackingConfidence = lead.TrackingConfidence,
        };
    }

    public async Task<LeadProcessResponseDto> UpdateUserTagAsync(CloudiaWebhookDto dto)
    {
        var externalId = dto?.Data?.Id;
        var tenantId = dto?.Data?.ClinicId;

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Tags recebidas para {ExternalId}: {Tags}",
                externalId,
                JsonSerializer.Serialize(dto?.Data?.Tags));
        }

        var lead = await _db.Leads
            .FirstOrDefaultAsync(l =>
                l.ExternalId == externalId &&
                l.TenantId == tenantId);

        if (lead is null)
        {
            _logger.LogWarning("Lead não encontrado para atualizar tags: {ExternalId} / Tenant {TenantId}",
                externalId, tenantId);
            return new LeadProcessResponseDto
            {
                LeadId = null,
                Message = "Lead não encontrado para atualização de tags",
                Result = ProcessResult.Ignored,
                Source = null,
                TrackingConfidence = null
            };
        }

        if (dto?.Data?.Tags is not null)
            lead.Tags = JsonSerializer.Serialize(dto.Data.Tags);

        lead.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Tags do lead atualizadas: {ExternalId} / Tenant {TenantId}", externalId, tenantId);
        }
        return new LeadProcessResponseDto
        {
            LeadId = lead.Id,
            Message = "Tags atualizadas",
            Result = ProcessResult.Updated,
            Source = lead.Source,
            TrackingConfidence = lead.TrackingConfidence
        };
    }

    public async Task<int> GetCheckClosedQueries(int clinicId)
    {
        return await _db.Leads
            .AsNoTracking()
            .Where(l =>
                l.UnitId == clinicId &&
                (l.CurrentStage == "10_EM_TRATAMENTO" || l.CurrentStage == "09_FECHOU_TRATAMENTO"))
            .Select(l => l.Id)
            .Distinct()
            .CountAsync();
    }

    public async Task<int> GetCheckStageWithoutPayment(int clinicId)
    {
        return await _db.Leads
            .AsNoTracking()
            .Where(l =>
                l.UnitId == clinicId &&
                l.CurrentStage == "04_AGENDADO_SEM_PAGAMENTO")
            .CountAsync();
    }

    public async Task<int> GetVerifyPaymentStep(int clinicId)
    {
        return await _db.Leads
            .AsNoTracking()
            .Where(l =>
                l.UnitId == clinicId &&
                l.CurrentStage == "05_AGENDADO_COM_PAGAMENTO")
            .CountAsync();
    }

    public async Task<List<object>> GetVerifySourceFinal(int clinicId)
    {
        return await _db.Leads
            .AsNoTracking()
            .Where(l => l.TenantId == clinicId)
            .GroupBy(l => l.Source)
            .Select(g => new
            {
                Origem = g.Key,
                Quantidade = g.Count()
            })
            .ToListAsync<object>();
    }

    public async Task<List<OrigemAgrupadaDto>> GetCheckSourceCloudia(int clinicId)
    {
        return await _db.Leads
            .AsNoTracking()
            .Where(l => l.TenantId == clinicId)
            .GroupBy(l => l.Source)
            .Select(g => new OrigemAgrupadaDto
            {
                Origem = g.Key,
                Quantidade = g.Count()
            })
            .ToListAsync();
    }

    public async Task<List<EtapaAgrupadaDto>> GetCheckGroupedStep(int clinicId)
    {
        return await _db.Leads
            .AsNoTracking()
            .Where(l => l.TenantId == clinicId)
            .GroupBy(l => string.IsNullOrWhiteSpace(l.CurrentStage)
                ? "SEM_ETAPA"
                : l.CurrentStage.Trim())
            .Select(g => new EtapaAgrupadaDto
            {
                Etapa = g.Key,
                Quantidade = g.Count()
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<Lead>> GetWeekendLeads(int clinicId)
    {
        var brazilTz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

        var leads = await _db.Leads
            .AsNoTracking()
            .Where(l => l.TenantId == clinicId)
            .ToListAsync();

        return [.. leads.Where(l =>
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(l.CreatedAt, brazilTz);
            return local.DayOfWeek switch
            {
                DayOfWeek.Saturday => local.Hour >= 18,
                DayOfWeek.Sunday => true,
                DayOfWeek.Monday => local.Hour < 18,
                _ => false
            };
        })];
    }

    public async Task<IEnumerable<Lead>> GetCampaignLeads(int clinicId)
    {
        return await _db.Leads
            .AsNoTracking()
            .Where(l => l.TenantId == clinicId && l.Campaign != null && l.Campaign != "DESCONHECIDO")
            .ToListAsync();
    }

    public async Task<List<Lead>> GetLeadAds(int clinicId)
    {
        return await _db.Leads
            .AsNoTracking()
            .Where(l => l.TenantId == clinicId && l.Ad != null && l.Ad != "DESCONHECIDO")
            .ToListAsync();
    }

    public async Task<IEnumerable<LeadsMesDto>> GetSearchStartMonthLeads(int clinicId, DateTime dataInicio, DateTime finalData)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

        var dataInicioUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(dataInicio, DateTimeKind.Unspecified), tz);

        var dataFinalUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(finalData, DateTimeKind.Unspecified), tz).AddDays(1);

        var leads = await _db.Leads
            .AsNoTracking()
            .Where(l =>
                l.TenantId == clinicId &&
                l.CreatedAt >= dataInicioUtc &&
                l.CreatedAt < dataFinalUtc)
            .ToListAsync();

        return [.. leads
            .GroupBy(l => new { l.CreatedAt.Year, l.CreatedAt.Month })
            .Select(g => new LeadsMesDto
            {
                Ano = g.Key.Year,
                Mes = g.Key.Month,
                Quantidade = g.Count()
            })
            .OrderBy(x => x.Ano)
            .ThenBy(x => x.Mes)];
    }

    public async Task<IEnumerable<LeadsMesDto>> GetQueryLeadsByPeriodService(FiltroLeadsPeriodoDto filtro)
    {
        var leadsQuery = _db.Leads
            .AsNoTracking()
            .Where(l => l.TenantId == filtro.ClinicId);

        var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

        var inicioAnoLocal = new DateTime(filtro.Ano, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var inicioAnoUtc = TimeZoneInfo.ConvertTimeToUtc(inicioAnoLocal, tz);
        var fimAnoUtc = TimeZoneInfo.ConvertTimeToUtc(inicioAnoLocal.AddYears(1), tz);

        leadsQuery = leadsQuery
            .Where(l => l.CreatedAt >= inicioAnoUtc && l.CreatedAt < fimAnoUtc);

        if (filtro.Mes.HasValue)
        {
            var inicioMesLocal = new DateTime(filtro.Ano, filtro.Mes.Value, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var inicioMesUtc = TimeZoneInfo.ConvertTimeToUtc(inicioMesLocal, tz);
            var fimMesUtc = TimeZoneInfo.ConvertTimeToUtc(inicioMesLocal.AddMonths(1), tz);

            leadsQuery = leadsQuery
                .Where(l => l.CreatedAt >= inicioMesUtc && l.CreatedAt < fimMesUtc);
        }

        if (filtro.Dia.HasValue)
        {
            var inicioDiaLocal = new DateTime(
                filtro.Ano,
                filtro.Mes ?? 1,
                filtro.Dia.Value,
                0, 0, 0,
                DateTimeKind.Unspecified);

            var inicioDiaUtc = TimeZoneInfo.ConvertTimeToUtc(inicioDiaLocal, tz);
            var fimDiaUtc = TimeZoneInfo.ConvertTimeToUtc(inicioDiaLocal.AddDays(1), tz);

            leadsQuery = leadsQuery
                .Where(l => l.CreatedAt >= inicioDiaUtc && l.CreatedAt < fimDiaUtc);
        }

        var resultado = await leadsQuery
            .GroupBy(l => new { l.CreatedAt.Year, l.CreatedAt.Month })
            .Select(g => new LeadsMesDto
            {
                Ano = g.Key.Year,
                Mes = g.Key.Month,
                Quantidade = g.Count()
            })
            .OrderBy(x => x.Ano)
            .ThenBy(x => x.Mes)
            .ToListAsync();

        return resultado;
    }

    // ═══════════════════════════════════════════════════════════════
    // 🛠️ MÉTODOS AUXILIARES (sem alteração)
    // ═══════════════════════════════════════════════════════════════

    private static (string Source, string Campaign, string? Ad, string Confidence) ResolverTracking(CloudiaLeadDataDto dto)
    {
        if (dto.AdData is not null && dto.AdData.Count > 0)
        {
            var item = dto.AdData.First();

            var source = !string.IsNullOrWhiteSpace(item.Source)
                ? item.Source.Trim().ToUpperInvariant()
                : (!string.IsNullOrWhiteSpace(dto.Origin)
                    ? dto.Origin.Trim().ToUpperInvariant()
                    : "DESCONHECIDO");

            var campaign = !string.IsNullOrWhiteSpace(item.AdId)
                ? item.AdId.Trim()
                : "DESCONHECIDO";

            var ad = !string.IsNullOrWhiteSpace(item.AdName)
                ? item.AdName.Trim()
                : "DESCONHECIDO";

            return (source, campaign, ad, "ALTA");
        }

        if (!string.IsNullOrWhiteSpace(dto.Origin))
        {
            return (dto.Origin.Trim().ToUpperInvariant(), "DESCONHECIDO", "DESCONHECIDO", "MEDIA");
        }

        return ("DESCONHECIDO", "DESCONHECIDO", "DESCONHECIDO", "BAIXA");
    }

    private static string ResolverChannel(CloudiaLeadDataDto dto)
    {
        if (dto.RegisteredOnWhatsApp == 1 ||
            !string.IsNullOrWhiteSpace(dto.IdWhatsApp) ||
            dto.IdChannelIntegration.HasValue)
        {
            return "WHATSAPP";
        }

        return "DESCONHECIDO";
    }

    private static bool GetAppointmentAvailable(string? stage)
    {
        if (string.IsNullOrWhiteSpace(stage))
            return false;

        return stage is "04_AGENDADO_SEM_PAGAMENTO"
            or "05_AGENDADO_COM_PAGAMENTO"
            or "09_FECHOU_TRATAMENTO"
            or "10_EM_TRATAMENTO";
    }

    private static bool GetHasPayment(string? stage)
    {
        if (string.IsNullOrWhiteSpace(stage))
            return false;

        return stage is "05_AGENDADO_COM_PAGAMENTO"
            or "09_FECHOU_TRATAMENTO"
            or "10_EM_TRATAMENTO";
    }

    private async Task<LeadProcessResponseDto> GetProcessAssignment(CloudiaWebhookDto dto)
    {
        var externalUserId = dto.AssignedUserId!.Value;
        var externalLeadId = dto.Customer!.Id;
        var tenantId = dto.Customer.ClinicId;

        var attendant = await _attendantService.GetOrCreateAsync(
            externalUserId,
            dto.AssignedUserName!,
            dto.AssignedUserEmail);

        var lead = await _db.Leads.FirstOrDefaultAsync(l =>
            l.ExternalId == externalLeadId &&
            l.TenantId == tenantId);

        if (lead is null)
        {
            _logger.LogWarning("Lead não encontrado para atribuição: {LeadId}", externalLeadId);
            return new LeadProcessResponseDto
            {
                Result = ProcessResult.Ignored,
                LeadId = null,
                Message = "Lead não encontrado"
            };
        }

        lead.AttendantId = attendant.Id;
        lead.UpdatedAt = DateTime.UtcNow;

        _db.LeadAssignments.Add(new LeadAssignment
        {
            LeadId = lead.Id,
            AttendantId = attendant.Id,
            Stage = dto.Customer.Stage,
            AssignedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation("Lead {LeadId} atribuído para {Name}", externalLeadId, dto.AssignedUserName);
        return new LeadProcessResponseDto
        {
            LeadId = lead.Id,
            Message = $"Lead atribuído para {dto.AssignedUserName}",
            Result = ProcessResult.Updated,
            Source = lead.Source,
            TrackingConfidence = lead.TrackingConfidence,
        };
    }
}

public enum ProcessResult
{
    Created,
    Updated,
    Ignored
}
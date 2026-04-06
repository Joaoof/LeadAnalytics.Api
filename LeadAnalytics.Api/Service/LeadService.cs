using LeadAnalytics.Api.Data;
using LeadAnalytics.Api.DTOs;
using LeadAnalytics.Api.Models;
using LeadAnalytics.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LeadAnalytics.Api.Service;

public class LeadService(AppDbContext db, ILogger<LeadService> logger, UnitService unitService, AttendantService attendantService)
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<LeadService> _logger = logger;
    private readonly UnitService _unitService = unitService;
    private readonly AttendantService _attendantService = attendantService; // ← adiciona

    public async Task<ProcessResult> SaveLeadAsync(CloudiaWebhookDto dto)
    {
        return dto.Type switch
        {
            "CUSTOMER_CREATED" => await CriarLead(dto.Data),
            "CUSTOMER_UPDATED" => await AtualizarLead(dto.Data),
            "CUSTOMER_TAGS_UPDATED" => await AtualizarTagUsuario(dto),
            "USER_ASSIGNED_TO_CUSTOMER" => await ProcessarAtribuicao(dto), // ← adiciona

            _ => ProcessResult.Ignored
        };
    }

    public async Task<List<Lead>> TrazerTodosLeads()
    {
        return await _db.Leads
            .AsNoTracking()
            .ToListAsync();
    }

    private async Task<ProcessResult> CriarLead(CloudiaLeadDataDto dto)
    {
        var externalId = dto.Id;
        var tenantId = dto.ClinicId;

        var leadExistente = await _db.Leads
            .Include(l => l.StageHistory)
            .FirstOrDefaultAsync(l =>
                l.ExternalId == externalId &&
                l.TenantId == tenantId);

        if (leadExistente is not null)
        {
            _logger.LogInformation("Lead já existe e será ignorado: {ExternalId} / Tenant {TenantId}", externalId, tenantId);
            return ProcessResult.Ignored;
        }

        var unit = await _unitService.GetOrCreateAsync(dto.ClinicId);

        var (source, campaign, ad, confidence) = ResolverTracking(dto);

        var stageLabel = dto.Stage;
        var stageId = dto.IdStage;

        var newLead = new Lead
        {
            ExternalId = externalId,
            TenantId = tenantId,

            Name = dto.Name ?? "Sem nome",
            Phone = dto.Phone ?? "Sem telefone",
            Email = dto.Email,
            Cpf = dto.Cpf,
            Gender = dto.Gender,
            Observations = dto.Observations,
            IdFacebookApp = dto.IdFacebookApp,
            HasHealthInsurancePlan = dto.HasHealthInsurancePlan,
            IdChannelIntegration = dto.IdChannelIntegration,
            LastAdId = dto.LastAdId,
            ConversationState = dto.ConversationState,

            Source = source,
            Channel = ResolverChannel(dto),
            Campaign = campaign,
            Ad = ad,
            TrackingConfidence = confidence,

            CurrentStage = stageLabel,
            CurrentStageId = stageId,

            Status = "new",
            HasAppointment = PossuiAgendamento(stageLabel),
            HasPayment = PossuiPagamento(stageLabel),

            Tags = dto.Tags is not null
                  ? JsonSerializer.Serialize(dto.Tags)
                  : null,

            AdData = dto.AdData is not null
                  ? JsonSerializer.Serialize(dto.AdData)
                  : null,

            UnitId = unit.Id,

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ConvertedAt = PossuiPagamento(stageLabel) ? DateTime.UtcNow : null,

            StageHistory = new List<LeadStageHistory>
            {
                new LeadStageHistory
                {
                    StageId = stageId ?? 0,
                    StageLabel = stageLabel,
                    ChangedAt = DateTime.UtcNow
                }
            }
        };

        _db.Leads.Add(newLead);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Lead criado: {ExternalId} / Tenant {TenantId}", externalId, tenantId);
        return ProcessResult.Created;
    }

    private async Task<ProcessResult> AtualizarLead(CloudiaLeadDataDto dto)
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
            _logger.LogWarning("Lead não encontrado para atualizar: {ExternalId} / Tenant {TenantId}", externalId, tenantId);
            return ProcessResult.Ignored;
        }

        // ── Campos simples ────────────────────────────────────────────
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
        if(dto.ConversationState is not null) lead.ConversationState = dto.ConversationState;

        if (dto.Tags is not null)
            lead.Tags = JsonSerializer.Serialize(dto.Tags);

        if (dto.AdData is not null)
            lead.AdData = JsonSerializer.Serialize(dto.AdData);

        // ── Tracking (só sobrescreve se vier dado melhor) ─────────────
        var (source, campaign, ad, confidence) = ResolverTracking(dto);

        if (source != "DESCONHECIDO") lead.Source = source;
        if (campaign != "DESCONHECIDO") lead.Campaign = campaign;
        if (ad != "DESCONHECIDO") lead.Ad = ad;
        if (confidence == "ALTA" || lead.TrackingConfidence != "ALTA")
            lead.TrackingConfidence = confidence;

        lead.Channel = ResolverChannel(dto);

        // ── Conversa / ConversationState ──────────────────────────────
        if (dto.ConversationState is not null &&
            dto.ConversationState != lead.ConversationState)
        {
            // Fecha a conversa aberta atual, se existir
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

            // Abre nova conversa com o novo estado
            var novaConversa = new LeadConversation
            {
                LeadId = lead.Id,
                Channel = lead.Channel,
                Source = lead.Source,
                ConversationState = dto.ConversationState,
                StartedAt = DateTime.UtcNow,
                Interactions = new List<LeadInteraction>
            {
                new LeadInteraction
                {
                    Type = "STATE_CHANGED",
                    Content = dto.ConversationState,
                    CreatedAt = DateTime.UtcNow
                }
            }
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

                // Registra como interação na conversa ativa
                var conversaAtiva = lead.Conversations.FirstOrDefault(c => c.EndedAt is null);
                if (conversaAtiva is not null)
                {
                    conversaAtiva.Interactions.Add(new LeadInteraction
                    {
                        Type = "STAGE_CHANGED",
                        Content = $"{lead.CurrentStage} → {novoStage}",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                lead.CurrentStage = novoStage;
                lead.CurrentStageId = novoStageId;
            }

            lead.HasAppointment = PossuiAgendamento(novoStage);

            var tinhaPayment = lead.HasPayment;
            lead.HasPayment = PossuiPagamento(novoStage);

            // ── Pagamento ─────────────────────────────────────────────
            if (!tinhaPayment && lead.HasPayment)
            {
                lead.ConvertedAt = DateTime.UtcNow;

                lead.Payments.Add(new Payment
                {
                    LeadId = lead.Id,
                    Amount = 0, // enriquecer quando tiver esse dado no DTO
                    PaidAt = DateTime.UtcNow
                });

                // Registra como interação
                var conversaAtiva = lead.Conversations.FirstOrDefault(c => c.EndedAt is null);
                if (conversaAtiva is not null)
                {
                    conversaAtiva.Interactions.Add(new LeadInteraction
                    {
                        Type = "PAYMENT",
                        Content = novoStage,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        lead.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Lead atualizado: {ExternalId} / Tenant {TenantId}", externalId, tenantId);
        return ProcessResult.Updated;
    }

    public async Task<ProcessResult> AtualizarTagUsuario(CloudiaWebhookDto dto)
    {
        var externalId = dto.Data.Id;
        var tenantId = dto.Data.ClinicId;

        _logger.LogInformation("Tags recebidas para {ExternalId}: {Tags}",
            externalId,
            JsonSerializer.Serialize(dto.Data.Tags));

        var lead = await _db.Leads
            .FirstOrDefaultAsync(l =>
                l.ExternalId == externalId &&
                l.TenantId == tenantId);

        if (lead is null)
        {
            _logger.LogWarning("Lead não encontrado para atualizar tags: {ExternalId} / Tenant {TenantId}", externalId, tenantId);
            return ProcessResult.Ignored;
        }

        if (dto.Data.Tags is not null)
            lead.Tags = JsonSerializer.Serialize(dto.Data.Tags);

        lead.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Tags do lead atualizadas: {ExternalId} / Tenant {TenantId}", externalId, tenantId);
        return ProcessResult.Updated;
    }

    public async Task<int> VerificarConsultasFechadas(int clinicId)
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

    public async Task<int> VerificarEtapaSemPagamento(int clinicId)
    {
        return await _db.Leads
            .AsNoTracking()
            .Where(l =>
                l.UnitId == clinicId &&
                l.CurrentStage == "04_AGENDADO_SEM_PAGAMENTO")
            .CountAsync();
    }

    public async Task<int> VerificarEtapaComPagamento(int clinicId)
    {
        return await _db.Leads
            .AsNoTracking()
            .Where(l =>
                l.UnitId == clinicId &&
                l.CurrentStage == "05_AGENDADO_COM_PAGAMENTO")
            .CountAsync();
    }

    public async Task<List<object>> VerificarSourceFinal(int clinicId)
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

    public async Task<List<OrigemAgrupadaDto>> VerificarOrigemCloudia(int clinicId)
    {
        return await _db.Leads
            .AsNoTracking()
            .Where(l => l.TenantId == clinicId)
            .GroupBy(l => l.Source)
            .Select(g => new OrigemAgrupadaDto
            {
                Origem = g.Key ?? "SEM_ORIGEM",
                Quantidade = g.Count()
            })
            .ToListAsync();
    }

    public async Task<List<EtapaAgrupadaDto>> VerificarEtapaAgrupada(int clinicId)
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

    public async Task<IEnumerable<Lead>> LeadsFinaldeSemana(int clinicId)
    {
        var brazilTz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

        var leads = await _db.Leads
            .AsNoTracking()
            .Where(l => l.TenantId == clinicId)
            .ToListAsync();

        return leads.Where(l =>
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(l.CreatedAt, brazilTz);
            return local.DayOfWeek switch
            {
                DayOfWeek.Saturday => local.Hour >= 18,
                DayOfWeek.Sunday => true,
                DayOfWeek.Monday => local.Hour < 18,
                _ => false
            };
        }).ToList();
    }

    public async Task<IEnumerable<Lead>> LeadsComCampanha(int clinicId)
    {
        return await _db.Leads
            .AsNoTracking()
            .Where(l => l.TenantId == clinicId && l.Campaign != null && l.Campaign != "DESCONHECIDO")
            .ToListAsync();
    }

    public async Task<List<Lead>> LeadsComAd(int clinicId)
    {
        return await _db.Leads
            .AsNoTracking()
            .Where(l => l.TenantId == clinicId && l.Ad != null && l.Ad != "DESCONHECIDO")
            .ToListAsync();
    }

    public async Task<IEnumerable<LeadsMesDto>> BuscarInicioEFimMesLeads(int clinicId, DateTime dataInicio, DateTime finalData)
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

        return leads
            .GroupBy(l => new { l.CreatedAt.Year, l.CreatedAt.Month })
            .Select(g => new LeadsMesDto
            {
                Ano = g.Key.Year,
                Mes = g.Key.Month,
                Quantidade = g.Count()
            })
            .OrderBy(x => x.Ano)
            .ThenBy(x => x.Mes)
            .ToList();
    }

    public async Task<IEnumerable<LeadsMesDto>> ConsultaLeadsPorPeriodoService(FiltroLeadsPeriodoDto filtro)
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

    private static bool PossuiAgendamento(string? stage)
    {
        if (string.IsNullOrWhiteSpace(stage))
            return false;

        return stage is "04_AGENDADO_SEM_PAGAMENTO"
            or "05_AGENDADO_COM_PAGAMENTO"
            or "09_FECHOU_TRATAMENTO"
            or "10_EM_TRATAMENTO";
    }

    private static bool PossuiPagamento(string? stage)
    {
        if (string.IsNullOrWhiteSpace(stage))
            return false;

        return stage is "05_AGENDADO_COM_PAGAMENTO"
            or "09_FECHOU_TRATAMENTO"
            or "10_EM_TRATAMENTO";
    }

    private async Task<ProcessResult> ProcessarAtribuicao(CloudiaWebhookDto dto)
    {
        // 1. Pega os dados do atendente e do lead
        var externalUserId = dto.AssignedUserId!.Value;
        var externalLeadId = dto.Customer!.Id;
        var tenantId = dto.Customer.ClinicId;

        // 2. Busca ou cria o atendente
        var attendant = await _attendantService.GetOrCreateAsync(
            externalUserId,
            dto.AssignedUserName!,
            dto.AssignedUserEmail);

        // 3. Busca o lead no banco
        var lead = await _db.Leads.FirstOrDefaultAsync(l =>
            l.ExternalId == externalLeadId &&
            l.TenantId == tenantId);

        if (lead is null)
        {
            _logger.LogWarning("Lead não encontrado para atribuição: {LeadId}", externalLeadId);
            return ProcessResult.Ignored;
        }

        // 4. Atualiza o atendente atual no lead
        lead.AttendantId = attendant.Id;
        lead.UpdatedAt = DateTime.UtcNow;

        // 5. Salva o histórico de atribuição
        _db.LeadAssignments.Add(new LeadAssignment
        {
            LeadId = lead.Id,
            AttendantId = attendant.Id,
            Stage = dto.Customer.Stage,
            AssignedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation("Lead {LeadId} atribuído para {Name}", externalLeadId, dto.AssignedUserName);
        return ProcessResult.Updated;
    }
}

public enum ProcessResult
{
    Created,
    Updated,
    Ignored
}
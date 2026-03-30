using LeadAnalytics.Api.Data;
using LeadAnalytics.Api.DTOs;
using LeadAnalytics.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LeadAnalytics.Api.Service;

public class LeadService(AppDbContext db, ILogger<LeadService> logger, UnitService unitService)
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<LeadService> _logger = logger;
    private readonly UnitService _unitService = unitService;

    public async Task<ProcessResult> SaveLeadAsync(CloudiaWebhookDto dto)
    {

        return dto.Type switch
        {
            "CUSTOMER_CREATED" => await CriarLead(dto.Data),
            "CUSTOMER_UPDATED" => await AtualizarLead(dto.Data),
            "CUSTOMER_TAGS_UPDATED" => await AtualizarTagUsuário(dto),
            _ => ProcessResult.Ignored
        };
    }


    public async Task<List<Lead>> TrazerTodosLeads()
    {
        var leads = await _db.Leads.ToListAsync();

        return leads;
    }

    private async Task<ProcessResult> CriarLead(CloudiaLeadDataDto dto)
    {
        var externalId = dto.Id;
        var tenantId = dto.ClinicId;

        // 1. Esse lead já existe no banco?
        var searchLead = await _db.Leads
            .FirstOrDefaultAsync(l =>
                l.ExternalId == externalId &&
                l.TenantId == tenantId);

        var unit = await _unitService.GetOrCreateAsync(dto.ClinicId);

        // 2. Já existe — ignora
        if (searchLead is not null)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
            }
            return ProcessResult.Ignored;
        }

        // 3. Não existe — cria
        var newLead = new Lead
        {
            Id = dto.Id,
            ExternalId = externalId,
            TenantId = tenantId,
            Name = dto.Name ?? "Sem nome",
            Phone = dto.Phone ?? "Sem telefone",
            Email = dto.Email,
            Origin = dto.Origin ?? "Sem origem",
            Stage = dto.Stage,
            Tags = dto.Tags is not null              // ← adiciona
                 ? JsonSerializer.Serialize(dto.Tags)
                 : null,
            AdData = dto.AdData is not null
                ? JsonSerializer.Serialize(dto.AdData)
                : null,
            UnitId = unit.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Leads.Add(newLead);
        await _db.SaveChangesAsync();

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Lead criado: {Id}", externalId);
        }

        return ProcessResult.Created;
    }

    private async Task<ProcessResult> AtualizarLead(CloudiaLeadDataDto dto)
    {
        var externalId = dto.Id;
        var tenantId = dto.ClinicId;

        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.ExternalId == externalId &&
            l.TenantId == tenantId);

        if (lead is null)
        {
            _logger.LogWarning("Lead não encontrado para atualizar: {Id}", externalId);
            return ProcessResult.Ignored;
        }

        // Existe — atualiza só os campos que vieram preenchidos
        if (dto.Name is not null) lead?.Name = dto.Name;
        if (dto.Phone is not null) lead?.Phone = dto.Phone;
        if (dto.Email is not null) lead?.Email = dto.Email;
        if (dto.Stage is not null) lead?.Stage = dto.Stage;
        if (dto.Tags is not null) lead?.Tags = JsonSerializer.Serialize(dto.Tags);
        if (dto.AdData is not null)
        {
            // salva JSON bruto
            lead?.AdData = JsonSerializer.Serialize(dto.AdData);

            var lista = dto.AdData
                .Select(a => new AdDataDto
                {
                    Id = a.AdId ?? string.Empty,
                    AdName = a.AdName ?? string.Empty,
                    source = a.Source ?? string.Empty
                })
                .ToList();

            if (dto.AdData is not null && dto.AdData.Count > 0)
            {
                var item = lista[0];

                lead?.Campaign = string.IsNullOrEmpty(item.Id) ? "DESCONHECIDA" : item.Id;
                lead?.Ad = string.IsNullOrEmpty(item.AdName) ? "DESCONHECIDO" : item.AdName;
                lead?.SourceFinal = !string.IsNullOrEmpty(item.source) ? item.source : dto.Origin ?? "DESCONHECIDO";
                lead?.TrackingConfidence = "ALTA";
            }
        }
        else
        {
            // fallback
            if (!string.IsNullOrEmpty(dto.Origin))
            {
                lead?.SourceFinal = dto.Origin;
                lead?.TrackingConfidence = "MEDIA";
            }
            else
            {
                lead?.SourceFinal = "DESCONHECIDO";
                lead?.TrackingConfidence = "BAIXA";
            }

            lead?.Campaign = "DESCONHECIDA";
            lead?.Ad = "DESCONHECIDO";

            if (dto.AdData is not null)
            {
                lead?.AdData = JsonSerializer.Serialize(dto.AdData);
            }

        }
        if (dto.Observations is not null) lead?.Observations = dto.Observations;

        if (dto.Stage == "10_EM_TRATAMENTO" || dto.Stage == "09_FECHOU_TRATAMENTO")
        {
            lead?.HasAppointment = true;
        }

        if (dto.Stage == "03_LEAD_QUENTE_QUALIFICADO")
        {
            lead?.HasAppointment = false;
        }

        lead?.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Lead atualizado: {Id}", externalId);
        return ProcessResult.Updated;
    }

    public async Task<ProcessResult> AtualizarTagUsuário(CloudiaWebhookDto dto)
    {
        var externalId = dto.Data.Id;
        var tenantId = dto.Data.ClinicId;

        _logger.LogInformation("Tags recebidas: {Tags}", JsonSerializer.Serialize(dto.Data.Tags));


        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.ExternalId == externalId &&
        l.TenantId == tenantId);

        if (lead is null)
        {
            _logger.LogWarning("Lead não encontrado para atualizar: {Id}", externalId);
            return ProcessResult.Ignored;
        }

        if (dto.Data.Tags is not null) lead?.Tags = JsonSerializer.Serialize(dto.Data.Tags);
        lead?.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Lead atualizado: {Id}", externalId);
        return ProcessResult.Updated;
    }

    public async Task<ProblemDetails> VerificarConsultasFechadas(int clinicId)
    {
        var verificarLeads = await _db.Units
            .Where(l => l.ClinicId == clinicId)
            .Select(l => l.Leads.Select(l => l.Stage == "10_EM_TRATAMENTO" || l.Stage == "9_FECHOU_TRATAMENTO"))
            .ToListAsync();

        if (verificarLeads.Count == 0)
        {
            return new ProblemDetails
            {
                Status = 404,
                Title = "Nenhuma consulta fechada" 
            };
        }

        return new ProblemDetails
        {
            Status = 200,
            Title = "Consultas encontradas",
            Extensions = new Dictionary<string, object?> { ["count"] = verificarLeads.Count }
        };
    }

    public async Task<ProblemDetails> VerificarEtapaSemPagamento(int clinicId)
    {
        var verificarPagamento = await _db.Units
            .Where(l => l.ClinicId == clinicId)
            .Select(l => l.Leads.Select(l => l.Stage == "04_AGENDADO_SEM_PAGAMENTO")).ToListAsync();

        if (verificarPagamento.Count == 0)
        {
            return new ProblemDetails
            {
                Status = 404,
                Title = "Nenhuma consulta agendada sem pagamento"
            };
        }

        return new ProblemDetails
        {
            Status = 200,
            Title = "Agendados sem pagamento",
            Extensions = new Dictionary<string, object?> { ["quantidade"] = verificarPagamento.Count }
        };
    }

    public async Task<int> VerificarEtapaComPagamento(int clinicId)
    {
        return await _db.Leads
            .Where(l => l.UnitId == clinicId &&
                        l.Stage == "05_AGENDADO_COM_PAGAMENTO")
            .CountAsync();
    }

    //public async Task<List<CloudiaWebhookDto>> PegarAnunciosLeads(CloudiaWebhookDto dto, int clinicId)
    //{
    //    var leads = await _db.Leads.Where(l => l.Unit.ClinicId == clinicId && l.AdData != null).ToListAsync();

    //    if(dto.Data.AdData)
    //    {

    //    }


    //}

    //public async Task<>
}

public enum ProcessResult { Created, Updated, Ignored }

using LeadAnalytics.Api.Data;
using LeadAnalytics.Api.DTOs;
using LeadAnalytics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LeadAnalytics.Api.Service;

public class LeadService(AppDbContext db, ILogger<LeadService> logger)
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<LeadService> _logger = logger;

    public async Task<ProcessResult> SaveLeadAsync(CloudiaWebhookDto dto)
    {
        
        return dto.Type switch
        {
            "CUSTOMER_CREATED" => await CriarLead(dto.Data),
            "CUSTOMER_UPDATED" => await AtualizarLead(dto.Data),
            _ => ProcessResult.Ignored
        };
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

        // 2. Já existe — ignora
        if (searchLead is not null)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Lead duplicado ignorado: {Id}", externalId);
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

        // Existe — atualiza só os campos que vieram preenchidos
        if (dto.Name is not null) lead.Name = dto.Name;
        if (dto.Phone is not null) lead.Phone = dto.Phone;
        if (dto.Email is not null) lead.Email = dto.Email;
        if (dto.Stage is not null) lead.Stage = dto.Stage;
        if (dto.Observations is not null) lead.Observations = dto.Observations;

        lead.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Lead atualizado: {Id}", externalId);
        return ProcessResult.Updated;
    }
}

public enum ProcessResult { Created, Updated, Ignored }

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
        var externalId = dto.Data.Id.ToString();
        var tenantId = dto.Data.ClinicId.ToString();

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
            Id = dto.Data.Id,
            ExternalId = externalId,
            TenantId = tenantId,
            Name = dto.Data.Name ?? "Sem nome",
            Phone = dto.Data.Phone ?? "Sem telefone",
            Email = dto.Data.Email,
            Origin = dto.Data.Origin ?? "Sem origem",
            Stage = dto.Data.Stage,
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
}

public enum ProcessResult { Created, Updated, Ignored }
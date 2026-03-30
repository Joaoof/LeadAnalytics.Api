using LeadAnalytics.Api.Data;
using LeadAnalytics.Api.DTOs;
using LeadAnalytics.Api.Models;
using LeadAnalytics.Api.Services;
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
            Tags = dto.Tags is not null              // ← adiciona
                 ? JsonSerializer.Serialize(dto.Tags)
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
}

public enum ProcessResult { Created, Updated, Ignored }

using LeadAnalytics.Api.Data;
using LeadAnalytics.Api.DTOs;
using LeadAnalytics.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LeadAnalytics.Api.Service;

public class SyncN8N(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    public async Task SyncLead(SyncLeadDto dto)
    {
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l =>
                l.ExternalId == dto.ExternalId &&
                l.TenantId == dto.TenantId);

        if (lead == null)
        {
            lead = new Lead
            {
                ExternalId = dto.ExternalId,
                TenantId = dto.TenantId,
                Name = dto.Name ?? "NOME DESCONHECIDO",
                Phone = dto.Phone ?? "TELEFONE DESCONHECIDO",
                CurrentStage = dto.Stage ?? "ESTÁGIO DESCONHECIDO",
                Tags = JsonSerializer.Serialize(dto.Tags ?? []),
                CreatedAt = dto.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = dto.UpdatedAt ?? DateTime.UtcNow
            };

            _db.Leads.Add(lead);
        }
        else
        {
            if (dto.Name != null)
                lead.Name = dto.Name;

            if (dto.Phone != null)
                lead.Phone = dto.Phone;

            if (dto.Stage != null)
                lead.CurrentStage = dto.Stage;

            if (dto.Tags != null && dto.Tags.Count > 0)
            {
                var existingTags = string.IsNullOrEmpty(lead.Tags)
                    ? []
                    : JsonSerializer.Deserialize<List<string>>(lead.Tags)!;

                var merged = existingTags
                    .Union(dto.Tags)
                    .Distinct()
                    .ToList();

                lead.Tags = JsonSerializer.Serialize(merged);
            }

            if(dto.CreatedAt != null)
                lead.CreatedAt = dto.CreatedAt.Value;

            if (dto.UpdatedAt != null)
                lead.UpdatedAt = dto.UpdatedAt.Value;
        }

        await _db.SaveChangesAsync();
    }
}

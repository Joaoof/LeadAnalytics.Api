using LeadAnalytics.Api.Data;
using LeadAnalytics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LeadAnalytics.Api.Service;

public class UnitService
{
    private readonly AppDbContext _db;
    private readonly ILogger<UnitService> _logger;

    public UnitService(AppDbContext db, ILogger<UnitService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Unit> GetOrCreateAsync(int clinicId)
    {
        var unit = await _db.Units
            .FirstOrDefaultAsync(u => u.ClinicId == clinicId);

        if (unit is null)
        {
            var name = clinicId == 8020
                ? $"Unidade de Araguaína {clinicId}"
                : $"Unidade {clinicId}";

            unit = new Unit
            {
                ClinicId = clinicId,
                Name = name,
                CreatedAt = DateTime.UtcNow
            };

            _db.Units.Add(unit);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Unidade criada automaticamente: {ClinicId}", clinicId);
        }

        return unit;
    }

    // Lista todas as unidades
    public async Task<List<Unit>> GetAllAsync()
    {
        return await _db.Units
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    public async Task<List<Unit>> GetQuantityLeadsUnit(int clinicId)
    {
        // Carrega unidades do clinicId
        var units = await _db.Units
            .Where(u => u.ClinicId == clinicId)
            .ToListAsync();

        if (units.Count == 0)
            return units;

        // Obtém os Ids das unidades carregadas
        var unitIds = units.Select(u => u.Id).ToList();

        // Carrega leads que pertençam a essas unidades
        var leads = await _db.Leads
            .Where(l => l.UnitId != null && unitIds.Contains(l.UnitId.Value))
            .ToListAsync();

        // Agrupa leads por UnitId e associa a cada unidade
        var leadsByUnit = leads
            .GroupBy(l => l.UnitId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var unit in units)
        {
            leadsByUnit.TryGetValue(unit.Id, out var list);
            unit.Leads = list ?? [];
        }

        return units;
    }
}
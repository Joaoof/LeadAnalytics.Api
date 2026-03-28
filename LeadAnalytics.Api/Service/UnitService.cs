using LeadAnalytics.Api.Data;
using LeadAnalytics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LeadAnalytics.Api.Services;

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
            unit = new Unit
            {
                ClinicId = clinicId,
                Name = $"Unidade {clinicId}",
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
}
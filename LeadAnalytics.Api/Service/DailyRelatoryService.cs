using LeadAnalytics.Api.Data;
using LeadAnalytics.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LeadAnalytics.Api.Service;

public class DailyRelatoryService(AppDbContext db)
{
    private readonly AppDbContext _db = db;
    public async Task<List<DailyRelatoryDto>> GenerateDailyRelatory(int tenantId, DateTime date)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

        var inicioDia = TimeZoneInfo.ConvertTimeToUtc(
            new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified), tz);

        var fimDia = TimeZoneInfo.ConvertTimeToUtc(
            new DateTime(date.Year, date.Month, date.Day, 23, 59, 59, DateTimeKind.Unspecified), tz);

        var assignment = await _db.LeadAssignments
            .Include(a => a.Attendant)
            .Include(a => a.Lead).ThenInclude(u => u.Unit)
            .Where(a =>
                a.Lead.TenantId == tenantId &&
                a.AssignedAt >= inicioDia &&
                a.AssignedAt <= fimDia)
            .ToListAsync();

        return [.. assignment
            .GroupBy(a => new { a.AttendantId, a.Attendant.Name, a.Attendant.Phone })
            .Select(g => new DailyRelatoryDto
            {
                Atendente = g.Key.Name,
                Telefone = g.Key.Phone,
                TotalLeads = g.Count(),
                Agendamentos = g.Count(a => a.Lead.HasAppointment),
                ComPagamento = g.Count(a => a.Lead.HasPayment),
                Unidades = [.. g
                    .Where(a => a.Lead.Unit != null)
                    .Select(a => a.Lead.Unit!.Name)
                    .Distinct()]
            })
            .OrderByDescending(x => x.TotalLeads)];
    }
}

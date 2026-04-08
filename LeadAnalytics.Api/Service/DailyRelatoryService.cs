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
                Agendamentos = g.Count(a => PossuiAgendamento(a.Lead.CurrentStage)),
                ComPagamento = g.Count(a => PossuiPagamento(a.Lead.CurrentStage)),
                Observacoes = string.Join(" | ", g
                .Where(a => a.Lead.Observations != null)
                .Select(a => a.Lead.Observations)),
                Unidades = [.. g
                    .Where(a => a.Lead.Unit != null)
                    .Select(a => a.Lead.Unit!.Name)
                    .Distinct()]
            })
            .OrderByDescending(x => x.TotalLeads)];
    }

    private static bool PossuiPagamento(string? stage)
    {
        return stage == "10_EM_TRATAMENTO"
            || stage == "09_FECHOU_TRATAMENTO";
    }

    private static bool PossuiAgendamento(string? stage)
    {
        return stage == "04_AGENDADO_SEM_PAGAMENTO"
            || stage == "05_AGENDADO_COM_PAGAMENTO";
    }
}

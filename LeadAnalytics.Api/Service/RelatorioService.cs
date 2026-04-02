using LeadAnalytics.Api.Data;
using LeadAnalytics.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LeadAnalytics.Api.Service;

/// <summary>
/// Orquestra a geração do relatório mensal:
/// consulta os dados com projeção otimizada, computa KPIs e agrupamentos em memória
/// e delega a renderização ao IPdfRelatorioService.
/// </summary>
public class RelatorioService(AppDbContext db, IPdfRelatorioService pdfService) : IRelatorioService
{
    private static readonly TimeZoneInfo BrazilTz =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public async Task<byte[]> GerarRelatorioMensalAsync(int clinicId, int mes, int ano, CancellationToken ct)
    {
        var (inicioUtc, fimUtc) = ObterIntervaloUtc(mes, ano);

        // ── Query principal ────────────────────────────────────────────────
        // Uma única consulta com projeção direta: sem ToList desnecessário,
        // sem carregar entidades completas, sem N+1 (Payments somados no banco).
        var leads = await db.Leads
            .AsNoTracking()
            .Where(l => l.TenantId == clinicId
                     && l.CreatedAt >= inicioUtc
                     && l.CreatedAt < fimUtc)
            .Select(l => new LeadProjecaoRelatorio(
                l.Name,
                l.Phone,
                l.Origin,
                l.Stage ?? l.CurrentStage,
                l.UnitId,
                l.HasAppointment,
                l.CreatedAt,
                l.Payments.Sum(p => (decimal?)p.Amount) ?? 0m
            ))
            .ToListAsync(ct);

        if (leads.Count == 0)
            return [];

        // ── Resolução dos nomes de unidade ────────────────────────────────
        // Query separada e pequena; evita N+1 ao não fazer join na query principal.
        var unitIds = leads
            .Where(l => l.UnitId.HasValue)
            .Select(l => l.UnitId!.Value)
            .Distinct()
            .ToList();

        var unidadesMap = unitIds.Count > 0
            ? await db.Units
                .AsNoTracking()
                .Where(u => u.ClinicId == clinicId && unitIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name })
                .ToDictionaryAsync(u => u.Id, u => u.Name, ct)
            : [];

        // Não existe entidade Tenant — usa o nome da primeira unidade como proxy
        var nomeClinica = unidadesMap.Values.FirstOrDefault() ?? $"Clínica #{clinicId}";

        // ── KPIs ──────────────────────────────────────────────────────────
        var totalLeads = leads.Count;

        var totalComConsulta = leads.Count(l => l.HasAppointment);
        var taxaConversao = totalLeads > 0
            ? Math.Round((decimal)totalComConsulta / totalLeads * 100, 2)
            : 0m;

        var leadsComPagamento = leads.Where(l => l.TotalPago > 0).ToList();
        var ticketMedio = leadsComPagamento.Count > 0
            ? Math.Round(leadsComPagamento.Average(l => l.TotalPago), 2)
            : 0m;

        // ── Agrupamentos (processados em memória sobre projeção leve) ──────
        var leadsPorOrigem = leads
            .GroupBy(l => l.Origem ?? "Não informado")
            .Select(g => new OrigemAgrupadaDto { Origem = g.Key, Quantidade = g.Count() })
            .OrderByDescending(x => x.Quantidade)
            .ToList();

        var leadsPorEtapa = leads
            .GroupBy(l => l.Stage ?? "Não informado")
            .Select(g => new EtapaAgrupadaDto { Etapa = g.Key, Quantidade = g.Count() })
            .OrderByDescending(x => x.Quantidade)
            .ToList();

        var leadsPorUnidade = leads
            .GroupBy(l => l.UnitId)
            .Select(g =>
            {
                var nome = g.Key.HasValue && unidadesMap.TryGetValue(g.Key.Value, out var n)
                    ? n
                    : "Sem unidade";
                return new UnidadeRelatorioDto(g.Key, nome, g.Count());
            })
            .OrderByDescending(x => x.QuantidadeLeads)
            .ToList();

        // Converte para timezone local apenas para agrupar por dia correto
        var leadsPorDia = leads
            .GroupBy(l => TimeZoneInfo.ConvertTimeFromUtc(l.CreatedAt, BrazilTz).Day)
            .Select(g => new LeadsPorDiaDto(g.Key, g.Count()))
            .OrderBy(x => x.Dia)
            .ToList();

        // ── Listagem detalhada ─────────────────────────────────────────────
        var listaDetalhada = leads
            .Select(l => new LeadRelatorioItemDto(
                l.Nome,
                l.Telefone,
                l.Origem ?? "Não informado",
                l.Stage ?? "Não informado",
                TimeZoneInfo.ConvertTimeFromUtc(l.CreatedAt, BrazilTz)
            ))
            .OrderBy(l => l.CriadoEm)
            .ToList();

        // ── Montagem do DTO e geração do PDF ──────────────────────────────
        var dados = new RelatorioMensalDadosDto
        {
            NomeClinica = nomeClinica,
            Mes = mes,
            Ano = ano,
            GeradoEm = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrazilTz),
            TotalLeads = totalLeads,
            TaxaConversaoPercent = taxaConversao,
            TicketMedio = ticketMedio,
            LeadsPorOrigem = leadsPorOrigem,
            LeadsPorEtapa = leadsPorEtapa,
            LeadsPorUnidade = leadsPorUnidade,
            LeadsPorDia = leadsPorDia,
            Leads = listaDetalhada
        };

        return pdfService.Gerar(dados);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (DateTime inicioUtc, DateTime fimUtc) ObterIntervaloUtc(int mes, int ano)
    {
        var inicioLocal = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var fimLocal = inicioLocal.AddMonths(1);

        return (
            TimeZoneInfo.ConvertTimeToUtc(inicioLocal, BrazilTz),
            TimeZoneInfo.ConvertTimeToUtc(fimLocal, BrazilTz)
        );
    }
}

// Projeção interna — não faz parte da API pública do serviço.
// Contém apenas os campos necessários para o relatório; evita carregar a entidade completa.
internal sealed record LeadProjecaoRelatorio(
    string Nome,
    string? Telefone,
    string? Origem,
    string Stage,
    int? UnitId,
    bool HasAppointment,
    DateTime CreatedAt,
    decimal TotalPago
);

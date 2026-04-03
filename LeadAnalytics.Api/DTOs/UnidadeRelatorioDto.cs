namespace LeadAnalytics.Api.DTOs;

public sealed record UnidadeRelatorioDto(
    int? UnitId,
    string NomeUnidade,
    int QuantidadeLeads
);

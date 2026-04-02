namespace LeadAnalytics.Api.DTOs;

public sealed record LeadRelatorioItemDto(
    string Nome,
    string? Telefone,
    string Origem,
    string Stage,
    DateTime CriadoEm
);

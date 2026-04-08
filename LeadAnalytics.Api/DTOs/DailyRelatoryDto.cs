namespace LeadAnalytics.Api.DTOs;

public class DailyRelatoryDto
{
    public string? Atendente { get; set; }
    public string? Telefone { get; set; }
    public int TotalLeads { get; set; }
    public int Agendamentos { get; set; }
    public int ComPagamento { get; set; }
    public List<string> Unidades { get; set; } = new();
}
namespace LeadAnalytics.Api.DTOs;

public class FiltroLeadsPeriodoDto
{
    public int ClinicId { get; set; }          // obrigatório
    public int Ano { get; set; }               // obrigatório
    public int? Mes { get; set; }              // opcional
    public int? Semana { get; set; }           // opcional (ISO)
    public int? Dia { get; set; }              // opcional
}

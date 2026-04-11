namespace LeadAnalytics.Api.DTOs.Response;

public class OrigemAgrupadaDto
{
    public string Origem { get; set; } = null!;
    public int Quantidade { get; set; }
}

public class EtapaAgrupadaDto
{
    public string Etapa { get; set; } = null!;
    public int Quantidade { get; set; }
}

public class LeadsMesDto
{
    public int Ano { get; set; }
    public int Mes { get; set; }
    public int Quantidade { get; set; }
}
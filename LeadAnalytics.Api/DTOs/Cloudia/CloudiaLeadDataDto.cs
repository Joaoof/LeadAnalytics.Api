namespace LeadAnalytics.Api.DTOs.Cloudia;

public class CloudiaLeadDataDto
{
    // Identidade
    public int Id { get; set; }
    public int ClinicId { get; set; }

    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Cpf { get; set; }
    public string? Gender { get; set; }

    public string? Stage { get; set; }
    public int? IdStage { get; set; }

    public string? IdWhatsApp { get; set; }
    public string? IdFacebookApp { get; set; }
    public int? IdChannelIntegration { get; set; }
    public int? RegisteredOnWhatsApp { get; set; }

    public string? ConversationState { get; set; }

    public string? Observations { get; set; }
    public bool? HasHealthInsurancePlan { get; set; }
    public string? LastAdId { get; set; }

    public string? Origin { get; set; }
    public List<CloudiaAdDataDto>? AdData { get; set; }
    public List<string>? Tags { get; set; }
}
namespace LeadAnalytics.Api.Models;

public class Lead
{
    // ─── Identificação ───────────────────────────
    public int Id { get; set; }
    public int ExternalId { get; set; }
    public int TenantId { get; set; }

    // ─── Dados do lead ───────────────────────────
    public string Name { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Email { get; set; }
    public string? Cpf { get; set; }
    public string? Gender { get; set; }
    public string? Origin { get; set; }
    public string? Observations { get; set; }
    public string? IdFacebookApp { get; set; }
    public bool? HasHealthInsurancePlan { get; set; }
    public string? Stage { get; set; }
    public int? IdStage { get; set; }
    public string Status { get; set; } = "new";
    public bool HasAppointment { get; set; } = false;
    public string? ConversationState { get; set; }

    // ─── Anúncio ─────────────────────────────────
    public string? LastAdId { get; set; }
    public int? IdChannelIntegration { get; set; }

    public string? Tags { get; set; }
    public string? AdData { get; set; }
    // Dados do anúncio salvos como JSON

    // ─── Datas ───────────────────────────────────
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ConvertedAt { get; set; }

    // Relacionamento com a unidade
    public int? UnitId { get; set; }
    public Unit? Unit { get; set; }
}
namespace LeadAnalytics.Api.Models;

public class Lead
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; } = null!;
    public string TenantId { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Email { get; set; }
    public string? Cpf { get; set; }
    public string? IdFacebookApp { get; set; }
    public string? Gender { get; set; }

    public string? Origin { get; set; }
    public string? AdData { get; set; }

    public string? Stage { get; set; }
    public int? IdStage { get; set; } // 🔥 novo

    public string Status { get; set; } = "new";
    public bool HasAppointment { get; set; } = false;

    public decimal? Value { get; set; } // 🔥 novo

    public string? Tags { get; set; }
    public string? Observations { get; set; }

    public string? ConversationState { get; set; } // 🔥 novo
    public string? CustomFields { get; set; } // 🔥 novo
    public string? LastAdId { get; set; } // 🔥 novo

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ConvertedAt { get; set; }
}
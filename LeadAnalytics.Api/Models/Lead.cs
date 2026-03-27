using System.ComponentModel;

namespace LeadAnalytics.Api.Models;

public class Lead
{
    public Guid Id { get; set; }
    [Description("o ID que vem da Cloudia ou qualquer outra empresa")]
    public string ExternalId { get; set; } = null!;
    [Description("Qual clínica pertence o id")]
    public string TenantId { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Email { get; set; }

    public string? Origin { get; set; }
    public string? Unit { get; set; }

    [Description("Em que etapa do funil está o lead")]
    public string Status { get; set; } = "new";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ConvertedAt { get; set; }
}
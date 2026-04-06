namespace LeadAnalytics.Api.Models;

public class Attendant
{
    public int Id { get; set; }
    // Gerado automaticamente pelo banco

    public int ExternalId { get; set; }
    // ID do atendente na Cloudia — vem do assigned_user_id

    public string Name { get; set; } = null!;
    // Nome do atendente — vem do assigned_user_name

    public string? Email { get; set; }
    // Email do atendente — vem do assigned_user_email

    public DateTime CreatedAt { get; set; }

    // Leads atribuídos a esse atendente
    public List<LeadAssignment> Assignments { get; set; } = new();
}
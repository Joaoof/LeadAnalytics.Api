namespace LeadAnalytics.Api.Models;

public class Attendant
{
    public int Id { get; set; }
    0public int ExternalId { get; set; }
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; } // ← adiciona
    public DateTime CreatedAt { get; set; }
    public List<LeadAssignment> Assignments { get; set; } = [];
}
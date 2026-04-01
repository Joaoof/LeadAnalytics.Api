namespace LeadAnalytics.Api.Models;

public class LeadStageHistory
{
    public int Id { get; set; }
    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;
    public string Stage { get; set; } = null!;
    public int? StageId { get; set; }
    public DateTime ChangedAt { get; set; }
}
using System.Text.Json.Serialization;

namespace LeadAnalytics.Api.DTOs;

public class SyncLeadDto
{
    public int ExternalId { get; set; }
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("Phone")]
    public string? Phone { get; set; }
    [JsonPropertyName("stage_label")]
    public string? Stage { get; set; }
    [JsonPropertyName("clinic_id")]
    public int TenantId { get; set; }
    [JsonPropertyName("creation_date")]
    public DateTime? CreatedAt { get; set; }
    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
using System.Text.Json.Serialization;

namespace LeadAnalytics.Api.DTOs.Cloudia;

public class AdDataDto
{
    [JsonPropertyName("ad_id")]
    public string Id { get; set; }
    [JsonPropertyName("ad_name")]
    public string AdName { get; set; }
    [JsonPropertyName("source")]
    public string Source { get; set; }
}

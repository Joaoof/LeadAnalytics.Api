using System.Text.Json.Serialization;

namespace LeadAnalytics.Api.DTOs;

public class CloudiaWebhookDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("data")]
    public CloudiaLeadDataDto Data { get; set; } = null!;
}

public class CloudiaLeadDataDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("origin")]
    public string? Origin { get; set; }

    [JsonPropertyName("clinic_id")]
    public int ClinicId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("stage")]
    public string? Stage { get; set; }

    [JsonPropertyName("observations")]
    public string? Observations { get; set; }

    [JsonPropertyName("ad_data")]
    public List<CloudiaAdDataDto>? AdData { get; set; }

    [JsonPropertyName("tags")]
    public List<CloudiaTagDto>? Tags { get; set; }
}

public class CloudiaAdDataDto
{
    [JsonPropertyName("ad_id")]
    public string? AdId { get; set; }

    [JsonPropertyName("ad_name")]
    public string? AdName { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }
}

public class CloudiaTagDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
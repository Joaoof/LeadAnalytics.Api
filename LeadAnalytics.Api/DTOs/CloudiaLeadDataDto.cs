using System.Text.Json.Serialization;

namespace LeadAnalytics.Api.DTOs;

public class CloudiaWebhookDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;
    // Ex: "CUSTOMER_CREATED", "CUSTOMER_UPDATED"

    [JsonPropertyName("data")]
    public CloudiaLeadDataDto Data { get; set; } = null!;
}

// Representa os dados do lead dentro do webhook
public class CloudiaLeadDataDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("cpf")]
    public string? Cpf { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("origin")]
    public string? Origin { get; set; }

    [JsonPropertyName("clinic_id")]
    public int ClinicId { get; set; }

    [JsonPropertyName("stage")]
    public string? Stage { get; set; }

    [JsonPropertyName("id_stage")]
    public int? IdStage { get; set; }

    [JsonPropertyName("observations")]
    public string? Observations { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("last_updated_at")]
    public DateTime? LastUpdatedAt { get; set; }

    [JsonPropertyName("ad_data")]
    public List<CloudiaAdDataDto>? AdData { get; set; }

    [JsonPropertyName("tags")]
    public List<CloudiaTagDto>? Tags { get; set; }
}

// Dados do anúncio
public class CloudiaAdDataDto
{
    [JsonPropertyName("ad_id")]
    public string? AdId { get; set; }

    [JsonPropertyName("ad_name")]
    public string? AdName { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }
}

// Tags do lead
public class CloudiaTagDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class CloudiaHasAppoimentDto
{
    [JsonPropertyName("has_appointment")]
    public bool HasAppointment { get; set; }
}
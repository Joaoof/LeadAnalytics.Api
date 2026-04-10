using System.Text.Json.Serialization;

namespace LeadAnalytics.Api.DTOs;

public class WhatsAppWebhookDto
{
    [JsonPropertyName("messaging_product")]
    public string? MessagingProduct { get; set; }

    [JsonPropertyName("metadata")]
    public WaMetadata? Metadata { get; set; }

    [JsonPropertyName("contacts")]
    public List<WaContact>? Contacts { get; set; }

    [JsonPropertyName("messages")]
    public List<WaMessage>? Messages { get; set; }

    [JsonPropertyName("field")]
    public string? Field { get; set; }
}

public class WaMetadata
{
    [JsonPropertyName("display_phone_number")]
    public string? DisplayPhoneNumber { get; set; }

    [JsonPropertyName("phone_number_id")]
    public string? PhoneNumberId { get; set; }
}

public class WaContact
{
    [JsonPropertyName("profile")]
    public WaProfile? Profile { get; set; }

    [JsonPropertyName("wa_id")]
    public string? WaId { get; set; }
}

public class WaProfile
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class WaMessage
{
    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("text")]
    public WaText? Text { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("referral")]
    public WaReferral? Referral { get; set; }

    [JsonPropertyName("context")]
    public WaContext? Context { get; set; }
}

public class WaText
{
    [JsonPropertyName("body")]
    public string? Body { get; set; }
}

public class WaReferral
{
    [JsonPropertyName("source_url")]
    public string? SourceUrl { get; set; }

    [JsonPropertyName("source_type")]
    public string? SourceType { get; set; }

    [JsonPropertyName("source_id")]
    public string? SourceId { get; set; }

    [JsonPropertyName("headline")]
    public string? Headline { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("ctwa_clid")]
    public string? CtwaClid { get; set; }
}

public class WaContext
{
    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
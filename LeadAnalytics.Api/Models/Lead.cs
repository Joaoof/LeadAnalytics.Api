using LeadAnalytics.Api.DTOs;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LeadAnalytics.Api.Models;

public class Lead
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("id")]
    public string ExternalId { get; set; }

    [JsonPropertyName("clinic_id")]
    public string TenantId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = null!;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("cpf")]
    public string? Cpf { get; set; }

    [JsonPropertyName("idfacebookapp")]
    public string? IdFacebookApp { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("observations")]
    public string? Observations { get; set; }

    [JsonPropertyName("ad_data")]
    [NotMapped]
    public List<object>? AdData { get; set; }

    [JsonPropertyName("last_ad_id")]
    public string? LastAdId { get; set; }

    [JsonPropertyName("stage")]
    public string? Stage { get; set; }

    [JsonPropertyName("id_stage")]
    public int? IdStage { get; set; }

    [JsonPropertyName("value")]
    public decimal? Value { get; set; }

    [JsonPropertyName("tags")]
    public List<TagDto>? Tags { get; set; }

    [JsonPropertyName("conversationState")]
    public string? ConversationState { get; set; }

    [JsonPropertyName("customFields")]
    [NotMapped]
    public List<object>? CustomFields { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("last_updated_at")]
    public DateTime UpdatedAt { get; set; }
}
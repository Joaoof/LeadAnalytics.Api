namespace LeadAnalytics.Api.DTOs.Cloudia;

public class CloudiaWebhookDto
{
    public string Type { get; set; } = null!;
    public CloudiaLeadDataDto Data { get; set; } = null!;

    // USER_ASSIGNED_TO_CUSTOMER
    public int? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public string? AssignedUserEmail { get; set; }
    public CloudiaCustomerDto? Customer { get; set; }
}

public class CloudiaCustomerDto
{
    public int Id { get; set; }
    public int ClinicId { get; set; }
    public string? Stage { get; set; }
}
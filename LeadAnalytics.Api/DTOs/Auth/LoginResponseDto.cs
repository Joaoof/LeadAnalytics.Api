namespace LeadAnalytics.Api.DTOs.Auth;

public class LoginResponseDto
{
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public UnitSelectorOptionDto SelectedUnit { get; set; } = new();
    public List<UnitSelectorOptionDto> AvailableUnits { get; set; } = [];
}

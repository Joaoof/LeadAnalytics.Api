using LeadAnalytics.Api.DTOs.Auth;

namespace LeadAnalytics.Api.Service;

public class AuthService
{
    private const int AraguainaUnitId = 1;
    private const int AraguainaClinicId = 8020;

    private static readonly List<UnitSelectorOptionDto> DefaultUnits =
    [
        new UnitSelectorOptionDto
        {
            Id = 1,
            ClinicId = 8020,
            Name = "Doutor Hérnia Unidade Araguaína",
            LogoUrl = "https://i0.wp.com/www.cloudia.com.br/wp-content/uploads/2024/01/Logo-800-sem-fundo.png",
            IsDefault = true
        },
        new UnitSelectorOptionDto
        {
            Id = 2,
            ClinicId = 8021,
            Name = "DOUTOR HÉRNIA UNIDADE MARABÁ",
            LogoUrl = "/assets/logo-maraba.png"
        },
        new UnitSelectorOptionDto
        {
            Id = 3,
            ClinicId = 8022,
            Name = "DOUTOR HÉRNIA UNIDADE PARAUAPEBAS",
            LogoUrl = "/assets/logo-parauapebas.png"
        },
        new UnitSelectorOptionDto
        {
            Id = 4,
            ClinicId = 8023,
            Name = "DOUTOR HÉRNIA IMPERATRIZ",
            LogoUrl = "/assets/logo-10anos.png"
        },
        new UnitSelectorOptionDto
        {
            Id = 5,
            ClinicId = 8024,
            Name = "DOUTOR HÉRNIA CANAÃ",
            LogoUrl = "/assets/logo-10anos.png"
        },
        new UnitSelectorOptionDto
        {
            Id = 6,
            ClinicId = 8025,
            Name = "DOUTOR HÉRNIA BALSAS",
            LogoUrl = "/assets/logo-10anos.png"
        },
        new UnitSelectorOptionDto
        {
            Id = 7,
            ClinicId = 8026,
            Name = "INSTITUTO TRAUMA",
            LogoUrl = "/assets/logo-trauma.png"
        }
    ];

    public IReadOnlyCollection<UnitSelectorOptionDto> GetUnitOptions() => DefaultUnits;

    public LoginResponseDto Login(LoginRequestDto request)
    {
        var userName = string.IsNullOrWhiteSpace(request.Name)
            ? "Usuário Cloudia"
            : request.Name.Trim();

        var selectedUnit = DefaultUnits.First(u => u.Id == AraguainaUnitId && u.ClinicId == AraguainaClinicId);

        return new LoginResponseDto
        {
            UserName = userName,
            Email = request.Email,
            SelectedUnit = selectedUnit,
            AvailableUnits = [.. DefaultUnits]
        };
    }
}

using LeadAnalytics.Api.DTOs.Auth;
using LeadAnalytics.Api.Options;
using Microsoft.Extensions.Options;

namespace LeadAnalytics.Api.Service;

public class AuthService(IOptions<AuthOptions> authOptions, JwtTokenService jwtTokenService)

namespace LeadAnalytics.Api.Service;

public class AuthService
{
    private const int AraguainaUnitId = 1;
    private const int AraguainaClinicId = 8020;

    private readonly AuthOptions _authOptions = authOptions.Value;
    private readonly JwtTokenService _jwtTokenService = jwtTokenService;

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
        new UnitSelectorOptionDto { Id = 2, ClinicId = 8021, Name = "DOUTOR HÉRNIA UNIDADE MARABÁ", LogoUrl = "/assets/logo-maraba.png" },
        new UnitSelectorOptionDto { Id = 3, ClinicId = 8022, Name = "DOUTOR HÉRNIA UNIDADE PARAUAPEBAS", LogoUrl = "/assets/logo-parauapebas.png" },
        new UnitSelectorOptionDto { Id = 4, ClinicId = 8023, Name = "DOUTOR HÉRNIA IMPERATRIZ", LogoUrl = "/assets/logo-10anos.png" },
        new UnitSelectorOptionDto { Id = 5, ClinicId = 8024, Name = "DOUTOR HÉRNIA CANAÃ", LogoUrl = "/assets/logo-10anos.png" },
        new UnitSelectorOptionDto { Id = 6, ClinicId = 8025, Name = "DOUTOR HÉRNIA BALSAS", LogoUrl = "/assets/logo-10anos.png" },
        new UnitSelectorOptionDto { Id = 7, ClinicId = 8026, Name = "INSTITUTO TRAUMA", LogoUrl = "/assets/logo-trauma.png" }
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
        var userName = string.IsNullOrWhiteSpace(request.Name) ? "Usuário Cloudia" : request.Name.Trim();
        var email = request.Email?.Trim().ToLowerInvariant();

        var adminEmails = ResolveSuperAdminEmails();

        var isSuperAdmin = !string.IsNullOrWhiteSpace(email)
            && adminEmails.Contains(email, StringComparer.OrdinalIgnoreCase);

        var role = isSuperAdmin ? "super-admin" : "user";

        var availableUnits = isSuperAdmin
            ? [.. DefaultUnits]
            : [DefaultUnits.First(u => u.Id == AraguainaUnitId && u.ClinicId == AraguainaClinicId)];

        var selectedUnit = availableUnits.First();

        var (token, expiresAtUtc) = _jwtTokenService.GenerateToken(request, role, availableUnits);
        var userName = string.IsNullOrWhiteSpace(request.Name)
            ? "Usuário Cloudia"
            : request.Name.Trim();

        var selectedUnit = DefaultUnits.First(u => u.Id == AraguainaUnitId && u.ClinicId == AraguainaClinicId);

        return new LoginResponseDto
        {
            UserName = userName,
            Email = email,
            Role = role,
            TokenType = "Bearer",
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc,
            SelectedUnit = selectedUnit,
            AvailableUnits = availableUnits
        };
    }

    private HashSet<string> ResolveSuperAdminEmails()
    {
        var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var email in _authOptions.SuperAdminEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
            emails.Add(email.Trim().ToLowerInvariant());

        if (!string.IsNullOrWhiteSpace(_authOptions.SuperAdminEmailsCsv))
        {
            var fromCsv = _authOptions.SuperAdminEmailsCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var email in fromCsv.Where(e => !string.IsNullOrWhiteSpace(e)))
                emails.Add(email.Trim().ToLowerInvariant());
        }

        return emails;
    }
            Email = request.Email,
            SelectedUnit = selectedUnit,
            AvailableUnits = [.. DefaultUnits]
        };
    }
}

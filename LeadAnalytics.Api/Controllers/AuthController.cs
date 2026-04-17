using LeadAnalytics.Api.DTOs.Auth;
using LeadAnalytics.Api.Service;
using Microsoft.AspNetCore.Mvc;

namespace LeadAnalytics.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService, UnitService unitService) : ControllerBase
{
    private readonly AuthService _authService = authService;
    private readonly UnitService _unitService = unitService;

    [HttpGet("unit-options")]
    [ProducesResponseType(typeof(IEnumerable<UnitSelectorOptionDto>), StatusCodes.Status200OK)]
    public IActionResult GetUnitOptions()
    {
        var options = _authService.GetUnitOptions();
        return Ok(options);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (request is null)
            return BadRequest(new { message = "Body de login é obrigatório." });

        // Garante que a unidade padrão (Araguaína/8020) exista na base.
        await _unitService.GetOrCreateAsync(8020);

        var response = _authService.Login(request);
        return Ok(response);
    }
}

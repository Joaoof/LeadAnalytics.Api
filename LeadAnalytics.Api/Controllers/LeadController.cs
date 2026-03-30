using LeadAnalytics.Api.DTOs;
using LeadAnalytics.Api.Service;
using Microsoft.AspNetCore.Mvc;

namespace LeadAnalytics.Api.Controllers;

[ApiController]
[Route("webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly LeadService _leadService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        LeadService leadService,
        ILogger<WebhooksController> logger)
    {
        _leadService = leadService;
        _logger = logger;
    }

    [HttpPost("cloudia")]
    public async Task<IActionResult> Cloudia([FromBody] CloudiaWebhookDto? dto)
    {
        // Loga o tipo do evento que chegou
        if(_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Webhook recebido: {Type}", dto.Type);
        }

        var result = await _leadService.SaveLeadAsync(dto);

        return Ok(new { result = result.ToString() });
    }

}
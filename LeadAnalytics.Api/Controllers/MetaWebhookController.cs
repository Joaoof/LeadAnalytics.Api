using LeadAnalytics.Api.DTOs;
using LeadAnalytics.Api.DTOs.Meta;
using LeadAnalytics.Api.Service;
using Microsoft.AspNetCore.Mvc;

namespace LeadAnalytics.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public class MetaWebhookController(
    MetaWebhookService metaWebhookService,
    ILogger<MetaWebhookController> logger) : ControllerBase
{
    private readonly MetaWebhookService _metaWebhookService = metaWebhookService;
    private readonly ILogger<MetaWebhookController> _logger = logger;

    /// <summary>
    /// Endpoint de verificação do webhook da Meta
    /// </summary>
    [HttpGet("meta")]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        const string VERIFY_TOKEN = "seu_token"; // TODO: Mover para appsettings.json

        if (mode == "subscribe" && token == VERIFY_TOKEN)
        {
            _logger.LogInformation("✅ Webhook verificado com sucesso");
            return Ok(challenge);
        }

        _logger.LogWarning("❌ Falha na verificação do webhook");
        return Unauthorized();
    }

    /// <summary>
    /// Recebe eventos da Meta via n8n
    /// </summary>
    [HttpPost("meta")]
    public async Task<IActionResult> ReceiveMetaWebhook([FromBody] MetaWebhookDto webhook)
    {
        try
        {
            // Guard logging that could evaluate potentially expensive properties when logging is disabled
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("📨 Webhook Meta recebido: {Object}", webhook.Object);
            }

            var result = await _metaWebhookService.ProcessWebhookAsync(webhook);

            return Ok(new
            {
                success = true,
                message = "Webhook processado com sucesso",
                eventsProcessed = result.EventsProcessed,
                originEventsCreated = result.OriginEventsCreated
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao processar webhook da Meta");

            // Retorna 200 mesmo com erro para não reenviar webhook
            return Ok(new
            {
                success = false,
                message = "Erro ao processar webhook",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Endpoint alternativo caso n8n envie formato customizado
    /// </summary>
    [HttpPost("meta/n8n")]
    public async Task<IActionResult> ReceiveN8nWebhook([FromBody] N8nWebhookDto webhook)
    {
        try
        {
            // Guard logging that could evaluate potentially expensive properties when logging is disabled
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("📨 Webhook n8n recebido para telefone: {Phone}", webhook.Phone);
            }

            var result = await _metaWebhookService.ProcessN8nWebhookAsync(webhook);

            return Ok(new
            {
                success = true,
                message = "Evento processado com sucesso",
                originEventId = result.OriginEventId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao processar webhook do n8n");

            return Ok(new
            {
                success = false,
                message = "Erro ao processar webhook",
                error = ex.Message
            });
        }
    }
}
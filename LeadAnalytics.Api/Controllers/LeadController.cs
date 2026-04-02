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

    [HttpGet]
    public IActionResult GetAllLeads()
    {
        var leads = _leadService.TrazerTodosLeads().Result;
        return Ok(leads);
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

    [HttpGet("consultas")]
    public async Task<IActionResult> GetHasAppoiment(int clinicId)
    {
        var result = await _leadService.VerificarConsultasFechadas(clinicId);
        return await Task.FromResult<IActionResult>(Ok(result));
    }


    [HttpGet("sem-pagamento")]
    public async Task<IActionResult> GetLeadsWithoutPayment(int clinicId)
    {

        var result = await _leadService.VerificarEtapaSemPagamento(clinicId);

        if(result == 0)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Nenhuma consulta agendada sem pagamento",
                Status = 404
            });
        }

        return Ok(new
        {
            mensagem = "Agendados sem pagamento",
            result
        });
    }

    [HttpGet("com-pagamento")]
    public async Task<IActionResult> VerificarEtapaComPagamento(int clinicId)
    {
        var quantidade = await _leadService.VerificarEtapaComPagamento(clinicId);

        if (quantidade == 0)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Nenhuma consulta agendada com pagamento",
                Status = 404
            });
        }

        return Ok(new
        {
            mensagem = "Agendados com pagamento",
            quantidade
        });
    }

    [HttpGet("source-final")]
    public async Task<IActionResult> GetSourceFinally(int clinicId)
    {
        var result = await _leadService.VerificarSourceFinal(clinicId);
        return Ok(result);
    }

    [HttpGet("origem-cloudia")]
    public async Task<IActionResult> GetOrigens(int clinicId)
    {
        var result = await _leadService.VerificarOrigemCloudia(clinicId);
        return Ok(result);
    }

    [HttpGet("fim-de-semana")]
    public async Task<IActionResult> GetLeadsFinaldeSemana(int clinicId)
    {
        var leads = await _leadService.LeadsFinaldeSemana(clinicId);
        return Ok(new { quantidade = leads.Count, leads });
    }

    [HttpGet("etapa-agrupada")]
    public async Task<IActionResult> GetEtapaAgrupada([FromQuery] int clinicId)
    {
        var result = await _leadService.VerificarEtapaAgrupada(clinicId);

        if (clinicId <= 0)
            return BadRequest("clinicId inválido");

        return Ok(result);
    }

    [HttpGet("buscar-inicio-fim")]
    public async Task<IActionResult> GetBuscarInicioFim([FromQuery] int clinicId, [FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim)
    {
        if (clinicId <= 0)
            return BadRequest("clinicId inválido");
        if (dataInicio > dataFim)
            return BadRequest("dataInicio deve ser menor ou igual a dataFim");
        var result = await _leadService.BuscarInicioEFimMesLeads(clinicId, dataInicio, dataFim);
        return Ok(result);
    }
}
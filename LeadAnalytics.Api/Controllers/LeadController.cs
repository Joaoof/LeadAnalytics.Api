using LeadAnalytics.Api.DTOs;
using LeadAnalytics.Api.Service;
using Microsoft.AspNetCore.Mvc;

namespace LeadAnalytics.Api.Controllers;

[ApiController]
[Route("webhooks")]
public class WebhooksController(
    LeadService leadService,
    ILogger<WebhooksController> logger) : ControllerBase
{
    private readonly LeadService _leadService = leadService;
    private readonly ILogger<WebhooksController> _logger = logger;

    [HttpGet]
    public async Task<IActionResult> GetAllLeads()
    {
        var leads = await _leadService.GetAllLeadsAsync();
        return Ok(leads);
    }

    [HttpPost("cloudia")]
    public async Task<IActionResult> Cloudia([FromBody] CloudiaWebhookDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _leadService.SaveLeadAsync(dto);
        return Ok(result);
    }

    [HttpGet("consultas")]
    public async Task<IActionResult> GetHasAppoiment(int clinicId)
    {
        var result = await _leadService.GetCheckClosedQueries(clinicId);
        return Ok(result);
    }


    [HttpGet("sem-pagamento")]
    public async Task<IActionResult> GetLeadsWithoutPayment(int clinicId)
    {

        var result = await _leadService.GetCheckStageWithoutPayment(clinicId);

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
        var quantidade = await _leadService.GetVerifyPaymentStep(clinicId);

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
        var result = await _leadService.GetVerifySourceFinal(clinicId);
        return Ok(result);
    }

    [HttpGet("origem-cloudia")]
    public async Task<IActionResult> GetOrigens(int clinicId)
    {
        var result = await _leadService.GetCheckSourceCloudia(clinicId);
        return Ok(result);
    }

    [HttpGet("fim-de-semana")]
    public async Task<IActionResult> GetLeadsFinaldeSemana(int clinicId)
    {
        var leads = await _leadService.GetWeekendLeads(clinicId);
        return Ok(leads);    
    }

    [HttpGet("etapa-agrupada")]
    public async Task<IActionResult> GetEtapaAgrupada([FromQuery] int clinicId)
    {
        var result = await _leadService.GetCheckGroupedStep(clinicId);

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
        var result = await _leadService.GetSearchStartMonthLeads(clinicId, dataInicio, dataFim);
        return Ok(result);
    }

    [HttpGet("consulta-periodos")]
    public async Task<IActionResult> GetConsultaPeriodos([FromQuery] FiltroLeadsPeriodoDto filtro)
    {
        if (filtro.ClinicId <= 0)
            return BadRequest("clinicId inválido");
        if (filtro.Ano <= 0)
            return BadRequest("Ano inválido");
        if (filtro.Mes.HasValue && (filtro.Mes < 1 || filtro.Mes > 12))
            return BadRequest("Mês deve ser entre 1 e 12");
        if (filtro.Dia.HasValue && (filtro.Dia < 1 || filtro.Dia > 31))
            return BadRequest("Dia deve ser entre 1 e 31");

        var result = await _leadService.GetQueryLeadsByPeriodService(filtro);
        return Ok(result);
    }
}
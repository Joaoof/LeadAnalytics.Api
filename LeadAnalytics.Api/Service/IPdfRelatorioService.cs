using LeadAnalytics.Api.DTOs;

namespace LeadAnalytics.Api.Service;

public interface IPdfRelatorioService
{
    byte[] Gerar(RelatorioMensalDadosDto dados);
}

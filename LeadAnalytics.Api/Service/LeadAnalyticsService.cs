using LeadAnalytics.Api.Data;
using LeadAnalytics.Api.DTOs.Response;
using Microsoft.EntityFrameworkCore;

namespace LeadAnalytics.Api.Service;

/// <summary>
/// Serviço para cálculo de métricas e analytics de leads
/// </summary>
public class LeadAnalyticsService(AppDbContext context, ILogger<LeadAnalyticsService> logger)
{
    private readonly AppDbContext _context = context;
    private readonly ILogger<LeadAnalyticsService> _logger = logger;

    /// <summary>
    /// Obter métricas completas de um lead específico
    /// </summary>
    public async Task<LeadMetricsDto?> GetLeadMetricsAsync(int leadId)
    {
        var lead = await _context.Leads
            .Include(l => l.Unit)
            .Include(l => l.Attendant)
            .FirstOrDefaultAsync(l => l.Id == leadId);

        if (lead == null)
        {
            _logger.LogWarning("Lead {LeadId} não encontrado", leadId);
            return null;
        }

        // Buscar todas as conversas (períodos de estado)
        var conversations = await _context.LeadConversations
            .Include(lc => lc.Attendant)
            .Where(lc => lc.LeadId == leadId)
            .OrderBy(lc => lc.StartedAt)
            .ToListAsync();

        // Calcular tempo em cada estado
        var timeInBot = conversations
            .Where(c => c.ConversationState == "bot" && c.EndedAt.HasValue)
            .Sum(c => (c.EndedAt!.Value - c.StartedAt).TotalMinutes);

        var timeInQueue = conversations
            .Where(c => c.ConversationState == "queue" && c.EndedAt.HasValue)
            .Sum(c => (c.EndedAt!.Value - c.StartedAt).TotalMinutes);

        var timeInService = conversations
            .Where(c => c.ConversationState == "service" && c.EndedAt.HasValue)
            .Sum(c => (c.EndedAt!.Value - c.StartedAt).TotalMinutes);

        var timeInConcluido = conversations
            .Where(c => c.ConversationState == "concluido" && c.EndedAt.HasValue)
            .Sum(c => (c.EndedAt!.Value - c.StartedAt).TotalMinutes);

        // Tempo até primeiro atendimento (bot/queue → service)
        var firstServiceConversation = conversations
            .FirstOrDefault(c => c.ConversationState == "service");
        
        double? timeToFirstResponse = firstServiceConversation != null
            ? (firstServiceConversation.StartedAt - lead.CreatedAt).TotalMinutes
            : null;

        // Tempo até resolução (criação → concluído)
        var firstConcluidoConversation = conversations
            .FirstOrDefault(c => c.ConversationState == "concluido");

        double? timeToResolution = firstConcluidoConversation != null
            ? (firstConcluidoConversation.StartedAt - lead.CreatedAt).TotalMinutes
            : null;

        // Verificar se está demorando (alertas)
        var (isDelayed, delayReason) = CheckIfDelayed(lead, conversations);

        // Timeline
        var timeline = conversations.Select(c => new ConversationPeriodDto
        {
            ConversationId = c.Id,
            State = c.ConversationState,
            StartedAt = c.StartedAt,
            EndedAt = c.EndedAt,
            DurationMinutes = c.EndedAt.HasValue 
                ? (c.EndedAt.Value - c.StartedAt).TotalMinutes 
                : null,
            AttendantId = c.AttendantId,
            AttendantName = c.Attendant?.Name,
            IsActive = !c.EndedAt.HasValue
        }).ToList();

        return new LeadMetricsDto
        {
            LeadId = lead.Id,
            ExternalId = lead.ExternalId,
            Name = lead.Name,
            Phone = lead.Phone,
            CurrentState = lead.ConversationState ?? "desconhecido",
            CreatedAt = lead.CreatedAt,
            LastUpdatedAt = lead.LastUpdatedAt,
            TimeInBotMinutes = timeInBot > 0 ? timeInBot : null,
            TimeInQueueMinutes = timeInQueue > 0 ? timeInQueue : null,
            TimeInServiceMinutes = timeInService > 0 ? timeInService : null,
            TimeInConcluidoMinutes = timeInConcluido > 0 ? timeInConcluido : null,
            TimeToFirstResponseMinutes = timeToFirstResponse,
            TimeToResolutionMinutes = timeToResolution,
            TotalTransitions = conversations.Count,
            CurrentAttendantId = lead.AttendantId,
            CurrentAttendantName = lead.Attendant?.Name,
            IsDelayed = isDelayed,
            DelayReason = delayReason,
            Timeline = timeline
        };
    }

    /// <summary>
    /// Obter métricas de múltiplos leads
    /// </summary>
    public async Task<List<LeadMetricsDto>> GetLeadsMetricsAsync(
        int unitId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? state = null)
    {
        var query = _context.Leads
            .Include(l => l.Unit)
            .Include(l => l.Attendant)
            .Where(l => l.UnitId == unitId);

        if (startDate.HasValue)
            query = query.Where(l => l.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(l => l.CreatedAt <= endDate.Value);

        if (!string.IsNullOrEmpty(state))
            query = query.Where(l => l.ConversationState == state);

        var leads = await query.ToListAsync();

        var metrics = new List<LeadMetricsDto>();

        foreach (var lead in leads)
        {
            var leadMetrics = await GetLeadMetricsAsync(lead.Id);
            if (leadMetrics != null)
            {
                metrics.Add(leadMetrics);
            }
        }

        return metrics;
    }

    /// <summary>
    /// Obter resumo agregado da clínica
    /// </summary>
    public async Task<ClinicSummaryDto> GetClinicSummaryAsync(
        int unitId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.Date.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var unit = await _context.Units.FindAsync(unitId) ?? throw new ArgumentException($"Unidade {unitId} não encontrada");

        // Total de leads no período
        var leadsQuery = _context.Leads
            .Where(l => l.UnitId == unitId && l.CreatedAt >= start && l.CreatedAt <= end);

        var totalLeads = await leadsQuery.CountAsync();

        var leadsByState = await leadsQuery
            .GroupBy(l => l.ConversationState)
            .Where(g => g.Key != null)
            .Select(g => new { State = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.State!, x => x.Count);

        var leadsInBot = leadsByState.GetValueOrDefault("bot", 0);
        var leadsInQueue = leadsByState.GetValueOrDefault("queue", 0);
        var leadsInService = leadsByState.GetValueOrDefault("service", 0);
        var leadsConcluded = leadsByState.GetValueOrDefault("concluido", 0);

        // Obter métricas de todos os leads
        var allMetrics = await GetLeadsMetricsAsync(unitId, start, end);

        // Calcular médias
        var avgTimeToFirstResponse = allMetrics
            .Where(m => m.TimeToFirstResponseMinutes.HasValue)
            .Average(m => m.TimeToFirstResponseMinutes);

        var avgTimeToResolution = allMetrics
            .Where(m => m.TimeToResolutionMinutes.HasValue)
            .Average(m => m.TimeToResolutionMinutes);

        var avgTimeInBot = allMetrics
            .Where(m => m.TimeInBotMinutes.HasValue)
            .Average(m => m.TimeInBotMinutes);

        var avgTimeInQueue = allMetrics
            .Where(m => m.TimeInQueueMinutes.HasValue)
            .Average(m => m.TimeInQueueMinutes);

        var avgTimeInService = allMetrics
            .Where(m => m.TimeInServiceMinutes.HasValue)
            .Average(m => m.TimeInServiceMinutes);

        var delayedCount = allMetrics.Count(m => m.IsDelayed);

        // Performance por atendente
        var attendantsPerformance = await GetAttendantsPerformanceAsync(unitId, start, end);

        return new ClinicSummaryDto
        {
            ClinicId = unitId,
            ClinicName = unit.Name,
            PeriodStart = start,
            PeriodEnd = end,
            TotalLeads = totalLeads,
            LeadsInBot = leadsInBot,
            LeadsInQueue = leadsInQueue,
            LeadsInService = leadsInService,
            LeadsConcluded = leadsConcluded,
            AverageTimeToFirstResponseMinutes = avgTimeToFirstResponse,
            AverageTimeToResolutionMinutes = avgTimeToResolution,
            AverageTimeInBotMinutes = avgTimeInBot,
            AverageTimeInQueueMinutes = avgTimeInQueue,
            AverageTimeInServiceMinutes = avgTimeInService,
            DelayedLeadsCount = delayedCount,
            AttendantsPerformance = attendantsPerformance,
            LeadsByState = leadsByState,
            LastCalculatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Obter performance de atendentes
    /// </summary>
    private async Task<List<AttendantPerformanceDto>> GetAttendantsPerformanceAsync(
        int unitId,
        DateTime startDate,
        DateTime endDate)
    {
        var attendants = await _context.Attendants
            .Where(a => a.UnitId == unitId)
            .ToListAsync();

        var performance = new List<AttendantPerformanceDto>();

        foreach (var attendant in attendants)
        {
            // Leads atendidos
            var handledLeads = await _context.LeadConversations
                .Include(lc => lc.Lead)
                .Where(lc => lc.AttendantId == attendant.Id 
                    && lc.Lead.UnitId == unitId
                    && lc.StartedAt >= startDate 
                    && lc.StartedAt <= endDate)
                .ToListAsync();

            var totalHandled = handledLeads.Select(lc => lc.LeadId).Distinct().Count();

            // Leads atualmente ativos
            var currentActive = await _context.Leads
                .CountAsync(l => l.AttendantId == attendant.Id 
                    && l.ConversationState == "service");

            // Leads concluídos
            var concluded = await _context.LeadConversations
                .Include(lc => lc.Lead)
                .Where(lc => lc.AttendantId == attendant.Id
                    && lc.ConversationState == "concluido"
                    && lc.Lead.UnitId == unitId
                    && lc.StartedAt >= startDate
                    && lc.StartedAt <= endDate)
                .Select(lc => lc.LeadId)
                .Distinct()
                .CountAsync();

            // Tempo médio de atendimento
            var avgServiceTime = handledLeads
                .Where(lc => lc.ConversationState == "service" && lc.EndedAt.HasValue)
                .Select(lc => (lc.EndedAt!.Value - lc.StartedAt).TotalMinutes)
                .DefaultIfEmpty(0)
                .Average();

            // Tempo médio até conclusão
            var avgResolutionTime = handledLeads
                .Where(lc => lc.ConversationState == "concluido" && lc.EndedAt.HasValue)
                .Select(lc => (lc.EndedAt!.Value - lc.StartedAt).TotalMinutes)
                .DefaultIfEmpty(0)
                .Average();

            var conversionRate = totalHandled > 0 
                ? (double)concluded / totalHandled 
                : 0;

            performance.Add(new AttendantPerformanceDto
            {
                AttendantId = attendant.Id,
                AttendantName = attendant.Name,
                TotalLeadsHandled = totalHandled,
                CurrentActiveLeads = currentActive,
                LeadsConcluded = concluded,
                AverageServiceTimeMinutes = avgServiceTime > 0 ? avgServiceTime : null,
                AverageResolutionTimeMinutes = avgResolutionTime > 0 ? avgResolutionTime : null,
                ConversionRate = conversionRate
            });
        }

        return performance.OrderByDescending(p => p.TotalLeadsHandled).ToList();
    }

    /// <summary>
    /// Verificar se lead está demorando muito
    /// </summary>
    private (bool IsDelayed, string? Reason) CheckIfDelayed(
        Models.Lead lead,
        List<Models.LeadConversation> conversations)
    {
        var now = DateTime.UtcNow;

        // Lead em BOT há mais de 30 minutos
        if (lead.ConversationState == "bot")
        {
            var currentBotTime = (now - lead.CreatedAt).TotalMinutes;
            if (currentBotTime > 30)
            {
                return (true, $"Em BOT há {currentBotTime:F0} minutos (limite: 30min)");
            }
        }

        // Lead em QUEUE há mais de 15 minutos
        if (lead.ConversationState == "queue")
        {
            var currentQueueConv = conversations
                .FirstOrDefault(c => c.ConversationState == "queue" && !c.EndedAt.HasValue);

            if (currentQueueConv != null)
            {
                var queueTime = (now - currentQueueConv.StartedAt).TotalMinutes;
                if (queueTime > 15)
                {
                    return (true, $"Na fila há {queueTime:F0} minutos (limite: 15min)");
                }
            }
        }

        // Lead em SERVICE há mais de 2 horas
        if (lead.ConversationState == "service")
        {
            var currentServiceConv = conversations
                .FirstOrDefault(c => c.ConversationState == "service" && !c.EndedAt.HasValue);

            if (currentServiceConv != null)
            {
                var serviceTime = (now - currentServiceConv.StartedAt).TotalMinutes;
                if (serviceTime > 120)
                {
                    return (true, $"Em atendimento há {serviceTime:F0} minutos (limite: 120min)");
                }
            }
        }

        return (false, null);
    }

    /// <summary>
    /// Obter leads com alertas (demorando muito)
    /// </summary>
    public async Task<List<LeadMetricsDto>> GetDelayedLeadsAsync(int unitId)
    {
        var allMetrics = await GetLeadsMetricsAsync(unitId);
        return [.. allMetrics.Where(m => m.IsDelayed)];
    }
}
# Diagrama de Classes — LeadAnalytics.Api

```mermaid
classDiagram
    %% ══════════════════════════════════════════
    %% DOMAIN MODELS
    %% ══════════════════════════════════════════

    class Lead {
        +int Id
        +int ExternalId
        +int TenantId
        +string Name
        +string Phone
        +string? Email
        +string? Cpf
        +string? Gender
        +string Source
        +string Channel
        +string Campaign
        +string? Ad
        +string TrackingConfidence
        +string CurrentStage
        +int? CurrentStageId
        +string Status
        +bool HasAppointment
        +bool HasPayment
        +string? ConversationState
        +string? Observations
        +bool? HasHealthInsurancePlan
        +string? IdFacebookApp
        +int? IdChannelIntegration
        +string? LastAdId
        +string? Tags
        +int? UnitId
        +int? AttendantId
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +DateTime? ConvertedAt
    }

    class Unit {
        +int Id
        +int ClinicId
        +string Name
        +DateTime CreatedAt
    }

    class Attendant {
        +int Id
        +int ExternalId
        +string Name
        +string? Email
        +string? Phone
        +DateTime CreatedAt
        +int UnitId
    }

    class LeadConversation {
        +int Id
        +int LeadId
        +string Channel
        +string? Source
        +string ConversationState
        +DateTime StartedAt
        +DateTime? EndedAt
        +int? AttendantId
    }

    class LeadInteraction {
        +int Id
        +int LeadConversationId
        +string Type
        +string? Content
        +string? Metadata
        +DateTime CreatedAt
    }

    class LeadStageHistory {
        +int Id
        +int LeadId
        +int StageId
        +string StageLabel
        +DateTime ChangedAt
    }

    class LeadAssignment {
        +int Id
        +int LeadId
        +int AttendantId
        +string? Stage
        +DateTime AssignedAt
    }

    class LeadAttribution {
        +int Id
        +int LeadId
        +string Phone
        +string CtwaClid
        +string? SourceId
        +string? SourceType
        +string MatchType
        +string Confidence
        +DateTime MatchedAt
        +int OriginEventId
        +int? TenantId
    }

    class Payment {
        +int Id
        +int LeadId
        +decimal Amount
        +DateTime PaidAt
    }

    class OriginEvent {
        +int Id
        +string Phone
        +string? ContactName
        +string CtwaClid
        +string? SourceId
        +string? SourceType
        +string? SourceUrl
        +string? Headline
        +string? Body
        +string? MessageId
        +DateTime? MessageTimestamp
        +DateTime ReceivedAt
        +bool Processed
        +int? TenantId
        +int? WebhookEventId
        +string Confidence
    }

    class WebhookEvent {
        +int Id
        +string Provider
        +string EventType
        +string PayloadJson
        +string? PhoneNumberId
        +int? TenantId
        +DateTime ReceivedAt
    }

    class AppConfiguration {
        +int Id
        +string Key
        +string Value
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +DateTime? ExpiresAt
    }

    %% ══════════════════════════════════════════
    %% DOMAIN RELATIONSHIPS
    %% ══════════════════════════════════════════

    Lead "N" --> "1" Unit                  : pertence a
    Lead "N" --> "0..1" Attendant          : atribuído a
    Lead "1" *-- "N" LeadStageHistory      : histórico de etapas
    Lead "1" *-- "N" LeadConversation      : conversas
    Lead "1" *-- "N" Payment               : pagamentos
    Lead "1" *-- "N" LeadAssignment        : atribuições

    LeadConversation "1" *-- "N" LeadInteraction : interações
    LeadConversation "N" --> "0..1" Attendant    : atendida por

    LeadAssignment "N" --> "1" Attendant   : atribuída a

    Attendant "N" --> "1" Unit             : vinculado a

    LeadAttribution "N" --> "1" OriginEvent : gerada de
    OriginEvent "1" --> "0..1" WebhookEvent : recebida via

    %% ══════════════════════════════════════════
    %% DTOs — CLOUDIA
    %% ══════════════════════════════════════════

    class CloudiaWebhookDto {
        +string Type
        +CloudiaLeadDataDto? Data
        +CloudiaLeadDataDto? Customer
        +int? AssignedUserId
        +string? AssignedUserName
        +string? AssignedUserEmail
    }

    class CloudiaLeadDataDto {
        +int Id
        +int ClinicId
        +string? Name
        +string? Phone
        +string? Email
        +string? Cpf
        +string? Gender
        +string? Origin
        +bool? HasHealthInsurancePlan
        +DateTime? CreatedAt
        +DateTime? LastUpdatedAt
        +string? Observations
        +string? LastAdId
        +int? IdChannelIntegration
        +string? IdFacebookApp
        +string? Stage
        +int? IdStage
        +string? ConversationState
        +string? IdWhatsApp
        +int? RegisteredOnWhatsApp
        +List~AdDataDto~ AdData
        +List~TagDto~ Tags
    }

    class ActiveLeadDto {
        +int Id
        +int ExternalId
        +string Name
        +string Phone
        +string ConversationState
        +int? AttendantId
        +int? UnitId
        +DateTime UpdatedAt
        +DateTime CreatedAt
    }

    class LeadsCountDto {
        +int Bot
        +int Queue
        +int Service
        +int Concluido
        +int Total
    }

    class CloudiaMetricsResponseDto {
        +CloudiaMetricsDto Metrics
        +List~CloudiaWaitingDto~ WaitingInQueueList
        +List~CloudiaWaitingDto~ WaitingForResponseList
        +List~CloudiaAttendantServiceDto~ AttendantsServicesList
    }

    class CloudiaMetricsDto {
        +int TotalInService
        +int TotalInQueue
        +double WaitResponseTimeAvg
        +double WaitFirstResponseTimeAvg
        +double MaxWaitFirstResponseTime
    }

    CloudiaWebhookDto "1" *-- "0..1" CloudiaLeadDataDto
    CloudiaMetricsResponseDto "1" *-- "1" CloudiaMetricsDto

    %% ══════════════════════════════════════════
    %% DTOs — META / N8N
    %% ══════════════════════════════════════════

    class MetaWebhookDto {
        +string Object
        +List~MetaEntryDto~ Entry
    }

    class MetaEntryDto {
        +string Id
        +List~MetaChangeDto~ Changes
    }

    class MetaChangeDto {
        +string Field
        +MetaValueDto Value
    }

    class MetaValueDto {
        +string MessagingProduct
        +MetaMetadataDto? Metadata
        +List~MetaContactDto~ Contacts
        +List~MetaMessageDto~ Messages
    }

    class MetaMessageDto {
        +string From
        +string Id
        +string Timestamp
        +string Type
        +MetaContextDto? Context
        +MetaReferralDto? Referral
    }

    class MetaReferralDto {
        +string? SourceUrl
        +string? SourceType
        +string? SourceId
        +string? Headline
        +string? Body
        +string? CtwaClid
    }

    class N8nWebhookDto {
        +string Phone
        +string? ContactName
        +string? CtwaClid
        +string? SourceId
        +string? SourceType
        +string? SourceUrl
        +string? Headline
        +string? Body
        +string? MessageId
        +string? MessageTimestamp
        +int? TenantId
    }

    MetaWebhookDto "1" *-- "N" MetaEntryDto
    MetaEntryDto "1" *-- "N" MetaChangeDto
    MetaChangeDto "1" *-- "1" MetaValueDto
    MetaValueDto "1" *-- "N" MetaMessageDto
    MetaMessageDto "1" *-- "0..1" MetaReferralDto

    %% ══════════════════════════════════════════
    %% DTOs — ANALYTICS
    %% ══════════════════════════════════════════

    class LeadMetricsDto {
        +int LeadId
        +int ExternalId
        +string Name
        +string? Phone
        +string CurrentState
        +DateTime CreatedAt
        +DateTime LastUpdatedAt
        +double? TimeInBotMinutes
        +double? TimeInQueueMinutes
        +double? TimeInServiceMinutes
        +double? TimeInConcluidoMinutes
        +double? TimeToFirstResponseMinutes
        +double? TimeToResolutionMinutes
        +int TotalTransitions
        +int? CurrentAttendantId
        +string? CurrentAttendantName
        +bool IsDelayed
        +string? DelayReason
        +List~ConversationPeriodDto~ Timeline
    }

    class ConversationPeriodDto {
        +int ConversationId
        +string State
        +DateTime StartedAt
        +DateTime? EndedAt
        +double? DurationMinutes
        +int? AttendantId
        +string? AttendantName
        +bool IsActive
    }

    class ClinicSummaryDto {
        +int ClinicId
        +string ClinicName
        +DateTime PeriodStart
        +DateTime PeriodEnd
        +int TotalLeads
        +int LeadsInBot
        +int LeadsInQueue
        +int LeadsInService
        +int LeadsConcluded
        +double? AverageTimeToFirstResponseMinutes
        +double? AverageTimeToResolutionMinutes
        +double? AverageTimeInBotMinutes
        +double? AverageTimeInQueueMinutes
        +double? AverageTimeInServiceMinutes
        +int DelayedLeadsCount
        +List~AttendantPerformanceDto~ AttendantsPerformance
        +Dictionary~string,int~ LeadsByState
        +DateTime LastCalculatedAt
    }

    class AttendantPerformanceDto {
        +int AttendantId
        +string AttendantName
        +int TotalLeadsHandled
        +int CurrentActiveLeads
        +int LeadsConcluded
        +double? AverageServiceTimeMinutes
        +double? AverageResolutionTimeMinutes
        +double? ConversionRate
    }

    class SyncLeadDto {
        +int ExternalId
        +List~string~ Tags
        +string? Name
        +string? Phone
        +string? Stage
        +int TenantId
        +DateTime? CreatedAt
        +DateTime? UpdatedAt
    }

    class FiltroLeadsPeriodoDto {
        +int ClinicId
        +int Ano
        +int? Mes
        +int? Dia
    }

    class SetApiKeyRequest {
        +string ApiKey
        +int? ExpiresInDays
    }

    LeadMetricsDto "1" *-- "N" ConversationPeriodDto
    ClinicSummaryDto "1" *-- "N" AttendantPerformanceDto

    %% ══════════════════════════════════════════
    %% SERVICES
    %% ══════════════════════════════════════════

    class LeadService {
        +GetAllLeadsAsync() Task~List~Lead~~
        +SaveLeadAsync(CloudiaWebhookDto) Task~LeadProcessResponseDto~
        +GetActiveLeadsAsync(int limit, int? unitId) Task~List~ActiveLeadDto~~
        +GetLeadsCountByStateAsync(int? unitId) Task~Dictionary~string,int~~
        +GetCheckClosedQueries(int clinicId) Task
        +GetCheckStageWithoutPayment(int clinicId) Task~int~
        +GetVerifyPaymentStep(int clinicId) Task~int~
        +GetVerifySourceFinal(int clinicId) Task
        +GetCheckSourceCloudia(int clinicId) Task
        +GetWeekendLeads(int clinicId) Task
        +GetCheckGroupedStep(int clinicId) Task
        +GetSearchStartMonthLeads(int, DateTime, DateTime) Task
        +GetQueryLeadsByPeriodService(FiltroLeadsPeriodoDto) Task
    }

    class MetricsService {
        +GetDashboardAsync(int clinicId, string attendantType) Task~CloudiaMetricsResponseDto~
        +GetDashboardComHistoricoAsync(int clinicId, AppDbContext) Task
    }

    class LeadAnalyticsService {
        +GetLeadMetricsAsync(int leadId) Task~LeadMetricsDto~
        +GetLeadsMetricsAsync(int unitId, DateTime?, DateTime?, string?) Task~List~LeadMetricsDto~~
        +GetClinicSummaryAsync(int unitId, DateTime?, DateTime?) Task~ClinicSummaryDto~
        +GetDelayedLeadsAsync(int unitId) Task~List~LeadMetricsDto~~
    }

    class RelatorioService {
        <<interface IRelatorioService>>
        +GerarRelatorioMensalAsync(int clinicId, int mes, int ano, CancellationToken) Task~byte[]~
    }

    class PdfRelatorioService {
        <<interface IPdfRelatorioService>>
        +GerarPdf(RelatorioMensalDadosDto dados) byte[]
    }

    class ConfigurationService {
        +SetCloudiaApiKeyAsync(string apiKey, DateTime expiresAt) Task
        +GetConfigurationAsync(string key) Task~AppConfiguration~
        +IsCloudiaApiKeyValidAsync() Task~bool~
    }

    class AttendantService {
        +GetAllAsync() Task~List~Attendant~~
        +GetAssignmentsByLeadAsync(int externalLeadId, int clinicId) Task~List~LeadAssignment~~
        +GetRankingAsync(int clinicId) Task
    }

    class UnitService {
        +GetAllAsync() Task~List~Unit~~
        +GetOrCreateAsync(int clinicId) Task~Unit~
        +GetQuantityLeadsUnit(int clinicId) Task
    }

    class SyncN8N {
        +SyncLead(SyncLeadDto leadData) Task
    }

    class DailyRelatoryService {
        +GenerateDailyRelatory(int tenantId, DateTime date) Task~DailyRelatoryDto~
    }

    class MetaWebhookService {
        +ProcessWebhookAsync(MetaWebhookDto webhook) Task~WebhookProcessResult~
        +ProcessN8nWebhookAsync(N8nWebhookDto webhook) Task~N8nProcessResult~
    }

    class LeadAttributionService {
        +AttributeLeadAsync(string phone, string ctwaClid) Task~LeadAttribution~
    }

    %% ══════════════════════════════════════════
    %% CONTROLLERS
    %% ══════════════════════════════════════════

    class WebhooksController {
        <<Route: /webhooks>>
        +GET / → GetAllLeads()
        +POST /cloudia → Cloudia(CloudiaWebhookDto)
        +GET /consultas → GetHasAppoiment(clinicId)
        +GET /sem-pagamento → GetLeadsWithoutPayment(clinicId)
        +GET /com-pagamento → VerificarEtapaComPagamento(clinicId)
        +GET /source-final → GetSourceFinally(clinicId)
        +GET /origem-cloudia → GetOrigens(clinicId)
        +GET /fim-de-semana → GetLeadsFinaldeSemana(clinicId)
        +GET /etapa-agrupada → GetEtapaAgrupada(clinicId)
        +GET /buscar-inicio-fim → GetBuscarInicioFim(clinicId, dataInicio, dataFim)
        +GET /consulta-periodos → GetConsultaPeriodos(FiltroLeadsPeriodoDto)
        +GET /active → GetActiveLeads(limit, unitId)
        +GET /count-by-state → GetLeadsCountByState(unitId)
        +GET /sync/health → GetSyncHealth()
    }

    class MetricsController {
        <<Route: /metrics>>
        +GET /dashboard → Dashboard(clinicId, attendantType)
        +GET /resumo → Resumo(clinicId)
        +GET /fila → Fila(clinicId)
        +GET /completo → Completo(clinicId)
    }

    class RelatorioController {
        <<Route: /api/relatorios>>
        +GET /mensal → ObterRelatorioMensal(clinicId, mes, ano) → PDF
    }

    class LeadAnalyticsController {
        <<Route: /api/analytics>>
        +GET /leads/{id}/metrics → GetLeadMetrics(id)
        +GET /units/{unitId}/leads-metrics → GetLeadsMetrics(unitId, startDate, endDate, state)
        +GET /units/{unitId}/summary → GetClinicSummary(unitId, startDate, endDate)
        +GET /units/{unitId}/alerts → GetDelayedLeads(unitId)
        +GET /units/{unitId}/dashboard/today → GetTodayDashboard(unitId)
    }

    class ConfigurationController {
        <<Route: /api/config>>
        +POST /cloudia-api-key → SetCloudiaApiKey(SetApiKeyRequest) [X-Admin-Key]
        +GET /cloudia-api-key/status → GetCloudiaApiKeyStatus() [X-Admin-Key]
        +DELETE /cloudia-api-key → DeleteCloudiaApiKey() [X-Admin-Key]
    }

    class AssignmentController {
        <<Route: /assignments>>
        +GET /attendants → GetAllAttendants()
        +GET /lead/{externalLeadId} → GetByLead(externalLeadId, clinicId)
        +GET /ranking → GetRanking(clinicId)
    }

    class SyncLeadController {
        <<Route: /assignments>>
        +POST /sync → SyncLead(SyncLeadDto)
    }

    class UnitController {
        <<Route: /units>>
        +GET / → GetAll()
        +GET /{clinicId} → GetByClinicId(clinicId)
        +PUT /{clinicId} → UpdateName(clinicId, name)
        +GET /quantity-leads → GetQuantityLeadsUnit(clinicId)
    }

    class DailyRelatoryController {
        <<Route: /daily-relatory>>
        +GET /generate → Generate(tenantId, date)
    }

    class MetaWebhookController {
        <<Route: /api/webhooks>>
        +GET /meta → VerifyWebhook(mode, token, challenge)
        +POST /meta → ReceiveMetaWebhook(MetaWebhookDto)
        +POST /meta/n8n → ReceiveN8nWebhook(N8nWebhookDto)
        +POST /cloudia → ReceiveCloudiaWebhook(CloudiaWebhookDto)
    }

    %% ══════════════════════════════════════════
    %% CONTROLLER → SERVICE DEPENDENCIES
    %% ══════════════════════════════════════════

    WebhooksController ..> LeadService
    MetricsController ..> MetricsService
    RelatorioController ..> RelatorioService
    LeadAnalyticsController ..> LeadAnalyticsService
    ConfigurationController ..> ConfigurationService
    AssignmentController ..> AttendantService
    SyncLeadController ..> SyncN8N
    UnitController ..> UnitService
    DailyRelatoryController ..> DailyRelatoryService
    MetaWebhookController ..> MetaWebhookService
    MetaWebhookController ..> LeadService

    %% ══════════════════════════════════════════
    %% SERVICE → MODEL DEPENDENCIES
    %% ══════════════════════════════════════════

    LeadService ..> Lead
    LeadService ..> LeadConversation
    LeadService ..> LeadAssignment
    LeadAnalyticsService ..> Lead
    LeadAnalyticsService ..> LeadConversation
    LeadAnalyticsService ..> Attendant
    AttendantService ..> Attendant
    AttendantService ..> LeadAssignment
    UnitService ..> Unit
    MetaWebhookService ..> OriginEvent
    MetaWebhookService ..> WebhookEvent
    LeadAttributionService ..> LeadAttribution
    LeadAttributionService ..> OriginEvent
    ConfigurationService ..> AppConfiguration
    RelatorioService ..> Lead
    DailyRelatoryService ..> Lead
```

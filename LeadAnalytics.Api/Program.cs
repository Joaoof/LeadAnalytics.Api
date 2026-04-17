using System.Text;
using LeadAnalytics.Api.Data;
using LeadAnalytics.Api.Options;
using LeadAnalytics.Api.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Variavel de ambiente não encontrada para conexão do banco");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? "troque-esta-chave-jwt-com-no-minimo-32-caracteres";
var jwtIssuer = jwtSection["Issuer"] ?? "LeadAnalytics.Api";
var jwtAudience = jwtSection["Audience"] ?? "LeadAnalytics.Frontend";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// 🔥 Swagger correto
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<LeadService>();
builder.Services.AddScoped<UnitService>();
builder.Services.AddScoped<AttendantService>();
builder.Services.AddScoped<IRelatorioService, RelatorioService>();
builder.Services.AddSingleton<IPdfRelatorioService, PdfRelatorioService>();

builder.Services.AddHttpClient<MetricsService>();
builder.Services.AddScoped<MetricsService>();
builder.Services.AddScoped<SyncN8N>();
builder.Services.AddScoped<DailyRelatoryService>();
builder.Services.AddScoped<LeadAttributionService>();
builder.Services.AddScoped<MetaWebhookService>();
builder.Services.AddScoped<ConfigurationService>();
builder.Services.AddScoped<LeadAnalyticsService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

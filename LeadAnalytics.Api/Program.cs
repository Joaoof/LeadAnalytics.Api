using LeadAnalytics.Api.Data;
using LeadAnalytics.Api.Service;
using LeadAnalytics.Api.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Variavel de ambiente não encontrada para conexão do banco");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();

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

var app = builder.Build();
   
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
    c.RoutePrefix = "swagger";
});


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared.Infrastructure.Caching;
using Shared.Infrastructure.Messaging;
using Shared.Infrastructure.Telemetry;
using Triage.Application;
using Triage.Application.Providers;
using Triage.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "TicketTriage.TriageService")
    .WriteTo.Console());

// This service owns nothing but the Triage module: it consumes TicketCreated off its own SQS
// inbox and publishes TicketTriaged/TicketTriageFailed back out — the same outbox/SQS contract
// Tickets always talked to Triage through, so extracting it out of the main Host required no
// protocol change (see docs/adr/006-extract-triage-as-standalone-service.md).
builder.Services.AddTriageApplication(builder.Configuration);
builder.Services.AddTriageInfrastructure(builder.Configuration);

builder.Services.AddSqsMessaging(builder.Configuration);
builder.Services.AddDistributedCaching(builder.Configuration);
builder.Services.AddTicketTriageTelemetry(builder.Configuration, TriageMetrics.MeterName);

var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Triage")!, name: "triage-db");

// Redis is optional (see AddDistributedCaching) — only check it if it's actually configured.
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
    healthChecksBuilder.AddRedis(redisConnectionString, name: "redis-cache");

var app = builder.Build();

using (var migrationScope = app.Services.CreateScope())
{
    await migrationScope.ServiceProvider.GetRequiredService<TriageDbContext>().Database.MigrateAsync();
}

app.UseSerilogRequestLogging();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();

public partial class Program { }

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Host.Endpoints;
using Host.Middleware;
using Host.Telemetry;
using Identity.Application;
using Identity.Infrastructure;
using Shared.Infrastructure.Caching;
using Shared.Infrastructure.Messaging;
using Tickets.Application;
using Tickets.Infrastructure;
using Triage.Application;
using Triage.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "TicketTriage")
    .WriteTo.Console());

// ---- Module registration: each module wires its own Application + Infrastructure layer. ----
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

builder.Services.AddTicketsApplication();
builder.Services.AddTicketsInfrastructure(builder.Configuration);

builder.Services.AddTriageApplication(builder.Configuration);
builder.Services.AddTriageInfrastructure(builder.Configuration);

builder.Services.AddSqsMessaging(builder.Configuration);
builder.Services.AddDistributedCaching(builder.Configuration);
builder.Services.AddTicketTriageTelemetry(builder.Configuration);

// ---- Cross-cutting: CORS, rate limiting, health checks, OpenAPI. ----
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Tickets")!, name: "tickets-db")
    .AddNpgSql(builder.Configuration.GetConnectionString("Triage")!, name: "triage-db")
    .AddNpgSql(builder.Configuration.GetConnectionString("Identity")!, name: "identity-db");

// Redis is optional (see AddDistributedCaching) — only check it if it's actually configured.
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
    healthChecksBuilder.AddRedis(redisConnectionString, name: "redis-cache");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Ticket Triage API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT bearer token."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

using (var migrationScope = app.Services.CreateScope())
{
    await migrationScope.ServiceProvider.GetRequiredService<TicketsDbContext>().Database.MigrateAsync();
    await migrationScope.ServiceProvider.GetRequiredService<TriageDbContext>().Database.MigrateAsync();
    await migrationScope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
    await IdentitySeeder.SeedAsync(migrationScope.ServiceProvider, app.Configuration);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseCors("Default");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapUsersEndpoints();
app.MapTicketsEndpoints();
app.MapUserPreferencesEndpoints();
app.MapOrgSettingsEndpoints();

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

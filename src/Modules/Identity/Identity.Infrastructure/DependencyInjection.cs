using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Identity.Application.Abstractions;
using Identity.Domain;
using Shared.Abstractions;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Identity"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.Schema)));

        services.AddIdentityCore<ApplicationUser>(o =>
            {
                o.Password.RequiredLength = 10;
                o.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        // Bound via IOptions<JwtOptions> (the same resolution JwtTokenService uses to sign a
        // token) rather than a plain POCO bound once at registration time — binding eagerly here
        // and lazily there let the two diverge whenever a configuration source is layered on
        // after this method runs (exactly what WebApplicationFactory's ConfigureAppConfiguration
        // does in tests): the token would get signed with one key and validated against another,
        // failing every authenticated request with 401 while login itself still succeeded.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptionsAccessor) =>
            {
                var jwtOptions = jwtOptionsAccessor.Value;

                // Without this, ASP.NET Core silently rewrites well-known claim types (e.g. "sub")
                // to legacy long-form URIs on the way in, breaking any code that reads the raw
                // JwtRegisteredClaimNames values back out of the ClaimsPrincipal.
                bearerOptions.MapInboundClaims = false;
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtOptions.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    // Tokens carry the short "role" claim type (see JwtTokenService) so the raw
                    // JWT payload stays readable by non-.NET clients (the Angular app decodes it
                    // directly) instead of the long ClaimTypes.Role URI.
                    RoleClaimType = "role",
                };
            });

        services.AddAuthorizationBuilder();
        services.AddAuthorization(options =>
        {
            foreach (var permission in new[]
            {
                Permissions.ViewTickets, Permissions.TriageTickets, Permissions.ResolveTickets,
                Permissions.ReassignTickets, Permissions.ManageUsers, Permissions.ViewReporting,
                Permissions.ManageOrgSettings
            })
            {
                options.AddPolicy(permission, policy => policy.RequireClaim("permission", permission));
            }
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IUserRegistrationService, UserRegistrationService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddScoped<IUserPreferenceService, UserPreferenceService>();
        services.AddScoped<IOrgSettingsRepository, OrgSettingsRepository>();

        return services;
    }
}

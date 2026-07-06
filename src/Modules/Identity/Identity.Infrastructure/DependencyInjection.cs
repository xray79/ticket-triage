using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        var jwtOptions = new JwtOptions();
        configuration.GetSection(JwtOptions.SectionName).Bind(jwtOptions);

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Without this, ASP.NET Core silently rewrites well-known claim types (e.g. "sub")
                // to legacy long-form URIs on the way in, breaking any code that reads the raw
                // JwtRegisteredClaimNames values back out of the ClaimsPrincipal.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
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
                Permissions.ReassignTickets, Permissions.ManageUsers, Permissions.ViewReporting
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

        return services;
    }
}

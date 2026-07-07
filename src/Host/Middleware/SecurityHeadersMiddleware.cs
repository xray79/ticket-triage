namespace Host.Middleware;

/// <summary>
/// Baseline security headers for an API that also serves Swagger UI in dev. CSP is
/// deliberately narrow (no inline scripts/styles beyond what Swagger UI itself needs)
/// rather than a wildcard, since a wildcard CSP is equivalent to not having one.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _isDevelopment;

    public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
    {
        _next = next;
        _isDevelopment = environment.IsDevelopment();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Swagger UI's bundled assets need inline script/style, so relax those two
        // directives in Development only — everywhere else gets the strict policy.
        context.Response.Headers["Content-Security-Policy"] = _isDevelopment
            ? "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; frame-ancestors 'none'; base-uri 'self'"
            : "default-src 'self'; frame-ancestors 'none'; base-uri 'self'";

        if (context.Request.IsHttps)
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        await _next(context);
    }
}

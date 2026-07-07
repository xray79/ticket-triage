using MediatR;
using Identity.Domain;
using Reporting.Application;

namespace Host.Endpoints;

public static class ReportingEndpoints
{
    public static void MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reporting").WithTags("Reporting").RequireAuthorization(Permissions.ViewReporting);

        group.MapGet("/summary", async (ISender sender, CancellationToken ct) =>
        {
            var summary = await sender.Send(new GetReportingSummaryQuery(), ct);
            return Results.Ok(summary);
        });
    }
}

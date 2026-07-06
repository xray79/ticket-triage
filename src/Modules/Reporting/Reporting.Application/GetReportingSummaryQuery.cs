using MediatR;

namespace Reporting.Application;

public sealed record GetReportingSummaryQuery : IRequest<ReportingSummaryDto>;

public sealed class GetReportingSummaryQueryHandler : IRequestHandler<GetReportingSummaryQuery, ReportingSummaryDto>
{
    private readonly ITicketReportRepository _repository;

    public GetReportingSummaryQueryHandler(ITicketReportRepository repository)
    {
        _repository = repository;
    }

    public Task<ReportingSummaryDto> Handle(GetReportingSummaryQuery request, CancellationToken cancellationToken) =>
        _repository.GetSummaryAsync(cancellationToken);
}

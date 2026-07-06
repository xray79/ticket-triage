FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TicketTriage.sln .
COPY src/Host/Host.csproj src/Host/
COPY src/Modules/Tickets/Tickets.Domain/Tickets.Domain.csproj src/Modules/Tickets/Tickets.Domain/
COPY src/Modules/Tickets/Tickets.Application/Tickets.Application.csproj src/Modules/Tickets/Tickets.Application/
COPY src/Modules/Tickets/Tickets.Infrastructure/Tickets.Infrastructure.csproj src/Modules/Tickets/Tickets.Infrastructure/
COPY src/Modules/Tickets/Tickets.Contracts/Tickets.Contracts.csproj src/Modules/Tickets/Tickets.Contracts/
COPY src/Modules/Triage/Triage.Contracts/Triage.Contracts.csproj src/Modules/Triage/Triage.Contracts/
COPY src/Modules/Identity/Identity.Domain/Identity.Domain.csproj src/Modules/Identity/Identity.Domain/
COPY src/Modules/Identity/Identity.Application/Identity.Application.csproj src/Modules/Identity/Identity.Application/
COPY src/Modules/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj src/Modules/Identity/Identity.Infrastructure/
COPY src/Modules/Identity/Identity.Contracts/Identity.Contracts.csproj src/Modules/Identity/Identity.Contracts/
COPY src/Modules/Notifications/Notifications.Domain/Notifications.Domain.csproj src/Modules/Notifications/Notifications.Domain/
COPY src/Modules/Notifications/Notifications.Application/Notifications.Application.csproj src/Modules/Notifications/Notifications.Application/
COPY src/Modules/Notifications/Notifications.Infrastructure/Notifications.Infrastructure.csproj src/Modules/Notifications/Notifications.Infrastructure/
COPY src/Modules/Notifications/Notifications.Contracts/Notifications.Contracts.csproj src/Modules/Notifications/Notifications.Contracts/
COPY src/Modules/Reporting/Reporting.Domain/Reporting.Domain.csproj src/Modules/Reporting/Reporting.Domain/
COPY src/Modules/Reporting/Reporting.Application/Reporting.Application.csproj src/Modules/Reporting/Reporting.Application/
COPY src/Modules/Reporting/Reporting.Infrastructure/Reporting.Infrastructure.csproj src/Modules/Reporting/Reporting.Infrastructure/
COPY src/Modules/Reporting/Reporting.Contracts/Reporting.Contracts.csproj src/Modules/Reporting/Reporting.Contracts/
COPY src/Shared/Shared.Kernel/Shared.Kernel.csproj src/Shared/Shared.Kernel/
COPY src/Shared/Shared.Abstractions/Shared.Abstractions.csproj src/Shared/Shared.Abstractions/
COPY src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj src/Shared/Shared.Infrastructure/
COPY tests/UnitTests/Tickets.Tests/Tickets.Tests.csproj tests/UnitTests/Tickets.Tests/
COPY tests/UnitTests/Triage.Tests/Triage.Tests.csproj tests/UnitTests/Triage.Tests/
COPY tests/UnitTests/Identity.Tests/Identity.Tests.csproj tests/UnitTests/Identity.Tests/
COPY tests/UnitTests/Notifications.Tests/Notifications.Tests.csproj tests/UnitTests/Notifications.Tests/
COPY tests/UnitTests/Reporting.Tests/Reporting.Tests.csproj tests/UnitTests/Reporting.Tests/
COPY tests/IntegrationTests/Tickets.IntegrationTests/Tickets.IntegrationTests.csproj tests/IntegrationTests/Tickets.IntegrationTests/
COPY tests/ArchitectureTests/ArchitectureTests.csproj tests/ArchitectureTests/

RUN dotnet restore src/Host/Host.csproj

COPY src/ src/

RUN dotnet publish src/Host/Host.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Host.dll"]

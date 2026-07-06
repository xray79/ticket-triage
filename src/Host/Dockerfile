FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TicketTriage.sln .
COPY src/Host/Host.csproj src/Host/
COPY src/Modules/Tickets/Tickets.Domain/Tickets.Domain.csproj src/Modules/Tickets/Tickets.Domain/
COPY src/Modules/Tickets/Tickets.Application/Tickets.Application.csproj src/Modules/Tickets/Tickets.Application/
COPY src/Modules/Tickets/Tickets.Infrastructure/Tickets.Infrastructure.csproj src/Modules/Tickets/Tickets.Infrastructure/
COPY src/Modules/Tickets/Tickets.Contracts/Tickets.Contracts.csproj src/Modules/Tickets/Tickets.Contracts/
COPY src/Modules/Triage/Triage.Domain/Triage.Domain.csproj src/Modules/Triage/Triage.Domain/
COPY src/Modules/Triage/Triage.Application/Triage.Application.csproj src/Modules/Triage/Triage.Application/
COPY src/Modules/Triage/Triage.Infrastructure/Triage.Infrastructure.csproj src/Modules/Triage/Triage.Infrastructure/
COPY src/Modules/Triage/Triage.Contracts/Triage.Contracts.csproj src/Modules/Triage/Triage.Contracts/
COPY src/Modules/Identity/Identity.Domain/Identity.Domain.csproj src/Modules/Identity/Identity.Domain/
COPY src/Modules/Identity/Identity.Application/Identity.Application.csproj src/Modules/Identity/Identity.Application/
COPY src/Modules/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj src/Modules/Identity/Identity.Infrastructure/
COPY src/Modules/Identity/Identity.Contracts/Identity.Contracts.csproj src/Modules/Identity/Identity.Contracts/
COPY src/Shared/Shared.Kernel/Shared.Kernel.csproj src/Shared/Shared.Kernel/
COPY src/Shared/Shared.Abstractions/Shared.Abstractions.csproj src/Shared/Shared.Abstractions/
COPY src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj src/Shared/Shared.Infrastructure/
COPY tests/UnitTests/Tickets.Tests/Tickets.Tests.csproj tests/UnitTests/Tickets.Tests/
COPY tests/UnitTests/Triage.Tests/Triage.Tests.csproj tests/UnitTests/Triage.Tests/
COPY tests/UnitTests/Identity.Tests/Identity.Tests.csproj tests/UnitTests/Identity.Tests/
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

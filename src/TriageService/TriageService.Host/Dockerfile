FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TicketTriage.sln .
COPY src/TriageService/TriageService.Host/TriageService.Host.csproj src/TriageService/TriageService.Host/
COPY src/Modules/Triage/Triage.Domain/Triage.Domain.csproj src/Modules/Triage/Triage.Domain/
COPY src/Modules/Triage/Triage.Application/Triage.Application.csproj src/Modules/Triage/Triage.Application/
COPY src/Modules/Triage/Triage.Infrastructure/Triage.Infrastructure.csproj src/Modules/Triage/Triage.Infrastructure/
COPY src/Modules/Triage/Triage.Contracts/Triage.Contracts.csproj src/Modules/Triage/Triage.Contracts/
COPY src/Modules/Tickets/Tickets.Contracts/Tickets.Contracts.csproj src/Modules/Tickets/Tickets.Contracts/
COPY src/Shared/Shared.Kernel/Shared.Kernel.csproj src/Shared/Shared.Kernel/
COPY src/Shared/Shared.Abstractions/Shared.Abstractions.csproj src/Shared/Shared.Abstractions/
COPY src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj src/Shared/Shared.Infrastructure/

RUN dotnet restore src/TriageService/TriageService.Host/TriageService.Host.csproj

COPY src/ src/

RUN dotnet publish src/TriageService/TriageService.Host/TriageService.Host.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TriageService.Host.dll"]

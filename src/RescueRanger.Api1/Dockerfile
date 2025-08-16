# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/RescueRanger.Api/RescueRanger.Api.csproj", "RescueRanger.Api/"]
COPY ["src/RescueRanger.Core/RescueRanger.Core.csproj", "RescueRanger.Core/"]
COPY ["src/RescueRanger.Infrastructure/RescueRanger.Infrastructure.csproj", "RescueRanger.Infrastructure/"]
COPY ["src/RescueRanger.Contracts/RescueRanger.Contracts.csproj", "RescueRanger.Contracts/"]

RUN dotnet restore "RescueRanger.Api/RescueRanger.Api.csproj"

# Copy source code
COPY src/ .

# Build the application
WORKDIR "/src/RescueRanger.Api"
RUN dotnet build "RescueRanger.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "RescueRanger.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .

# Create non-root user
RUN adduser --disabled-password --gecos '' --shell /bin/bash --home /home/appuser appuser && \
    chown -R appuser:appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

EXPOSE 5000
ENTRYPOINT ["dotnet", "RescueRanger.Api.dll"]
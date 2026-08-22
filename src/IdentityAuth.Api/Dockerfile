# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for layer caching
COPY ["src/IdentityAuth.Api/IdentityAuth.Api.csproj", "src/IdentityAuth.Api/"]
COPY ["src/IdentityAuth.Application/IdentityAuth.Application.csproj", "src/IdentityAuth.Application/"]
COPY ["src/IdentityAuth.Domain/IdentityAuth.Domain.csproj", "src/IdentityAuth.Domain/"]
COPY ["src/IdentityAuth.Infrastructure/IdentityAuth.Infrastructure.csproj", "src/IdentityAuth.Infrastructure/"]

RUN dotnet restore "src/IdentityAuth.Api/IdentityAuth.Api.csproj"

# Copy all source code and build
COPY . .
WORKDIR "/src/src/IdentityAuth.Api"
RUN dotnet build "IdentityAuth.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "IdentityAuth.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser

COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

USER appuser

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "IdentityAuth.Api.dll"]

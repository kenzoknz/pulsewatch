# ── Stage 1: Build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["PulseWatch.Api.csproj", "./"]
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install Playwright system dependencies (required for Chromium Deep Check)
RUN apt-get update && apt-get install -y \
    libnss3 libnspr4 libatk1.0-0 libatk-bridge2.0-0 libcups2 \
    libdrm2 libdbus-1-3 libxkbcommon0 libxcomposite1 libxdamage1 \
    libxfixes3 libxrandr2 libgbm1 libasound2 libpango-1.0-0 \
    libcairo2 libatspi2.0-0 libwayland-client0 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Install Playwright browsers after copying published output
RUN dotnet tool install --global Microsoft.Playwright.CLI 2>/dev/null || true
ENV PATH="$PATH:/root/.dotnet/tools"
RUN pwsh -c "playwright install chromium" 2>/dev/null \
    || dotnet PulseWatch.Api.dll --install-playwright 2>/dev/null \
    || true

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "PulseWatch.Api.dll"]

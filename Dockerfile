# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Önce sadece proje dosyalarını kopyala (restore cache'i için)
COPY OtoRehber.sln ./
COPY OtoRehber/OtoRehber.csproj OtoRehber/
COPY OtoRehber.Domain/OtoRehber.Domain.csproj OtoRehber.Domain/
COPY OtoRehber.Infrastructure/OtoRehber.Infrastructure.csproj OtoRehber.Infrastructure/
RUN dotnet restore OtoRehber/OtoRehber.csproj

# Kalan kaynak
COPY . .
RUN dotnet publish OtoRehber/OtoRehber.csproj -c Release -o /app --no-restore

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Root olmayan kullanıcı
RUN adduser --disabled-password --gecos "" appuser
COPY --from=build /app ./
RUN mkdir -p /data/keys && chown -R appuser /app /data
USER appuser

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Database__Provider=Postgres \
    DataProtection__KeyPath=/data/keys
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "OtoRehber.dll"]

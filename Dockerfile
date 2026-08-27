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
RUN chown -R appuser /app
USER appuser

# DataProtection anahtarları PostgreSQL'de saklanır (volume gerektirmez).
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Database__Provider=Postgres
EXPOSE 8080

# Sağlık kontrolü: platformun (Railway/Render) healthcheck yolu "/health" olarak ayarlanmalı.
# (aspnet runtime imajında curl/wget bulunmadığı için Docker HEALTHCHECK eklenmedi.)

ENTRYPOINT ["dotnet", "OtoRehber.dll"]

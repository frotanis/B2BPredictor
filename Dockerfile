# Етап 1: Збірка та публікація
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копіюємо файли конфігурації рішення та проєктів для кешування restore
COPY Directory.Packages.props ./
COPY Directory.Build.props ./
COPY src/B2BPredictor.Domain/B2BPredictor.Domain.csproj src/B2BPredictor.Domain/
COPY src/B2BPredictor.Application/B2BPredictor.Application.csproj src/B2BPredictor.Application/
COPY src/B2BPredictor.Infrastructure/B2BPredictor.Infrastructure.csproj src/B2BPredictor.Infrastructure/
COPY src/B2BPredictor.Api/B2BPredictor.Api.csproj src/B2BPredictor.Api/

RUN dotnet restore src/B2BPredictor.Api/B2BPredictor.Api.csproj

# Копіюємо решту вихідного коду та публікуємо Release-збірку
COPY src/ src/
RUN dotnet publish src/B2BPredictor.Api/B2BPredictor.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Етап 2: Легкий runtime-образ без SDK
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Створюємо каталог для SQLite БД та передаємо права вбудованому non-root користувачу
RUN mkdir -p /app/data && chown -R $APP_UID:$APP_UID /app/data

COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "B2BPredictor.Api.dll"]
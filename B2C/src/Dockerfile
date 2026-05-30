# Этап 1 (build): компилируем проект
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем .csproj отдельно — для кэширования слоя с restore.
# Если изменятся только .cs файлы, restore не будет пересоздаваться.
COPY ["B2C.Api/B2C.Api.csproj", "B2C.Api/"]
COPY ["B2C.Application/B2C.Application.csproj", "B2C.Application/"]
COPY ["B2C.Domain/B2C.Domain.csproj", "B2C.Domain/"]
COPY ["B2C.Infrastructure/B2C.Infrastructure.csproj", "B2C.Infrastructure/"]

# Восстанавливаем NuGet пакеты для всего графа зависимостей через корневой Api.csproj
RUN dotnet restore "B2C.Api/B2C.Api.csproj"

# Копируем весь исходный код (тестовые проекты и obj/bin исключатся через .dockerignore)
COPY . .

# Билдим Api в Release
WORKDIR "/src/B2C.Api"
RUN dotnet build "B2C.Api.csproj" -c Release -o /app/build

# Этап 2 (publish): создаём готовый к запуску набор
FROM build AS publish
RUN dotnet publish "B2C.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Этап 3 (final): минимальный runtime образ (только ASP.NET, без SDK)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Внутри контейнера слушаем 8080 (как и B2B — единый порт по всем сервисам).
# Маппинг наружу делает docker-compose: 8083:8080.
EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "B2C.Api.dll"]
# Этап 1 (build): тут компилируем проект
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем .csproj отдельно — для кэширования слоя с restore
COPY ["B2B.API/B2B.Api.csproj", "B2B.Api/"]
COPY ["B2B.Application/B2B.Application.csproj", "B2B.Application/"]
COPY ["B2B.Domain/B2B.Domain.csproj", "B2B.Domain/"]
COPY ["B2B.Infrastructure/B2B.Infrastructure.csproj", "B2B.Infrastructure/"]

# Восстанавливаем NuGet пакеты
RUN dotnet restore "B2B.Api/B2B.Api.csproj"

# Копируем весь исходный код
COPY . .

# Билдим приложение в Release
WORKDIR "/src/B2B.API"
RUN dotnet build "B2B.Api.csproj" -c Release -o /app/build

# Этап 2 (publish): создаём готовый к запуску набор файлов
FROM build AS publish
RUN dotnet publish "B2B.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Этап 3 (final): минимальный runtime образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "B2B.Api.dll"]
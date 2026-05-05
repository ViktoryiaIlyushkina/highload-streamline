# ЭТАП 1: Сборка и восстановление зависимостей
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файлы проектов с учетом новой структуры (контекст - корень решения)
COPY ["src/HighLoad.Streamline.Api/HighLoad.Streamline.Api.csproj", "src/HighLoad.Streamline.Api/"]
COPY ["src/HighLoad.Streamline.Shared/HighLoad.Streamline.Shared.csproj", "src/HighLoad.Streamline.Shared/"]

# Восстанавливаем пакеты
RUN dotnet restore "src/HighLoad.Streamline.Api/HighLoad.Streamline.Api.csproj"

# Копируем все исходники и переходим в папку проекта API
COPY . .
WORKDIR "/src/src/HighLoad.Streamline.Api"

# Строим проект
RUN dotnet build "HighLoad.Streamline.Api.csproj" -c Release -o /app/build

# ЭТАП 2: Публикация (теперь у этого этапа есть имя 'publish')
FROM build AS publish
RUN dotnet publish "HighLoad.Streamline.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ЭТАП 3: Финальный образ для запуска (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80

# Копируем результат из этапа publish
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "HighLoad.Streamline.Api.dll"]
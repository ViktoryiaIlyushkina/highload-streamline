# ЭТАП 1: Сборка
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем проекты из папки src (контекст - корень решения)
COPY ["src/HighLoad.Streamline.Worker/HighLoad.Streamline.Worker.csproj", "src/HighLoad.Streamline.Worker/"]
COPY ["src/HighLoad.Streamline.Shared/HighLoad.Streamline.Shared.csproj", "src/HighLoad.Streamline.Shared/"]

# Восстанавливаем зависимости конкретно для воркера
RUN dotnet restore "src/HighLoad.Streamline.Worker/HighLoad.Streamline.Worker.csproj"

# Копируем все исходники решения
COPY . .

# Переходим в глубокую папку проекта внутри контейнера
WORKDIR "/src/src/HighLoad.Streamline.Worker"

# Строим проект
RUN dotnet build "HighLoad.Streamline.Worker.csproj" -c Release -o /app/build

# ЭТАП 2: Публикация (теперь у этого этапа есть имя 'publish')
FROM build AS publish
RUN dotnet publish "HighLoad.Streamline.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ЭТАП 3: Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Для воркера EXPOSE 80 обычно не нужен, но оставим, если он слушает метрики
EXPOSE 80

# Копируем результат из этапа publish
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "HighLoad.Streamline.Worker.dll"]
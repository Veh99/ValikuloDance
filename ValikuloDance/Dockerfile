# Этап 1: Базовый образ для runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Этап 2: Сборка проекта
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем csproj
COPY ["ValikuloDance.csproj", "."]
RUN dotnet restore "./ValikuloDance.csproj"

# Копируем весь код
COPY . .

# Собираем проект
RUN dotnet build "./ValikuloDance.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этап 3: Публикация
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ValikuloDance.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этап 4: Финальный образ
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ValikuloDance.dll"]
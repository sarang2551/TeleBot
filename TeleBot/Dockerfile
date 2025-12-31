# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/telebot

# Copy only the csproj first (for restore caching)
COPY TeleBot.csproj .
RUN dotnet restore "TeleBot.csproj"

# Copy the rest of the source (including appsettings.json)
COPY . .
RUN dotnet build "TeleBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TeleBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8001
ENTRYPOINT ["dotnet", "TeleBot.dll"]

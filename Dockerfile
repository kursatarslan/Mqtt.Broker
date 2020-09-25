FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
EXPOSE 8883 1883 8080

# 1883  MQTT
# 8883  MQTT/SSL
# 8080  MQTT WebSockets

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src

# Copy only API for restore packages

COPY ["Mqtt.Data/Mqtt.Data.csproj", "Mqtt.Data/"]
COPY ["Mqtt.Context/Mqtt.Context.csproj", "Mqtt.Context/"]
COPY ["Mqtt.Client/Mqtt.Client.csproj", "Mqtt.Client/"]
COPY ["Mqtt.Domain/Mqtt.Domain.csproj", "Mqtt.Domain/"]
COPY ["Mqtt.Application/Mqtt.Application.csproj", "Mqtt.Application/"]
COPY ["Mqtt.Server/Mqtt.Server.csproj", "Mqtt.Server/"]

# Restore packages
RUN dotnet restore "Mqtt.Server/Mqtt.Server.csproj" 


# Copy rest of the Project
COPY . .

# Build API
WORKDIR "/src/Mqtt.Server"

RUN dotnet build "Mqtt.Server.csproj" -c Release -o /app/build

# Publish
WORKDIR "/src/Mqtt.Server"
FROM build AS publish
RUN dotnet publish "Mqtt.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Mqtt.Server.dll"]

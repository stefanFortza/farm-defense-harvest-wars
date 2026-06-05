# Stage 1: Build .NET Backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy csproj and restore
COPY ["FarmDefenseHarvestWars.Backend/FarmDefenseHarvestWars.Backend.csproj", "FarmDefenseHarvestWars.Backend/"]
COPY ["FarmDefenseHarvestWars.Shared/FarmDefenseHarvestWars.Shared.csproj", "FarmDefenseHarvestWars.Shared/"]
RUN dotnet restore "FarmDefenseHarvestWars.Backend/FarmDefenseHarvestWars.Backend.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/app/FarmDefenseHarvestWars.Backend"
RUN dotnet publish "FarmDefenseHarvestWars.Backend.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install basic dependencies for Headless Godot (Ubuntu 24.04 compatible)
RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    libdbus-1-3 \
    libxcursor1 \
    libxinerama1 \
    libxrandr2 \
    libxi6 \
    libgl1 \
    && rm -rf /var/lib/apt/lists/*

# 1. Copy the built .NET Backend
COPY --from=build /app/publish .

# 2. Copy the EXPORTED Godot Server
# IMPORTANT: You must export your Godot project for Linux (Dedicated Server)
# and place the files in the 'ServerExport' folder in the root of the repo.
COPY ServerExport/ ./ServerExport/

# Ensure the exported Linux binary is executable
# Replace 'FarmWarsServer.x86_64' with your actual exported binary name if different
RUN chmod +x ./ServerExport/*.x86_64 || true

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80

# Configure the Orchestrator to use the exported binary.
# We leave GodotServer__ProjectPath EMPTY because we are running an exported build.
ENV GodotServer__ExecutablePath=./ServerExport/FarmDefenseHarvestWars.GameClient.x86_64
ENV GodotServer__ProjectPath=""
ENV GodotServer__Host=0.0.0.0

EXPOSE 80
EXPOSE 7777-7800

ENTRYPOINT ["dotnet", "FarmDefenseHarvestWars.Backend.dll"]

# Godot Server Configuration Guide

This document explains how to configure the .NET backend to spawn and communicate with Godot headless servers for match execution.

---

## Overview

When players queue for a match, the .NET backend ([ProcessMatchServerOrchestrator.cs](../FarmDefenseHarvestWars.Backend/Services/ProcessMatchServerOrchestrator.cs)) spawns a dedicated Godot server process with:

- **Command-line arguments**: Port, match ID, server flag
- **Environment variables**: Match ID, Defender deck, Attacker deck (as JSON)

The Godot server reads these via [CmdArgs.cs](../FarmDefenseHarvestWars.GameClient/Scripts/Utils/CmdArgs.cs) and initializes the match.

---

## Configuration: appsettings.json

Edit `FarmDefenseHarvestWars.Backend/appsettings.json` to specify Godot server paths and network settings:

```json
{
  "GodotServer": {
    "ExecutablePath": "/path/to/godot/executable",
    "ProjectPath": "/path/to/FarmDefenseHarvestWars.GameClient",
    "Host": "127.0.0.1",
    "StartingPort": 7777
  }
}
```

### Configuration Parameters

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `ExecutablePath` | string | Absolute or relative path to Godot 4.3+ executable | `/usr/bin/godot`, `C:\Godot\godot.exe`, `./godot` |
| `ProjectPath` | string | Absolute or relative path to Godot project root (contains `project.godot`) | `/home/user/farm-defense-harvest-wars/FarmDefenseHarvestWars.GameClient`, `./FarmDefenseHarvestWars.GameClient` |
| `Host` | string | Network bind address (localhost for dev, `0.0.0.0` for external) | `127.0.0.1`, `0.0.0.0`, `192.168.1.100` |
| `StartingPort` | integer | Base port number (incremented per match to avoid collisions) | `7777` |

---

## Platform-Specific Setup

### Linux

1. **Install Godot 4.3+ (headless)**:
   ```bash
   # Download from https://godotengine.org/download
   # Extract to a known location
   wget https://github.com/godotengine/godot-builds/releases/download/4.3-stable/Godot_v4.3-stable_linux.x86_64.zip
   unzip Godot_v4.3-stable_linux.x86_64.zip
   sudo mv Godot_v4.3-stable_linux.x86_64 /usr/local/bin/godot
   chmod +x /usr/local/bin/godot
   ```

2. **Update appsettings.json**:
   ```json
   {
     "GodotServer": {
       "ExecutablePath": "/usr/local/bin/godot",
       "ProjectPath": "/home/user/farm-defense-harvest-wars/FarmDefenseHarvestWars.GameClient",
       "Host": "127.0.0.1",
       "StartingPort": 7777
     }
   }
   ```

3. **Verify installation**:
   ```bash
   /usr/local/bin/godot --version
   ```

---

### Windows

1. **Install Godot 4.3+**:
   - Download from https://godotengine.org/download
   - Extract to a known location (e.g., `C:\Godot`)

2. **Update appsettings.json**:
   ```json
   {
     "GodotServer": {
       "ExecutablePath": "C:\\Godot\\godot.exe",
       "ProjectPath": "C:\\Users\\YourName\\farm-defense-harvest-wars\\FarmDefenseHarvestWars.GameClient",
       "Host": "127.0.0.1",
       "StartingPort": 7777
     }
   }
   ```

3. **Verify installation**:
   ```cmd
   C:\Godot\godot.exe --version
   ```

---

### macOS

1. **Install Godot 4.3+**:
   ```bash
   # Download from https://godotengine.org/download
   # Extract and move to Applications
   mv Godot.app /Applications/
   chmod +x /Applications/Godot.app/Contents/MacOS/Godot
   ```

2. **Update appsettings.json**:
   ```json
   {
     "GodotServer": {
       "ExecutablePath": "/Applications/Godot.app/Contents/MacOS/Godot",
       "ProjectPath": "/Users/yourname/farm-defense-harvest-wars/FarmDefenseHarvestWars.GameClient",
       "Host": "127.0.0.1",
       "StartingPort": 7777
     }
   }
   ```

3. **Verify installation**:
   ```bash
   /Applications/Godot.app/Contents/MacOS/Godot --version
   ```

---

## Environment Variables (Alternative Configuration)

For CI/CD, Docker, or production deployments, configure via environment variables instead of editing appsettings.json.

### .NET Configuration Binding with `__` Separator

.NET uses `__` (double underscore) to denote nested JSON keys. Map as follows:

| appsettings.json Key | Environment Variable |
|---------------------|----------------------|
| `GodotServer:ExecutablePath` | `GodotServer__ExecutablePath` |
| `GodotServer:ProjectPath` | `GodotServer__ProjectPath` |
| `GodotServer:Host` | `GodotServer__Host` |
| `GodotServer:StartingPort` | `GodotServer__StartingPort` |

### Linux / macOS

```bash
export GodotServer__ExecutablePath="/usr/local/bin/godot"
export GodotServer__ProjectPath="/home/user/farm-defense-harvest-wars/FarmDefenseHarvestWars.GameClient"
export GodotServer__Host="127.0.0.1"
export GodotServer__StartingPort="7777"

# Then run backend
cd FarmDefenseHarvestWars.Backend
dotnet run
```

Or set inline:

```bash
GodotServer__ExecutablePath="/usr/local/bin/godot" \
GodotServer__ProjectPath="/home/user/farm-defense-harvest-wars/FarmDefenseHarvestWars.GameClient" \
  dotnet run -p FarmDefenseHarvestWars.Backend
```

### Windows (PowerShell)

```powershell
$env:GodotServer__ExecutablePath = "C:\Godot\godot.exe"
$env:GodotServer__ProjectPath = "C:\path\to\FarmDefenseHarvestWars.GameClient"
$env:GodotServer__Host = "127.0.0.1"
$env:GodotServer__StartingPort = "7777"

cd FarmDefenseHarvestWars.Backend
dotnet run
```

### Windows (Command Prompt)

```cmd
setx GodotServer__ExecutablePath "C:\Godot\godot.exe"
setx GodotServer__ProjectPath "C:\path\to\FarmDefenseHarvestWars.GameClient"
setx GodotServer__Host "127.0.0.1"
setx GodotServer__StartingPort "7777"

cd FarmDefenseHarvestWars.Backend
dotnet run
```

---

## Docker / Production Deployment

### Docker Compose Example

Include Godot configuration in environment variables:

```yaml
version: '3.8'

services:
  backend:
    image: farm-defense-backend:latest
    environment:
      GodotServer__ExecutablePath: /usr/local/bin/godot
      GodotServer__ProjectPath: /app/FarmDefenseHarvestWars.GameClient
      GodotServer__Host: "0.0.0.0"  # Accept from all interfaces
      GodotServer__StartingPort: "7777"
    ports:
      - "5000:5000"
    volumes:
      - /path/to/godot:/usr/local/bin/godot  # Mount Godot executable
      - /path/to/project:/app/FarmDefenseHarvestWars.GameClient  # Mount project

  game-server:
    image: godot:4.3-headless
    # Godot server processes spawned dynamically by backend
```

---

## Verification

### 1. Test Backend Can Find Godot

```bash
# Navigate to backend directory
cd FarmDefenseHarvestWars.Backend

# Run the backend
dotnet run

# Check logs for errors related to GodotServer configuration
# Should see: "Loaded configuration from appsettings.json"
```

### 2. Manual Godot Server Startup

Test that Godot spawns correctly:

```bash
# Set environment variables for test
export MATCH_ID="test-123"
export DEFENDER_DECK_JSON='["Chicken","Cow"]'
export ATTACKER_DECK_JSON='["Wolf","Fox"]'

# Run Godot server in headless mode
/usr/local/bin/godot \
  --headless \
  --path /home/user/farm-defense-harvest-wars/FarmDefenseHarvestWars.GameClient \
  -- \
  --server \
  --port 7777 \
  --match-id test-123

# Godot console should show:
# [GameState] Match configured | MatchId: test-123 | Defender deck: Chicken, Cow | Attacker deck: Wolf, Fox
# [GameplayManager] Spawned Chicken at (6, 3) for Defender
# [GameplayManager] Spawned Cow at (6, 4) for Defender
# [GameplayManager] Spawned Wolf at (16, 3) for Attacker
# [GameplayManager] Spawned Fox at (16, 4) for Attacker
```

### 3. Full Match Test

1. Start backend:
   ```bash
   cd FarmDefenseHarvestWars.Backend
   dotnet run
   ```

2. Start two Godot clients:
   ```bash
   # Terminal 1 - Defender
   /usr/local/bin/godot --path /path/to/FarmDefenseHarvestWars.GameClient

   # Terminal 2 - Attacker
   /usr/local/bin/godot --path /path/to/FarmDefenseHarvestWars.GameClient
   ```

3. Queue both players via UI → Backend spawns Godot server → Check server logs for match ID and deck composition

---

## Troubleshooting

### "GodotServer:ExecutablePath is empty"

**Issue**: `InvalidOperationException: GodotServer:ExecutablePath and GodotServer:ProjectPath must be configured.`

**Solution**:
- Verify appsettings.json has `ExecutablePath` set (not empty string)
- Or set environment variable: `GodotServer__ExecutablePath=/usr/local/bin/godot`

### "Godot executable not found"

**Issue**: Backend logs show process failed to start.

**Solution**:
- Verify the executable path exists: `ls -la /usr/local/bin/godot`
- Check path doesn't have spaces (or quote in appsettings.json: `"ExecutablePath": "/path with spaces/godot"`)
- On Windows, use backslashes: `C:\\Godot\\godot.exe`

### "Failed to parse DEFENDER_DECK_JSON"

**Issue**: Godot logs: `Failed to deserialize DEFENDER_DECK_JSON: ...`

**Solution**:
- Backend serializes enums correctly (checked in ProcessMatchServerOrchestrator.cs)
- Verify Godot has System.Text.Json available (it does in C# GameClient)
- Check JSON format: `["Chicken","Wolf"]` (lowercase enum values)

### Godot Server Crashes Immediately

**Issue**: Server process exits quickly after start.

**Solution**:
- Check Godot version is 4.3+ (older versions may not support headless mode the same way)
- Check ProjectPath points to correct location with `project.godot` file
- Run manually with debug flags:
  ```bash
  /usr/local/bin/godot --headless --path /path/to/project -- --server --port 7777 2>&1 | tee godot_debug.log
  ```

### Port Already in Use

**Issue**: Backend fails to spawn multiple servers on same port range.

**Solution**:
- `StartingPort` is automatically incremented per match in ProcessMatchServerOrchestrator.cs
- If you see "Address already in use", ensure old Godot processes are terminated:
  ```bash
  pkill -f "godot.*--server"
  ```

---

## Development Tips

### Relative Paths

You can use relative paths in appsettings.json (relative to backend's working directory):

```json
{
  "GodotServer": {
    "ExecutablePath": "../Godot/godot",
    "ProjectPath": "../FarmDefenseHarvestWars.GameClient"
  }
}
```

### Debugging Spawned Processes

To see Godot server output, redirect stdout/stderr in ProcessMatchServerOrchestrator.cs:

```csharp
RedirectStandardOutput = true,
RedirectStandardError = true
```

Then capture and log process output.

### CI/CD Integration

In GitHub Actions, Docker, or GitLab CI:

```yaml
# Example: GitHub Actions
- name: Configure Godot Server
  env:
    GodotServer__ExecutablePath: /usr/bin/godot
    GodotServer__ProjectPath: ${{ github.workspace }}/FarmDefenseHarvestWars.GameClient
  run: |
    cd FarmDefenseHarvestWars.Backend
    dotnet run
```

---

## Related Files

- Backend Orchestrator: [ProcessMatchServerOrchestrator.cs](../FarmDefenseHarvestWars.Backend/Services/ProcessMatchServerOrchestrator.cs)
- Godot CmdArgs Parser: [CmdArgs.cs](../FarmDefenseHarvestWars.GameClient/Scripts/Utils/CmdArgs.cs)
- Godot Game State: [GameState.cs](../FarmDefenseHarvestWars.GameClient/_Autoload/GameState.cs)
- Godot Gameplay Initialize: [GameplayManager.cs](../FarmDefenseHarvestWars.GameClient/Scenes/Gameplay/GameplayManager/GameplayManager.cs)
- Map Layout: [MAP_LAYOUT.md](MAP_LAYOUT.md)

---

## Summary Checklist

- [ ] Download Godot 4.3+ to known location
- [ ] Update `appsettings.json` with correct `ExecutablePath` and `ProjectPath`
- [ ] Verify paths exist: `ls -la <ExecutablePath>` and `ls -la <ProjectPath>/project.godot`
- [ ] Test match spawn: Queue 2 players and check logs for match ID & deck
- [ ] Verify units spawn from queued decks (not hardcoded test units)

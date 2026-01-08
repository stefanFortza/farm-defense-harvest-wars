# Farm Defense: Harvest Wars - AI Coding Agent Instructions

## 🎮 Project Overview

**Farm Defense: Harvest Wars** is a 1v1 asymmetric lane-defense strategy game with a hybrid architecture:

- **.NET Backend** (WebAPI): Authentication, persistence, meta-game management
- **Godot Client**: Cross-platform UI and visualization
- **Godot Headless Server**: Authoritative game simulation, economy logic, pathfinding

**Current Phase**: Core gameplay loop (Phase 2-3 of MVP roadmap)

---

## 🏗️ Architecture & Component Boundaries

### Backend (.NET 10 WebAPI)

- **Location**: `FarmDefenseHarvestWars.Backend/`
- **Responsibility**: Auth via ASP.NET Identity + JWT, player persistence, profile management
- **Key Files**:
  - `Program.cs`: Configuration, Identity setup (lenient password rules for dev), Swagger
  - `Controllers/GameController.cs`: API endpoints
  - `Data/ApplicationDbContext.cs`: EF Core with SQLite (dev) / PostgreSQL (prod)
- **Critical Pattern**: Stateless REST API—**NO game logic here**. Server doesn't validate moves or run simulation.

### Shared Library (.NET Standard)

- **Location**: `FarmDefenseHarvestWars.Shared/`
- **Responsibility**: DTOs, enums, API contracts shared between Backend + Client
- **Key Files**:
  - `API/IGameApi.cs`: Refit interface for HTTP calls (Register, Login, GetProfile)
  - `Constants/ApiRoutes.cs`: Hardcoded API paths
  - `Models/`: Auth/Game DTOs (PlayerProfileDto, UnitType enums, etc.)
  - `Enums/`: GameState, PlayerRole, UnitType (Defender vs Attacker units)

### Game Client (Godot 4.3 + C#)

- **Location**: `FarmDefenseHarvestWars.GameClient/`
- **Dual Connection Model**:
  - **Menu/Auth**: HTTP via `NetworkManager.Api` (Refit)
  - **Gameplay**: ENet/UDP via `MultiplayerPeer` (Godot's built-in networking)
- **Autoload Singletons** (`_Autoload/`):
  - `GameState.cs`: Holds logged-in player profile, emits signals for UI reactivity
  - `NetworkManager.cs`: Manages both HTTP client and ENet peer, token storage, role assignment
  - `AudioController.cs`: Global audio management
- **Game Flow**:
  1. Auth screens → HTTP login → token stored → profile loaded into `GameState`
  2. Connect to Godot server (ENet) → role assigned (Defender/Attacker) based on join order
  3. Gameplay: Multiplayer synchronization via `MultiplayerSynchronizer` + RPC calls

---

## 🎮 Asymmetric Gameplay & Economy

### Defender (Farmer) - Left side of grid

- **Resource**: Milk (passive +2/sec baseline)
- **Win Condition**: Survive timer (time-based victory)
- **Unit Types**: Chicken (ranged), Cow (tank), Sheep (income), Pig (trap/mine)

### Attacker (Predator) - Right side of grid

- **Resource**: Meat (passive +5/sec baseline, bounty rewards on kill)
- **Win Condition**: Destroy barn (barn HP → 0)
- **Unit Types**: Wolf (grunt), Fox (speedster/jumper), Bear (siege tank)

**Critical Design**: Asymmetry drives different decision trees. AI must respect role-specific constraints.

---

## 🔄 Data Flow & Cross-Component Communication

```
Auth Flow:
  Client → POST /register or /login → Backend (JWT) → GameState stores token + profile

Gameplay Flow:
  1. Client connects to Godot Server (ENet)
  2. Server detects client order → assigns role (Defender/Attacker)
  3. Client calls SpawnUnitOnGrid(unitType, gridPos)
  4. GridManager.GetWorldPosition(gridPos) → Unit placed on map
  5. Server runs physics/collision in _PhysicsProcess
  6. MultiplayerSynchronizer broadcasts position updates
  7. Attacks via RPC: Server validates collision → damage → QueueFree if dead
```

**Key Pattern**: Server is authoritative. Clients send intent, server validates and broadcasts results via RPC/Synchronizer.

---

## 📁 Critical Paths by Component

### Game Loop & Managers

- `Scenes/Gameplay/GameplayController.cs`: Scene root, orchestrates managers
- `Entities/Units/`: BaseUnit hierarchy (DefenderUnit, AttackerUnit, specific unit classes)
- `Scripts/Core/`: GridManager, UnitManager, GameUI
- `Resources/UnitStats/`: Per-unit stat configs (cost, HP, attack speed, etc.)

### Networking & Multiplayer

- `_Autoload/NetworkManager.cs`: ENet peer setup, role assignment logic
- `Scenes/Gameplay/`: MultiplayerSpawner for dynamic unit instantiation
- Unit state syncing via `MultiplayerSynchronizer` (position, HP, animation state)

### UI & Menus

- `Scenes/Authentication/`: Login/Register screens
- `Scenes/Menus/`: Main menu, settings
- `UI/`: HUD, Components (resource counters, unit selection, placement tools)

---

## 🛠️ Developer Workflows

### Backend Setup

```bash
# Start PostgreSQL (or SQLite auto-creates)
docker-compose up -d

# Run migrations
cd FarmDefenseHarvestWars.Backend
dotnet ef database update

# Start API server
dotnet run
# Swagger at http://localhost:5000/swagger
```

### Client Build & Debug

- **Godot Editor**: Open `project.godot` in Godot 4.3
- **C# Scripts**: Edit in VS Code or VS2022 (IntelliSense via C# plugin)
- **Testing Locally**: Run two Godot instances or one headless server + one client

### Testing Multiplayer

- **Server**: `godot --headless --server` (port 7777)
- **Clients**: Two instances connecting to server, auto-role assignment triggers

---

## 📋 Project-Specific Conventions

### Naming & Structure

- **C# Files**: PascalCase (GameplayController, BaseUnit, UnitManager)
- **Autoload Singletons**: Accessed via `ClassName.Instance` (NOT `GetNode<T>("path")`)
- **Enums**: Located in `Shared/Enums/` (source of truth for UnitType, GameState, PlayerRole)
- **Godot Scenes**: `.tscn` paired with optional C# root node

### Multiplayer Patterns

- **Authority**: Server only runs `_PhysicsProcess` and validates actions
- **Synchronization**: Use `@export` + `MultiplayerSynchronizer` for continuous properties (position, HP)
- **Events**: Use RPC (`[Rpc]` attribute) for discrete events (unit died, attack fired, etc.)
- **Player Role**: Retrieved via `NetworkManager.Instance.GetCurrentRole()` at runtime

### API & HTTP

- **Refit Interface**: Defined in `IGameApi.cs`, instantiated in `NetworkManager._Ready()`
- **Token Management**: Stored in `NetworkManager._accessToken`, injected via `[Headers("Authorization: Bearer")]`
- **Request/Response Models**: Always use DTOs from Shared (never raw JSON in controller)

### Game Economy & Unit Spawning

- **Grid Coordinates**: `Vector2I` (column, row) → world position via `GridManager.GetWorldPosition(gridPos)`
- **Server-Side Validation**: Attacker can only spawn at right edge, Defender only in left 80% (implement in `SpawnUnit`)
- **Cost Deduction**: Checked before spawn; refund mechanic on unit death for Attacker

---

## ⚠️ Common Pitfalls & Gotchas

1. **Stateful Backend**: Don't cache game state in .NET (players disconnect, server reboots). All game logic lives in Godot server.
2. **Client Authority**: Never trust client input. Godot server must validate grid placement, attack targets, resource costs.
3. **Token Expiry**: JWT tokens from .NET API may expire mid-session; plan for token refresh or re-login flow.
4. **MultiplayerSynchronizer vs RPC**: Use Synchronizer for continuous state (position), RPC for events (death, attack).
5. **Role Assignment Order**: First connected client = Defender, second = Attacker. Document clearly to avoid swap bugs.

---

## 📚 References

- **Game Context**: [docs/GAME_CONTEXT.md](../docs/GAME_CONTEXT.md)
- **Map Layout**: [docs/MAP_LAYOUT.md](../docs/MAP_LAYOUT.md)
- **Godot Multiplayer Docs**: https://docs.godotengine.org/en/stable/tutorials/networking/high_level_multiplayer.html
- **Refit**: Declarative HTTP client for .NET
- **ASP.NET Identity**: Built-in auth framework (configured in `Program.cs`)

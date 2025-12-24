# 🚜 Farm Defense: Harvest Wars - System Context

## 1. Project Vision

**Title:** Farm Defense: Harvest Wars
**Genre:** Asymmetric 1v1 Lane Defense Strategy.
**Core Concept:** A tug-of-war between a Farmer (Defender) and a Predator (Attacker).
**Winning Condition:**

* **Defender:** Survives until the timer runs out (Time Limit Victory).
* **Attacker:** Destroys the Defender's Barn (HP reaches 0).

## 2. Technical Architecture (Hybrid Model)

### **A. Master Server (.NET 10 WebAPI)**

* **Role:** Meta-Game Manager.
* **Responsibilities:** Auth, Persistent Inventory, Matchmaking.
* **No Game Logic:** Does not run the simulation.

### **B. Game Server (Godot 4.3 Headless)**

* **Role:** The Authority / Referee.
* **Responsibilities:** Runs the physics, economy logic, pathfinding, and validates actions.
* **Protocol:** `ENet` (UDP) via `HighLevelMultiplayer`.

### **C. Game Client (Godot 4.3)**

* **Role:** Visualization.
* **Dual Connection:**
* **Menu:** HTTP (Refit) -> .NET API.
* **Gameplay:** ENet (UDP) -> Godot Server.



---

## 3. Gameplay Mechanics (Asymmetric Design)

### **A. Economy System**

The two players operate on completely different economic models to encourage different playstyles.

#### **Player 1: Defender (The Farmer)**

* **Resource:** `Milk`
* **Playstyle:** **Investment & Defense**.
* **Passive Income:** **Low** (e.g., +2 Milk/sec).
* **Active Income:** Must place **Sheep** units.
* *Sheep Mechanic:* Occupies a grid tile. Generates a burst of Milk every X seconds.


* **Risk:** Investing in Sheep leaves you vulnerable to early attacks.

#### **Player 2: Attacker (The Predator)**

* **Resource:** `Meat`
* **Playstyle:** **Aggression & Snowball**.
* **Passive Income:** **Medium** (e.g., +5 Meat/sec).
* **Active Income (Bounty System):** Earns Meat by **killing Defender units**.
* *Kill Reward:* ~30% of the destroyed unit's cost is refunded to the Attacker as Meat.


* **Risk:** Passive play results in a loss because the Farmer's economy will outscale the Attacker's over time.

### **B. The Grid (Lane System)**

* **Layout:** 5 Horizontal Lanes.
* **Defender Zone:** Can place units on the left 80% of the grid.
* **Attacker Zone:** Can only spawn units at the far right edge of the lanes.

### **C. Units & Abilities**

#### **Defender Units (Static)**

1. **Chicken (Ranged DPS):**
* *Cost:* Low.
* *Attack:* Shoots eggs in a straight line. Single target.


2. **Cow (Tank/Wall):**
* *Cost:* Medium.
* *Attack:* None. High HP. Blocks enemy movement.


3. **Sheep (Economy Generator):**
* *Cost:* Medium.
* *Attack:* None. Low HP.
* *Ability:* Adds `+25 Milk` to the player's bank every 5 seconds.


4. **Pig (Trap/Mine):**
* *Cost:* High.
* *Ability:* Explodes on contact, dealing massive AoE damage to nearby enemies.



#### **Attacker Units (Mobile)**

1. **Wolf (Grunt):**
* *Cost:* Low.
* *Stats:* Balanced Speed/HP/Damage.


2. **Fox (Speedster):**
* *Cost:* Medium.
* *Ability:* Jumps over the first obstacle (Cow) it encounters. Low HP.


3. **Bear (Siege Tank):**
* *Cost:* High.
* *Stats:* Very slow movement. Massive HP. Deals bonus damage to "Cow" units.



---

## 4. Coding Standards & Implementation Rules

### **Networking (Godot)**

* **RPCs:** Use `[Rpc(MultiplayerApi.RpcMode.AnyPeer)]` for client-to-server actions (e.g., `RequestSpawnUnit`).
* **Validation:** The Server **MUST** check if the player has enough Milk/Meat before spawning.
* **Sync:** Use `MultiplayerSynchronizer` for syncing Unit HP and Position.
* **Spawning:** Use `MultiplayerSpawner` node for creating units dynamically.

### **Data Structures (Shared Project)**

* Use `UnitType` Enum to identify units.
* Use `PlayerRole` Enum (`Defender`, `Attacker`).

### **Server-Side Loop (GameServer)**

The `_Process` loop on the Server handles the economy:

```csharp
// Pseudo-code logic for Copilot reference
if (IsServer) {
    // Passive Income
    Defender.Milk += 2 * delta;
    Attacker.Meat += 5 * delta;

    // Sheep Logic
    foreach (var sheep in ActiveSheep) {
        if (sheep.TimerFinished) {
             Defender.Milk += 25;
             sheep.ResetTimer();
        }
    }
}

```

---

## 5. File Structure Targets

* `Entities/Units/Defender/Sheep.tscn` + `Sheep.cs`
* `Entities/Units/Attacker/Wolf.tscn` + `Wolf.cs`
* `Scenes/Gameplay/GameWorld.tscn`: The main arena.
* `Scripts/Core/GameManager.cs`: Server-side logic for keeping score and time.
* `Scripts/UI/GameplayHUD.cs`: Displays Milk for P1 and Meat for P2.

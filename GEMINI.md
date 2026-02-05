CONTEXT TEHNIC: Farm Defense - Harvest Wars (Licență)

1. DESCRIERE PROIECT

Tip: Multiplayer PvP Tower Defense (Lane-based Strategy).
Concept: Similar cu "Plants vs Zombies" dar 1vs1, unde un jucător este DEFENDER (pune turnuri/plante) și celălalt este ATTACKER (trimite valuri de inamici).
Platformă: PC (Windows/Linux).
Engine: Godot 4.5 (.NET Version).

2. STACK TEHNOLOGIC

Game Client: Godot 4.5 (C#) - Interfața grafică, input, randare.

Game Server: Godot 4.5 (Headless Mode) - Autoritate server, fizică, logică joc, stare unități. Rulează dedicat.

Backend API: ASP.NET Core 8.0 - Autentificare (JWT), Bază de date (Useri, Rank), Matchmaking Queue.

Protocol Rețea: * API -> Client: HTTP / SignalR (pentru matchmaking events).

GameServer <-> Client: ENet (Godot High-Level Multiplayer API).

3. STRUCTURA FOLDERELOR

/
├── FarmDefenseHarvestWars.GameClient/   # Proiect Godot (Client + Server Logic)
│   ├── Scenes/                          # Scene (.tscn)
│   │   ├── Authentication/              # Login/Register UI
│   │   ├── Gameplay/                    # Harta, Unități, HUD
│   │   └── Menus/                       # Meniu principal, Lobby
│   ├── Scripts/                         # Scripturi C#
│   │   ├── Core/                        # GridSystem, Pathfinding, NetworkManager
│   │   ├── Gameplay/                    # Logică specifică (UnitManager, Economy)
│   │   └── Entities/                    # Clasele de bază (BaseUnit, AttackerUnit)
│   ├── Resources/                       # ScriptableObjects (Stats, Items)
│   └── Assets/                          # Grafică (Cute Fantasy Pack)
│
├── FarmDefenseHarvestWars.Backend/      # ASP.NET Core API
│   ├── Controllers/                     # API Endpoints
│   ├── Models/                          # DB Models
│   └── Services/                        # Matchmaking Service
│
└── FarmDefenseHarvestWars.Shared/       # Librărie de clase partajată (DTOs, Enums)
    ├── Enums/                           # UnitType, GameState
    └── Models/                          # DTO-uri pentru pachete de rețea


4. REGULI DE GAMEPLAY & MECANICI

A. Harta și Grid-ul

Harta este împărțită în 5 Benzi (Lanes) orizontale.

Nu există mișcare pe verticală pentru unități.

Grid-ul este logic (stocat într-un Array bidimensional Unit[,] pe server).

B. Roluri

DEFENDER (Stânga):

Construiește Turnuri (Vaci, Găini) și Ferme pe grid.

Scop: Să protejeze BAZA din stânga.

Resursă: Primește bani pasiv + bonus din Ferme.

ATTACKER (Dreapta):

Spawnează Mobs (Lupi, Scheleți) pe benzile alese.

Scop: Să distrugă BAZA din stânga.

Mobs se mișcă automat de la dreapta la stânga (Vector2.Left).

C. Condiții de Victorie

Attacker Win: HP-ul Bazei Defender-ului scade la 0.

Defender Win: Timpul (ex: 5 minute) expiră.

5. ARHITECTURA DE REȚEA (CRITIC)

Fluxul de Conectare

Login: Client -> POST /api/login -> Primește JWT.

Matchmaking: Client -> SignalR JoinQueue -> Așteaptă.

Start Meci: API -> Lansează GodotServer.exe -> Trimite IP:Port la ambii clienți prin SignalR.

Handshake: Clienții se conectează la Godot Server via ENet (CreateClient).

Sync: Serverul setează rolurile (RpcId) și începe jocul.

Sincronizare (Godot)

RPC (Remote Procedure Calls): Folosite pentru acțiuni unice (Spawn Unit, Build Tower, Use Ability).

MultiplayerSynchronizer: Folosit pentru poziția inamicilor și HP-ul unităților.

Server Authority: Clientul NU scade HP. Clientul trimite "Vreau să atac", Serverul calculează damage-ul și trimite înapoi "Unitate distrusă".

6. CONVENȚII DE COD (C#)

Namespace-uri: Respectă structura folderelor.

Godot Nodes: Folosește [Export] pentru referințe din editor. Evită GetNode() în _Process.

Clean Code:

Clase derivate din BaseUnit.

Logica de rețea separată în NetworkManager sau metode marcate cu [Rpc].

Folosire Signal pentru decuplare (ex: OnUnitDied emis de unitate, ascultat de GameManager).

Logging: Folosește clasa custom Logger.Log() în loc de GD.Print().

7. OBIECTIVE CURENTE (ROADMAP)

[ ] Phase 1: Rulare instanțe multiple (Server + 2 Clienți) și conectare ENet.

[ ] Phase 2: Implementare Grid Click -> RPC -> Spawn Visuals (Hardcoded visuals).

[ ] Phase 3: Mișcare liniară inamici (Server-side movement, Client-side interpolation).
# 🚜 Farm Defense: Harvest Wars - The "30-Day MVP" Roadmap

## 🎯 Obiectiv

Un joc funcțional 1v1 (MVP), unde jucătorii se loghează prin .NET API, iar meciul rulează pe un Godot Dedicated Server autoritar.

---

## 📅 Phase 1: Infrastructure (Backend & Auth) - ✅ DONE

*Fundația proiectului este finalizată.*

* [x] **Project Setup**: Soluție .NET Monorepo (Client, Backend, Shared).
* [x] **Auth API**: Login/Register via ASP.NET Identity & JWT.
* [x] **Database**: EF Core Configurat.
* [x] **Client Networking**: Godot Client cu Refit și NetworkManager.

---

## 📅 Phase 2: The "Handshake" (Networking Core)

*Scop: Să conectăm 2 clienți la un server Godot, stabilind rolurile.*

* [x] **Server Bootstrapper:** Script care pornește Godot cu argumentul `--headless` și ascultă pe portul `7777`.
* [x] **Direct Connect:** Implementare buton "Debug Join" în meniu pentru conectare directă la IP (`127.0.0.1`).
* [x] **Role Assignment:** Serverul identifică ordinea conectării:
* Client 1 -> **Defender** (Stânga).
* Client 2 -> **Attacker** (Dreapta).
* [x] **Lobby Sync:** Meciul începe automat când ambii jucători sunt conectați.

---

## 📅 Phase 3: Gameplay Loop (Multiplayer First)

*Scop: Mecanica de joc funcțională, sincronizată prin ENet (UDP).*

* [x] **Unit Architecture:** Implementare ierarhie clase: `BaseUnit`, `DefenderUnit`, `AttackerUnit` (cu State Machine simplu).
* [x] **Grid & Spawning:**
* Implementare `MultiplayerSpawner` pentru instanțiere dinamică.
* Validare Server-side: Jucătorul poate plasa doar în zona lui.

* [ ] **Movement Logic:**
* Unitățile se mișcă pe Server (`_PhysicsProcess`).
* `MultiplayerSynchronizer` actualizează poziția pe Client.
* [ ] **Combat System:**
* Detectare coliziune (Server).
* Scădere HP și RPC pentru moarte (`QueueFree`).
* [ ] **Win Condition:**
* Hambar HP == 0 -> Attacker Wins.
* Timer == 0 -> Defender Wins.

---

## 📅 Phase 4: Integration & Economy (Matchmaking is Here!)

*Scop: Transformăm demo-ul tehnic într-un produs finit și îl legăm de Backend.*

* [ ] **Economy System:**
* Implementare resurse distincte: `Milk` (Defender) vs `Meat` (Attacker).
* Logică pasivă de generare pe Server.
* [ ] **HUD Final:** Afișare resurse și timer în UI (sincronizate).
* [ ] **Matchmaking Simplificat (.NET API):**
* Backend: Endpoint `POST /match/find` (Queue in-memory).
* Logic: Returnează IP-ul serverului (`127.0.0.1` pt demo) când găsește pereche.
* Frontend: Butonul "Find Match" face tranziția automată în joc.
* [ ] **Game Reporting:** La finalul meciului, Godot Server trimite `POST /api/match/result` către .NET (Cine a câștigat/XP).

---

## 📅 Phase 5: Persistence & Polish (The "Wow" Factor)

*Scop: Finisaje vizuale și sistem de progresie.*

* [ ] **Shop System (UI & API):**
* Tab-uri în Meniu: Deck / Shop.
* Endpoint `POST /shop/buy` pentru deblocare unități.
* [ ] **Visuals:**
  * Înlocuire cuburi cu Sprite-uri (Pixel Art).
  * Animații (Idle, Walk, Attack) folosind State Machine.
* [ ] **Audio:** Sunete de fundal și efecte (Attack, Spawn).
* [ ] **Export & Test:** Build final Windows/Linux și testare Local LAN.

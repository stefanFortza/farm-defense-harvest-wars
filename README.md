# 🚜 Farm Defense: Harvest Wars - Development Roadmap

Acest document urmărește progresul dezvoltării jocului **Farm Defense**, un joc de strategie 1v1 construit pe arhitectură modernă .NET.

## 🛠 Tech Stack

* **Game Engine:** Godot 4.3+ (.NET Module)
* **Backend:** ASP.NET Core Web API (.NET 10 Preview)
* **Database:** PostgreSQL (Production) / SQLite (Dev)
* **ORM:** Entity Framework Core
* **Networking:** Refit (REST) + SignalR (Real-time)
* **Auth:** JWT (JSON Web Tokens) + ASP.NET Identity

---

## 📅 Phase 1: Infrastructure & Authentication (Backend Core)

Focus pe securitate, baza de date și comunicarea Client-Server.

* [x] **Project Setup**: Inițializare Soluție .NET (Monorepo: Client, Backend, Shared).
* [x] **Database Architecture**: Configurare EF Core și Migrări (User table).
* [x] **Authentication API**: Endpoint-uri pentru `/register` și `/login` (Identity).
* [x] **Security**: Implementare JWT Bearer Tokens și Swagger Auth.
* [ ] **Networking Client**: Configurare `HttpClient` și `Refit` în Godot.
* [ ] **State Management**: Implementare `GameState` și `NetworkManager` (Singleton/Autoload).

## 🎮 Phase 2: Core Gameplay Loop (Frontend)

Construirea scenei de joc și a mecanicilor de bază (fără server momentan).

* [ ] **Game Scene**: Creare scenă `Gameplay.tscn` (1v1 Arena).
* [ ] **Grid System**: Implementare TileMap și logică de plasare pe grid.
* [ ] **Unit Spawning**: Sistem de instanțiere dinamică a unităților (Vaci, Găini).
* [ ] **Basic AI**: Pathfinding simplu (NavigationServer2D) către baza inamică.
* [ ] **Combat Logic**: Sistem de Health, Damage și Attack Range.
* [ ] **Economy**: Generare pasivă de resurse (Aur) și costuri de unități.

## 📡 Phase 3: Multiplayer Synchronization

Sincronizarea stării jocului între server și client pentru modul 1v1.

* [ ] **Matchmaking**: Endpoint simplu pentru a găsi un oponent.
* [ ] **Real-time Comms**: Integrare **SignalR** pentru evenimente live.
* [ ] **Action Sync**: Trimiterea acțiunilor (Spawn Unit) către server via RPC.
* [ ] **State Validation**: Serverul validează dacă jucătorul are bani pentru unitatea cerută.
* [ ] **Game Loop**: Gestionare Start Meci -> Luptă -> Game Over.

## 💾 Phase 4: Persistence & Meta-Game

Salvarea progresului și elemente RPG.

* [ ] **Profile API**: Endpoint pentru încărcarea/salvarea XP-ului și nivelului.
* [ ] **Inventory System**: Structură DB pentru unități deblocate.
* [ ] **Shop UI**: Interfață pentru deblocarea unităților noi cu banii câștigați.
* [ ] **Scoreboard**: Listă cu cei mai buni jucători (Leaderboard).

## ✨ Phase 5: Polish & UI/UX

Finisarea experienței vizuale.

* [ ] **Feedback Vizual**: Animații de atac, particule la moartea unităților.
* [ ] **Responsive UI**: Meniuri funcționale (Main Menu, Settings, GameOver).
* [ ] **Sound**: Adăugare efecte sonore și muzică de fundal.
* [ ] **Deployment**: Dockerizare Backend și Export Client (Windows/Linux).

---

### 📝 Note de Dezvoltare

* *Versiunea actuală rulează pe .NET 10 (Experimental) pentru Backend.*
* *Comunicarea HTTP este strict tipizată folosind DTO-uri partajate.*

---

### Sfat pentru Licență:

Pe măsură ce lucrezi, intră în acest fișier pe GitHub și bifează căsuțele (schimbă `[ ]` în `[x]`).
Profesorilor le place enorm să vadă "activitate" și un plan care devine verde treptat. Arată că ești organizat!

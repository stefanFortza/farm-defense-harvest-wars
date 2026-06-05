# 🚀 Ghid de Deployment și Configurare

Acest ghid explică modul în care poți împacheta aplicația folosind Docker într-un mod profesional (Production-Ready) și cum să configurezi sistemul.

---

## 📦 1. Build Imagine Docker (Producție)

Spre deosebire de mediul de development, imaginea de producție folosește un **Export precompilat** al serverului de Godot. Acest lucru reduce dimensiunea imaginii și elimină timpul de compilare la pornirea meciurilor.

### Pasul 1: Exportă Serverul din Godot
1. Deschide proiectul în Godot.
2. Mergi la **Project -> Export**.
3. Adaugă un preset **Linux/X11**, bifează **Export as Dedicated Server**.
4. Exportă fișierele în folderul `ServerExport/` din rădăcina repository-ului tău.
   - Fișiere rezultate: `ServerExport/FarmWarsServer.x86_64` și `ServerExport/FarmWarsServer.pck`.

### Pasul 2: Comandă Build Docker
După ce ai pus fișierele în `ServerExport/`, rulează:

```bash
docker build -t farm-defense-backend:latest -f FarmDefenseHarvestWars.Backend/Dockerfile .
```

*Imaginea va conține acum doar backend-ul .NET și serverul precompilat.*

---

## 🎮 2. Structura Directorului de Export

Pentru ca Docker-ul să funcționeze corect, structura ta locală trebuie să arate așa:
```text
farm-defense-harvest-wars/
├── ServerExport/
│   ├── FarmWarsServer.x86_64
│   └── FarmWarsServer.pck
├── FarmDefenseHarvestWars.Backend/
│   └── Dockerfile
└── ...
```

---

## 🛠️ 3. Configurare Sistem

### A. Folosind Docker Compose (Recomandat)
Cea mai simplă metodă de a rula backend-ul.

1. Asigură-te că fișierul `docker-compose.yml` este configurat corect (SQLite este activat implicit).
2. Pornirea serviciilor:
   ```bash
   docker-compose up -d
   ```

### B. Configurare Client Godot
Clientul are nevoie de adresa backend-ului pentru a funcționa.

#### 1. Fișier `config.cfg` (Local)
Creează un fișier numit `config.cfg` în folderul `FarmDefenseHarvestWars.GameClient/`:

```ini
[Network]
backend_url="http://localhost:5177"

[Auth]
email="user@test.com"
password="password123"
```
*Acest fișier este ignorat de Git, deci setările tale rămân private.*

#### 2. Variabile de Mediu (CI/CD sau Docker)
Dacă rulezi clientul dintr-un script sau container, poți seta:
- `BACKEND_BASE_URL`: URL-ul backend-ului.
- `GAME_EMAIL` / `GAME_PASSWORD`: Pentru testare rapidă.

---

## 🌐 3. Expunerea în Rețea (Porturi)

Pentru ca jucătorii să se poată conecta, următoarele porturi trebuie să fie deschise în firewall:

| Port | Protocol | Descriere |
| :--- | :--- | :--- |
| **5177** | TCP | REST API (Login, Matchmaking) |
| **7777 - 7800** | UDP | Trafic de Joc (Godot ENet) |

> **IMPORTANT**: Dacă rulezi pe un VPS (ex: DigitalOcean, AWS), asigură-te că porturile UDP sunt deschise, altfel clienții vor rămâne blocați la "Connecting...".

---

## 🚀 4. Rularea Serverului (Build vs Source)

Backend-ul este acum suficient de inteligent să ruleze serverul de Godot în două moduri, în funcție de variabilele de mediu setate:

### Modul A: Folosind Proiectul Sursă (Recomandat pentru Dev/Docker)
Dacă vrei să rulezi rapid fără să exporți proiectul:
- `GodotServer__ExecutablePath`: Calea către binarul Godot (ex: `/usr/local/bin/godot`).
- `GodotServer__ProjectPath`: Calea către folderul proiectului (ex: `./FarmDefenseHarvestWars.GameClient`).

### Modul B: Folosind un Build Exportat (Performanță Maximă)
Dacă ai deja binarul exportat (ex: `FarmWarsServer.x86_64`):
- `GodotServer__ExecutablePath`: Calea către binarul exportat.
- `GodotServer__ProjectPath`: **Lasă-l gol** (sau șterge variabila). 

Backend-ul va detecta că nu există `ProjectPath` și va rula binarul direct ca un server autonom.

---

## 💾 5. Persistența Datelor (SQLite)

În configurarea Docker, baza de date este salvată într-un volum numit `farm_data`.
- Locația în container: `/app/data/game.db`
- Locația pe gazdă: Gestionată de Docker (poți vedea volumul cu `docker volume inspect farm_data`).

Dacă vrei să resetezi baza de date:
```bash
docker-compose down -v
```

---

## 🔍 5. Verificare Status

După ce ai pornit containerele, poți verifica dacă totul este OK:

1. **API Check**: Accesează `http://localhost:5177/swagger` în browser.
2. **Logs**: Rulează `docker logs -f farm_backend` pentru a vedea cererile de matchmaking în timp real.
3. **Client**: Pornește jocul din Godot. Dacă adresa este corectă, ar trebui să poți face Login/Register imediat.

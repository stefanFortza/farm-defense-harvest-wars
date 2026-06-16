# 📝 Scenariu Înregistrare Demo: Farm Defense (3:30 min)

**Pregătire tehnică (Pre-Recording):**
1. **Baza de date:** Timp deblocare cufăr setat la 1 secundă.
2. **Echilibrare:** Daune unități atacatoare crescute (pentru final rapid).
3. **OBS Layout:** 
   - Stânga: Client Jucător 1 (Attacker)
   - Dreapta: Client Jucător 2 (Defender)
   - Jos/Centru: Terminal SSH conectat la VPS (`docker compose logs -f`)

---

## Faza 1: Autentificare și Metagame (0:00 - 0:50)

**Acțiune (Ce faci):**
* **0:00 - 0:10:** Pornire simultană. Te loghezi pe contul din stânga. Arăți scurt meniul principal.
* **0:10 - 0:25:** Intri la **Settings**. Miști sliderele de volum și schimbi rezoluția/fullscreen. Salvezi.
* **0:25 - 0:45:** Intri în **Deck Building**. Muți rapid 4-5 unități în sloturi (demonstrezi debouncing-ul: deși faci multe click-uri, terminalul de jos nu arată activitate HTTP până nu te oprești).
* **0:45 - 0:50:** Mergi la cufere și apeși pe **Start Unlock**. Cufărul intră în starea de deblocare.

**Narațiune tehnică (Ce spui):**
> "Începem prin autentificarea securizată via ASP.NET Identity. Sistemul de setări și managementul inventarului utilizează o coadă asincronă pe client cu mecanism de debouncing. Aceasta permite o experiență de utilizare fluidă, comasând multiplele modificări de pachet într-un singur apel API eficient către baza de date SQLite."

---

## Faza 2: Matchmaking și Orchestrare (0:50 - 1:20)

**Acțiune (Ce faci):**
* **0:50 - 1:00:** Ambii jucători apasă **Find Match**. Apare ecranul de "Searching...".
* **1:00 - 1:15:** **FOCUS PE TERMINAL:** Se vede cum log-ul se mișcă. Apare mesajul de instanțiere a procesului Godot și portul UDP alocat (ex: 7777).
* **1:15 - 1:20:** Ambii clienți fac tranziția automată către harta de joc.

**Narațiune tehnică (Ce spui):**
> "Matchmaking-ul este gestionat de un orchestrator thread-safe. În momentul în care doi jucători sunt asociați, backend-ul lansează dinamic un proces izolat de Godot Headless pe VPS-ul Linux. Observăm în terminal alocarea portului UDP și transmiterea coordonatelor de conectare către clienți."

---

## Faza 3: Gameplay Authoritative (1:20 - 2:05)

**Acțiune (Ce faci):**
* **1:20 - 1:40:** Jucătorul din dreapta plasează o unitate defensivă (Găina). Ea începe să tragă. Jucătorul din stânga plasează un atacator.
* **1:40 - 2:05:** Se vede lupta. Proiectilele lovesc, viața scade. Miști mouse-ul pe ambele ecrane pentru a arăta sincronizarea.

**Narațiune tehnică (Ce spui):**
> "Sistemul este strict Server-Authoritative. Poziția unităților, calculul daunelor și inteligența artificială sunt procesate exclusiv pe serverul dedicat. Clienții primesc starea jocului prin RPC-uri și utilizează interpolare liniară (Lerp) pentru a masca latența rețelei și a asigura o fluiditate vizuală de 60 FPS."

---

## Faza 4: Reziliență - Disconnect & Reconnect (2:05 - 2:45)

**Acțiune (Ce faci):**
* **2:05 - 2:10:** Închizi brusc fereastra jucătorului din dreapta (Alt+F4).
* **2:10 - 2:20:** Jucătorul din stânga continuă să joace scurt. Terminalul arată pierderea conexiunii, dar meciul rămâne activ pe server.
* **2:20 - 2:35:** Redeschizi clientul din dreapta. Intri în meci (Reconnect).
* **2:35 - 2:45:** Unitatea apărătorului reapare pe ecran exact unde era, cu viața actualizată.

**Narațiune tehnică (Ce spui):**
> "Simulăm acum o eroare critică de rețea. La deconectarea clientului, serverul menține sesiunea activă. La reconectare, se execută un transfer de tip 'Full State Snapshot'. Serverul transmite noului client întreaga ierarhie de entități și variabilele de stare, permițând reluarea meciului fără pierderea progresului."

---

## Faza 5: Final și Persistență (2:45 - 3:30)

**Acțiune (Ce faci):**
* **2:45 - 3:00:** Atacatorul spawnează rapid unități. Baza este distrusă. Apare ecranul de **Victory/Defeat**.
* **3:00 - 3:15:** Revenire în meniul principal. **FOCUS PE GOLD/XP:** Se vede cum valorile s-au incrementat.
* **3:15 - 3:30:** Deschizi cufărul (care s-a deblocat între timp), primești fragmente și dai **Upgrade** la o unitate.

**Narațiune tehnică (Ce spui):**
> "La finalul meciului, instanța de joc trimite un callback securizat către API, care finalizează tranzacția în baza de date. Procesul Godot este închis pentru a elibera resursele. Revenind în meniu, observăm persistența recompenselor: aurul și fragmentele primite sunt acum disponibile pentru upgrade-uri, închizând astfel ciclul principal de gameplay."
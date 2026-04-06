# GEMINI - Engineering Guidelines (GameClient)

Status: activ
Scope: FarmDefenseHarvestWars.GameClient
Limbaj principal: C# (Godot 4)
Nivel: normativ (MUST/SHOULD)

Acest document definește regulile obligatorii pentru codul din GameClient.
Când codul existent nu respectă încă regula, abaterea este listată la "Excepții temporare".

## 1) Principii de bază

1. Regula de aur: codul nou MUST urma acest document chiar dacă există cod legacy care nu este încă aliniat.
2. Schimbările SHOULD fie locale, mici, și să nu rupă API-uri publice fără nevoie.
3. Godot lifecycle MUST rămână previzibil: wiring în _Ready(), cleanup în _ExitTree().
4. Starea globală MUST fi mutată prin servicii dedicate și semnale, nu prin side-effect-uri ascunse.

## 2) Arhitectură proiect

### 2.1 Straturi

1. _Autoload/ MUST conține servicii singleton de infrastructură (state, deck, networking, audio).
2. Entities/ MUST conține gameplay runtime (unități, componente, state machine, proiectile).
3. Scenes/ MUST conține prezentare și compoziție UI/scene.
4. Scripts/ MUST conține utilitare, contracte, și data models reutilizabile.
5. addons/ MUST conține tooling de editor, separat de gameplay runtime.

### 2.2 Direcție de dependențe

1. UI MAY apela servicii din _Autoload.
2. Serviciile MUST NOT depinde de scene concrete de UI.
3. Componentele de gameplay SHOULD depinde de contracte/interfețe, nu de noduri UI.
4. Plugin-urile editor MUST NOT introduce dependențe runtime în gameplay.

## 3) Convenții C#

### 3.1 Structură fișier

1. Namespace file-scoped MUST fi folosit pentru fișierele noi.
2. Tipurile Godot SHOULD rămâne partial class unde modelul engine-ului o cere.
3. Câmpurile private SHOULD folosi prefix _camelCase.
4. API-ul public SHOULD fi minim și explicit.

### 3.2 Nullability

1. Referințele [Export] MUST fi validate în _Ready() înainte de utilizare.
2. Pentru validări de dependențe MUST folosi extension-ul EnsureNotNull (Scripts/Core/Utils/ValidationExtensions.cs).
3. Null-forgiving operator (!) MAY fi folosit doar când există garanție de lifecycle (ex: singleton setat în _Ready()).

### 3.3 Colecții și imutabilitate

1. Contractele publice SHOULD expune IReadOnlyList/IReadOnlyCollection, nu List direct.
2. Snapshot-urile de stare MUST returna copii defensive când există concurență.
3. Colecțiile interne mutate concurent MUST fi protejate cu lock sau mecanism echivalent.

## 4) Convenții Godot runtime

### 4.1 Exports, semnale, wiring

1. [Export] MUST fi folosit pentru referințe de noduri/config necesare din editor.
2. [Signal] MUST fi folosit pentru evenimente de domeniu între noduri.
3. Subscribe la evenimente MUST avea unsubscribe simetric în _ExitTree() când există binding runtime.
4. Nodurile de gameplay SHOULD valida toate dependențele la start și fail-fast la lipsă.

### 4.2 Componentizare

1. Unitățile MUST folosi compoziție prin componente (Health, Movement, Hurtbox, Vision etc.).
2. Inițializarea componentelor SHOULD urma pattern-ul IInitializable<T>.
3. Comportamentele de state machine MUST fi înregistrate explicit la startup (ex: RegisterStates()).

## 5) Stare globală și semnale

Referință actuală: _Autoload/GameState.cs

1. GameState rămâne punct central pentru profil, deck curent, rol atribuit și semnale globale.
2. Actualizările de stare MUST emite semnale dedicate (ProfileUpdated, DeckUpdated, DeckSaveStatusChanged etc.).
3. Citirea stării partajate MUST folosi snapshot-uri când există lock intern.
4. Orice nouă stare globală SHOULD fi adăugată doar dacă nu poate fi localizată într-un service de domeniu.

## 6) Networking și deck synchronization

Referințe principale:
- _Autoload/DeckService.cs
- _Autoload/Networking/MenuNetwork.cs
- _Autoload/Networking/NetworkBootstrap.cs

### 6.1 Comportament curent (Current)

1. DeckService normalizează deck-ul pe rol (max 5, fără duplicate, doar unități compatibile).
2. Salvarea deck-ului folosește coadă per rol + versioning per rol + loop de procesare.
3. Save status este raportat prin GameState.SetDeckSaveInProgress și NotifyDeckSaveResult.
4. Sync de la server există per rol (SyncDeckForRoleFromServerAsync), cu opțiune skip când există save in-flight.

### 6.2 Direcție target (Target)

1. Refresh deck MUST exista la intrare/revenire în contextul de deck management (tab focus/scene entry).
2. Retry pentru erori tranzitorii SHOULD fi introdus la update deck (backoff limitat).
3. Versiunile SHOULD fi urmărite clar până la confirmare server, pentru feedback UI robust.
4. Când există mismatch local-vs-server, serverul MUST rămâne source of truth.

### 6.3 Reguli de implementare networking

1. Api client bootstrap MUST rămână centralizat în NetworkBootstrap.
2. UI MUST comunica prin servicii/network layer, nu direct cu request-uri ad-hoc.
3. Erorile API MUST fi logate cu context minim (rol, operație) fără leak de secrete.
4. Operațiile de scriere SHOULD evita concurență necontrolată (gate/lock/version).

## 7) UI tabs routing și role-based visibility

Referințe principale:
- Scenes/UI/Components/TabsManager.cs
- memorii repo: ui-tabs-routing

### 7.1 Reguli pentru TabsManager

1. Cheia de tab MUST corespunde cu numele paginilor din zone (mapping pe node name).
2. Show/Hide pagini MUST fi centralizat în TabsManager, nu duplicat în fiecare pagină.
3. Schimbarea de stare a tab-ului SHOULD actualiza și ZIndex, și vizibilitatea paginilor.

### 7.2 Reguli pentru role-based UI

1. Vizibilitatea elementelor specifice rolului MUST fi reaplicată după schimbarea tab-ului.
2. Dacă există animații de tab, reaplicarea role visibility SHOULD rula după semnalul de finalizare animație.
3. Componentele role-aware MUST trata explicit ambele roluri (Defender/Attacker) și fallback sigur.
4. UI MUST NOT presupune că toate nodurile role-specifice sunt active automat după tab switch.

## 8) Tooling de editor (C# plugins)

Referință principală: addons/unit_creator/UnitCreatorPlugin.cs

1. Plugin-urile C# MUST fi încapsulate în #if TOOLS pentru a evita execuție runtime în build game.
2. Înregistrarea meniurilor editor MUST avea cleanup simetric în _ExitTree().
3. Operațiile de generare asset/scene SHOULD valida existența resurselor înainte de scriere.
4. Plugin-urile SHOULD forța refresh de filesystem după generare.
5. Convențiile de tooling din acest document se aplică doar plugin-urilor C#.

## 9) Logging și handling erori

1. Erorile fatale de wiring MUST oprească fluxul curent devreme (fail-fast) cu mesaj clar.
2. Erorile recuperabile SHOULD continua aplicația și notifica UI/core prin semnal/status.
3. Logging-ul MUST păstra context operațional minim: componentă, operație, rol/id relevant.

## 10) Checklist pentru PR-uri în GameClient

1. A fost validată orice dependență [Export] înainte de utilizare?
2. Subscribe/unsubscribe la evenimente este simetric?
3. Deck operations respectă normalize + versioning + save status?
4. UI tabs respectă mapping pe cheie și role-based visibility după animații?
5. Contractele publice expun tipuri read-only unde e posibil?
6. Plugin/editor code este izolat de runtime gameplay?

## 11) Excepții temporare (debt list)

Acestea sunt abateri acceptate temporar până la alinierea completă:

1. Namespace style mixt
- Regula: fișierele noi MUST file-scoped namespace.
- Stare curentă: există fișiere fără namespace file-scoped.
- Direcție: migrare incrementală când se editează fișierul.

2. Logging neuniform (GD.Print direct)
- Regula: logging consistent, contextual.
- Stare curentă: coexistă apeluri directe GD.Print/GD.PrintErr.
- Direcție: standardizare graduală în fluxurile critice.

3. Deck sync hardening incomplet
- Regula target: refresh/retry/version confirm robust.
- Stare curentă: există versioning și loop per rol, dar retry/backoff și refresh automat nu sunt complet standardizate.
- Direcție: implementare în iterații, menținând backend ca source of truth.

4. Role-based wiring distribuit
- Regula: role visibility MUST reaplicată determinist după tab switch/animație.
- Stare curentă: există mecanism principal, dar wiring-ul complet poate varia pe ecrane.
- Direcție: consolidare prin pattern unic reutilizabil.

## 12) Politica de evoluție a documentului

1. Orice regulă nouă MUST include: scop, motiv, impact, exemplu de aplicare.
2. Orice excepție nouă MUST include: motiv, risc, direcție de remediere.
3. Când implementarea ajunge la target, secțiunea "Excepții temporare" SHOULD fi redusă explicit.

---

Versiune: 2026-04-04
Owner: GameClient team

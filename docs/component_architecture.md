# Arhitectura pe Componente - Entități și Sistemul de Luptă (PvZ Style)

> **NOTĂ IMPORTANTĂ:** Aceasta este o listă arhitecturală de referință. **Nu toate componentele descrise mai jos vor fi implementate obligatoriu în versiunea finală a jocului.** Ele vor fi integrate iterativ, strict în funcție de necesitățile mecanice ale fiecărei faze de dezvoltare. Scopul acestui document este de a stabili un standard de proiectare (Composition over Inheritance).

Aceste componente sunt concepute pentru a decupla logica de baza unității (`BaseUnit`) și a State Machine-ului, fiind optimizate pentru un joc multiplayer pe benzi (Lane-based Tower Defense).

---

## 1. Componente de Combat și Vitalitate
Aceste componente gestionează ciclul de viață al unităților și interacțiunile de damage.

### `HealthComponent` (Extinde `Node`)
* **Rol:** Container pur de date pentru vitalitate și gestionarea morții. Se sincronizează prin `MultiplayerSynchronizer`.
* **Proprietăți:** `MaxHealth`, `CurrentHealth`.
* **Metode:** `TakeDamage(int amount)`, `Heal(int amount)`.
* **Semnale:** `HealthChanged(newHp, maxHp)`, `Died()`.

### `HurtboxComponent` (Extinde `Area2D`)
* **Rol:** Zona fizică vulnerabilă a entității (receptorul de damage).
* **Dependențe:** Necesită o referință către nodul frate `HealthComponent` (via `[Export]`).
* **Setări Coliziune:** Plasat pe layere specifice taberei (ex: `Defender_Hurtbox` sau `Attacker_Hurtbox`).
* **Metode:** `ReceiveHit(int damage)` -> apelează intern `HealthComponent.TakeDamage(damage)`.

### `HitboxComponent` (Extinde `Area2D`)
* **Rol:** Zona fizică de atac (ex: o armă, zona de impact a unui proiectil sau lovitura unui inamic).
* **Proprietăți:** `DamageAmount`.
* **Setări Coliziune:** Masca (Mask) trebuie să corespundă cu layer-ul Hurtbox-ului inamic.
* **Comportament:** Când se suprapune cu un `HurtboxComponent`, apelează metoda `hurtbox.ReceiveHit(DamageAmount)`. Este activat/dezactivat dinamic de către `AttackState` doar pe frame-urile corecte ale animației.

---

## 2. Componente de Percepție și Aliniere
Aceste componente gestionează identificarea țintelor, optimizate pentru mișcarea exclusiv orizontală.

### `TeamComponent` (Extinde `Node`)
* **Rol:** Identifică facțiunea entității.
* **Proprietăți:** `Faction` (Enum: `Defender`, `Attacker`, `Neutral`).
* **Utilizare:** Ajută Hitbox-urile și Raycast-urile să valideze instantaneu dacă o entitate lovită este inamic sau aliat.

### `LinearVisionComponent` (Extinde `RayCast2D`)
* **Rol:** Înlocuiește sistemele complexe de pathfinding (NavigationAgent2D). Scanează axa X pentru ținte valide.
* **Proprietăți:** `Range` (Lungimea razei), `Direction` (`Vector2.Left` sau `Vector2.Right`).
* **Metode:** `GetFirstValidEnemy()` -> Filtrează unitățile moarte sau aliate și returnează primul `HurtboxComponent` (sau `BaseUnit`) valid lovit.

### `LaneComponent` (Extinde `Node`)
* **Rol:** Stochează poziția logică pe grid pe Server.
* **Proprietăți:** `RowIndex` (0 până la 4), `ColIndex` (folosit doar pentru `DefenderUnit`).
* **Utilizare:** Filtrare extrem de rapidă cu complexitate `O(N)`. Permite proiectilelor sau abilităților să verifice coliziunile strict împotriva entităților care împart același `RowIndex`.

---

## 3. Componente de Mișcare și Economie
Gestionează acțiunile autonome deconectate de vizual.

### `LinearMovementComponent` (Extinde `Node`)
* **Rol:** Încapsulează matematica deplasării stricte pe axă.
* **Proprietăți:** `MovementSpeed`, `IsMoving`.
* **Metode:** `MoveLeft(double delta)`, `Stop()`.
* **Utilizare:** Apelat de `WalkState.Update()`. Manipulează direct viteza sau poziția nodului părinte de tip `CharacterBody2D`.

### `ResourceGeneratorComponent` (Extinde `Node`) 
* **Rol:** Generează venit pasiv (specific clădirilor de economie, ex: Ferme).
* **Proprietăți:** `GenerationInterval`, `Amount`.
* **Comportament:** Rulează un `Timer` intern (strict Server-side). Trimite un semnal sau execută un RPC către `EconomyManager` pentru a adăuga resurse jucătorului (Defender).

---

## 4. Exemplu de Asamblare Arhitecturală (Scene Tree)

Modul corect de ierarhizare a nodurilor în editorul Godot pentru o unitate derivată (ex: `AttackerUnit.tscn`):

```text
WolfUnit (CharacterBody2D + Script: WolfUnit.cs moștenește BaseUnit)
│
├── Visuals (Node2D)
│   ├── AnimatedSprite2D
│   └── AnimationPlayer
│
├── Components (Node)
│   ├── HealthComponent (Node)
│   ├── TeamComponent (Node) -> Faction = Attacker
│   ├── LaneComponent (Node)
│   ├── LinearMovementComponent (Node)
│   │
│   ├── HurtboxComponent (Area2D) -> Layer: Attacker_Hurtbox
│   │   └── CollisionShape2D
│   │
│   ├── HitboxComponent (Area2D) -> Mask: Defender_Hurtbox
│   │   └── CollisionShape2D (Disabilitat implicit)
│   │
│   └── LinearVisionComponent (RayCast2D) -> Direcție: Vector2.Left
│
├── StateMachine (Node + Script: GenericStateMachine.cs)
│
└── MultiplayerSynchronizer (Node de rețea)
    └── Sincronizează: Transform.X și HealthComponent:CurrentHealth
```

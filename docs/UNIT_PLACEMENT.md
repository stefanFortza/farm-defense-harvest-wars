# WORKFLOW: Unit Placement System

## Faza 1: Interacțiunea UI & "Ghosting" (Client Side Only)
*Această fază este pur vizuală. Serverul NU știe că tu plimbi mouse-ul.*

1.  **Selection:** Jucătorul dă click pe Cardul "Vaca" din UI.
    * Variabila locală `_selectedUnitType` devine `UnitType.Cow`.
    * Instanțiem un obiect temporar `GhostUnit` (sprite-ul vacii cu `Modulate.a = 0.5` - transparent).
2.  **Hovering (în `_Process`):**
    * Calculăm poziția grid-ului sub mouse: `Vector2I gridPos = tileMap.LocalToMap(mousePos)`.
    * Facem "Snap" vizual: `GhostUnit.Position = tileMap.MapToLocal(gridPos)`.
3.  **Validation Visual:**
    * Verificăm local (Client Prediction) dacă `gridPos` este valid (e.g., e pe banda mea? am bani?).
    * Dacă DA: Pătratul de sub "Ghost" se face VERDE.
    * Dacă NU: Pătratul se face ROȘU.

---

## Faza 2: Cererea de Construcție (Client -> Server)
*Momentul adevărului. Jucătorul confirmă acțiunea.*

1.  **Input:** Jucătorul apasă `Click Stânga`.
2.  **Request:** Clientul trimite un RPC către Server (Host).
    ```csharp
    // GameplayController.cs (Client)
    RpcId(1, nameof(RequestSpawnUnit), _selectedUnitType, currentGridPos);
    ```
3.  **Local State:** Clientul NU scade banii încă! Așteaptă confirmarea. "Ghost"-ul rămâne activ sau se ascunde temporar.

---

## Faza 3: Autoritatea și Validarea (Server Side)
*Serverul primește cererea și judecă.*

1.  **Validation:** În metoda `RequestSpawnUnit`:
    * Verifică distanța: Este serverul de acord că `currentGridPos` aparține jucătorului care a cerut?
    * Verifică resursele: `if (Players[senderId].Money >= UnitCost)`.
    * Verifică coliziunea logică: `if (ServerGrid[x,y] == null)`.
2.  **Transaction:**
    * Dacă valid: `Players[senderId].Money -= UnitCost`.
    * Updatează grid-ul logic: `ServerGrid[x,y] = newUnitData`.
3.  **Broadcast:** Serverul trimite un RPC către **TOȚI** clienții (inclusiv cel care a cerut) pentru a spawna unitatea reală.
    ```csharp
    Rpc(nameof(SpawnUnitOnClient), uniqueNetId, _selectedUnitType, gridPos, ownerId);
    ```

---

## Faza 4: Replicarea și Feedback-ul (Toți Clienții)
*Toată lumea vede rezultatul.*

1.  **Spawn:** Clienții primesc `SpawnUnitOnClient`.
    * Instanțiază scena `CowUnit.tscn`.
    * O pun la coordonata `MapToLocal(gridPos)`.
2.  **Initialization:**
    * Setează `AnimationPlayer` pe "Idle".
    * Dacă ești proprietarul, bara de UI cu bani se actualizează (via `MultiplayerSynchronizer` sau RPC separat de update bani).
3.  **Cleanup:** Clientul care a plasat șterge obiectul "Ghost".
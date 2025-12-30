# 🗺️ Farm Defense: Map Layout (Final PvZ Style)

Configurație pentru gameplay tip "Static Turrets" (Vacile nu se mișcă, doar trag).

## 1. Setări Display
* **Rezoluție:** `320 x 180` px
* **Tile Size:** `16 x 16` px
* **Grid:** `20` coloane x `11` rânduri

## 2. Structura Hărții (Vizual)

### A. Orizontală (Coloane X: 0 - 19)
| Col (X) | Tip | Detalii |
| :--- | :--- | :--- |
| **0 - 1** | **Margine Stânga** | Decor, Câmpie goală. |
| **2 - 4** | **BAZA (Hambar)** | Hambarul mare (3x3). Aceasta e "Inima" fermei. |
| **5 - 16** | **GRID ACTIV** | **ZONA DE JOC** (12 Tile-uri lungime). Aici pui vacile. |
| **17 - 19** | **SPAWN INAMICI** | Pădurea întunecată. |

### B. Verticală (Rânduri Y: 0 - 10)
| Rând (Y) | Tip | Detalii |
| :--- | :--- | :--- |
| **0 - 1** | **Margine Sus** | Cer, Nori. (Loc pentru UI Scor/Resurse). |
| **2** | **Gard Sus** | `Fence Big`. Delimitează arena. |
| **3 - 7** | **BENZI DE JOC** | **5 BENZI** de Iarbă simplă. |
| **8** | **Gard Jos** | `Fence Big`. Delimitează arena. |
| **9 - 10** | **Margine Jos** | Iarbă, Flori. (Loc pentru UI Shop). |

## 3. Diagrama

```text
       0 1   2 3 4     5 6 . . . . . . . . 16   17 18 19
     +-----+-------+-------------------------+----------+
 0-1 |   MARGINE   |       MARGINE SUS       |  MARGINE |
     +-----+-------+-------------------------+----------+
  2  |     | Gard  |       Gard Sus          |          |
     +-----+-------+-------------------------+----------+
  3  |     |       | [ ] [ ] [ ] [ ] [ ] [ ] |          |
  4  |     | HAM-  | [ ] [ ] [ ] [ ] [ ] [ ] |  PĂDURE  |
  5  |     | BAR   | [ ] [ ] [ ] [ ] [ ] [ ] |  (WOLVES)|
  6  |     |       | [ ] [ ] [ ] [ ] [ ] [ ] |          |
  7  |     |       | [ ] [ ] [ ] [ ] [ ] [ ] |          |
     +-----+-------+-------------------------+----------+
  8  |     | Gard  |       Gard Jos          |          |
     +-----+-------+-------------------------+----------+
 9-10|   MARGINE   |       MARGINE JOS       |  MARGINE |
     +-----+-------+-------------------------+----------+
# Strategii de Testare pentru Licență

Aceste notițe descriu abordarea optimă pentru capitolul de testare, echilibrând rigoarea academică cu efortul de implementare.

## 1. Strategia Generală
La FMI, este mai valoros să ai câteva teste reale și relevante decât o suită mare de teste "de formă".
- **Backend (ASP.NET):** Teste unitare automate (xUnit, Moq).
- **Client (Godot):** Testare manuală/funcțională documentată prin tabele de cazuri de testare (Test Cases).

---

## 2. Testarea Unitară pe Backend (xUnit)
Se recomandă testarea logicii pure, cum ar fi `MatchmakingService`.

### Exemplu de structură (AAA - Arrange, Act, Assert):
```csharp
[Fact]
public void AddPlayerToQueue_ShouldIncreaseQueueSize()
{
    // Arrange
    var matchmakingService = new MatchmakingService();
    var playerTicket = new MatchTicket { UserId = "user123", Role = "Attacker" };

    // Act
    matchmakingService.EnqueuePlayer(playerTicket);

    // Assert
    int queueSize = matchmakingService.GetAttackerQueueSize();
    Assert.Equal(1, queueSize);
}
```

### Mocking (Moq)
Utilizarea librăriei **Moq** pentru a simula baza de date (Entity Framework) demonstrează înțelegerea izolării sistemelor în testare.

---

## 3. Testarea Funcțională (Godot)
Pentru motorul grafic, abordarea standard este documentarea testării manuale prin tabele de *Test Cases*.

### Structură recomandată pentru tabel în LaTeX:
| Test ID | Componentă | Precondiții | Acțiune | Rezultat Așteptat | Rezultat Obținut |
| :--- | :--- | :--- | :--- | :--- | :--- |
| TC-01 | State Machine | Inamic în raza de acțiune | Rulare gameplay | Trecere în AttackState, scădere viață | Succes |

---

## 4. Recomandări Prezentare Comisie
1. **Capturi de ecran:** Include imagini cu testele trecute (cu verde) din Visual Studio / Rider.
2. **Relevanță:** Testează doar logica critică (matchmaking, validări securitate, calcul daune).
3. **Sinceritate:** Nu inventa teste false; un singur fișier de teste reale este suficient pentru a demonstra competența inginerescă.

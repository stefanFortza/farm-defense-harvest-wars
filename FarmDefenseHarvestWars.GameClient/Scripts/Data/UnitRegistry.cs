using Godot;
using Godot.Collections; // Pentru Array-ul exportat in editor
using System.Collections.Generic; // Pentru Dictionary-ul intern C#
using FarmDefenseHarvestWars.Shared.Enums;

namespace FarmDefenseHarvestWars.GameClient.Scripts.Data;

[GlobalClass]
public partial class UnitRegistry : Resource
{
    // Asta vezi în Editor și aici tragi fișierele .tres
    [Export] public Array<UnitData> AllUnits { get; set; } = [];

    // Cache intern pentru acces O(1) la runtime
    private System.Collections.Generic.Dictionary<UnitType, UnitData> _lookupTable = null!;

    // Metoda de inițializare a dicționarului (o apelăm o singură dată la startul jocului)
    public void InitializeLookup()
    {
        _lookupTable = [];

        foreach (var unit in AllUnits)
        {
            if (unit == null) continue;

            if (_lookupTable.ContainsKey(unit.Type))
            {
                GD.PrintErr($"[UnitRegistry] DUPLICATE UNIT TYPE DETECTED: {unit.Type}. Verifică lista din editor!");
                continue;
            }

            _lookupTable.Add(unit.Type, unit);
        }

        GD.Print($"[UnitRegistry] Indexat {_lookupTable.Count} unități.");
    }

    // Metoda rapidă de acces
    public UnitData GetUnitData(UnitType type)
    {
        // Lazy initialization (siguranță în caz că am uitat să apelăm Initialize)
        if (_lookupTable == null) InitializeLookup();

        if (_lookupTable.TryGetValue(type, out var data))
        {
            return data;
        }

        GD.PrintErr($"[UnitRegistry] CRITICAL: Unit Type '{type}' nu a fost găsit în registry!");
        return new UnitData();
    }
}
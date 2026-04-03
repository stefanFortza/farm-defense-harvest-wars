using Godot;
using Godot.Collections; // Pentru Array-ul exportat in editor
using System.Collections.Generic; // Pentru Dictionary-ul intern C#
using System.IO;
using System.Text.Json;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;

namespace FarmDefenseHarvestWars.GameClient.Scripts.Data;

[GlobalClass]
[Tool]
public partial class UnitRegistry : Resource
{
    // Asta vezi în Editor și aici tragi fișierele .tres
    [Export] public Array<UnitData> AllUnits { get; set; } = [];

    [ExportGroup("Sync")]
    [Export]
    public bool ManualExport
    {
        get => false;
        set
        {
            if (value) ExportAllToBackend();
        }
    }

    // Cache intern pentru acces O(1) la runtime
    private System.Collections.Generic.Dictionary<UnitType, UnitData> _lookupTable = null!;

    public UnitRegistryDto ToDto()
    {
        var dto = new UnitRegistryDto();
        foreach (var unit in AllUnits)
        {
            if (unit != null)
                dto.Units.Add(unit.ToDto());
        }
        return dto;
    }

    public void ExportAllToBackend()
    {
        try
        {
            var dto = ToDto();
            string jsonString = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });

            string absolutePath = ProjectSettings.GlobalizePath("res://../FarmDefenseHarvestWars.Backend/Data/Units/UnitRegistry.json");
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            File.WriteAllText(absolutePath, jsonString);

            GD.PrintRich("[color=cyan][Sync][/color] UnitRegistry exportat cu succes în Backend.");
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"[Export Error] {e.Message}");
        }
    }

    // Metoda de inițializare a dicționarului (o apelăm o singură dată la startul jocului)
    public void InitializeLookup()
    {
        if (_lookupTable != null)
        {
            // GD.Print("[UnitRegistry] Lookup deja inițializat, sărim peste re-initializare.");
            return;
        }

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

        // GD.Print($"[UnitRegistry] Indexat {_lookupTable.Count} unități.");
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

    public bool TryGetUnitData(UnitType type, out UnitData? data)
    {
        if (_lookupTable == null) InitializeLookup();

        if (_lookupTable.TryGetValue(type, out var found))
        {
            data = found;
            return true;
        }

        data = null;
        return false;
    }

    public bool IsRoleCompatible(UnitType type, PlayerRole role)
    {
        if (!TryGetUnitData(type, out var unitData) || unitData == null)
        {
            GD.PrintErr($"[UnitRegistry] Unit '{type}' missing from registry. Treating as incompatible for role '{role}'.");
            return false;
        }

        return unitData.Role == role;
    }
}
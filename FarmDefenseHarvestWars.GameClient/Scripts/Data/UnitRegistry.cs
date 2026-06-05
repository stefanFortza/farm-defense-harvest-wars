using Godot;
using Godot.Collections;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;

namespace FarmDefenseHarvestWars.GameClient.Scripts.Data;

[GlobalClass]
// [Tool] - ELIMINAT. Acesta bloca descărcarea asamblărilor deoarece Registry-ul este mereu deschis în fundal.
public partial class UnitRegistry : Resource
{
    [Export] public Array<UnitData> AllUnits { get; set; } = [];
    [Export] public Array<PackedScene> Projectiles { get; set; } = [];
    [Export] public Array<Texture2D> Avatars { get; set; } = [];

    // Cache intern (Runtime only)
    private System.Collections.Generic.Dictionary<UnitType, UnitData>? _lookupTable;

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

    // Metoda de inițializare (O(1) acces)
    public void InitializeLookup()
    {
        if (_lookupTable != null) return;

        _lookupTable = new System.Collections.Generic.Dictionary<UnitType, UnitData>();

        foreach (var unit in AllUnits)
        {
            if (unit == null) continue;

            if (_lookupTable.ContainsKey(unit.Type))
            {
                GD.PrintErr($"[UnitRegistry] DUPLICATE UNIT TYPE: {unit.Type}.");
                continue;
            }

            _lookupTable.Add(unit.Type, unit);
        }
    }

    public UnitData GetUnitData(UnitType type)
    {
        if (_lookupTable == null) InitializeLookup();

        if (_lookupTable!.TryGetValue(type, out var data))
        {
            return data;
        }

        GD.PrintErr($"[UnitRegistry] CRITICAL: Unit Type '{type}' not found!");
        return new UnitData();
    }

    public bool TryGetUnitData(UnitType type, out UnitData? data)
    {
        if (_lookupTable == null) InitializeLookup();
        return _lookupTable!.TryGetValue(type, out data);
    }

    public bool IsRoleCompatible(UnitType type, PlayerRole role)
    {
        if (!TryGetUnitData(type, out var unitData) || unitData == null) return false;
        return unitData.Role == role;
    }

    // Metoda de export o păstrăm, dar o vom apela dintr-un EditorScript extern 
    // sau dintr-un buton de tip "Quick Play" pentru a evita modul [Tool] permanent.
    public void ExportAllToBackend()
    {
        // Mută logica de JsonSerializer aici doar la apel, 
        // sau folosește-o într-un script separat de tip EditorScript.
        try
        {
            var dto = ToDto();
            string jsonString = System.Text.Json.JsonSerializer.Serialize(dto, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            string absolutePath = ProjectSettings.GlobalizePath("res://../FarmDefenseHarvestWars.Backend/Data/Units/UnitRegistry.json");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(absolutePath)!);
            System.IO.File.WriteAllText(absolutePath, jsonString);
            GD.PrintRich("[color=cyan][Sync][/color] UnitRegistry exportat.");
        }
        catch (System.Exception e) { GD.PrintErr(e.Message); }
    }
}
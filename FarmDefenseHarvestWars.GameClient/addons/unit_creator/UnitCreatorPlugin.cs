#if TOOLS
using Godot;
using System;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;

[Tool]
public partial class UnitCreatorPlugin : EditorPlugin
{
    private const string DefenderBaseScene = "res://Entities/Units/Base/DefenderUnit/DefenderUnit.tscn";
    private const string AttackerBaseScene = "res://Entities/Units/Base/AttackerUnit/AttackerUnit.tscn";
    private const string UnitStatsPath = "res://Resources/Units/UnitStats/";
    private const string DefenderFolderPath = "res://Entities/Units/Defenders/";
    private const string EnemyFolderPath = "res://Entities/Units/Enemies/";
    private const string UnitRegistryPath = "res://Resources/Units/UnitRegistry.tres";

    private ConfirmationDialog? _dialog;
    private LineEdit? _nameEdit;
    private OptionButton? _roleOption;
    private SpinBox? _typeEdit;

    public override void _EnterTree()
    {
        AddToolMenuItem("Create New Unit", Callable.From(ShowDialog));
    }

    public override void _ExitTree()
    {
        RemoveToolMenuItem("Create New Unit");
        _dialog?.QueueFree();
    }

    private void ShowDialog()
    {
        if (_dialog == null)
        {
            _dialog = new ConfirmationDialog();
            _dialog.Title = "Create New Unit";
            _dialog.Size = new Vector2I(350, 250);

            var vBox = new VBoxContainer();
            _dialog.AddChild(vBox);

            vBox.AddChild(new Label { Text = "Unit Name (e.g. Spider):" });
            _nameEdit = new LineEdit { Text = "NewUnit" };
            vBox.AddChild(_nameEdit);

            vBox.AddChild(new Label { Text = "Role:" });
            _roleOption = new OptionButton();
            _roleOption.AddItem("Defender");
            _roleOption.AddItem("Attacker");
            vBox.AddChild(_roleOption);

            vBox.AddChild(new Label { Text = "Unit Type Value (ID):" });
            _typeEdit = new SpinBox { MinValue = 0, MaxValue = 1000, Value = 0 };
            vBox.AddChild(_typeEdit);

            _dialog.Confirmed += OnConfirmed;
            GetEditorInterface().GetBaseControl().AddChild(_dialog);
        }

        _dialog.PopupCentered();
    }

    private void OnConfirmed()
    {
        if (_nameEdit == null || _roleOption == null || _typeEdit == null) return;

        string unitName = _nameEdit.Text.Trim();
        bool isDefender = _roleOption.Selected == 0;
        int typeValue = (int)_typeEdit.Value;

        if (string.IsNullOrEmpty(unitName))
        {
            GD.PrintErr("Unit name cannot be empty!");
            return;
        }

        string baseScenePath = isDefender ? DefenderBaseScene : AttackerBaseScene;
        string folderRoot = isDefender ? DefenderFolderPath : EnemyFolderPath;
        string targetFolderName = $"{unitName}Unit";
        string targetFolderPath = $"{folderRoot}{targetFolderName}/";
        string sceneName = $"{targetFolderName}.tscn";
        string scenePath = $"{targetFolderPath}{sceneName}";
        string dataPath = $"{UnitStatsPath}{unitName}Data.tres";

        // 1. Create Folder
        var dir = DirAccess.Open("res://");
        if (!dir.DirExists(targetFolderPath))
        {
            dir.MakeDirRecursive(targetFolderPath);
            GD.Print($"Created folder: {targetFolderPath}");
        }

        // 2. Create Scene (Inherited)
        if (!FileAccess.FileExists(scenePath))
        {
            var sceneCreateError = CreateInheritedSceneFile(scenePath, baseScenePath, targetFolderName);
            if (sceneCreateError == Error.Ok)
            {
                GD.Print($"Created inherited scene: {scenePath}");
            }
            else
            {
                GD.PrintErr($"Failed to create inherited scene {scenePath}. Error: {sceneCreateError}");
            }
        }

        // 3. Create UnitData Resource
        var unitData = new UnitData();
        unitData.Name = unitName;
        unitData.Role = isDefender ? PlayerRole.Defender : PlayerRole.Attacker;
        unitData.Type = (UnitType)typeValue;
        unitData.UnitScenePath = scenePath;

        // Default Stats
        unitData.MatchCost = 50;
        unitData.MaxHealth = 100;
        unitData.Damage = 10;
        unitData.AttackRange = 64f;
        unitData.OptimalRange = 56f;
        unitData.MeleeRange = 24f;
        unitData.AttackSpeed = 1.0f;
        unitData.Speed = isDefender ? 0f : 50f;

        ResourceSaver.Save(unitData, dataPath);
        GD.Print($"Created UnitData: {dataPath}");

        // 4. Update UnitRegistry
        UpdateUnitRegistry(unitData);

        // Refresh FileSystem
        GetEditorInterface().GetResourceFilesystem().Scan();
    }

    private void UpdateUnitRegistry(UnitData newUnitData)
    {
        if (!FileAccess.FileExists(UnitRegistryPath))
        {
            GD.PrintErr($"UnitRegistry not found at {UnitRegistryPath}");
            return;
        }

        var registry = GD.Load<UnitRegistry>(UnitRegistryPath);
        if (registry == null)
        {
            GD.PrintErr($"Failed to load UnitRegistry from {UnitRegistryPath}");
            return;
        }

        // Check if already exists
        bool exists = false;
        foreach (var unit in registry.AllUnits)
        {
            if (unit != null && unit.Name == newUnitData.Name && unit.Role == newUnitData.Role)
            {
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            registry.AllUnits.Add(newUnitData);
            ResourceSaver.Save(registry, UnitRegistryPath);
            GD.Print($"Updated UnitRegistry: Added {newUnitData.Name}");
        }
    }

    private static Error CreateInheritedSceneFile(string scenePath, string baseScenePath, string rootNodeName)
    {
        string sceneText = BuildInheritedSceneText(baseScenePath, rootNodeName);

        using var file = FileAccess.Open(scenePath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            return FileAccess.GetOpenError();
        }

        file.StoreString(sceneText);
        return Error.Ok;
    }

    private static string BuildInheritedSceneText(string baseScenePath, string rootNodeName)
    {
        string escapedRootName = rootNodeName.Replace("\"", "\\\"");

        return
            "[gd_scene load_steps=2 format=3]\n\n" +
            $"[ext_resource type=\"PackedScene\" path=\"{baseScenePath}\" id=\"1_base\"]\n\n" +
            $"[node name=\"{escapedRootName}\" instance=ExtResource(\"1_base\")]\n";
    }
}
#endif

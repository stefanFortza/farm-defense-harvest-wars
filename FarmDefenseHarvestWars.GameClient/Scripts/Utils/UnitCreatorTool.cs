using Godot;
using System;
using System.IO;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;

namespace FarmDefenseHarvestWars.GameClient.Scripts.Utils;

[Tool]
public partial class UnitCreatorTool : EditorScript
{
    private const string DefenderBaseScene = "res://Entities/Units/Base/DefenderUnit/DefenderUnit.tscn";
    private const string AttackerBaseScene = "res://Entities/Units/Base/AttackerUnit/AttackerUnit.tscn";
    private const string UnitStatsPath = "res://Resources/Units/UnitStats/";
    private const string DefenderFolderPath = "res://Entities/Units/Defenders/";
    private const string EnemyFolderPath = "res://Entities/Units/Enemies/";
    private const string UnitRegistryPath = "res://Resources/Units/UnitRegistry.tres";

    public override void _Run()
    {
        ShowDialog();
    }

    private void ShowDialog()
    {
        var dialog = new ConfirmationDialog();
        dialog.Title = "Create New Unit (Tool)";
        dialog.Size = new Vector2I(350, 250);

        var vBox = new VBoxContainer();
        dialog.AddChild(vBox);

        vBox.AddChild(new Label { Text = "Unit Name (e.g. Spider):" });
        var nameEdit = new LineEdit { Text = "NewUnit" };
        vBox.AddChild(nameEdit);

        vBox.AddChild(new Label { Text = "Role:" });
        var roleOption = new OptionButton();
        roleOption.AddItem("Defender");
        roleOption.AddItem("Attacker");
        vBox.AddChild(roleOption);

        vBox.AddChild(new Label { Text = "Unit Type Value (ID):" });
        var typeEdit = new SpinBox { MinValue = 0, MaxValue = 1000, Value = 0 };
        vBox.AddChild(typeEdit);

        dialog.Confirmed += () =>
        {
            OnConfirmed(nameEdit.Text.Trim(), roleOption.Selected == 0, (int)typeEdit.Value);
            dialog.QueueFree();
        };

        dialog.Canceled += () => dialog.QueueFree();

        var editorInterface = GetEditorInterface();
        editorInterface.GetBaseControl().AddChild(dialog);
        dialog.PopupCentered();
    }

    private void OnConfirmed(string unitName, bool isDefender, int typeValue)
    {
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

        // 2. Create Inherited Scene
        if (!Godot.FileAccess.FileExists(scenePath))
        {
            var baseScene = GD.Load<PackedScene>(baseScenePath);
            if (baseScene != null)
            {
                var instance = baseScene.Instantiate();
                instance.Name = targetFolderName;
                var packed = new PackedScene();
                var error = packed.Pack(instance);
                if (error == Error.Ok)
                {
                    ResourceSaver.Save(packed, scenePath);
                    GD.Print($"Created scene: {scenePath}");
                }
                instance.Free();
            }
        }

        // 3. Create UnitData Resource
        var unitData = new UnitData();
        unitData.Name = unitName;
        unitData.Role = isDefender ? PlayerRole.Defender : PlayerRole.Attacker;
        unitData.Type = (UnitType)typeValue;
        unitData.UnitScenePath = scenePath;

        unitData.MatchCost = 50;
        unitData.MaxHealth = 100;
        unitData.Damage = 10;
        unitData.AttackRange = 64f;
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
        if (!Godot.FileAccess.FileExists(UnitRegistryPath)) return;
        var registry = GD.Load<UnitRegistry>(UnitRegistryPath);
        if (registry == null) return;

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
}

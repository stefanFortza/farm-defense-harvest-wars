using Godot;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;

// [SceneTree] - Necessary for running via 'godot -s' from CLI.
public partial class SyncUnitsScript : SceneTree
{
    public override async void _Initialize()
    {
        GD.PrintRich("[color=yellow][Sync][/color] Starting Unit Registry sync to Backend...");

        // Small delay to ensure everything is initialized
        await ToSignal(CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);

        try
        {
            var registryPath = "res://Resources/Units/UnitRegistry.tres";
            if (!FileAccess.FileExists(registryPath))
            {
                GD.PrintErr($"[Sync] Error: Could not find UnitRegistry at {registryPath}");
                Quit(1);
                return;
            }

            var registry = ResourceLoader.Load<UnitRegistry>(registryPath);
            if (registry == null)
            {
                GD.PrintErr("[Sync] Error: Failed to load UnitRegistry resource.");
                Quit(1);
                return;
            }

            registry.ExportAllToBackend();
            GD.PrintRich("[color=green][Sync][/color] Unit Registry sync completed successfully!");
            Quit(0);
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"[Sync] CRITICAL ERROR: {e.Message}");
            Quit(1);
        }
    }
}

using Godot;
using System;

public partial class GridManager : Node2D
{
    [Export] public TileMapLayer GroundLayer { get; set; } = null!;

    public override void _Ready()
    {
        // Auto-wire if not set in editor
        if (GroundLayer == null)
        {
            GroundLayer = GetNodeOrNull<TileMapLayer>("Ground");
        }

        if (GroundLayer == null)
        {
            GD.PrintErr("GridManager: Ground TileMap not found!");
        }
    }

    // Convert World Position (Mouse) -> Grid Coordinates
    public Vector2I GetGridPosition(Vector2 globalPosition)
    {
        if (GroundLayer == null) return Vector2I.Zero;
        return GroundLayer.LocalToMap(GroundLayer.ToLocal(globalPosition));
    }

    // Convert Grid Coordinates -> World Position (Center of tile)
    public Vector2 GetWorldPosition(Vector2I gridPosition)
    {
        if (GroundLayer == null) return Vector2.Zero;
        return GroundLayer.ToGlobal(GroundLayer.MapToLocal(gridPosition));
    }

    // Check if a tile is valid for placement (Placeholder)
    public bool IsTileBuildable(Vector2I gridPosition)
    {
        // Logic: Check if tile exists and is not occupied
        // For now, return true if the cell is not empty
        return GroundLayer.GetCellSourceId(gridPosition) != -1;
    }
}

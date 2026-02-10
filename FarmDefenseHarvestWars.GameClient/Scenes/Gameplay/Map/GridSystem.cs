using FarmDefenseHarvestWars.GameClient.Entities.Units.Base;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.Map;

public partial class GridSystem : Node
{
	[Export] public TileMapLayer GroundLayer { get; set; } = null!;

	// Matricea LOGICĂ - Aici stă adevărul, nu în TileMap
	private BaseUnit?[,] _gridOccupancy = null!;
	private Vector2I _gridSize = new(20, 10);

	public override void _Ready()
	{
		_gridOccupancy = new BaseUnit[_gridSize.X, _gridSize.Y];
	}

	public bool IsCellOccupied(Vector2I pos)
	{
		if (!IsInsideBounds(pos)) return true; // Nu poți construi în afara hărții
		return _gridOccupancy[pos.X, pos.Y] != null;
	}

	public bool IsInsideBounds(Vector2I pos)
	{
		return pos.X >= 0 && pos.X < _gridSize.X && pos.Y >= 0 && pos.Y < _gridSize.Y;
	}

	public void RegisterUnit(Vector2I pos, BaseUnit unit)
	{
		_gridOccupancy[pos.X, pos.Y] = unit;
	}

	public void UnregisterUnit(Vector2I pos)
	{
		_gridOccupancy[pos.X, pos.Y] = null;
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

	public bool IsTileBuildable(Vector2I gridPosition)
	{
		// Logic: Check if tile exists and is not occupied
		// For now, return true if the cell is not empty
		return GroundLayer.GetCellSourceId(gridPosition) != -1;
	}
}

using Godot;
using System.Collections.Generic;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base;
using FarmDefenseHarvestWars.Shared.Enums; // Asumând că ai PlayerRole aici

namespace FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.Map;

public partial class GridSystem : Node
{
	[Export] private TileMapLayer PlacementGrid = null!;

	// 1. Definim zonele de plasare separat
	// Defender: de la coloana 6, rândul 3, pe o zonă de 14x5
	private readonly Rect2I _defenderPlacementArea = new Rect2I(6, 3, 10, 5);

	// Attacker: Ex - de la coloana 22, rândul 3, pe o zonă de 5x5 (modifică după designul hărții tale)
	private readonly Rect2I _attackerPlacementArea = new Rect2I(16, 3, 4, 5);

	// 2. Folosim un Dictionary în loc de Array 2D. 
	// Elimină complet erorile de IndexOutOfBounds și nu consumă memorie pe celulele goale.
	private readonly Dictionary<Vector2I, BaseUnit> _gridOccupancy = new();

	// 3. Validarea primește acum Rolul celui care plasează
	public bool IsValidPlacement(Vector2I cell, PlayerRole role)
	{
		bool isInsideFactionArea = role == PlayerRole.Defender
			? _defenderPlacementArea.HasPoint(cell)
			: _attackerPlacementArea.HasPoint(cell);

		return isInsideFactionArea && !IsCellOccupied(cell);
	}

	public bool IsCellOccupied(Vector2I pos)
	{
		return _gridOccupancy.ContainsKey(pos);
	}

	public void RegisterUnit(Vector2I pos, BaseUnit unit)
	{
		// Când folosim Dictionary, pur și simplu asociem cheia (coordonata) cu valoarea (unitatea)
		_gridOccupancy[pos] = unit;
	}

	public void UnregisterUnit(Vector2I pos)
	{
		if (_gridOccupancy.ContainsKey(pos))
		{
			_gridOccupancy.Remove(pos);
		}
	}

	// Funcțiile de conversie rămân neschimbate
	public Vector2I GetGridPosition(Vector2 globalPosition)
	{
		if (PlacementGrid == null) return Vector2I.Zero;
		return PlacementGrid.LocalToMap(PlacementGrid.ToLocal(globalPosition));
	}

	public Vector2 GetWorldPosition(Vector2I gridPosition)
	{
		if (PlacementGrid == null) return Vector2.Zero;
		return PlacementGrid.ToGlobal(PlacementGrid.MapToLocal(gridPosition));
	}

	public bool IsTileBuildable(Vector2I gridPosition)
	{
		if (PlacementGrid == null) return false;
		return PlacementGrid.GetCellSourceId(gridPosition) != -1;
	}

	public bool IsInsideBounds(Vector2I gridPosition)
	{
		if (PlacementGrid == null) return false;
		var mapSize = PlacementGrid.GetUsedRect().Size;
		return gridPosition.X >= 0 && gridPosition.Y >= 0 && gridPosition.X < mapSize.X && gridPosition.Y < mapSize.Y;
	}
}

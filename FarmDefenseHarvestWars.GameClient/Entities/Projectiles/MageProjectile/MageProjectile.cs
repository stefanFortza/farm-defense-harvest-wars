using Godot;

namespace FarmDefenseHarvestWars.GameClient.Entities.Projectiles;

public partial class MageProjectile : BaseProjectile
{
    protected override Color GetImpactColor()
    {
        return new Color(0.6f, 0.4f, 1.0f); // Magic purple
    }
}

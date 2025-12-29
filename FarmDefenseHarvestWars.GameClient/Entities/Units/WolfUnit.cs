using Godot;

public partial class WolfUnit : AttackerUnit
{
    public override string UnitName => "Wolf";

    public override void _Ready()
    {
        base._Ready();
        MaxHealth = 100;
        CurrentHealth = MaxHealth;
        Speed = 150.0f;
        Damage = 15;
    }
}

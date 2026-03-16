using FarmDefenseHarvestWars.GameClient.Core.Utils;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Environment;

public partial class FlickeringLight : PointLight2D
{
    [ExportCategory("Flicker Configuration")]
    [Export] public bool IsFlickering { get; set; } = true;

    [Export(PropertyHint.Range, "0.1, 20.0, 0.1")]
    public float Speed { get; set; } = 5.0f;

    [ExportGroup("Energy Amplitude")]
    [Export(PropertyHint.Range, "0.0, 5.0, 0.05")]
    public float EnergyIntensity { get; set; } = 0.4f;

    [ExportGroup("Spatial Amplitude")]
    [Export(PropertyHint.Range, "0.0, 50.0, 0.5")]
    public float MoveIntensity { get; set; } = 2.0f;

    private FastNoiseLite _noise;
    private float _time;
    private float _baseEnergy;
    private Vector2 _baseOffset;

    public override void _Ready()
    {
        // Salvăm starea inițială setată în editor
        _baseEnergy = Energy;
        _baseOffset = Offset;

        // Inițializăm zgomotul cu parametri optimi pentru foc
        _noise = new FastNoiseLite
        {
            Seed = (int)GD.Randi(),
            Frequency = 0.5f,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin
        };
    }

    public override void _Process(double delta)
    {
        if (!IsFlickering)
        {
            // Resetăm la starea de bază dacă pâlpâitul este oprit
            if (Energy != _baseEnergy) Energy = _baseEnergy;
            if (Offset != _baseOffset) Offset = _baseOffset;
            return;
        }

        _time += (float)delta * Speed;

        // Calculăm variația energiei
        Energy = _baseEnergy + (_noise.GetNoise2D(_time, 0) * EnergyIntensity);

        // Calculăm variația poziției texturii (Offset)
        float offX = _noise.GetNoise2D(_time, 100) * MoveIntensity;
        float offY = _noise.GetNoise2D(_time, 200) * MoveIntensity;
        Offset = _baseOffset + new Vector2(offX, offY);
    }
}

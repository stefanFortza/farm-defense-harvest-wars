using Godot;
using System.Collections.Generic;

namespace FarmDefenseHarvestWars.GameClient.Entities.Projectiles;

public partial class AoeExplosionEffect : Node2D
{
    public float Radius { get; set; } = 24f;
    public Color Color { get; set; } = Colors.White;

    private float _currentProgress = 0f;
    private readonly List<ShardData> _shards = new();

    private struct ShardData
    {
        public Vector2 Direction;
        public float Length;
        public float Width;
    }

    public override void _Ready()
    {
        // Generate randomized splatter shards
        int shardCount = (int)GD.RandRange(6, 10);
        for (int i = 0; i < shardCount; i++)
        {
            float angle = GD.Randf() * Mathf.Tau;
            _shards.Add(new ShardData
            {
                Direction = Vector2.FromAngle(angle),
                Length = (float)GD.RandRange(Radius * 0.5f, Radius * 1.2f),
                Width = (float)GD.RandRange(2f, 4f)
            });
        }

        Tween tween = CreateTween();
        
        // Progress from 0 to 1
        tween.TweenMethod(Callable.From<float>(v => { 
            _currentProgress = v; 
            QueueRedraw(); 
        }), 0f, 1f, 0.4f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
        
        // Fade out
        tween.Parallel().TweenProperty(this, "modulate:a", 0f, 0.4f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        
        tween.Finished += QueueFree;
    }

    public override void _Draw()
    {
        // 1. Draw a faint filled expanding circle (the core "blast")
        Color fillColor = Color;
        fillColor.A = 0.4f * (1.0f - _currentProgress);
        DrawCircle(Vector2.Zero, Radius * _currentProgress * 0.8f, fillColor);
        
        // 2. Draw an outlined ring that moves outward
        Color ringColor = Color;
        ringColor.A = 1.0f - _currentProgress;
        DrawArc(Vector2.Zero, Radius * _currentProgress, 0, Mathf.Tau, 32, ringColor, 1.0f, true);

        // 3. Draw the randomized "splatter" shards
        foreach (var shard in _shards)
        {
            Vector2 start = shard.Direction * (Radius * 0.3f * _currentProgress);
            Vector2 end = shard.Direction * (shard.Length * _currentProgress);
            
            Color shardColor = Color;
            shardColor.A = 1.0f - _currentProgress;
            
            DrawLine(start, end, shardColor, shard.Width * (1.0f - _currentProgress), true);
        }
    }
}

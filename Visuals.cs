using Microsoft.Xna.Framework;

namespace MyGame;

public class VisualEffect
{
    public Vector2 Position { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public Color Color { get; set; }
    public float Duration { get; set; } // remaining duration in seconds
}

public class HolyWaterPool
{
    public Vector2 Position { get; set; }
    public float Radius { get; set; }
    public float Damage { get; set; }
    public float Duration { get; set; } // remaining duration in seconds
    public float TickTimer { get; set; } = 0f; // damages quái every 0.25 seconds
}

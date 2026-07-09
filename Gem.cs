using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MyGame;

public class Gem
{
    public Vector2 Position { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public float XPValue { get; set; }
    public float MagnetSpeed { get; set; }
    public bool IsCollected { get; set; }

    public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

    public Gem(Vector2 position, float xpValue)
    {
        Position = position;
        // Standard gem size 12x12
        Width = 12;
        Height = 12;
        XPValue = xpValue;
        MagnetSpeed = 150f;
        IsCollected = false;
    }

    public void Update(GameTime gameTime, Vector2 playerCenter, float magnetRange)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float distance = Vector2.Distance(Position, playerCenter);

        // Magnet range: scales with player's AttractorbLevel
        if (distance < magnetRange)
        {
            Vector2 direction = playerCenter - Position;
            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                Position += direction * MagnetSpeed * deltaTime;
                
                // Increase magnet speed as it gets closer
                MagnetSpeed = MathHelper.Min(MagnetSpeed + 250f * deltaTime, 600f);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D xpGemTexture, Texture2D pixelTexture, GameTime gameTime)
    {
        // 1. Draw green/cyan pulsing glow behind the gem
        float time = (float)gameTime.TotalGameTime.TotalSeconds;
        float pulse = (float)Math.Sin(time * 7.0) * 3f;
        
        Rectangle haloRect = new Rectangle(
            (int)(Position.X - 3 - pulse / 2f), 
            (int)(Position.Y - 3 - pulse / 2f), 
            (int)(Width + 6 + pulse), 
            (int)(Height + 6 + pulse)
        );
        spriteBatch.Draw(pixelTexture, haloRect, Color.Lime * 0.22f);

        // 2. Draw sparkling green/cyan lens flare
        float alpha = 0.35f + (float)Math.Sin(time * 10.0) * 0.15f;
        if (alpha > 0f)
        {
            Color flareColor = Color.Cyan * alpha;
            int flareSize = 5 + (int)(pulse * 0.5f);
            
            Rectangle hFlare = new Rectangle((int)Position.X + Width / 2 - flareSize, (int)Position.Y + Height / 2 - 1, flareSize * 2, 2);
            Rectangle vFlare = new Rectangle((int)Position.X + Width / 2 - 1, (int)Position.Y + Height / 2 - flareSize, 2, flareSize * 2);
            
            spriteBatch.Draw(pixelTexture, hFlare, flareColor);
            spriteBatch.Draw(pixelTexture, vFlare, flareColor);
        }

        // 3. Draw the gem itself
        spriteBatch.Draw(xpGemTexture, Bounds, Color.White);
    }
}

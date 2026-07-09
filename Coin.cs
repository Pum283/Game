using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace MyGame;

public class Coin
{
    public Vector2 Position { get; set; }
    public int Width { get; set; } = 16;
    public int Height { get; set; } = 16;
    public bool IsCollected { get; set; } = false;
    public int Value { get; set; } = 1; // 1 coin by default, boss drops more!

    public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

    public Coin(Vector2 position, int value = 1)
    {
        Position = position;
        Value = value;
    }

    public void Update(GameTime gameTime, Vector2 playerCenter, float magnetRange)
    {
        float distance = Vector2.Distance(Position, playerCenter);

        if (distance < magnetRange)
        {
            // Magnet pull effect: move toward the player
            Vector2 dir = playerCenter - Position;
            if (dir != Vector2.Zero)
            {
                dir.Normalize();
                // Pull speed scales up as it gets closer
                float pullSpeed = MathHelper.Lerp(450f, 150f, distance / magnetRange);
                Position += dir * pullSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D coinTexture, Texture2D pixelTexture, GameTime gameTime)
    {
        // 1. Draw golden pulsing glow behind the coin
        float time = (float)gameTime.TotalGameTime.TotalSeconds;
        float pulse = (float)Math.Sin(time * 8.0) * 3f;
        
        Rectangle haloRect = new Rectangle(
            (int)(Position.X - 3 - pulse / 2f), 
            (int)(Position.Y - 3 - pulse / 2f), 
            (int)(Width + 6 + pulse), 
            (int)(Height + 6 + pulse)
        );
        spriteBatch.Draw(pixelTexture, haloRect, Color.Gold * 0.22f);

        // 2. Draw sparkling lens flare (cross hair lines) that expands and shrinks
        float alpha = 0.35f + (float)Math.Sin(time * 12.0) * 0.15f;
        if (alpha > 0f)
        {
            Color flareColor = Color.LightYellow * alpha;
            int flareSize = 6 + (int)(pulse * 0.5f);
            
            // Horizontal flare line
            Rectangle hFlare = new Rectangle((int)Position.X + Width / 2 - flareSize, (int)Position.Y + Height / 2 - 1, flareSize * 2, 2);
            // Vertical flare line
            Rectangle vFlare = new Rectangle((int)Position.X + Width / 2 - 1, (int)Position.Y + Height / 2 - flareSize, 2, flareSize * 2);
            
            spriteBatch.Draw(pixelTexture, hFlare, flareColor);
            spriteBatch.Draw(pixelTexture, vFlare, flareColor);
        }

        // 3. Draw the coin itself
        spriteBatch.Draw(coinTexture, Bounds, Color.White);
    }
}

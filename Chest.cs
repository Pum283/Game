using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MyGame;

public class Chest
{
    public Vector2 Position { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsCollected { get; set; }

    public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

    public Chest(Vector2 position)
    {
        Position = position;
        // BigTreasureChest.png aspect ratio is 32x14
        Width = 32;
        Height = 14;
        IsCollected = false;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D chestTexture, Texture2D pixelTexture, GameTime gameTime)
    {
        // 1. Draw large golden pulsing glow behind the chest
        float time = (float)gameTime.TotalGameTime.TotalSeconds;
        float pulse = (float)Math.Sin(time * 5.0) * 4f;
        
        Rectangle haloRect = new Rectangle(
            (int)(Position.X - 5 - pulse / 2f), 
            (int)(Position.Y - 5 - pulse / 2f), 
            (int)(Width + 10 + pulse), 
            (int)(Height + 10 + pulse)
        );
        spriteBatch.Draw(pixelTexture, haloRect, Color.Gold * 0.16f);

        // 2. Draw sparkling lens flare
        float alpha = 0.4f + (float)Math.Sin(time * 9.0) * 0.2f;
        if (alpha > 0f)
        {
            Color flareColor = Color.Yellow * alpha;
            int flareSize = 8 + (int)(pulse * 0.5f);
            
            Rectangle hFlare = new Rectangle((int)Position.X + Width / 2 - flareSize, (int)Position.Y + Height / 2 - 1, flareSize * 2, 2);
            Rectangle vFlare = new Rectangle((int)Position.X + Width / 2 - 1, (int)Position.Y + Height / 2 - flareSize, 2, flareSize * 2);
            
            spriteBatch.Draw(pixelTexture, hFlare, flareColor);
            spriteBatch.Draw(pixelTexture, vFlare, flareColor);
        }

        // 3. Draw chest itself
        spriteBatch.Draw(chestTexture, Bounds, Color.White);
    }
}

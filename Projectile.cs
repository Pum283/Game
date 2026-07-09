using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MyGame;

public class Projectile
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public float Damage { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsDestroyed { get; set; }
    
    // Weapon projectile flags
    public bool IsFireball { get; set; } = false;
    public bool IsAxe { get; set; } = false;
    public bool HasGravity { get; set; } = false;
    public bool IsEnemyProjectile { get; set; } = false;

    // NEW projectile flags
    public bool IsCross { get; set; } = false;
    public bool IsReturning { get; set; } = false;
    public bool IsBible { get; set; } = false;
    public float BibleAngle { get; set; } = 0f;
    public bool IsKnife { get; set; } = false;
    
    public int PenetrationCount { get; set; } = 1;
    public float MaxRange { get; set; } = 400f;   // Max travel distance before auto-destroy
    public float DistanceTraveled { get; private set; } = 0f;

    // Center point of projectile for rotation origin
    public Vector2 Center => new Vector2(Position.X + Width / 2f, Position.Y + Height / 2f);

    public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

    public Projectile(Vector2 startPosition, Vector2 targetPosition, float speed, float damage, bool isFireball = false, bool isAxe = false, bool hasGravity = false, int penetrationCount = 1, bool isEnemyProjectile = false)
    {
        Position = startPosition;
        Damage = damage;
        IsDestroyed = false;
        IsFireball = isFireball;
        IsAxe = isAxe;
        HasGravity = hasGravity;
        PenetrationCount = penetrationCount;
        IsEnemyProjectile = isEnemyProjectile;

        if (IsFireball)
        {
            Width = 24;
            Height = 24;
            MaxRange = 500f;
        }
        else if (IsAxe)
        {
            Width = 24;
            Height = 24;
            MaxRange = 350f;
        }
        else if (IsEnemyProjectile)
        {
            Width = 20;
            Height = 8;
            MaxRange = 600f;
        }
        else
        {
            Width = 16;
            Height = 16;
            MaxRange = 380f;
        }

        Vector2 direction = targetPosition - startPosition;
        if (direction != Vector2.Zero)
        {
            direction.Normalize();
        }
        else
        {
            direction = new Vector2(1, 0);
        }
        Velocity = direction * speed;
    }

    public void Update(GameTime gameTime, Vector2 playerCenter, Player player)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (IsBible)
        {
            // Bible orbits player. Speed scales with Bracer
            BibleAngle += (3.2f + player.BracerLevel * 0.15f) * deltaTime;
            float radius = 70f + player.SpellbinderLevel * 12f;
            Position = playerCenter - new Vector2(Width / 2f, Height / 2f) + new Vector2((float)Math.Cos(BibleAngle) * radius, (float)Math.Sin(BibleAngle) * radius);
            
            // Track elapsed time as distance (* 100)
            DistanceTraveled += 100f * deltaTime;
        }
        else if (IsCross)
        {
            if (!IsReturning && DistanceTraveled >= MaxRange * 0.5f)
            {
                IsReturning = true;
            }

            if (IsReturning)
            {
                Vector2 toPlayer = playerCenter - Center;
                if (toPlayer != Vector2.Zero)
                {
                    toPlayer.Normalize();
                    float returnSpeed = 380f * (1f + player.BracerLevel * 0.15f);
                    Velocity = toPlayer * returnSpeed;
                }
            }

            Vector2 step = Velocity * deltaTime;
            DistanceTraveled += step.Length();
            Position += step;
        }
        else
        {
            if (HasGravity)
            {
                Velocity = new Vector2(Velocity.X, Velocity.Y + 700f * deltaTime);
            }

            Vector2 step = Velocity * deltaTime;
            DistanceTraveled += step.Length();
            Position += step;
        }

        // Destroy when it exceeds its max range
        if (DistanceTraveled >= MaxRange)
        {
            IsDestroyed = true;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D texture, GameTime gameTime)
    {
        if (IsFireball)
        {
            int frame = (int)(gameTime.TotalGameTime.TotalSeconds * 12.0) % 4;
            Rectangle sourceRect = new Rectangle(frame * 16, 0, 16, 16);
            spriteBatch.Draw(texture, Bounds, sourceRect, Color.White);
        }
        else if (IsAxe)
        {
            float rotation = (float)(gameTime.TotalGameTime.TotalSeconds * 15.0);
            spriteBatch.Draw(texture, Center, null, Color.White, rotation, new Vector2(5.5f, 6.5f), new Vector2(Width / 11f, Height / 13f), SpriteEffects.None, 0f);
        }
        else if (IsEnemyProjectile)
        {
            float angle = (float)Math.Atan2(Velocity.Y, Velocity.X);
            spriteBatch.Draw(texture, Center, null, Color.White, angle, new Vector2(6.5f, 2.5f), new Vector2(Width / 13f, Height / 5f), SpriteEffects.None, 0f);
        }
        else if (IsBible)
        {
            float rotation = (float)(gameTime.TotalGameTime.TotalSeconds * 5.0);
            spriteBatch.Draw(texture, Center, null, Color.White, rotation, new Vector2(texture.Width / 2f, texture.Height / 2f), new Vector2(Width / (float)texture.Width, Height / (float)texture.Height), SpriteEffects.None, 0f);
        }
        else if (IsCross)
        {
            float rotation = (float)(gameTime.TotalGameTime.TotalSeconds * 22.0);
            spriteBatch.Draw(texture, Center, null, Color.White, rotation, new Vector2(texture.Width / 2f, texture.Height / 2f), new Vector2(Width / (float)texture.Width, Height / (float)texture.Height), SpriteEffects.None, 0f);
        }
        else if (IsKnife)
        {
            float angle = (float)Math.Atan2(Velocity.Y, Velocity.X);
            spriteBatch.Draw(texture, Center, null, Color.White, angle, new Vector2(texture.Width / 2f, texture.Height / 2f), new Vector2(Width / (float)texture.Width, Height / (float)texture.Height), SpriteEffects.None, 0f);
        }
        else
        {
            int frame = (int)(gameTime.TotalGameTime.TotalSeconds * 12.0) % 4;
            Rectangle sourceRect = new Rectangle(frame * 16, 0, 16, 16);
            spriteBatch.Draw(texture, Bounds, sourceRect, Color.White);
        }
    }
}

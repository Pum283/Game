using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MyGame;

public enum EnemyType
{
    Zombie,   // Slime sprite
    Bat,      // Flying bat sprite
    Skeleton, // Skeleton archer sprite
    Golem,    // Tanky stone golem (mid-tier elite)
    GolemBoss // Red Demon boss sprite
}

public class Enemy
{
    public Vector2 Position { get; set; }
    public float Speed { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public float HP { get; set; }
    public float MaxHP { get; set; }
    public float Damage { get; set; }
    public EnemyType Type { get; set; }
    
    public bool IsDead => HP <= 0;

    // Ranged attack timer for Skeleton
    private float _attackTimer = 0f;
    public bool ShouldShoot { get; set; } = false;

    // Animation variables
    private float _animTimer = 0f;
    private int _currentFrame = 0;
    private int _directionRow = 0; // 0 = Down, 1 = Up, 2 = Left, 3 = Right

    public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

    public Enemy(Vector2 startPosition, EnemyType type)
    {
        Position = startPosition;
        Type = type;
        _attackTimer = (float)new Random().NextDouble() * 1.5f;

        switch (Type)
        {
            case EnemyType.Zombie:
                Speed = 60f; // reduced from 80
                Width = 24;
                Height = 24;
                HP = 14f;    // reduced from 20
                Damage = 10f; // reduced from 15
                break;

            case EnemyType.Bat:
                Speed = 110f; // reduced from 150
                Width = 20;
                Height = 20;
                HP = 5f;     // reduced from 8
                Damage = 5f;  // reduced from 8
                break;

            case EnemyType.Skeleton:
                Speed = 50f; // reduced from 70
                Width = 32;
                Height = 32;
                HP = 17f;    // reduced from 25
                Damage = 7f;  // reduced from 10
                break;

            case EnemyType.Golem:
                Speed = 28f;
                Width = 40;
                Height = 40;
                HP = 55f;
                Damage = 16f;
                break;

            case EnemyType.GolemBoss:
                Speed = 35f; // reduced from 45
                Width = 64;
                Height = 64; // Massive Boss
                HP = 200f;   // reduced from 300
                Damage = 25f; // reduced from 40
                break;
        }
        MaxHP = HP;
    }

    public void Update(GameTime gameTime, Vector2 playerCenter, List<Obstacle> obstacles, byte[,] terrain)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector2 enemyCenter = new Vector2(Position.X + Width / 2f, Position.Y + Height / 2f);
        Vector2 direction = playerCenter - enemyCenter;
        float distance = direction.Length();

        if (direction != Vector2.Zero)
        {
            direction.Normalize();

            // Set animation row based on dominant movement axis
            if (Math.Abs(direction.Y) > Math.Abs(direction.X))
            {
                _directionRow = direction.Y > 0 ? 0 : 1; // 0 = Down, 1 = Up
            }
            else
            {
                _directionRow = direction.X < 0 ? 2 : 3; // 2 = Left, 3 = Right
            }

            // Advance walking frame
            _animTimer += deltaTime;
            if (_animTimer >= 0.15f)
            {
                _currentFrame = (_currentFrame + 1) % 4;
                _animTimer = 0f;
            }
        }

        if (Type == EnemyType.Skeleton)
        {
            // Skeleton ranged AI: maintain distance
            Vector2 nextPos = Position;

            if (distance > 240f)
            {
                nextPos.X += direction.X * Speed * deltaTime;
                Position = new Vector2(nextPos.X, Position.Y);
                if (CollidesWithAny(obstacles) || CollidesWithTerrain(terrain)) nextPos.X = Position.X - direction.X * Speed * deltaTime;

                nextPos.Y += direction.Y * Speed * deltaTime;
                Position = new Vector2(nextPos.X, nextPos.Y);
                if (CollidesWithAny(obstacles) || CollidesWithTerrain(terrain)) nextPos.Y = Position.Y - direction.Y * Speed * deltaTime;
            }
            else if (distance < 160f)
            {
                nextPos.X -= direction.X * Speed * deltaTime;
                Position = new Vector2(nextPos.X, Position.Y);
                if (CollidesWithAny(obstacles) || CollidesWithTerrain(terrain)) nextPos.X = Position.X + direction.X * Speed * deltaTime;

                nextPos.Y -= direction.Y * Speed * deltaTime;
                Position = new Vector2(nextPos.X, nextPos.Y);
                if (CollidesWithAny(obstacles) || CollidesWithTerrain(terrain)) nextPos.Y = Position.Y + direction.Y * Speed * deltaTime;
            }
            else
            {
                // Strafe
                Vector2 strafeDir = new Vector2(-direction.Y, direction.X);
                nextPos.X += strafeDir.X * (Speed * 0.5f) * deltaTime;
                Position = new Vector2(nextPos.X, Position.Y);
                if (CollidesWithAny(obstacles) || CollidesWithTerrain(terrain)) nextPos.X = Position.X - strafeDir.X * (Speed * 0.5f) * deltaTime;

                nextPos.Y += strafeDir.Y * (Speed * 0.5f) * deltaTime;
                Position = new Vector2(nextPos.X, nextPos.Y);
                if (CollidesWithAny(obstacles) || CollidesWithTerrain(terrain)) nextPos.Y = Position.Y - strafeDir.Y * (Speed * 0.5f) * deltaTime;
            }

            Position = nextPos;

            _attackTimer += deltaTime;
            if (_attackTimer >= 2.5f)
            {
                ShouldShoot = true;
                _attackTimer = 0f;
            }
        }
        else
        {
            // Direct chasing AI
            if (direction != Vector2.Zero)
            {
                Vector2 nextPos = Position;

                nextPos.X += direction.X * Speed * deltaTime;
                Position = new Vector2(nextPos.X, Position.Y);
                if (CollidesWithAny(obstacles) || CollidesWithTerrain(terrain)) nextPos.X = Position.X - direction.X * Speed * deltaTime;

                nextPos.Y += direction.Y * Speed * deltaTime;
                Position = new Vector2(nextPos.X, nextPos.Y);
                if (CollidesWithAny(obstacles) || CollidesWithTerrain(terrain)) nextPos.Y = Position.Y - direction.Y * Speed * deltaTime;

                Position = nextPos;
            }
        }
    }

    private bool CollidesWithAny(List<Obstacle> obstacles)
    {
        foreach (var obs in obstacles)
        {
            if (obs.IsDestroyed || !obs.BlocksMovement) continue;
            if (Bounds.Intersects(obs.Bounds))
                return true;
        }
        return false;
    }

    private bool CollidesWithTerrain(byte[,] terrain)
    {
        return !MapTerrain.IsWalkableAt(terrain, Position, Width, Height);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D enemyTexture, Texture2D pixelTexture)
    {
        // All enemies now use standard 16x16 frames in their walk sheets
        Rectangle sourceRect = new Rectangle(_currentFrame * 16, _directionRow * 16, 16, 16);
        spriteBatch.Draw(enemyTexture, Bounds, sourceRect, Color.White);

        // Draw HP bar for elite/boss enemies
        if ((Type == EnemyType.Golem || Type == EnemyType.GolemBoss) && HP > 0)
        {
            Rectangle bgBar = new Rectangle((int)Position.X, (int)Position.Y - 10, Width, 4);
            Rectangle fgBar = new Rectangle((int)Position.X, (int)Position.Y - 10, (int)(Width * (HP / MaxHP)), 4);
            
            spriteBatch.Draw(pixelTexture, bgBar, Color.Black * 0.5f);
            spriteBatch.Draw(pixelTexture, fgBar, Color.Red);
        }
    }
}

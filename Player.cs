using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MyGame;

public class Player
{
    public Vector2 Position { get; set; }
    public float Speed { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public float HP { get; set; }
    public float MaxHP { get; set; }
    public float DamageMultiplier { get; set; } = 1.0f;
    
    // XP and Leveling
    public int Level { get; set; }
    public float XP { get; set; }
    public float TargetXP { get; set; }

    // Weapon Levels (0 = Not Unlocked) - Unused but kept for backward compatibility if any
    public int MagicWandLevel { get; set; } = 1;
    public int GarlicAuraLevel { get; set; } = 0;
    public int FireballLevel { get; set; } = 0;

    // Passive Levels (In-Run)
    public int SpeedBootsLevel { get; set; } = 0;
    public int RegenRingLevel { get; set; } = 0;
    public int ShieldLevel { get; set; } = 0;
    public int SpinachLevel { get; set; } = 0;
    public int AttractorbLevel { get; set; } = 0;
    public int DuplicatorLevel { get; set; } = 0;
    
    // NEW Support Passives
    public int CloverLevel { get; set; } = 0;      // Luck +15% per level
    public int HollowHeartLevel { get; set; } = 0;  // Max HP +20% per level
    public int PummarolaLevel { get; set; } = 0;    // Regen +0.5 HP/s per level
    public int BracerLevel { get; set; } = 0;       // Proj Speed +15% per level
    public int SpellbinderLevel { get; set; } = 0;  // Proj Duration/Size +15% per level
    public int EmptyTomeLevel { get; set; } = 0;    // Cooldown reduction -8% per level

    // Permanent Stats from Lobby Shop (SaveData)
    public float PermanentArmor { get; set; } = 0f;
    public float PermanentMagnetBoost { get; set; } = 1.0f;
    public float PermanentGrowthBoost { get; set; } = 1.0f;
    public float PermanentGreedBoost { get; set; } = 1.0f;
    public float PermanentLuckBoost { get; set; } = 1.0f;
    public float PermanentCooldownReduction { get; set; } = 0f;

    // Run-specific consumable/boost state
    public int CurrentRevives { get; set; } = 0;
    public int CurrentPotions { get; set; } = 0;
    public bool GoldBoosterActive { get; set; } = false;

    // Stage mechanic modifiers (set each frame by StageMechanics)
    public float ExternalSpeedMultiplier { get; set; } = 1f;
    public Vector2 ExternalForce { get; set; }
    public float FreezeTimer { get; set; }
    public bool IcePhysics { get; set; }
    private Vector2 _iceVelocity;

    // Animation variables
    private float _animTimer = 0f;
    private int _currentFrame = 0;
    private int _directionRow = 0; // 0 = Down, 1 = Up, 2 = Left, 3 = Right

    // Center point of the player for spawning projectiles
    public Vector2 Center => new Vector2(Position.X + Width / 2f, Position.Y + Height / 2f);

    public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

    public Player(Vector2 startPosition, SaveData save)
    {
        Position = startPosition;
        
        Width = 32;
        Height = 32;

        // Apply permanent upgrades from Shop (save.json)
        MaxHP = 100f * (1f + save.MaxHPUpgradeLevel * 0.10f);
        HP = MaxHP;
        Speed = 190f * (1f + save.SpeedUpgradeLevel * 0.05f); // Base speed 190
        DamageMultiplier = 1.0f + save.DamageUpgradeLevel * 0.10f;
        
        PermanentArmor = save.ArmorUpgradeLevel; // reduces 1 damage flat per level
        PermanentMagnetBoost = 1.0f + save.MagnetUpgradeLevel * 0.15f;
        PermanentGrowthBoost = 1.0f + save.GrowthUpgradeLevel * 0.08f;
        PermanentGreedBoost = 1.0f + save.GreedUpgradeLevel * 0.10f;
        PermanentLuckBoost = 1.0f + save.LuckUpgradeLevel * 0.10f;
        PermanentCooldownReduction = save.CooldownUpgradeLevel * 0.04f;

        CurrentRevives = save.ReviveCount;
        CurrentPotions = save.StartingPotions;
        GoldBoosterActive = save.GoldBoosterActive;

        Level = 1;
        XP = 0f;
        TargetXP = 10f;
        
        MagicWandLevel = 1;
        GarlicAuraLevel = 0;
        FireballLevel = 0;
        SpeedBootsLevel = 0;
        RegenRingLevel = 0;
        ShieldLevel = 0;
        SpinachLevel = 0;
        AttractorbLevel = 0;
        DuplicatorLevel = 0;

        CloverLevel = 0;
        HollowHeartLevel = 0;
        PummarolaLevel = 0;
        BracerLevel = 0;
        SpellbinderLevel = 0;
        EmptyTomeLevel = 0;
    }

    public void Update(GameTime gameTime, int mapWidth, int mapHeight, List<Obstacle> obstacles, byte[,] terrainGrid = null)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var keyboardState = Keyboard.GetState();
        Vector2 direction = Vector2.Zero;

        if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
            direction.Y -= 1;
        if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
            direction.Y += 1;
        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
            direction.X -= 1;
        if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
            direction.X += 1;

        float actualSpeed = Speed * (1f + SpeedBootsLevel * 0.15f) * ExternalSpeedMultiplier;
        Vector2 moveDelta = Vector2.Zero;

        if (FreezeTimer <= 0f && direction != Vector2.Zero)
        {
            direction.Normalize();

            if (direction.Y > 0) _directionRow = 0;
            else if (direction.Y < 0) _directionRow = 1;
            else if (direction.X < 0) _directionRow = 2;
            else if (direction.X > 0) _directionRow = 3;

            _animTimer += deltaTime;
            if (_animTimer >= 0.14f)
            {
                _currentFrame = (_currentFrame + 1) % 4;
                _animTimer = 0f;
            }

            if (IcePhysics)
            {
                _iceVelocity += direction * actualSpeed * deltaTime * 2.2f;
                float maxIceSpeed = actualSpeed * 1.35f;
                if (_iceVelocity.Length() > maxIceSpeed)
                {
                    _iceVelocity.Normalize();
                    _iceVelocity *= maxIceSpeed;
                }
            }
            else
            {
                moveDelta = direction * actualSpeed * deltaTime;
                _iceVelocity = Vector2.Zero;
            }
        }
        else if (direction == Vector2.Zero)
        {
            _currentFrame = 0;
            _animTimer = 0f;
            if (IcePhysics)
                _iceVelocity *= MathHelper.Clamp(1f - deltaTime * 1.8f, 0f, 1f);
            else
                _iceVelocity = Vector2.Zero;
        }

        if (IcePhysics && _iceVelocity != Vector2.Zero)
            moveDelta += _iceVelocity * deltaTime;

        if (ExternalForce != Vector2.Zero)
            moveDelta += ExternalForce * deltaTime;

        if (moveDelta != Vector2.Zero)
        {
            Vector2 nextPos = Position;
            nextPos.X += moveDelta.X;
            Position = new Vector2(nextPos.X, Position.Y);
            if (CollidesWithAny(obstacles) || !MapTerrain.IsWalkableAt(terrainGrid, Position, Width, Height))
                nextPos.X = Position.X - moveDelta.X;

            nextPos.Y += moveDelta.Y;
            Position = new Vector2(nextPos.X, nextPos.Y);
            if (CollidesWithAny(obstacles) || !MapTerrain.IsWalkableAt(terrainGrid, Position, Width, Height))
                nextPos.Y = Position.Y - moveDelta.Y;

            Position = nextPos;
        }

        // Keep inside map bounds
        float clampX = MathHelper.Clamp(Position.X, 0, mapWidth - Width);
        float clampY = MathHelper.Clamp(Position.Y, 0, mapHeight - Height);
        Position = new Vector2(clampX, clampY);
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

    public void Draw(SpriteBatch spriteBatch, Texture2D playerTexture)
    {
        Rectangle sourceRect = new Rectangle(_currentFrame * 16, _directionRow * 16, 16, 16);
        spriteBatch.Draw(playerTexture, Bounds, sourceRect, Color.White);
    }
}

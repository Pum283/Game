using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MyGame;

// Base class for all weapons
public abstract class Weapon
{
    public string Name { get; set; }
    public int Level { get; set; } // 0 = Not Unlocked, 1-5 = Levels
    public float CooldownTimer { get; protected set; }

    public Weapon(string name)
    {
        Name = name;
        Level = 0;
        CooldownTimer = 0f;
    }

    public abstract void Update(GameTime gameTime, Player player, List<Enemy> enemies, List<Projectile> projectiles, Random random);
    public abstract void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, Player player);
}

// 1. Magic Wand
public class MagicWandWeapon : Weapon
{
    private float _baseInterval = 1.0f;

    public MagicWandWeapon() : base("Magic Wand")
    {
        Level = 1; // Default starting weapon
    }

    public override void Update(GameTime gameTime, Player player, List<Enemy> enemies, List<Projectile> projectiles, Random random)
    {
        if (Level == 0 || enemies.Count == 0) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        CooldownTimer -= deltaTime;

        if (CooldownTimer <= 0)
        {
            // Cooldown reduction calculation
            float cooldownReduce = player.EmptyTomeLevel * 0.08f + player.PermanentCooldownReduction;
            float interval = _baseInterval * (Level >= 4 ? 0.8f : 1.0f) * Math.Max(0.2f, 1f - cooldownReduce);
            
            // Damage scaling
            float dmg = 10f * (Level >= 2 ? 1.2f : 1.0f);
            dmg *= (1f + player.SpinachLevel * 0.10f); // Spinach bonus

            // Penetration
            int pierce = Level >= 5 ? 2 : 1;
            
            // Projectile speed scales with Bracer
            float speed = 350f * (1f + player.BracerLevel * 0.15f);

            // Find nearest enemy
            Enemy nearestEnemy = FindNearestEnemy(player.Center, enemies);
            if (nearestEnemy != null)
            {
                Vector2 target = nearestEnemy.Position + new Vector2(nearestEnemy.Width / 2f, nearestEnemy.Height / 2f);

                // Projectile count: base + Duplicator level
                int projectilesCount = (Level >= 3 ? 2 : 1) + player.DuplicatorLevel;

                for (int i = 0; i < projectilesCount; i++)
                {
                    float spread = (i - (projectilesCount - 1) / 2f) * 16f;
                    Vector2 startOffset = new Vector2(spread, -spread);
                    var p = new Projectile(player.Center + startOffset, target, speed, dmg, isFireball: false, penetrationCount: pierce);
                    p.MaxRange = 280f + Level * 30f;
                    projectiles.Add(p);
                }

                CooldownTimer = interval;
            }
        }
    }

    private Enemy FindNearestEnemy(Vector2 playerCenter, List<Enemy> enemies)
    {
        Enemy nearest = null;
        float minDistSq = float.MaxValue;
        foreach (var enemy in enemies)
        {
            float distSq = Vector2.DistanceSquared(playerCenter, enemy.Position);
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                nearest = enemy;
            }
        }
        return nearest;
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, Player player)
    {
    }
}

// 2. Garlic Aura
public class GarlicWeapon : Weapon
{
    private float _tickTimer = 0f;

    public GarlicWeapon() : base("Garlic Aura")
    {
        Level = 0;
    }

    public override void Update(GameTime gameTime, Player player, List<Enemy> enemies, List<Projectile> projectiles, Random random)
    {
        if (Level == 0 || enemies.Count == 0) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _tickTimer += deltaTime;

        if (_tickTimer >= 0.5f) // Damage every 0.5 seconds
        {
            // Radius scales with Spellbinder level
            float radius = (50f + Level * 12f) * (1f + player.SpellbinderLevel * 0.15f);
            
            // Damage scales with level and Spinach
            float damage = (1.5f + Level * 1.0f);
            if (Level == 5) damage *= 2.0f; // Double damage at max level

            damage *= (1f + player.SpinachLevel * 0.10f);

            foreach (var enemy in enemies)
            {
                float dist = Vector2.Distance(player.Center, enemy.Position);
                if (dist <= radius)
                {
                    enemy.HP -= damage;

                    // Knockback at Lvl 4+
                    if (Level >= 4)
                    {
                        Vector2 kbDir = enemy.Position - player.Center;
                        if (kbDir != Vector2.Zero)
                        {
                            kbDir.Normalize();
                            enemy.Position += kbDir * 18f;
                        }
                    }
                }
            }
            _tickTimer = 0f;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, Player player)
    {
        if (Level == 0) return;

        float radius = (50f + Level * 12f) * (1f + player.SpellbinderLevel * 0.15f);
        int segments = 36;
        for (int i = 0; i < segments; i++)
        {
            double angle = (double)i / segments * Math.PI * 2;
            Vector2 offset = new Vector2((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius);
            Vector2 pos = player.Center + offset;
            
            spriteBatch.Draw(pixelTexture, new Rectangle((int)pos.X - 2, (int)pos.Y - 2, 4, 4), Color.PaleGreen * 0.25f);
        }
    }
}

// 3. Fireball Weapon
public class FireballWeapon : Weapon
{
    private float _baseInterval = 3.0f;

    public FireballWeapon() : base("Fireball")
    {
        Level = 0;
    }

    public override void Update(GameTime gameTime, Player player, List<Enemy> enemies, List<Projectile> projectiles, Random random)
    {
        if (Level == 0) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        CooldownTimer -= deltaTime;

        if (CooldownTimer <= 0)
        {
            // Cooldown reduction
            float cooldownReduce = player.EmptyTomeLevel * 0.08f + player.PermanentCooldownReduction;
            float interval = _baseInterval * Math.Max(0.2f, 1f - cooldownReduce);

            int count = (Level == 5 ? 2 : 1) + player.DuplicatorLevel;
            float dmg = 15f + Level * 5f;
            dmg *= (1f + player.SpinachLevel * 0.10f); // Spinach bonus
            
            int pierce = 2 + Level;
            float speed = (180f + (Level >= 2 ? 60f : 0f)) * (1f + player.BracerLevel * 0.15f);

            for (int c = 0; c < count; c++)
            {
                double angle = random.NextDouble() * Math.PI * 2;
                Vector2 target = player.Center + new Vector2(
                    (float)Math.Cos(angle) * 100f,
                    (float)Math.Sin(angle) * 100f
                );
                var fb = new Projectile(player.Center, target, speed, dmg, isFireball: true, penetrationCount: pierce);
                fb.MaxRange = 350f + Level * 30f;
                projectiles.Add(fb);
            }

            CooldownTimer = interval;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, Player player)
    {
    }
}

// 4. Axe Weapon
public class AxeWeapon : Weapon
{
    private float _baseInterval = 2.0f;

    public AxeWeapon() : base("Axe")
    {
        Level = 0;
    }

    public override void Update(GameTime gameTime, Player player, List<Enemy> enemies, List<Projectile> projectiles, Random random)
    {
        if (Level == 0) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        CooldownTimer -= deltaTime;

        if (CooldownTimer <= 0)
        {
            float cooldownReduce = player.EmptyTomeLevel * 0.08f + player.PermanentCooldownReduction;
            float interval = _baseInterval * Math.Max(0.2f, 1f - cooldownReduce);

            float dmg = 20f + Level * 5f;
            dmg *= (1f + player.SpinachLevel * 0.10f);

            int count = 1 + (Level >= 3 ? 1 : 0) + (Level == 5 ? 1 : 0) + player.DuplicatorLevel;
            int pierce = Level >= 4 ? 999 : 4;

            for (int i = 0; i < count; i++)
            {
                float speedX = random.Next(-180, 180) * (1f + player.BracerLevel * 0.1f);
                float speedY = -480f * (1f + player.BracerLevel * 0.15f);
                
                if (count > 1)
                {
                    float angleFraction = (float)i / (count - 1);
                    speedX = MathHelper.Lerp(-220f, 220f, angleFraction) * (1f + player.BracerLevel * 0.1f);
                }

                Vector2 startPos = player.Center;
                Vector2 targetPos = startPos + new Vector2(speedX, speedY);

                projectiles.Add(new Projectile(
                    startPos,
                    targetPos,
                    speed: 1.0f,
                    dmg,
                    isFireball: false,
                    isAxe: true,
                    hasGravity: true,
                    penetrationCount: pierce
                ) { Velocity = new Vector2(speedX, speedY), MaxRange = 350f });
            }

            CooldownTimer = interval;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, Player player)
    {
    }
}

// 5. Lightning Ring
public class LightningWeapon : Weapon
{
    private float _baseInterval = 4.0f;

    public LightningWeapon() : base("Lightning Ring")
    {
        Level = 0;
    }

    public override void Update(GameTime gameTime, Player player, List<Enemy> enemies, List<Projectile> projectiles, Random random)
    {
        if (Level == 0 || enemies.Count == 0) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        CooldownTimer -= deltaTime;

        if (CooldownTimer <= 0)
        {
            float cooldownReduce = player.EmptyTomeLevel * 0.08f + player.PermanentCooldownReduction;
            float interval = _baseInterval * (Level >= 4 ? 0.75f : 1.0f) * Math.Max(0.2f, 1f - cooldownReduce);

            // Scales with Spinach and permanent DamageMultiplier
            float dmg = (30f + Level * 10f);
            dmg *= (1f + player.SpinachLevel * 0.10f);

            int strikes = (Level >= 3 ? 2 : 1) + player.DuplicatorLevel;
            if (Level == 5) strikes += 1;

            for (int s = 0; s < strikes; s++)
            {
                if (enemies.Count == 0) break;
                int idx = random.Next(enemies.Count);
                Enemy target = enemies[idx];

                target.HP -= dmg;

                Game1.AddVisualEffect(new VisualEffect
                {
                    Position = new Vector2(target.Position.X + target.Width / 2 - 4, target.Position.Y - 500),
                    Width = 8,
                    Height = 500 + target.Height,
                    Color = Color.LightSkyBlue * 0.8f,
                    Duration = 0.15f
                });
            }

            CooldownTimer = interval;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, Player player)
    {
    }
}

// 6. Holy Water
public class HolyWaterWeapon : Weapon
{
    private float _baseInterval = 3.5f;

    public HolyWaterWeapon() : base("Holy Water")
    {
        Level = 0;
    }

    public override void Update(GameTime gameTime, Player player, List<Enemy> enemies, List<Projectile> projectiles, Random random)
    {
        if (Level == 0) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        CooldownTimer -= deltaTime;

        if (CooldownTimer <= 0)
        {
            float cooldownReduce = player.EmptyTomeLevel * 0.08f + player.PermanentCooldownReduction;
            float interval = _baseInterval * (Level >= 4 ? 0.8f : 1.0f) * Math.Max(0.2f, 1f - cooldownReduce);

            float dmg = 5f + Level * 1.5f;
            dmg *= (1f + player.SpinachLevel * 0.10f);
            
            // Radius and pool duration scale with Spellbinder
            float radius = (40f + Level * 10f) * (1f + player.SpellbinderLevel * 0.15f);
            float poolDuration = (3.0f + Level * 0.5f) * (1f + player.SpellbinderLevel * 0.15f);
            
            int count = (Level >= 3 ? 2 : 1) + player.DuplicatorLevel;
            if (Level == 5) count += 1;

            for (int c = 0; c < count; c++)
            {
                double angle = random.NextDouble() * Math.PI * 2;
                float dist = random.Next(100, 250);
                Vector2 landPos = player.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist
                );

                Game1.AddHolyWaterPool(new HolyWaterPool
                {
                    Position = landPos,
                    Radius = radius,
                    Damage = dmg,
                    Duration = poolDuration
                });
            }

            CooldownTimer = interval;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, Player player)
    {
    }
}

// 7. Cross Weapon (Cay Thap Tu)
public class CrossWeapon : Weapon
{
    private float _baseInterval = 2.0f;

    public CrossWeapon() : base("Cross")
    {
        Level = 0;
    }

    public override void Update(GameTime gameTime, Player player, List<Enemy> enemies, List<Projectile> projectiles, Random random)
    {
        if (Level == 0) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        CooldownTimer -= deltaTime;

        if (CooldownTimer <= 0)
        {
            float cooldownReduce = player.EmptyTomeLevel * 0.08f + player.PermanentCooldownReduction;
            float interval = _baseInterval * Math.Max(0.2f, 1f - cooldownReduce);

            float dmg = 12f + Level * 4f;
            dmg *= (1f + player.SpinachLevel * 0.10f); // Spinach bonus

            int pierce = 2 + Level; // pierce increases with level
            float speed = 250f * (1f + player.BracerLevel * 0.15f); // Bracer bonus

            // Target nearest enemy
            Enemy targetEnemy = null;
            if (enemies.Count > 0)
            {
                float minDist = float.MaxValue;
                foreach (var enemy in enemies)
                {
                    float dist = Vector2.Distance(player.Center, enemy.Position);
                    if (dist < minDist) { minDist = dist; targetEnemy = enemy; }
                }
            }

            Vector2 targetPos = targetEnemy != null ? targetEnemy.Position : player.Center + new Vector2(1, 0);

            // Projectiles count: base + Duplicator count
            int count = (Level >= 3 ? 2 : 1) + (Level == 5 ? 1 : 0) + player.DuplicatorLevel;

            for (int i = 0; i < count; i++)
            {
                float spread = (i - (count - 1) / 2f) * 20f;
                Vector2 startPos = player.Center + new Vector2(spread, -spread);
                
                var p = new Projectile(startPos, targetPos, speed, dmg, penetrationCount: pierce)
                {
                    IsCross = true,
                    MaxRange = 360f + Level * 30f
                };
                projectiles.Add(p);
            }

            CooldownTimer = interval;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, Player player)
    {
    }
}

// 8. King Bible Weapon (Kinh Thanh)
public class BibleWeapon : Weapon
{
    private float _baseInterval = 3.0f;

    public BibleWeapon() : base("King Bible")
    {
        Level = 0;
    }

    public override void Update(GameTime gameTime, Player player, List<Enemy> enemies, List<Projectile> projectiles, Random random)
    {
        if (Level == 0) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        CooldownTimer -= deltaTime;

        if (CooldownTimer <= 0)
        {
            float cooldownReduce = player.EmptyTomeLevel * 0.08f + player.PermanentCooldownReduction;
            float interval = _baseInterval * Math.Max(0.2f, 1f - cooldownReduce);

            float dmg = 8f + Level * 3f;
            dmg *= (1f + player.SpinachLevel * 0.10f);

            int count = Level + player.DuplicatorLevel; // 1 at L1, 2 at L2...
            float durationSec = 3.0f + player.SpellbinderLevel * 0.45f; // duration scales with Spellbinder

            for (int i = 0; i < count; i++)
            {
                float angleOffset = (float)i / count * (float)Math.PI * 2f;
                
                var p = new Projectile(player.Center, player.Center, speed: 0f, dmg, penetrationCount: 999)
                {
                    IsBible = true,
                    BibleAngle = angleOffset,
                    MaxRange = durationSec * 100f // 100 units = 1 second
                };
                projectiles.Add(p);
            }

            CooldownTimer = interval;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, Player player)
    {
    }
}

// 9. Knife Weapon (Dao Phong)
public class KnifeWeapon : Weapon
{
    private float _baseInterval = 1.2f;

    public KnifeWeapon() : base("Knife")
    {
        Level = 0;
    }

    public override void Update(GameTime gameTime, Player player, List<Enemy> enemies, List<Projectile> projectiles, Random random)
    {
        if (Level == 0) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        CooldownTimer -= deltaTime;

        if (CooldownTimer <= 0)
        {
            float cooldownReduce = player.EmptyTomeLevel * 0.08f + player.PermanentCooldownReduction;
            float interval = (_baseInterval - Level * 0.15f) * Math.Max(0.2f, 1f - cooldownReduce);

            float dmg = 9f + Level * 3f;
            dmg *= (1f + player.SpinachLevel * 0.10f);

            int count = (Level >= 3 ? 2 : 1) + (Level == 5 ? 1 : 0) + player.DuplicatorLevel;
            int pierce = Level >= 4 ? 2 : 1;
            float speed = 400f * (1f + player.BracerLevel * 0.15f);

            // Knife flies in keyboard movement direction
            var kb = Keyboard.GetState();
            Vector2 dir = Vector2.Zero;
            if (kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up)) dir.Y -= 1;
            if (kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down)) dir.Y += 1;
            if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left)) dir.X -= 1;
            if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right)) dir.X += 1;

            if (dir == Vector2.Zero)
            {
                dir = new Vector2(1, 0); // Default to right
            }
            dir.Normalize();

            Vector2 targetPos = player.Center + dir * 100f;

            for (int i = 0; i < count; i++)
            {
                Vector2 perp = new Vector2(-dir.Y, dir.X);
                float offset = (i - (count - 1) / 2f) * 12f;
                Vector2 startPos = player.Center + perp * offset;

                var p = new Projectile(startPos, targetPos, speed, dmg, penetrationCount: pierce)
                {
                    IsKnife = true,
                    MaxRange = 380f + Level * 20f
                };
                projectiles.Add(p);
            }

            CooldownTimer = interval;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, Player player)
    {
    }
}

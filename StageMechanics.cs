using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MyGame;

public sealed class StageMechanics
{
    public bool SpikesActive { get; private set; }
    public bool SandstormActive { get; private set; }
    public Vector2 SandstormDirection { get; private set; } = Vector2.UnitX;
    public float FogRadius { get; private set; } = 240f;

    private int _stage;
    private float _spikeCycle;
    private float _sandstormTimer;
    private float _sandstormDuration;
    private float _lavaSpawnTimer;
    private float _skyDropTimer;
    private readonly List<Point> _lavaCells = new();

    public void Reset(int stage, byte[,] terrain)
    {
        _stage = stage;
        _spikeCycle = 0f;
        _sandstormTimer = 0f;
        _sandstormDuration = 0f;
        _lavaSpawnTimer = 0f;
        _skyDropTimer = 0f;
        SpikesActive = false;
        SandstormActive = false;
        SandstormDirection = new Vector2(1f, 0.25f);
        if (SandstormDirection != Vector2.Zero) SandstormDirection.Normalize();

        _lavaCells.Clear();
        if (terrain == null || stage != 7) return;

        int cols = terrain.GetLength(0);
        int rows = terrain.GetLength(1);
        for (int tx = 0; tx < cols; tx++)
            for (int ty = 0; ty < rows; ty++)
                if (terrain[tx, ty] == MapTerrain.Lava)
                    _lavaCells.Add(new Point(tx, ty));
    }

    public void Update(
        float deltaTime,
        Player player,
        byte[,] terrain,
        List<Enemy> enemies,
        Random random,
        Action<Vector2, EnemyType> spawnEnemy)
    {
        if (player == null) return;

        player.ExternalSpeedMultiplier = 1f;
        player.ExternalForce = Vector2.Zero;
        player.IcePhysics = false;

        if (player.FreezeTimer > 0f)
            player.FreezeTimer = MathHelper.Max(0f, player.FreezeTimer - deltaTime);

        byte underFoot = MapTerrain.GetTerrainAt(terrain, player.Position, player.Width, player.Height);

        switch (_stage)
        {
            case 4:
                if (underFoot == MapTerrain.Poison)
                {
                    player.ExternalSpeedMultiplier = 0.55f;
                    player.HP = MathHelper.Max(0f, player.HP - 4f * deltaTime);
                }
                break;

            case 6:
                UpdateSpikeTraps(deltaTime, player, terrain);
                break;

            case 7:
                UpdateLavaSpawns(deltaTime, player, random, spawnEnemy);
                break;

            case 8:
                UpdateSandstorm(deltaTime, player);
                break;

            case 9:
                UpdateSkyDrops(deltaTime, player, random, spawnEnemy);
                break;

            case 10:
                player.IcePhysics = underFoot == MapTerrain.Ice || underFoot == MapTerrain.Snow;
                break;
        }
    }

    private void UpdateSpikeTraps(float deltaTime, Player player, byte[,] terrain)
    {
        _spikeCycle += deltaTime;
        if (_spikeCycle >= 10f)
        {
            _spikeCycle = 0f;
            SpikesActive = true;
        }
        else if (_spikeCycle >= 2f)
        {
            SpikesActive = false;
        }

        if (!SpikesActive || terrain == null) return;

        byte cell = MapTerrain.GetTerrainAt(terrain, player.Position, player.Width, player.Height);
        if (cell == MapTerrain.Spike)
            player.HP = MathHelper.Max(0f, player.HP - 18f * deltaTime);
    }

    private void UpdateSandstorm(float deltaTime, Player player)
    {
        if (SandstormActive)
        {
            _sandstormDuration -= deltaTime;
            player.ExternalForce = SandstormDirection * 140f;
            if (_sandstormDuration <= 0f)
                SandstormActive = false;
            return;
        }

        _sandstormTimer += deltaTime;
        if (_sandstormTimer >= 180f)
        {
            _sandstormTimer = 0f;
            SandstormActive = true;
            _sandstormDuration = 12f;
            int dir = (_stage * 3) % 4;
            SandstormDirection = dir switch
            {
                0 => new Vector2(1f, 0f),
                1 => new Vector2(-1f, 0f),
                2 => new Vector2(0.35f, 1f),
                _ => new Vector2(-0.35f, -1f)
            };
            SandstormDirection.Normalize();
        }
    }

    private void UpdateLavaSpawns(float deltaTime, Player player, Random random, Action<Vector2, EnemyType> spawnEnemy)
    {
        if (_lavaCells.Count == 0) return;

        _lavaSpawnTimer += deltaTime;
        if (_lavaSpawnTimer < 4.5f) return;
        _lavaSpawnTimer = 0f;

        Point best = _lavaCells[random.Next(_lavaCells.Count)];
        float bestDist = float.MaxValue;
        for (int i = 0; i < 8; i++)
        {
            var cell = _lavaCells[random.Next(_lavaCells.Count)];
            var world = new Vector2(cell.X * MapTerrain.CellSize + 8, cell.Y * MapTerrain.CellSize + 8);
            float d = Vector2.Distance(world, player.Center);
            if (d < bestDist && d < 700f)
            {
                bestDist = d;
                best = cell;
            }
        }

        var spawnPos = new Vector2(best.X * MapTerrain.CellSize + 4, best.Y * MapTerrain.CellSize + 4);
        spawnEnemy(spawnPos, EnemyType.Zombie);
    }

    private void UpdateSkyDrops(float deltaTime, Player player, Random random, Action<Vector2, EnemyType> spawnEnemy)
    {
        _skyDropTimer += deltaTime;
        if (_skyDropTimer < 2.2f) return;
        _skyDropTimer = 0f;

        float offsetX = random.Next(-60, 61);
        var spawnPos = new Vector2(player.Center.X + offsetX - 12, player.Center.Y - 420f);
        EnemyType type = random.Next(100) < 45 ? EnemyType.Bat : EnemyType.Zombie;
        spawnEnemy(spawnPos, type);
    }

    public void DrawFog(SpriteBatch spriteBatch, Texture2D pixel, Vector2 playerScreenCenter, int screenW, int screenH)
    {
        if (_stage != 5) return;

        float radius = FogRadius;
        float cx = playerScreenCenter.X;
        float cy = playerScreenCenter.Y;
        Color fog = Color.Black * 0.72f;

        int topH = (int)Math.Max(0, cy - radius);
        int botY = (int)Math.Min(screenH, cy + radius);
        int leftW = (int)Math.Max(0, cx - radius);
        int rightX = (int)Math.Min(screenW, cx + radius);

        if (topH > 0)
            spriteBatch.Draw(pixel, new Rectangle(0, 0, screenW, topH), fog);
        if (botY < screenH)
            spriteBatch.Draw(pixel, new Rectangle(0, botY, screenW, screenH - botY), fog);
        if (leftW > 0)
            spriteBatch.Draw(pixel, new Rectangle(0, topH, leftW, botY - topH), fog);
        if (rightX < screenW)
            spriteBatch.Draw(pixel, new Rectangle(rightX, topH, screenW - rightX, botY - topH), fog);

        for (int ring = 8; ring >= 1; ring--)
        {
            float t = ring / 8f;
            float r = radius * (0.75f + t * 0.35f);
            int size = (int)(r * 2f);
            var rect = new Rectangle((int)(cx - r), (int)(cy - r), size, size);
            spriteBatch.Draw(pixel, rect, Color.Black * (0.04f * t));
        }
    }

    public void DrawSandstorm(SpriteBatch spriteBatch, Texture2D pixel, int screenW, int screenH)
    {
        if (_stage != 8 || !SandstormActive) return;
        spriteBatch.Draw(pixel, new Rectangle(0, 0, screenW, screenH), new Color(210, 170, 90) * 0.28f);
    }

    public void DrawActiveSpikes(SpriteBatch spriteBatch, Texture2D pixel, byte[,] terrain)
    {
        if (_stage != 6 || !SpikesActive || terrain == null) return;

        int cols = terrain.GetLength(0);
        int rows = terrain.GetLength(1);
        for (int tx = 0; tx < cols; tx++)
        {
            for (int ty = 0; ty < rows; ty++)
            {
                if (terrain[tx, ty] != MapTerrain.Spike) continue;
                var dest = new Rectangle(tx * MapTerrain.CellSize, ty * MapTerrain.CellSize, MapTerrain.CellSize, MapTerrain.CellSize);
                spriteBatch.Draw(pixel, dest, new Color(255, 60, 60) * 0.65f);
            }
        }
    }
}

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MyGame;

public enum ObstacleStyle
{
    StoneBrick,
    DesertColumn,
    TreeOak,
    TreePine,
    TreeDead,
    Bush,
    RockSmall,
    RockLarge,
    IceBoulder,
    Crystal,
    WoodPlank,
    DarkBrick,
    Tombstone,
    DoorArch,
    FenceWood,
    GateTorii,
    Lantern,
    Barrel,
    PillarStone,
    Shrine,
    BridgeWood,
    WallFortified,
    Crate,
    Statue,
    Bookshelf,
    Cactus
}

public enum ObstacleLoot
{
    None,
    Gold,
    Health
}

public class Obstacle
{
    public Vector2 Position { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public ObstacleStyle Style { get; set; }
    public int Variant { get; set; }
    public bool BlocksMovement { get; set; } = true;
    public bool PassableByFlying { get; set; }
    public bool BlocksProjectiles { get; set; } = true;
    public bool IsDestructible { get; set; }
    public bool IsDestroyed { get; set; }
    public float HP { get; set; }
    public float MaxHP { get; set; }
    public ObstacleLoot Loot { get; set; } = ObstacleLoot.None;
    public int LootAmount { get; set; }
    public bool DealsContactDamage { get; set; }

    public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

    public Obstacle(
        Vector2 position,
        int width,
        int height,
        ObstacleStyle style = ObstacleStyle.StoneBrick,
        int variant = 0,
        bool blocksMovement = true)
    {
        Position = position;
        Width = width;
        Height = height;
        Style = style;
        Variant = variant;
        BlocksMovement = blocksMovement;
        BlocksProjectiles = blocksMovement && style != ObstacleStyle.Bush && style != ObstacleStyle.Lantern;
        PassableByFlying = style == ObstacleStyle.Tombstone;
        DealsContactDamage = style == ObstacleStyle.Cactus;
    }

    public void ConfigureDestructible(float hp, ObstacleLoot loot, int lootAmount)
    {
        IsDestructible = true;
        MaxHP = hp;
        HP = hp;
        Loot = loot;
        LootAmount = lootAmount;
    }

    // Toa do sprite da duoc kiem chung truc tiep tren cac file tileset.
    public void Draw(SpriteBatch spriteBatch, Texture2D wallTex, Texture2D natureTex, Texture2D reliefTex,
        Texture2D desertTex, Texture2D houseTex, Texture2D elementTex, Texture2D villageTex, Texture2D waterTex)
    {
        if (IsDestroyed) return;

        int v = Variant % 3;
        switch (Style)
        {
            case ObstacleStyle.TreeOak:
            case ObstacleStyle.TreePine:
            case ObstacleStyle.TreeDead:
            case ObstacleStyle.Bush:
            case ObstacleStyle.RockSmall:
            case ObstacleStyle.RockLarge:
            case ObstacleStyle.IceBoulder:
            case ObstacleStyle.Crystal:
            case ObstacleStyle.Cactus:
                DrawNatureSprite(spriteBatch, natureTex);
                break;

            case ObstacleStyle.Bookshelf:
                // Ke sach go voi sach mau (TilesetElement)
                spriteBatch.Draw(elementTex, Bounds, new Rectangle(v % 2 == 0 ? 144 : 176, 96, 32, 48), Color.White);
                break;

            case ObstacleStyle.DoorArch:
                spriteBatch.Draw(houseTex, Bounds, new Rectangle(128 + (v % 2) * 16, 0, 16, 32), Color.White);
                break;

            case ObstacleStyle.FenceWood:
                DrawTiled(spriteBatch, houseTex, new Rectangle(0, 144, 16, 16));
                break;

            case ObstacleStyle.GateTorii:
                // Cong Torii do (TilesetHouse)
                spriteBatch.Draw(houseTex, Bounds, new Rectangle(0, 80, 48, 48), Color.White);
                break;

            case ObstacleStyle.Lantern:
                // Den da kieu Nhat (TilesetHouse)
                spriteBatch.Draw(houseTex, Bounds, new Rectangle(448, 240, 32, 48), Color.White);
                break;

            case ObstacleStyle.Barrel:
                spriteBatch.Draw(elementTex, Bounds, new Rectangle(v == 0 ? 96 : 64, 16, 16, 16), Color.White);
                break;

            case ObstacleStyle.Crate:
                spriteBatch.Draw(elementTex, Bounds, new Rectangle(0, 0, 16, 16), Color.White);
                break;

            case ObstacleStyle.PillarStone:
                // Cot da phu reu (TilesetVillageAbandoned)
                spriteBatch.Draw(villageTex, Bounds, new Rectangle(v % 2 == 0 ? 32 : 48, 64, 16, 48), Color.White);
                break;

            case ObstacleStyle.Statue:
                if (v % 2 == 0)
                    spriteBatch.Draw(houseTex, Bounds, new Rectangle(16, 240, 32, 48), Color.White);   // Tuong Phat vang
                else
                    spriteBatch.Draw(houseTex, Bounds, new Rectangle(0, 304, 48, 48), Color.White);    // Tuong da xam
                break;

            case ObstacleStyle.Shrine:
                spriteBatch.Draw(houseTex, Bounds, new Rectangle(448, 64, 64, 64), Color.White);
                break;

            case ObstacleStyle.BridgeWood:
                // Coc go + nam (TilesetVillageAbandoned)
                spriteBatch.Draw(villageTex, Bounds, new Rectangle(96, 128, 32, 48), Color.White);
                break;

            case ObstacleStyle.WallFortified:
                DrawTiled(spriteBatch, reliefTex, v % 2 == 0
                    ? new Rectangle(192, 96, 16, 16)
                    : new Rectangle(208, 96, 16, 16));
                break;

            case ObstacleStyle.DesertColumn:
                // Cay co (TilesetDesert)
                spriteBatch.Draw(desertTex, Bounds, new Rectangle(160, v % 2 == 0 ? 64 : 128, 48, 64), Color.White);
                break;

            case ObstacleStyle.WoodPlank:
                DrawTiled(spriteBatch, waterTex, new Rectangle(16, 208, 16, 16));
                break;

            case ObstacleStyle.DarkBrick:
                // Dong da do nat (TilesetVillageAbandoned)
                spriteBatch.Draw(villageTex, Bounds, new Rectangle(96, 64, 32, 16), Color.White);
                break;

            case ObstacleStyle.Tombstone:
                // Bia mo / cay thanh gia (TilesetElement)
                Rectangle tombSrc = v switch
                {
                    0 => new Rectangle(64, 32, 16, 32),
                    1 => new Rectangle(80, 32, 16, 32),
                    _ => new Rectangle(176, 32, 16, 32)
                };
                spriteBatch.Draw(elementTex, Bounds, tombSrc, Color.White);
                break;

            default:
                DrawTiled(spriteBatch, reliefTex, new Rectangle(16, 0, 16, 16));
                break;
        }

        if (IsDestructible && HP < MaxHP)
        {
            // Vet nut tren vat pha duoc
            spriteBatch.Draw(wallTex, Bounds, new Rectangle(0, 0, 16, 16), Color.Black * 0.25f);
        }
    }

    private void DrawNatureSprite(SpriteBatch spriteBatch, Texture2D natureTex)
    {
        Rectangle source = GetNatureSource();
        Color tint = Style == ObstacleStyle.Cactus ? new Color(150, 235, 130) : Color.White;
        spriteBatch.Draw(natureTex, Bounds, source, tint);
    }

    private Rectangle GetNatureSource()
    {
        int v = Variant % 3;
        return Style switch
        {
            // Cay xanh lon / cay hoa anh dao
            ObstacleStyle.TreeOak => v switch
            {
                0 => new Rectangle(96, 0, 32, 48),
                1 => new Rectangle(0, 0, 32, 48),
                _ => new Rectangle(224, 0, 32, 48)
            },
            // Thong lon (cap cay 64x64)
            ObstacleStyle.TreePine => v % 2 == 0
                ? new Rectangle(0, 64, 64, 64)
                : new Rectangle(64, 64, 64, 64),
            ObstacleStyle.TreeDead => new Rectangle(64, 0, 32, 48),
            ObstacleStyle.Bush => new Rectangle(v % 2 == 0 ? 144 : 160, 176, 16, 16),
            ObstacleStyle.RockSmall => v switch
            {
                0 => new Rectangle(64, 208, 16, 16),
                1 => new Rectangle(80, 208, 16, 16),
                _ => new Rectangle(96, 208, 16, 16)
            },
            // Cum da tang lon (nau / xam)
            ObstacleStyle.RockLarge => v % 2 == 0
                ? new Rectangle(176, 128, 64, 64)
                : new Rectangle(256, 128, 64, 64),
            // Tang da tuyet / da xanh
            ObstacleStyle.IceBoulder => v % 2 == 0
                ? new Rectangle(32, 192, 32, 32)
                : new Rectangle(0, 192, 32, 32),
            ObstacleStyle.Crystal => new Rectangle((Variant % 4) * 16, 224, 16, 16),
            // Bui gai (pha le xanh la nhon)
            ObstacleStyle.Cactus => new Rectangle(48, 224, 16, 16),
            _ => new Rectangle(64, 208, 16, 16)
        };
    }

    private void DrawTiled(SpriteBatch spriteBatch, Texture2D texture, Rectangle sourceRect)
    {
        int tileSize = 16;
        for (int x = 0; x < Width; x += tileSize)
        {
            for (int y = 0; y < Height; y += tileSize)
            {
                int drawW = Math.Min(tileSize, Width - x);
                int drawH = Math.Min(tileSize, Height - y);
                Rectangle destRect = new Rectangle((int)Position.X + x, (int)Position.Y + y, drawW, drawH);
                Rectangle cropSource = new Rectangle(sourceRect.X, sourceRect.Y, drawW, drawH);
                spriteBatch.Draw(texture, destRect, cropSource, Color.White);
            }
        }
    }
}

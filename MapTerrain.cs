using Microsoft.Xna.Framework;

namespace MyGame;

public static class MapTerrain
{
    public const int CellSize = 32;

    public const byte Grass = 0;
    public const byte Grass2 = 1;
    public const byte Dirt = 2;
    public const byte Sand = 3;
    public const byte Stone = 4;
    public const byte Water = 5;
    public const byte Snow = 6;
    public const byte Lava = 7;
    public const byte Dungeon = 8;
    public const byte Void = 9;
    public const byte Cliff = 10;
    public const byte Path = 11;
    public const byte WoodFloor = 12;
    public const byte Moss = 13;
    public const byte Poison = 14;
    public const byte Spike = 15;
    public const byte Ice = 16;

    public static bool IsLiquid(byte cell) => cell == Water || cell == Lava;

    public static bool IsHazard(byte cell) => cell == Poison || cell == Spike || cell == Lava;

    public static bool IsWalkable(byte cell) =>
        cell != Void && cell != Cliff && cell != Water && cell != Lava;

    public static bool IsBlockingTerrain(byte cell) => !IsWalkable(cell);

    public static bool IsSlippery(byte cell) => cell == Ice || cell == Snow;

    public static bool IsWalkableAt(byte[,] grid, Vector2 position, int width, int height)
    {
        if (grid == null) return true;

        int cols = grid.GetLength(0);
        int rows = grid.GetLength(1);
        int tx = (int)((position.X + width / 2f) / CellSize);
        int ty = (int)((position.Y + height / 2f) / CellSize);

        if (tx < 0 || ty < 0 || tx >= cols || ty >= rows)
            return false;

        return IsWalkable(grid[tx, ty]);
    }

    public static byte GetTerrainAt(byte[,] grid, Vector2 position, int width, int height)
    {
        if (grid == null) return Grass;

        int cols = grid.GetLength(0);
        int rows = grid.GetLength(1);
        int tx = (int)((position.X + width / 2f) / CellSize);
        int ty = (int)((position.Y + height / 2f) / CellSize);

        if (tx < 0 || ty < 0 || tx >= cols || ty >= rows)
            return Void;

        return grid[tx, ty];
    }

    public static int Hash(int x, int y, int seed = 0)
    {
        return ((x * 73856093) ^ (y * 19349663) ^ seed) & 0x7FFFFFFF;
    }
}

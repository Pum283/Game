using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MyGame;

public sealed class StageMapBuilder
{
    public byte[,] Terrain { get; private set; }
    public List<Obstacle> Obstacles { get; private set; } = new();
    public int Cols { get; private set; }
    public int Rows { get; private set; }

    private Random _rand;
    private int _stage;
    private int _mapW;
    private int _mapH;
    private float _cx;
    private float _cy;

    public void Build(int stageIndex, int mapWidth, int mapHeight)
    {
        _stage = stageIndex;
        _mapW = mapWidth;
        _mapH = mapHeight;
        Cols = mapWidth / MapTerrain.CellSize;
        Rows = mapHeight / MapTerrain.CellSize;
        _cx = Cols / 2f;
        _cy = Rows / 2f;
        _rand = new Random(stageIndex * 7919 + 104729);

        Terrain = new byte[Cols, Rows];
        Obstacles = new List<Obstacle>();

        for (int tx = 0; tx < Cols; tx++)
            for (int ty = 0; ty < Rows; ty++)
                Terrain[tx, ty] = MapTerrain.Void;

        bool openMap = false;
        switch (stageIndex)
        {
            case 1: BuildPeacefulMeadow(); openMap = true; break;
            case 2: BuildAncientCemetery(); break;
            case 3: BuildForbiddenLibrary(); break;
            case 4: BuildPoisonSwamp(); break;
            case 5: BuildMistyForest(); break;
            case 6: BuildRuinedCastle(); break;
            case 7: BuildLavaMine(); break;
            case 8: BuildDeadDesert(); openMap = true; break;
            case 9: BuildRopeBridge(); break;
            default: BuildIcePeak(); break;
        }

        if (!openMap)
            ApplyCliffBorders();

        ClearSpawnArea();
        BuildStageObstacles();
    }

    // =====================================================================
    // MAP 1: THẢO NGUYÊN YÊN BÌNH
    // Đồng cỏ mở rộng lớn — con đường zigzag xuyên giữa, hồ nước nhỏ ở 4 góc
    // =====================================================================
    private void BuildPeacefulMeadow()
    {
        // Nền cỏ xanh tươi toàn map
        for (int tx = 0; tx < Cols; tx++)
            for (int ty = 0; ty < Rows; ty++)
                Terrain[tx, ty] = MapTerrain.Grass;

        // Biến thể cỏ tự nhiên
        AddVariation(MapTerrain.Grass, MapTerrain.Grass2, 20);
        AddVariation(MapTerrain.Grass, MapTerrain.Moss,   8);

        // Con đường zigzag lớn từ tây sang đông
        PaintWavyPath(4, (int)_cy, Cols - 4, (int)_cy, amplitude: 12, width: 3, MapTerrain.Path);

        // Con đường dọc từ bắc xuống nam
        PaintWavyPath((int)_cx, 4, (int)_cx, Rows - 4, amplitude: 8, width: 2, MapTerrain.Path);

        // 4 hồ nước nhỏ ở 4 góc map
        int lakeR = 7;
        PaintBlob(18,        18,        lakeR, MapTerrain.Water);
        PaintBlob(Cols - 18, 18,        lakeR, MapTerrain.Water);
        PaintBlob(18,        Rows - 18, lakeR, MapTerrain.Water);
        PaintBlob(Cols - 18, Rows - 18, lakeR, MapTerrain.Water);

        // Dải cỏ dày xung quanh hồ
        PaintBlob(18,        18,        lakeR + 4, MapTerrain.Moss);
        PaintBlob(Cols - 18, 18,        lakeR + 4, MapTerrain.Moss);
        PaintBlob(18,        Rows - 18, lakeR + 4, MapTerrain.Moss);
        PaintBlob(Cols - 18, Rows - 18, lakeR + 4, MapTerrain.Moss);
        // Đè lại hồ để moss không che hồ
        PaintBlob(18,        18,        lakeR, MapTerrain.Water);
        PaintBlob(Cols - 18, 18,        lakeR, MapTerrain.Water);
        PaintBlob(18,        Rows - 18, lakeR, MapTerrain.Water);
        PaintBlob(Cols - 18, Rows - 18, lakeR, MapTerrain.Water);

        // Vài mảng Grass2 tự nhiên hữu cơ
        for (int i = 0; i < 8; i++)
        {
            int gx = _rand.Next(10, Cols - 10);
            int gy = _rand.Next(10, Rows - 10);
            PaintBlob(gx, gy, _rand.Next(4, 9), MapTerrain.Grass2);
        }
    }

    // =====================================================================
    // MAP 2: NGHĨA TRANG CỔ
    // Đất tối u ám — đường lát đá grid, bia mộ theo hàng, rêu xanh góc tối
    // =====================================================================
    private void BuildAncientCemetery()
    {
        // Nền đất tối toàn hình ellipse
        FillShape(IsEllipse(0.46f, 0.44f, 0.015f), MapTerrain.Dirt);

        // Rêu xanh ở rìa ngoài ellipse
        AddVariation(MapTerrain.Dirt, MapTerrain.Grass2, 10);

        // Lối đi đá lát theo hình thập tự lớn
        for (int tx = 8; tx < Cols - 8; tx++)
        {
            if (!InPlay(tx, (int)_cy)) continue;
            PaintCell(tx, (int)_cy - 1, MapTerrain.Stone);
            PaintCell(tx, (int)_cy,     MapTerrain.Stone);
            PaintCell(tx, (int)_cy + 1, MapTerrain.Stone);
        }
        for (int ty = 8; ty < Rows - 8; ty++)
        {
            if (!InPlay((int)_cx, ty)) continue;
            PaintCell((int)_cx - 1, ty, MapTerrain.Stone);
            PaintCell((int)_cx,     ty, MapTerrain.Stone);
            PaintCell((int)_cx + 1, ty, MapTerrain.Stone);
        }

        // Thêm đường Path lát sỏi dọc theo lối chéo
        int diagCount = 4;
        for (int d = 0; d < diagCount; d++)
        {
            int sx = (int)(_cx + Math.Cos(d * Math.PI / 2 + Math.PI / 4) * 18);
            int sy = (int)(_cy + Math.Sin(d * Math.PI / 2 + Math.PI / 4) * 18);
            int ex = (int)(_cx + Math.Cos(d * Math.PI / 2 + Math.PI / 4) * (Cols * 0.38f));
            int ey = (int)(_cy + Math.Sin(d * Math.PI / 2 + Math.PI / 4) * (Rows * 0.38f));
            PaintLine(sx, sy, ex, ey, MapTerrain.Path);
        }

        // Mảng Moss tối ở 4 góc vùng chơi
        PaintBlob((int)(_cx - 20), (int)(_cy - 16), 9, MapTerrain.Moss);
        PaintBlob((int)(_cx + 20), (int)(_cy - 16), 9, MapTerrain.Moss);
        PaintBlob((int)(_cx - 20), (int)(_cy + 16), 9, MapTerrain.Moss);
        PaintBlob((int)(_cx + 20), (int)(_cy + 16), 9, MapTerrain.Moss);

        // Rải đá Stone cục bộ tạo cảm giác hoang tàn
        AddVariation(MapTerrain.Dirt, MapTerrain.Stone, 6);
    }

    // =====================================================================
    // MAP 3: THƯ VIỆN CẤM
    // Mê cung phòng WoodFloor chữ thập + phòng phụ Dungeon 4 góc
    // =====================================================================
    private void BuildForbiddenLibrary()
    {
        int corridorH = (int)(Rows * 0.28f);
        int corridorB = (int)(Rows * 0.72f);

        // Hành lang ngang chính rộng hơn
        for (int tx = 3; tx < Cols - 3; tx++)
            for (int ty = corridorH; ty <= corridorB; ty++)
                Terrain[tx, ty] = MapTerrain.WoodFloor;

        // Hành lang dọc chính
        int corrL = (int)(Cols * 0.30f);
        int corrR = (int)(Cols * 0.70f);
        for (int ty = 3; ty < Rows - 3; ty++)
        {
            for (int tx = corrL; tx <= corrR; tx++)
                if (Terrain[tx, ty] == MapTerrain.Void || Terrain[tx, ty] == MapTerrain.Cliff)
                    Terrain[tx, ty] = MapTerrain.WoodFloor;
        }

        // Phòng Dungeon tối ở 4 góc
        CarveRoom(6, 6, corrL - 4, corridorH - 4, MapTerrain.Dungeon);
        CarveRoom(corrR + 4, 6, Cols - 6, corridorH - 4, MapTerrain.Dungeon);
        CarveRoom(6, corridorB + 4, corrL - 4, Rows - 6, MapTerrain.Dungeon);
        CarveRoom(corrR + 4, corridorB + 4, Cols - 6, Rows - 6, MapTerrain.Dungeon);

        // Tường Cliff bao xung quanh
        for (int tx = 0; tx < Cols; tx++)
        {
            for (int ty = 0; ty < Rows; ty++)
            {
                if (Terrain[tx, ty] == MapTerrain.Void)
                    Terrain[tx, ty] = MapTerrain.Cliff;
            }
        }

        // Biến thể sàn gỗ
        AddVariation(MapTerrain.WoodFloor, MapTerrain.Dungeon, 4);
        AddVariation(MapTerrain.Dungeon, MapTerrain.Stone, 5);
    }

    // =====================================================================
    // MAP 4: ĐẦM LẦY ĐỘC
    // Mê cung hữu cơ rêu/Moss — hồ độc lớn rải rác, đảo Dirt, cây khô chặn
    // =====================================================================
    private void BuildPoisonSwamp()
    {
        // Nền rêu Moss hình blob hữu cơ
        FillShape(IsForestBlob(), MapTerrain.Moss);

        // Biến thể tự nhiên
        AddVariation(MapTerrain.Moss, MapTerrain.Dirt,   16);
        AddVariation(MapTerrain.Moss, MapTerrain.Grass2, 10);

        // Nhiều hồ Poison rải đều (lớn hơn, nhiều hơn)
        for (int i = 0; i < 32; i++)
        {
            int px = _rand.Next(12, Cols - 12);
            int py = _rand.Next(12, Rows - 12);
            if (!InPlay(px, py)) continue;
            // Tránh spawn quá gần tâm
            if (Vector2.Distance(new Vector2(px, py), new Vector2(_cx, _cy)) < 12f) continue;
            int r = _rand.Next(3, 7);
            PaintBlob(px, py, r, MapTerrain.Poison);
        }

        // Đảo Dirt nhỏ an toàn ở vài vị trí chiến lược
        PaintBlob((int)(_cx - 22), (int)(_cy + 14),  5, MapTerrain.Dirt);
        PaintBlob((int)(_cx + 24), (int)(_cy - 10),  5, MapTerrain.Dirt);
        PaintBlob((int)(_cx - 12), (int)(_cy - 22),  4, MapTerrain.Dirt);
        PaintBlob((int)(_cx + 14), (int)(_cy + 24),  4, MapTerrain.Dirt);

        // Vùng trung tâm rõ ràng để spawn
        PaintBlob((int)_cx, (int)_cy, 8, MapTerrain.Moss);

        // Đường Dirt nhỏ nối các đảo
        PaintLine((int)_cx, (int)_cy, (int)(_cx - 22), (int)(_cy + 14), MapTerrain.Dirt);
        PaintLine((int)_cx, (int)_cy, (int)(_cx + 24), (int)(_cy - 10), MapTerrain.Dirt);
    }

    // =====================================================================
    // MAP 5: RỪNG SƯƠNG MÙ
    // Rừng rậm hữu cơ — cụm cây khổng lồ 5 vị trí, lối mòn Dirt ngoằn ngoèo
    // =====================================================================
    private void BuildMistyForest()
    {
        // Nền cỏ tối
        for (int tx = 0; tx < Cols; tx++)
            for (int ty = 0; ty < Rows; ty++)
                Terrain[tx, ty] = MapTerrain.Grass2;

        // Biến thể cỏ tự nhiên
        AddVariation(MapTerrain.Grass2, MapTerrain.Grass,  18);
        AddVariation(MapTerrain.Grass2, MapTerrain.Moss,   14);
        AddVariation(MapTerrain.Grass2, MapTerrain.Dirt,   8);

        // Lối mòn Dirt ngoằn ngoèo
        PaintWavyPath(4,        (int)_cy,     Cols - 4, (int)_cy,     amplitude: 16, width: 2, MapTerrain.Dirt);
        PaintWavyPath((int)_cx, 4,            (int)_cx, Rows - 4,     amplitude: 14, width: 2, MapTerrain.Dirt);
        PaintWavyPath(4,        4,            Cols - 4, Rows - 4,     amplitude: 10, width: 1, MapTerrain.Path);
        PaintWavyPath(Cols - 4, 4,            4,        Rows - 4,     amplitude: 10, width: 1, MapTerrain.Path);

        // 5 cụm cây lớn (mọc dày, chặn đường)
        int[][] treeClusters = {
            new[]{ (int)_cx - 24, (int)_cy - 18, 11 },
            new[]{ (int)_cx + 26, (int)_cy - 14, 10 },
            new[]{ (int)_cx - 20, (int)_cy + 20,  9 },
            new[]{ (int)_cx + 18, (int)_cy + 22, 10 },
            new[]{ (int)_cx,      (int)_cy - 28,  8 }
        };
        // Đánh dấu vùng cây là Moss để phân biệt + không để obstacle đặt sai
        foreach (var c in treeClusters)
            PaintBlob(c[0], c[1], c[2], MapTerrain.Moss);

        // Vùng trống sáng ở trung tâm
        PaintBlob((int)_cx, (int)_cy, 9, MapTerrain.Grass);
    }

    // =====================================================================
    // MAP 6: LÂU ĐÀI ĐỔ NÁT
    // Mạng lưới phòng Dungeon + hành lang Stone + gai Spike pattern
    // =====================================================================
    private void BuildRuinedCastle()
    {
        // Nền Dungeon hình chữ nhật bo góc lớn
        FillShape(IsRoundedRect(0.45f, 0.43f, 9), MapTerrain.Dungeon);

        // Hành lang thập tự Stone rộng
        for (int tx = 8; tx < Cols - 8; tx++)
        {
            if (Terrain[tx, (int)_cy] == MapTerrain.Dungeon)
            {
                Terrain[tx, (int)_cy - 1] = MapTerrain.Stone;
                Terrain[tx, (int)_cy]     = MapTerrain.Stone;
                Terrain[tx, (int)_cy + 1] = MapTerrain.Stone;
            }
        }
        for (int ty = 8; ty < Rows - 8; ty++)
        {
            if (Terrain[(int)_cx, ty] == MapTerrain.Dungeon)
            {
                Terrain[(int)_cx - 1, ty] = MapTerrain.Stone;
                Terrain[(int)_cx, ty]     = MapTerrain.Stone;
                Terrain[(int)_cx + 1, ty] = MapTerrain.Stone;
            }
        }

        // Phòng lớn 4 góc (có Path trải sàn)
        int offX = 20, offY = 16;
        CarveRoom((int)_cx - offX - 8, (int)_cy - offY - 6, (int)_cx - offX + 8, (int)_cy - offY + 6, MapTerrain.Path);
        CarveRoom((int)_cx + offX - 8, (int)_cy - offY - 6, (int)_cx + offX + 8, (int)_cy - offY + 6, MapTerrain.Path);
        CarveRoom((int)_cx - offX - 8, (int)_cy + offY - 6, (int)_cx - offX + 8, (int)_cy + offY + 6, MapTerrain.Path);
        CarveRoom((int)_cx + offX - 8, (int)_cy + offY - 6, (int)_cx + offX + 8, (int)_cy + offY + 6, MapTerrain.Path);

        // Gai Spike pattern đều đặn theo grid (không random!)
        // Chỉ đặt gai ở ô Dungeon cách trung tâm đủ xa
        for (int tx = (int)(_cx - 36); tx <= (int)(_cx + 36); tx += 6)
        {
            for (int ty = (int)(_cy - 28); ty <= (int)(_cy + 28); ty += 6)
            {
                if (Terrain[tx, ty] != MapTerrain.Dungeon) continue;
                if (Math.Abs(tx - _cx) < 6 || Math.Abs(ty - _cy) < 6) continue;
                // Cách các phòng góc
                bool nearRoom = (Math.Abs(tx - (_cx - offX)) < 10 && Math.Abs(ty - (_cy - offY)) < 8) ||
                                (Math.Abs(tx - (_cx + offX)) < 10 && Math.Abs(ty - (_cy - offY)) < 8) ||
                                (Math.Abs(tx - (_cx - offX)) < 10 && Math.Abs(ty - (_cy + offY)) < 8) ||
                                (Math.Abs(tx - (_cx + offX)) < 10 && Math.Abs(ty - (_cy + offY)) < 8);
                if (!nearRoom)
                    PaintCell(tx, ty, MapTerrain.Spike);
            }
        }

        // Biến thể gạch
        AddVariation(MapTerrain.Dungeon, MapTerrain.Stone, 6);
    }

    // =====================================================================
    // MAP 7: HẦM MỎ DUNG NHAM
    // Hầm đá zigzag hẹp — sông Lava rộng ngăn, cầu WoodFloor bắc qua
    // =====================================================================
    private void BuildLavaMine()
    {
        int halfW = 11;

        // Hầm đá dọc zigzag
        for (int ty = 4; ty < Rows - 4; ty++)
        {
            // Hầm zigzag: mỗi 12 row dịch chuyển trái/phải
            int shift = ((ty / 12) % 2 == 0) ? 0 : 4;
            int left  = (int)_cx - halfW + shift;
            int right = (int)_cx + halfW + shift;
            left  = Math.Max(4, Math.Min(Cols - 4, left));
            right = Math.Max(4, Math.Min(Cols - 4, right));
            for (int tx = left; tx <= right; tx++)
                Terrain[tx, ty] = MapTerrain.Stone;
        }

        // Sông Lava ngang rộng (mỗi 10 row) — thêm nhiều hơn
        for (int ty = 10; ty < Rows - 10; ty += 10)
        {
            int shift = ((ty / 12) % 2 == 0) ? 0 : 4;
            int left  = (int)_cx - halfW + shift;
            int right = (int)_cx + halfW + shift;
            left  = Math.Max(4, Math.Min(Cols - 4, left));
            right = Math.Max(4, Math.Min(Cols - 4, right));

            int bridgeCenter = (left + right) / 2;
            int bridgeHalfWidth = 2; // cau rong 5 o
            for (int tx = left; tx <= right; tx++)
            {
                // Cầu WoodFloor ở giữa, còn lại là Lava
                if (Math.Abs(tx - bridgeCenter) <= bridgeHalfWidth)
                    Terrain[tx, ty] = MapTerrain.WoodFloor;
                else
                    Terrain[tx, ty] = MapTerrain.Lava;
            }
            // Lava rộng hơn: thêm 1 row trên/dưới.
            // Quan trọng: giu hanh lang cau xuyen suot 3 row de khong bi "khong co loi di qua".
            for (int tx = left; tx <= right; tx++)
            {
                bool isBridgeLane = Math.Abs(tx - bridgeCenter) <= bridgeHalfWidth;
                if (ty - 1 >= 4 && Terrain[tx, ty - 1] == MapTerrain.Stone)
                    Terrain[tx, ty - 1] = isBridgeLane ? MapTerrain.WoodFloor : MapTerrain.Lava;
                if (ty + 1 < Rows - 4 && Terrain[tx, ty + 1] == MapTerrain.Stone)
                    Terrain[tx, ty + 1] = isBridgeLane ? MapTerrain.WoodFloor : MapTerrain.Lava;
            }
        }

        // Path trung tâm dọc hầm để dễ di chuyển
        for (int ty = 6; ty < Rows - 6; ty++)
        {
            int shift = ((ty / 12) % 2 == 0) ? 0 : 4;
            int cx = (int)_cx + shift;
            if (Terrain[cx, ty] == MapTerrain.Stone)
                Terrain[cx, ty] = MapTerrain.Path;
        }
    }

    // =====================================================================
    // MAP 8: SA MẠC CHẾT CHÓC
    // Sa mạc mở — di tích Stone vòng tròn, oasis nước giữa, Dirt path
    // =====================================================================
    private void BuildDeadDesert()
    {
        // Nền cát toàn map
        for (int tx = 0; tx < Cols; tx++)
            for (int ty = 0; ty < Rows; ty++)
                Terrain[tx, ty] = MapTerrain.Sand;

        // Biến thể cát
        AddVariation(MapTerrain.Sand, MapTerrain.Dirt,   12);
        AddVariation(MapTerrain.Sand, MapTerrain.Stone,   5);

        // 4 di tích Stone vòng tròn tại 4 điểm chiến lược
        int[] ruinCX = { (int)(_cx - 26), (int)(_cx + 26), (int)(_cx - 26), (int)(_cx + 26) };
        int[] ruinCY = { (int)(_cy - 20), (int)(_cy - 20), (int)(_cy + 20), (int)(_cy + 20) };
        for (int i = 0; i < 4; i++)
        {
            // Vòng tròn Stone
            PaintRing(ruinCX[i], ruinCY[i], radius: 7, thickness: 2, MapTerrain.Stone);
            // Nền Dirt bên trong
            PaintBlob(ruinCX[i], ruinCY[i], 6, MapTerrain.Dirt);
            // Vòng Stone đè lên
            PaintRing(ruinCX[i], ruinCY[i], radius: 7, thickness: 2, MapTerrain.Stone);
        }

        // Oasis nước giữa map
        PaintBlob((int)_cx, (int)_cy, 8, MapTerrain.Water);
        PaintBlob((int)_cx, (int)_cy, 12, MapTerrain.Moss);  // Rêu xung quanh oasis
        PaintBlob((int)_cx, (int)_cy,  8, MapTerrain.Water); // Đè lại

        // Đường Dirt từ oasis đến các di tích
        for (int i = 0; i < 4; i++)
            PaintLine((int)_cx, (int)_cy, ruinCX[i], ruinCY[i], MapTerrain.Dirt);
    }

    // =====================================================================
    // MAP 9: CẦU TREO VỰC THẲM
    // Cầu WoodFloor rộng 5 cell + platform Stone giữa + vực Void 2 bên
    // =====================================================================
    private void BuildRopeBridge()
    {
        int bridgeY = (int)_cy;

        // Cầu chính rộng 5 cell
        for (int tx = 8; tx < Cols - 8; tx++)
        {
            for (int w = -2; w <= 2; w++)
                Terrain[tx, bridgeY + w] = MapTerrain.WoodFloor;
        }

        // Platform Stone nhỏ tại 4 vị trí dọc cầu (điểm nghỉ)
        int[] platformX = {
            (int)(_cx * 0.35f),
            (int)(_cx * 0.65f),
            (int)(_cx * 1.35f),
            (int)(_cx * 1.65f)
        };
        foreach (int px in platformX)
        {
            for (int tx = px - 5; tx <= px + 5; tx++)
                for (int ty = bridgeY - 5; ty <= bridgeY + 5; ty++)
                    PaintCell(tx, ty, MapTerrain.Stone);
            // Khôi phục cầu trên platform
            for (int tx = px - 5; tx <= px + 5; tx++)
                for (int w = -2; w <= 2; w++)
                    PaintCell(tx, bridgeY + w, MapTerrain.WoodFloor);
        }

        // Đầu cầu bên trái (landing pad)
        CarveRoom(4, bridgeY - 8, 18, bridgeY + 8, MapTerrain.Stone);

        // Đầu cầu bên phải
        CarveRoom(Cols - 18, bridgeY - 8, Cols - 4, bridgeY + 8, MapTerrain.Stone);
    }

    // =====================================================================
    // MAP 10: ĐỈNH NÚI BĂNG GIÁ
    // Arena hoa tuyết lớn — tường băng vòng quanh, Crystal 8 hướng, Snow dày
    // =====================================================================
    private void BuildIcePeak()
    {
        // Arena Ice lớn hình ellipse
        FillShape(IsEllipse(0.47f, 0.47f, 0.01f), MapTerrain.Ice);

        // Snow theo hình hoa tuyết 6 cánh
        int armLen = (int)(Math.Min(Cols, Rows) * 0.30f);
        for (int arm = 0; arm < 6; arm++)
        {
            double angle = arm * Math.PI / 3.0;
            for (int r = 0; r <= armLen; r++)
            {
                int sx = (int)(_cx + Math.Cos(angle) * r);
                int sy = (int)(_cy + Math.Sin(angle) * r);
                PaintBlob(sx, sy, 3, MapTerrain.Snow);
                // Nhánh phụ
                if (r % 6 == 0 && r > 8)
                {
                    for (int branch = -1; branch <= 1; branch += 2)
                    {
                        double ba = angle + branch * Math.PI / 3.0;
                        int blen = armLen / 4;
                        for (int br = 0; br <= blen; br++)
                        {
                            int bx = sx + (int)(Math.Cos(ba) * br);
                            int by = sy + (int)(Math.Sin(ba) * br);
                            PaintBlob(bx, by, 2, MapTerrain.Snow);
                        }
                    }
                }
            }
        }

        // Stone ring rìa arena làm tường
        PaintRing((int)_cx, (int)_cy, radius: (int)(Math.Min(Cols, Rows) * 0.45f), thickness: 3, MapTerrain.Stone);

        // Vùng trung tâm Ice sáng sạch
        PaintBlob((int)_cx, (int)_cy, 10, MapTerrain.Ice);

        // Biến thể tự nhiên
        AddVariation(MapTerrain.Ice, MapTerrain.Snow, 16);
        AddVariation(MapTerrain.Ice, MapTerrain.Stone, 4);
    }

    // =====================================================================
    // OBSTACLE BUILDERS
    // =====================================================================

    private void BuildStageObstacles()
    {
        switch (_stage)
        {
            case 1:  BuildMeadowProps();    break;
            case 2:  BuildCemeteryProps();  break;
            case 3:  BuildLibraryProps();   break;
            case 4:  BuildSwampProps();     break;
            case 5:  BuildForestProps();    break;
            case 6:  BuildCastleProps();    break;
            case 7:  BuildLavaMineProps();  break;
            case 8:  BuildDesertProps();    break;
            case 9:  BuildBridgeProps();    break;
            default: BuildIceProps();       break;
        }
    }

    // MAP 1 — Thảo nguyên: bụi cây dày, đá nhỏ, cổng Torii dọc path, đền Shrine ven hồ
    private void BuildMeadowProps()
    {
        ScatterDecor(55, ObstacleStyle.Bush,      16, 16,  80f, false);
        ScatterDecor(22, ObstacleStyle.RockSmall, 16, 16, 100f, false);
        ScatterBlocking(6, ObstacleStyle.RockLarge, 32, 32, 180f);

        // Cổng Torii dọc theo path trung tâm
        int gateStep = Cols / 5;
        for (int i = 1; i <= 4; i++)
        {
            int gx = i * gateStep * MapTerrain.CellSize;
            int gy = (int)(_cy * MapTerrain.CellSize) - 32;
            AddObstacle(gx, gy, 48, 48, ObstacleStyle.GateTorii, i % 2, false, false, false);
        }

        // Shrine ven 4 hồ nước
        AddObstacle(CellToWorld(20),        CellToWorld(20),        48, 48, ObstacleStyle.Shrine,   0, false, false, false);
        AddObstacle(CellToWorld(Cols - 22), CellToWorld(20),        48, 48, ObstacleStyle.Shrine,   1, false, false, false);
        AddObstacle(CellToWorld(20),        CellToWorld(Rows - 22), 48, 48, ObstacleStyle.Shrine,   0, false, false, false);
        AddObstacle(CellToWorld(Cols - 22), CellToWorld(Rows - 22), 48, 48, ObstacleStyle.Shrine,   1, false, false, false);

        // Cay xanh diem xuyet ria dong co
        for (int i = 0; i < 10; i++)
        {
            int tx = _rand.Next(6, Cols - 6);
            int ty = _rand.Next(6, Rows - 6);
            if (!IsCellWalkable(tx, ty)) continue;
            AddObstacle(CellToWorld(tx), CellToWorld(ty), 32, 48, ObstacleStyle.TreeOak, _rand.Next(3), true, false, true);
        }
    }

    // MAP 2 — Nghĩa trang: bia mộ hàng đều, cây khô, đèn lồng Path, tường DarkBrick
    private void BuildCemeteryProps()
    {
        // Bia mộ xếp hàng theo grid — để lại khoảng trống lách qua
        int tombRows = 5, tombCols = 7;
        float startX = _cx - tombCols * 7;
        float startY = _cy - tombRows * 7;
        for (int r = 0; r < tombRows; r++)
        {
            for (int c = 0; c < tombCols; c++)
            {
                // Hàng 2 bên mở ra khoảng trống lớn hơn ở giữa (để path)
                if (c == tombCols / 2 && r == tombRows / 2) continue; // Tâm trống
                float ox = startX + c * 16;
                float oy = startY + r * 14;
                if (Vector2.Distance(new Vector2(ox, oy), new Vector2(_cx * 32, _cy * 32)) < 80f) continue;
                AddObstacle((int)ox * MapTerrain.CellSize / 32, (int)oy * MapTerrain.CellSize / 32,
                    16, 28, ObstacleStyle.Tombstone, (c + r) % 3, true, true, false);
            }
        }
        ScatterBlocking(14, ObstacleStyle.Tombstone, 16, 28, 100f, passableByFlying: true);
        ScatterBlocking(10, ObstacleStyle.TreeDead,  32, 48, 130f);
        ScatterDecor(12,    ObstacleStyle.Lantern,   18, 28, 80f, false);

        // DarkBrick wall fragments tạo cảm giác tường cổ
        for (int i = 0; i < 8; i++)
        {
            double ang = i * Math.PI / 4.0;
            int wx = (int)(_cx + Math.Cos(ang) * 28) * MapTerrain.CellSize;
            int wy = (int)(_cy + Math.Sin(ang) * 22) * MapTerrain.CellSize;
            AddObstacle(wx, wy, 32, 16, ObstacleStyle.DarkBrick, i % 2, true, false, true);
        }
    }

    // MAP 3 — Thư viện: kệ sách dày hành lang, đèn lồng, rương, thùng
    private void BuildLibraryProps()
    {
        int corridorH = (int)(Rows * 0.28f);
        int corridorB = (int)(Rows * 0.72f);
        int topY  = corridorH * MapTerrain.CellSize - 48;
        int botY  = corridorB * MapTerrain.CellSize + 8;
        int corrL = (int)(Cols * 0.30f) * MapTerrain.CellSize;
        int corrR = (int)(Cols * 0.70f) * MapTerrain.CellSize;

        // Kệ sách 2 hàng dọc theo hành lang ngang (dày)
        for (int x = corrL + 32; x < corrR - 32; x += 40)
        {
            AddObstacle(x, topY,  32, 48, ObstacleStyle.Bookshelf, x / 40 % 3, true, false, true);
            AddObstacle(x, botY,  32, 48, ObstacleStyle.Bookshelf, x / 40 % 3, true, false, true);
        }

        // Kệ sách dọc hành lang dọc
        for (int y = (int)(Rows * 0.28f) * MapTerrain.CellSize + 32;
                 y < (int)(Rows * 0.72f) * MapTerrain.CellSize - 32; y += 56)
        {
            AddObstacle(corrL - 40, y, 32, 48, ObstacleStyle.Bookshelf, y / 56 % 2, true, false, true);
            AddObstacle(corrR + 8,  y, 32, 48, ObstacleStyle.Bookshelf, y / 56 % 2, true, false, true);
        }

        ScatterDecor(14, ObstacleStyle.Lantern, 18, 28,  70f, false);
        ScatterDecor(8,  ObstacleStyle.Crate,   16, 16, 100f, false);
        ScatterDecor(6,  ObstacleStyle.Barrel,  16, 16, 100f, false);

        // Altar giữa hành lang chính
        AddObstacle((int)(_cx * MapTerrain.CellSize) - 24, (int)(_cy * MapTerrain.CellSize) - 60,
            48, 48, ObstacleStyle.Shrine, 0, false, false, false);
    }

    // MAP 4 — Đầm lầy: cây khô nhiều, đá lớn, bụi cây độc, shrine đảo
    private void BuildSwampProps()
    {
        ScatterBlocking(20, ObstacleStyle.TreeDead,  32, 48, 120f);
        ScatterBlocking(16, ObstacleStyle.RockLarge, 32, 32, 110f);
        ScatterDecor(14,    ObstacleStyle.Bush,      16, 16,  80f, false);
        ScatterDecor(6,     ObstacleStyle.RockSmall, 16, 16,  80f, false);

        // Shrine trên đảo Dirt chiến lược
        int[] islandX = { (int)(_cx - 22), (int)(_cx + 24) };
        int[] islandY = { (int)(_cy + 14), (int)(_cy - 10) };
        for (int i = 0; i < 2; i++)
        {
            AddObstacle(CellToWorld(islandX[i]) + 4, CellToWorld(islandY[i]) - 8,
                48, 48, ObstacleStyle.Shrine, i, false, false, false);
        }

        // Crystal nhỏ rải rác tạo điểm nhấn
        ScatterDecor(6, ObstacleStyle.Crystal, 16, 20, 120f, false);
    }

    // MAP 5 — Rừng sương mù: cây Oak/Pine dày, Crystal tâm, bụi, đá
    private void BuildForestProps()
    {
        // Cây lớn tại 5 cụm cây
        int[][] treeClusters = {
            new[]{ (int)_cx - 24, (int)_cy - 18 },
            new[]{ (int)_cx + 26, (int)_cy - 14 },
            new[]{ (int)_cx - 20, (int)_cy + 20 },
            new[]{ (int)_cx + 18, (int)_cy + 22 },
            new[]{ (int)_cx,      (int)_cy - 28 }
        };
        foreach (var c in treeClusters)
        {
            for (int t = 0; t < 6; t++)
            {
                int dx = _rand.Next(-8, 8);
                int dy = _rand.Next(-8, 8);
                bool pine = _rand.Next(3) == 0;
                AddObstacle(CellToWorld(c[0] + dx), CellToWorld(c[1] + dy),
                    pine ? 48 : 32, 48, pine ? ObstacleStyle.TreePine : ObstacleStyle.TreeOak,
                    _rand.Next(3), true, false, true);
            }
        }

        // Cây rải thêm toàn map
        for (int i = 0; i < 12; i++)
        {
            int tx = _rand.Next(6, Cols - 6);
            int ty = _rand.Next(6, Rows - 6);
            bool pine = _rand.Next(2) != 0;
            AddObstacle(CellToWorld(tx), CellToWorld(ty), pine ? 48 : 32, 48,
                pine ? ObstacleStyle.TreePine : ObstacleStyle.TreeOak, _rand.Next(3), true, false, true);
        }

        ScatterDecor(16, ObstacleStyle.Bush,      16, 16, 80f, false);
        ScatterDecor(10, ObstacleStyle.RockSmall, 16, 16, 90f, false);

        // Crystal cluster tại trung tâm (điểm sáng trong sương mù)
        for (int i = 0; i < 6; i++)
        {
            double ang = i * Math.PI / 3.0;
            int cx = (int)(_cx + Math.Cos(ang) * 6);
            int cy = (int)(_cy + Math.Sin(ang) * 6);
            AddObstacle(CellToWorld(cx), CellToWorld(cy), 16, 20, ObstacleStyle.Crystal, i % 4, false, false, false);
        }
    }

    // MAP 6 — Lâu đài đổ nát: tường cường cố, cột đá, tượng, đèn, thùng, rương
    private void BuildCastleProps()
    {
        // Tường fortified destructible theo pattern + hành lang
        for (int i = 0; i < 12; i++)
        {
            double ang = i * Math.PI / 6.0;
            int tx = (int)(_cx + Math.Cos(ang) * 16);
            int ty = (int)(_cy + Math.Sin(ang) * 12);
            if (!IsCellWalkable(tx, ty)) continue;
            var wall = AddObstacle(CellToWorld(tx), CellToWorld(ty), 32, 32,
                ObstacleStyle.WallFortified, i % 2, true, false, true);
            wall?.ConfigureDestructible(40f, i % 2 == 0 ? ObstacleLoot.Gold : ObstacleLoot.Health,
                i % 2 == 0 ? 10 : 20);
        }

        // Cột đá ở 4 phòng góc
        int[] roomCX = { (int)_cx - 20, (int)_cx + 20, (int)_cx - 20, (int)_cx + 20 };
        int[] roomCY = { (int)_cy - 16, (int)_cy - 16, (int)_cy + 16, (int)_cy + 16 };
        for (int i = 0; i < 4; i++)
        {
            AddObstacle(CellToWorld(roomCX[i] - 4), CellToWorld(roomCY[i] - 3),
                16, 44, ObstacleStyle.PillarStone, i, true, false, true);
            AddObstacle(CellToWorld(roomCX[i] + 4), CellToWorld(roomCY[i] - 3),
                16, 44, ObstacleStyle.PillarStone, i, true, false, true);
            AddObstacle(CellToWorld(roomCX[i]), CellToWorld(roomCY[i] + 2),
                32, 48, ObstacleStyle.Statue, i % 2, false, false, false);
        }

        ScatterDecor(12, ObstacleStyle.Lantern, 18, 28,  70f, false);
        ScatterBlocking(10, ObstacleStyle.Barrel, 16, 16, 100f);
        ScatterBlocking(8,  ObstacleStyle.Crate,  16, 16, 100f);
    }

    // MAP 7 — Hầm lava: đá lớn chặn hầm, thùng, đèn, WoodPlank
    private void BuildLavaMineProps()
    {
        ScatterBlocking(12, ObstacleStyle.RockLarge,  32, 32, 80f);
        ScatterDecor(8,     ObstacleStyle.Barrel,     16, 16, 60f, false);
        ScatterDecor(8,     ObstacleStyle.Lantern,    18, 28, 60f, false);
        ScatterDecor(6,     ObstacleStyle.RockSmall,  16, 16, 50f, false);
        ScatterDecor(5,     ObstacleStyle.Crystal,    16, 20, 90f, false);

        // WoodPlank dọc theo path hầm tạo cảm giác sàn gia cố
        for (int i = 0; i < 8; i++)
        {
            int ty = 18 + i * 12;
            if (ty >= Rows - 10) break;
            int shift = ((ty / 12) % 2 == 0) ? 0 : 4;
            int tx = (int)_cx + shift + 4;
            if (IsCellWalkable(tx, ty))
                AddObstacle(CellToWorld(tx), CellToWorld(ty), 32, 16, ObstacleStyle.WoodPlank, i % 2, false, false, false);
        }
    }

    // MAP 8 — Sa mạc: xương rồng dày, cột sa mạc, đá lớn, shrine oasis
    private void BuildDesertProps()
    {
        ScatterBlocking(26, ObstacleStyle.Cactus,        24, 24, 100f);
        ScatterDecor(12,    ObstacleStyle.RockSmall,     16, 16,  80f, false);
        ScatterBlocking(8,  ObstacleStyle.RockLarge,     32, 32, 140f);

        // Cay co quanh oasis trung tam
        for (int i = 0; i < 6; i++)
        {
            double ang = i * Math.PI / 3.0 + 0.4;
            int tx = (int)(_cx + Math.Cos(ang) * 14);
            int ty = (int)(_cy + Math.Sin(ang) * 12);
            if (IsCellWalkable(tx, ty))
                AddObstacle(CellToWorld(tx), CellToWorld(ty) - 16, 36, 48,
                    ObstacleStyle.DesertColumn, i % 2, true, false, true);
        }

        // Shrine oasis tại trung tâm (ven hồ)
        AddObstacle((int)(_cx * MapTerrain.CellSize) - 64, (int)(_cy * MapTerrain.CellSize) - 64,
            48, 48, ObstacleStyle.Shrine, 0, false, false, false);

        // Cay co tại mỗi di tích vòng tròn
        int[] ruinCX = { (int)(_cx - 26), (int)(_cx + 26), (int)(_cx - 26), (int)(_cx + 26) };
        int[] ruinCY = { (int)(_cy - 20), (int)(_cy - 20), (int)(_cy + 20), (int)(_cy + 20) };
        for (int i = 0; i < 4; i++)
        {
            if (IsCellWalkable(ruinCX[i], ruinCY[i]))
                AddObstacle(CellToWorld(ruinCX[i]) - 8, CellToWorld(ruinCY[i]) - 16,
                    36, 48, ObstacleStyle.DesertColumn, i % 2, false, false, false);
        }
    }

    // MAP 9 — Cầu treo: rào FenceWood 2 bên, thùng destructible, pillar cầu
    private void BuildBridgeProps()
    {
        int bridgeY = (int)_cy;

        // Lan can FenceWood 2 bên cầu — destructible
        for (int tx = 10; tx < Cols - 10; tx += 4)
        {
            int wy = (bridgeY - 3) * MapTerrain.CellSize;
            var fenceTop = AddObstacle(CellToWorld(tx), wy, 32, 16,
                ObstacleStyle.FenceWood, tx % 2, true, false, false);
            fenceTop?.ConfigureDestructible(20f, ObstacleLoot.Gold, 5);

            wy = (bridgeY + 3) * MapTerrain.CellSize;
            var fenceBot = AddObstacle(CellToWorld(tx), wy, 32, 16,
                ObstacleStyle.FenceWood, tx % 2, true, false, false);
            fenceBot?.ConfigureDestructible(20f, ObstacleLoot.Gold, 5);
        }

        // Thùng dọc cầu — destructible loot
        for (int i = 0; i < 12; i++)
        {
            int tx = 14 + i * 12;
            if (tx >= Cols - 12) break;
            int bx = CellToWorld(tx);
            int by = CellToWorld(bridgeY) - 8;
            var barrel = AddObstacle(bx, by, 24, 24, ObstacleStyle.Barrel, i % 3, true, false, false);
            barrel?.ConfigureDestructible(20f, i % 3 == 0 ? ObstacleLoot.Health : ObstacleLoot.Gold,
                i % 3 == 0 ? 25 : 8);
        }

        // BridgeWood pillar tại 4 platform Stone
        int[] platformX = {
            (int)(_cx * 0.35f),
            (int)(_cx * 0.65f),
            (int)(_cx * 1.35f),
            (int)(_cx * 1.65f)
        };
        foreach (int px in platformX)
        {
            AddObstacle(CellToWorld(px) - 16, (bridgeY - 5) * MapTerrain.CellSize, 32, 48,
                ObstacleStyle.BridgeWood, 0, false, false, false);
            AddObstacle(CellToWorld(px) - 16, (bridgeY + 5) * MapTerrain.CellSize, 32, 48,
                ObstacleStyle.BridgeWood, 1, false, false, false);
        }
    }

    // MAP 10 — Đỉnh băng: IceBoulder nhẫn kép, Crystal 8 hướng, PillarStone vòng
    private void BuildIceProps()
    {
        // Vòng nhẫn IceBoulder lớn bao quanh tâm
        for (int i = 0; i < 12; i++)
        {
            double ang = i * Math.PI / 6.0;
            int tx = (int)(_cx + Math.Cos(ang) * 10);
            int ty = (int)(_cy + Math.Sin(ang) * 10);
            AddObstacle(CellToWorld(tx), CellToWorld(ty), 48, 48, ObstacleStyle.IceBoulder, i % 3, true, false, true);
        }

        // Crystal tại 8 hướng — điểm nhấn lấp lánh
        for (int i = 0; i < 8; i++)
        {
            double ang = i * Math.PI / 4.0;
            int tx = (int)(_cx + Math.Cos(ang) * 18);
            int ty = (int)(_cy + Math.Sin(ang) * 18);
            AddObstacle(CellToWorld(tx), CellToWorld(ty), 16, 20, ObstacleStyle.Crystal, i % 4, false, false, false);
        }

        // PillarStone tại vòng ngoài
        for (int i = 0; i < 8; i++)
        {
            double ang = i * Math.PI / 4.0 + Math.PI / 8.0;
            int tx = (int)(_cx + Math.Cos(ang) * 26);
            int ty = (int)(_cy + Math.Sin(ang) * 26);
            if (IsCellWalkable(tx, ty))
                AddObstacle(CellToWorld(tx), CellToWorld(ty), 16, 44, ObstacleStyle.PillarStone, i % 2, true, false, true);
        }

        // IceBoulder rải ngẫu nhiên thêm
        ScatterBlocking(8, ObstacleStyle.IceBoulder, 32, 32, 200f);

        // Shrine giữa — đài bang huyền bí
        AddObstacle((int)(_cx * MapTerrain.CellSize) - 24, (int)(_cy * MapTerrain.CellSize) - 72,
            48, 48, ObstacleStyle.Shrine, 0, false, false, false);
    }

    // =====================================================================
    // SHAPE HELPERS
    // =====================================================================

    private void FillShape(Func<int, int, bool> shape, byte fill)
    {
        for (int tx = 0; tx < Cols; tx++)
            for (int ty = 0; ty < Rows; ty++)
                if (shape(tx, ty)) Terrain[tx, ty] = fill;
    }

    private bool InPlay(int tx, int ty) =>
        tx >= 0 && ty >= 0 && tx < Cols && ty < Rows && Terrain[tx, ty] != MapTerrain.Void;

    private bool IsCellWalkable(int tx, int ty)
    {
        if (tx < 0 || ty < 0 || tx >= Cols || ty >= Rows) return false;
        return MapTerrain.IsWalkable(Terrain[tx, ty]);
    }

    private Func<int, int, bool> IsEllipse(float rx, float ry, float noiseAmp) => (tx, ty) =>
    {
        float dx = (tx - _cx) / (Cols * rx);
        float dy = (ty - _cy) / (Rows * ry);
        float n = (MapTerrain.Hash(tx, ty, _stage) % 15 - 7) * noiseAmp;
        return dx * dx + dy * dy <= 1f + n;
    };

    private Func<int, int, bool> IsForestBlob() => (tx, ty) =>
    {
        float d1 = Vector2.Distance(new Vector2(tx, ty), new Vector2(_cx, _cy)) / (Cols * 0.35f);
        float d2 = Vector2.Distance(new Vector2(tx, ty), new Vector2(_cx - 16, _cy + 8)) / (Cols * 0.23f);
        float d3 = Vector2.Distance(new Vector2(tx, ty), new Vector2(_cx + 18, _cy - 10)) / (Cols * 0.25f);
        float d4 = Vector2.Distance(new Vector2(tx, ty), new Vector2(_cx - 8, _cy - 22)) / (Cols * 0.20f);
        float n  = (MapTerrain.Hash(tx, ty, _stage) % 11 - 5) * 0.012f;
        return Math.Min(Math.Min(d1, d2), Math.Min(d3, d4)) <= 1f + n;
    };

    private Func<int, int, bool> IsRoundedRect(float rx, float ry, float corner) => (tx, ty) =>
    {
        float left   = Cols * (0.5f - rx);
        float right  = Cols * (0.5f + rx);
        float top    = Rows * (0.5f - ry);
        float bottom = Rows * (0.5f + ry);
        if (tx < left  + corner && ty < top    + corner) return Vector2.Distance(new Vector2(tx, ty), new Vector2(left  + corner, top    + corner)) <= corner;
        if (tx > right - corner && ty < top    + corner) return Vector2.Distance(new Vector2(tx, ty), new Vector2(right - corner, top    + corner)) <= corner;
        if (tx < left  + corner && ty > bottom - corner) return Vector2.Distance(new Vector2(tx, ty), new Vector2(left  + corner, bottom - corner)) <= corner;
        if (tx > right - corner && ty > bottom - corner) return Vector2.Distance(new Vector2(tx, ty), new Vector2(right - corner, bottom - corner)) <= corner;
        return tx >= left && tx <= right && ty >= top && ty <= bottom;
    };

    private void CarveRoomGrid(int roomsX, int roomsY, int roomW, int roomH, int wallThick)
    {
        int startX = (int)(_cx - (roomsX * (roomW + wallThick)) / 2f);
        int startY = (int)(_cy - (roomsY * (roomH + wallThick)) / 2f);

        for (int rx = 0; rx < roomsX; rx++)
        {
            for (int ry = 0; ry < roomsY; ry++)
            {
                int ox = startX + rx * (roomW + wallThick);
                int oy = startY + ry * (roomH + wallThick);

                for (int wx = 0; wx < roomW + wallThick; wx++)
                    for (int wy = 0; wy < wallThick; wy++)
                    {
                        PaintCell(ox + wx, oy - wy, MapTerrain.Cliff);
                        PaintCell(ox + wx, oy + roomH + wy, MapTerrain.Cliff);
                    }
                for (int wy = 0; wy < roomH; wy++)
                    for (int wx = 0; wx < wallThick; wx++)
                    {
                        PaintCell(ox - wx, oy + wy, MapTerrain.Cliff);
                        PaintCell(ox + roomW + wx, oy + wy, MapTerrain.Cliff);
                    }

                for (int ix = 1; ix < roomW; ix++)
                    for (int iy = 1; iy < roomH; iy++)
                        PaintCell(ox + ix, oy + iy, MapTerrain.Dungeon);
            }
        }
    }

    // Vẽ phòng hình chữ nhật (cell coords)
    private void CarveRoom(int x0, int y0, int x1, int y1, byte fill)
    {
        for (int tx = x0; tx <= x1; tx++)
            for (int ty = y0; ty <= y1; ty++)
                PaintCell(tx, ty, fill);
    }

    // Vẽ vòng tròn rỗng (ring)
    private void PaintRing(int cx, int cy, int radius, int thickness, byte terrain)
    {
        for (int tx = cx - radius - thickness; tx <= cx + radius + thickness; tx++)
        {
            for (int ty = cy - radius - thickness; ty <= cy + radius + thickness; ty++)
            {
                float d = Vector2.Distance(new Vector2(tx, ty), new Vector2(cx, cy));
                if (d >= radius - thickness && d <= radius + thickness)
                    PaintCell(tx, ty, terrain);
            }
        }
    }

    // Vẽ đường thẳng Bresenham
    private void PaintLine(int x0, int y0, int x1, int y1, byte terrain)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        while (true)
        {
            PaintCell(x0, y0, terrain);
            if (x0 == x1 && y0 == y1) break;
            int e2 = err * 2;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 <  dx) { err += dx; y0 += sy; }
        }
    }

    // Vẽ path uốn lượn (wavy) với width cell
    private void PaintWavyPath(int x0, int y0, int x1, int y1, int amplitude, int width, byte terrain)
    {
        int steps = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
        if (steps == 0) return;
        bool horizontal = Math.Abs(x1 - x0) >= Math.Abs(y1 - y0);

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            float wave = (float)(amplitude * Math.Sin(t * Math.PI * 3.0));
            int tx, ty;
            if (horizontal)
            {
                tx = (int)MathHelper.Lerp(x0, x1, t);
                ty = (int)(MathHelper.Lerp(y0, y1, t) + wave);
            }
            else
            {
                tx = (int)(MathHelper.Lerp(x0, x1, t) + wave);
                ty = (int)MathHelper.Lerp(y0, y1, t);
            }
            for (int w = -width; w <= width; w++)
                PaintCell(horizontal ? tx : tx + w, horizontal ? ty + w : ty, terrain);
        }
    }

    private bool IsVoidAt(int tx, int ty)
    {
        if (tx < 0 || ty < 0 || tx >= Cols || ty >= Rows) return true;
        return Terrain[tx, ty] == MapTerrain.Void;
    }

    private void ApplyCliffBorders()
    {
        var copy = (byte[,])Terrain.Clone();
        for (int tx = 0; tx < Cols; tx++)
        {
            for (int ty = 0; ty < Rows; ty++)
            {
                byte c = copy[tx, ty];
                if (!MapTerrain.IsWalkable(c)) continue;

                bool nearVoid = IsVoidAt(tx - 1, ty) || IsVoidAt(tx + 1, ty) ||
                                IsVoidAt(tx, ty - 1) || IsVoidAt(tx, ty + 1);
                if (nearVoid)
                    Terrain[tx, ty] = MapTerrain.Cliff;
            }
        }
    }

    private void ClearSpawnArea()
    {
        byte floor = GetBaseTerrain(_stage);
        int radius = _stage == 9 ? 4 : 7;
        PaintBlob((int)_cx, (int)_cy, radius, floor);

        if (_stage == 3)
        {
            int corridorTop    = (int)(Rows * 0.28f);
            int corridorBottom = (int)(Rows * 0.72f);
            for (int tx = 8; tx < Cols - 8; tx++)
                for (int ty = corridorTop + 2; ty <= corridorBottom - 2; ty++)
                    if (Math.Abs(ty - _cy) <= 3)
                        Terrain[tx, ty] = MapTerrain.WoodFloor;
        }
        else if (_stage != 9)
        {
            for (int i = -10; i <= 10; i++)
            {
                PaintCell((int)_cx + i, (int)_cy, floor);
                PaintCell((int)_cx, (int)_cy + i, floor);
            }
        }
        else
        {
            for (int tx = (int)_cx - 8; tx <= (int)_cx + 8; tx++)
                for (int w = -2; w <= 2; w++)
                    PaintCell(tx, (int)_cy + w, MapTerrain.WoodFloor);
        }
    }

    private byte GetBaseTerrain(int stage) => stage switch
    {
        2  => MapTerrain.Dirt,
        3  => MapTerrain.WoodFloor,
        4  => MapTerrain.Moss,
        5  => MapTerrain.Grass2,
        6 or 7 => MapTerrain.Stone,
        8  => MapTerrain.Sand,
        9  => MapTerrain.WoodFloor,
        10 => MapTerrain.Ice,
        _  => MapTerrain.Grass
    };

    private void AddVariation(byte from, byte to, int chance)
    {
        for (int tx = 0; tx < Cols; tx++)
            for (int ty = 0; ty < Rows; ty++)
                if (Terrain[tx, ty] == from && MapTerrain.Hash(tx, ty, _stage + 50) % 100 < chance)
                    Terrain[tx, ty] = to;
    }

    private void PaintCell(int tx, int ty, byte terrain)
    {
        if (tx < 0 || ty < 0 || tx >= Cols || ty >= Rows) return;
        Terrain[tx, ty] = terrain;
    }

    private void PaintBlob(int cx, int cy, int radius, byte terrain)
    {
        for (int tx = cx - radius; tx <= cx + radius; tx++)
            for (int ty = cy - radius; ty <= cy + radius; ty++)
            {
                if (tx < 0 || ty < 0 || tx >= Cols || ty >= Rows) continue;
                float d = Vector2.Distance(new Vector2(tx, ty), new Vector2(cx, cy));
                if (d <= radius + (MapTerrain.Hash(tx, ty, 99) % 3) * 0.35f)
                    Terrain[tx, ty] = terrain;
            }
    }

    private int CellToWorld(int cell) => cell * MapTerrain.CellSize;

    private Obstacle AddObstacle(int ox, int oy, int w, int h, ObstacleStyle style, int variant,
        bool blocks, bool passableByFlying, bool blocksProjectiles)
    {
        if (blocks && !IsCellWalkable(ox / MapTerrain.CellSize, oy / MapTerrain.CellSize) &&
            _stage != 3 && _stage != 9)
            return null;

        if (blocks && Vector2.Distance(new Vector2(ox, oy), new Vector2(_cx * 32, _cy * 32)) < 130f)
            return null;

        var candidate = new Rectangle(ox, oy, w, h);
        foreach (var o in Obstacles)
            if (!o.IsDestroyed && candidate.Intersects(o.Bounds)) return null;

        var obs = new Obstacle(new Vector2(ox, oy), w, h, style, variant, blocks)
        {
            PassableByFlying = passableByFlying,
            BlocksProjectiles = blocksProjectiles
        };
        Obstacles.Add(obs);
        return obs;
    }

    private void ScatterBlocking(int count, ObstacleStyle style, int w, int h, float minCenterDist, bool passableByFlying = false)
    {
        int placed = 0, attempts = 0;
        while (placed < count && attempts < count * 30)
        {
            attempts++;
            int tx = _rand.Next(4, Cols - 4);
            int ty = _rand.Next(4, Rows - 4);
            if (!IsCellWalkable(tx, ty)) continue;
            int ox = CellToWorld(tx);
            int oy = CellToWorld(ty);
            if (Vector2.Distance(new Vector2(ox, oy), new Vector2(_cx * 32, _cy * 32)) < minCenterDist) continue;
            if (AddObstacle(ox, oy, w, h, style, _rand.Next(3), true, passableByFlying, style != ObstacleStyle.Bush) != null)
                placed++;
        }
    }

    private void ScatterDecor(int count, ObstacleStyle style, int w, int h, float minCenterDist, bool blocks)
    {
        int placed = 0, attempts = 0;
        while (placed < count && attempts < count * 25)
        {
            attempts++;
            int tx = _rand.Next(2, Cols - 2);
            int ty = _rand.Next(2, Rows - 2);
            if (_stage != 1 && _stage != 5 && _stage != 8 && !IsCellWalkable(tx, ty)) continue;
            int ox = CellToWorld(tx);
            int oy = CellToWorld(ty);
            if (Vector2.Distance(new Vector2(ox, oy), new Vector2(_cx * 32, _cy * 32)) < minCenterDist) continue;
            if (AddObstacle(ox, oy, w, h, style, _rand.Next(3), blocks, false, false) != null)
                placed++;
        }
    }
}

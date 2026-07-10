using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace MyGame;

/// <summary>
/// Normalized SQL schema (no JSON blob on GameUsers):
/// - GameUsers: authentication only
/// - UserProgress: gold, permanent upgrades, consumables, selected skin, records summary
/// - UserSettings: display / audio / gameplay preferences (one column per setting)
/// - UserUnlockedSkins: one row per unlocked skin
/// - UserEquipment: owned + equipped gear
/// - UserStageRecords: per-stage high scores
/// - UserShopRolls: current equipment shop rotation
/// </summary>
public static class DatabaseManager
{
    private const string ConnectionString = "Server=db59140.public.databaseasp.net; Database=db59140; User Id=db59140; Password=4k@Z!Wz8p-5X; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

    public static async Task InitializeDatabaseAsync()
    {
        try
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string createTablesSql = @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GameUsers]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[GameUsers](
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [Username] NVARCHAR(50) NOT NULL UNIQUE,
                        [PasswordHash] NVARCHAR(256) NOT NULL,
                        [CreatedAt] DATETIME DEFAULT GETDATE(),
                        [UpdatedAt] DATETIME DEFAULT GETDATE()
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserProgress]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[UserProgress](
                        [Username] NVARCHAR(50) PRIMARY KEY REFERENCES [dbo].[GameUsers]([Username]) ON DELETE CASCADE,
                        [Gold] INT DEFAULT 0,
                        [MaxHPUpgradeLevel] INT DEFAULT 0,
                        [DamageUpgradeLevel] INT DEFAULT 0,
                        [SpeedUpgradeLevel] INT DEFAULT 0,
                        [ArmorUpgradeLevel] INT DEFAULT 0,
                        [MagnetUpgradeLevel] INT DEFAULT 0,
                        [CooldownUpgradeLevel] INT DEFAULT 0,
                        [GrowthUpgradeLevel] INT DEFAULT 0,
                        [GreedUpgradeLevel] INT DEFAULT 0,
                        [LuckUpgradeLevel] INT DEFAULT 0,
                        [RegenUpgradeLevel] INT DEFAULT 0,
                        [AreaUpgradeLevel] INT DEFAULT 0,
                        [DurationUpgradeLevel] INT DEFAULT 0,
                        [ProjSpeedUpgradeLevel] INT DEFAULT 0,
                        [ReviveCount] INT DEFAULT 0,
                        [StartingPotions] INT DEFAULT 0,
                        [GoldBoosterActive] BIT DEFAULT 0,
                        [SelectedSkin] NVARCHAR(50) DEFAULT 'Knight',
                        [BestSurvivalTime] FLOAT DEFAULT 0,
                        [LastSelectedStage] INT DEFAULT 1,
                        [LastShopRefresh] DATETIME DEFAULT '1970-01-01'
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserSettings]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[UserSettings](
                        [Username] NVARCHAR(50) PRIMARY KEY REFERENCES [dbo].[GameUsers]([Username]) ON DELETE CASCADE,
                        [ResolutionIndex] INT DEFAULT 2,
                        [Fullscreen] BIT DEFAULT 0,
                        [EffectsQuality] INT DEFAULT 1,
                        [Vsync] BIT DEFAULT 1,
                        [FpsCap] INT DEFAULT 2,
                        [MasterVolume] INT DEFAULT 8,
                        [MusicVolume] INT DEFAULT 6,
                        [SfxVolume] INT DEFAULT 8,
                        [ShowFps] BIT DEFAULT 0,
                        [ShowMinimap] BIT DEFAULT 1,
                        [UiScale] INT DEFAULT 1,
                        [Difficulty] INT DEFAULT 0,
                        [ScreenShake] BIT DEFAULT 1
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserUnlockedSkins]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[UserUnlockedSkins](
                        [Username] NVARCHAR(50) NOT NULL REFERENCES [dbo].[GameUsers]([Username]) ON DELETE CASCADE,
                        [SkinCode] NVARCHAR(50) NOT NULL,
                        CONSTRAINT [PK_UserUnlockedSkins] PRIMARY KEY ([Username], [SkinCode])
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserEquipment]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[UserEquipment](
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [Username] NVARCHAR(50) NOT NULL REFERENCES [dbo].[GameUsers]([Username]) ON DELETE CASCADE,
                        [EquipmentId] NVARCHAR(50) NOT NULL,
                        [IsEquipped] BIT DEFAULT 0,
                        [EquippedSlot] NVARCHAR(50) NULL
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserStageRecords]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[UserStageRecords](
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [Username] NVARCHAR(50) NOT NULL REFERENCES [dbo].[GameUsers]([Username]) ON DELETE CASCADE,
                        [StageIndex] INT NOT NULL,
                        [BestSurvivalTime] FLOAT DEFAULT 0,
                        [BestKills] INT DEFAULT 0,
                        [BestGold] INT DEFAULT 0,
                        CONSTRAINT [UQ_UserStageRecords_UserStage] UNIQUE ([Username], [StageIndex])
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserShopRolls]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[UserShopRolls](
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [Username] NVARCHAR(50) NOT NULL REFERENCES [dbo].[GameUsers]([Username]) ON DELETE CASCADE,
                        [EquipmentId] NVARCHAR(50) NOT NULL,
                        [SlotIndex] INT NOT NULL,
                        CONSTRAINT [UQ_UserShopRolls_UserSlot] UNIQUE ([Username], [SlotIndex])
                    );
                END;";

            using (var cmd = new SqlCommand(createTablesSql, conn))
                await cmd.ExecuteNonQueryAsync();

            await MigrateSettingsFromUserProgressAsync(conn);
            await EnsureDefaultUnlockedSkinsAsync(conn);
            await MigrateLegacyGameUsersSaveDataColumnAsync(conn);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database Init Error: {ex.Message}");
            throw;
        }
    }

    private static async Task MigrateSettingsFromUserProgressAsync(SqlConnection conn)
    {
        const string migrateSql = @"
            IF EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'UserProgress' AND COLUMN_NAME = 'ResolutionIndex'
            )
            INSERT INTO [dbo].[UserSettings] (
                [Username], [ResolutionIndex], [Fullscreen], [EffectsQuality], [Vsync], [FpsCap],
                [MasterVolume], [MusicVolume], [SfxVolume], [ShowFps], [ShowMinimap], [UiScale], [Difficulty], [ScreenShake]
            )
            SELECT
                p.[Username], p.[ResolutionIndex], p.[Fullscreen], p.[EffectsQuality], p.[Vsync], p.[FpsCap],
                p.[MasterVolume], p.[MusicVolume], p.[SfxVolume], p.[ShowFps], p.[ShowMinimap], p.[UiScale], p.[Difficulty], p.[ScreenShake]
            FROM [dbo].[UserProgress] p
            WHERE NOT EXISTS (SELECT 1 FROM [dbo].[UserSettings] s WHERE s.[Username] = p.[Username]);";

        using var cmd = new SqlCommand(migrateSql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task EnsureDefaultUnlockedSkinsAsync(SqlConnection conn)
    {
        const string sql = @"
            INSERT INTO [dbo].[UserUnlockedSkins] ([Username], [SkinCode])
            SELECT u.[Username], 'Knight'
            FROM [dbo].[GameUsers] u
            WHERE NOT EXISTS (
                SELECT 1 FROM [dbo].[UserUnlockedSkins] s WHERE s.[Username] = u.[Username]
            );";

        using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task MigrateLegacyGameUsersSaveDataColumnAsync(SqlConnection conn)
    {
        const string checkColSql = @"
            SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'GameUsers' AND COLUMN_NAME = 'SaveData';";

        using (var checkCmd = new SqlCommand(checkColSql, conn))
        {
            int hasColumn = (int)await checkCmd.ExecuteScalarAsync();
            if (hasColumn == 0) return;
        }

        const string selectSql = "SELECT [Username], [SaveData] FROM [dbo].[GameUsers] WHERE [SaveData] IS NOT NULL AND LEN([SaveData]) > 2;";
        var rows = new List<(string Username, string Json)>();

        using (var selectCmd = new SqlCommand(selectSql, conn))
        using (var reader = await selectCmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                rows.Add((reader.GetString(0), reader.GetString(1)));
        }

        foreach (var (username, json) in rows)
        {
            try
            {
                var sd = JsonSerializer.Deserialize<SaveData>(json);
                if (sd != null)
                    await SaveUserDataAsync(username, sd);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Legacy SaveData migrate failed for {username}: {ex.Message}");
            }
        }

        const string dropColSql = @"
            IF EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'GameUsers' AND COLUMN_NAME = 'SaveData'
            )
            ALTER TABLE [dbo].[GameUsers] DROP COLUMN [SaveData];";

        using var dropCmd = new SqlCommand(dropColSql, conn);
        await dropCmd.ExecuteNonQueryAsync();
    }

    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var sb = new StringBuilder();
        foreach (byte b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    public static async Task<string> RegisterUserAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            return "Ten dang nhap phai dai it nhat 3 ky tu.";
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return "Mat khau phai dai it nhat 6 ky tu.";

        try
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string checkSql = "SELECT COUNT(1) FROM [dbo].[GameUsers] WHERE [Username] = @Username";
            using (var checkCmd = new SqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@Username", username);
                if ((int)await checkCmd.ExecuteScalarAsync() > 0)
                    return "Ten dang nhap da ton tai.";
            }

            string hash = HashPassword(password);
            using (var insertCmd = new SqlCommand(
                "INSERT INTO [dbo].[GameUsers] ([Username], [PasswordHash]) VALUES (@Username, @PasswordHash)", conn))
            {
                insertCmd.Parameters.AddWithValue("@Username", username);
                insertCmd.Parameters.AddWithValue("@PasswordHash", hash);
                await insertCmd.ExecuteNonQueryAsync();
            }

            using (var progCmd = new SqlCommand("INSERT INTO [dbo].[UserProgress] ([Username]) VALUES (@Username)", conn))
            {
                progCmd.Parameters.AddWithValue("@Username", username);
                await progCmd.ExecuteNonQueryAsync();
            }

            using (var settingsCmd = new SqlCommand("INSERT INTO [dbo].[UserSettings] ([Username]) VALUES (@Username)", conn))
            {
                settingsCmd.Parameters.AddWithValue("@Username", username);
                await settingsCmd.ExecuteNonQueryAsync();
            }

            using (var skinCmd = new SqlCommand(
                "INSERT INTO [dbo].[UserUnlockedSkins] ([Username], [SkinCode]) VALUES (@Username, 'Knight')", conn))
            {
                skinCmd.Parameters.AddWithValue("@Username", username);
                await skinCmd.ExecuteNonQueryAsync();
            }

            return null;
        }
        catch (Exception ex)
        {
            return $"Loi ket noi co so du lieu: {ex.Message}";
        }
    }

    public static async Task<(bool Success, SaveData SaveData, string ErrorMessage)> LoginUserAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, null, "Vui long nhap day du thong tin.");

        try
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string hash = HashPassword(password);
            using (var loginCmd = new SqlCommand(
                "SELECT COUNT(1) FROM [dbo].[GameUsers] WHERE [Username] = @Username AND [PasswordHash] = @PasswordHash", conn))
            {
                loginCmd.Parameters.AddWithValue("@Username", username);
                loginCmd.Parameters.AddWithValue("@PasswordHash", hash);
                if ((int)await loginCmd.ExecuteScalarAsync() == 0)
                    return (false, null, "Sai ten dang nhap hoac mat khau.");
            }

            var sd = new SaveData { LoggedInUsername = username };
            await EnsureUserRowsExistAsync(conn, username);
            await LoadProgressAsync(conn, username, sd);
            await LoadSettingsAsync(conn, username, sd);
            await LoadUnlockedSkinsAsync(conn, username, sd);
            await LoadEquipmentAsync(conn, username, sd);
            await LoadStageRecordsAsync(conn, username, sd);
            await LoadShopRollsAsync(conn, username, sd);

            return (true, sd, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Loi ket noi: {ex.Message}");
        }
    }

    private static async Task EnsureUserRowsExistAsync(SqlConnection conn, string username)
    {
        const string sql = @"
            IF NOT EXISTS (SELECT 1 FROM [dbo].[UserProgress] WHERE [Username] = @Username)
                INSERT INTO [dbo].[UserProgress] ([Username]) VALUES (@Username);
            IF NOT EXISTS (SELECT 1 FROM [dbo].[UserSettings] WHERE [Username] = @Username)
                INSERT INTO [dbo].[UserSettings] ([Username]) VALUES (@Username);
            IF NOT EXISTS (SELECT 1 FROM [dbo].[UserUnlockedSkins] WHERE [Username] = @Username)
                INSERT INTO [dbo].[UserUnlockedSkins] ([Username], [SkinCode]) VALUES (@Username, 'Knight');";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Username", username);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task LoadProgressAsync(SqlConnection conn, string username, SaveData sd)
    {
        const string sql = @"
            SELECT [Gold], [MaxHPUpgradeLevel], [DamageUpgradeLevel], [SpeedUpgradeLevel], [ArmorUpgradeLevel],
                   [MagnetUpgradeLevel], [CooldownUpgradeLevel], [GrowthUpgradeLevel], [GreedUpgradeLevel], [LuckUpgradeLevel],
                   [RegenUpgradeLevel], [AreaUpgradeLevel], [DurationUpgradeLevel], [ProjSpeedUpgradeLevel],
                   [ReviveCount], [StartingPotions], [GoldBoosterActive], [SelectedSkin],
                   [BestSurvivalTime], [LastSelectedStage], [LastShopRefresh]
            FROM [dbo].[UserProgress] WHERE [Username] = @Username";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Username", username);
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return;

        sd.Gold = reader.GetInt32(0);
        sd.MaxHPUpgradeLevel = reader.GetInt32(1);
        sd.DamageUpgradeLevel = reader.GetInt32(2);
        sd.SpeedUpgradeLevel = reader.GetInt32(3);
        sd.ArmorUpgradeLevel = reader.GetInt32(4);
        sd.MagnetUpgradeLevel = reader.GetInt32(5);
        sd.CooldownUpgradeLevel = reader.GetInt32(6);
        sd.GrowthUpgradeLevel = reader.GetInt32(7);
        sd.GreedUpgradeLevel = reader.GetInt32(8);
        sd.LuckUpgradeLevel = reader.GetInt32(9);
        sd.RegenUpgradeLevel = reader.GetInt32(10);
        sd.AreaUpgradeLevel = reader.GetInt32(11);
        sd.DurationUpgradeLevel = reader.GetInt32(12);
        sd.ProjSpeedUpgradeLevel = reader.GetInt32(13);
        sd.ReviveCount = reader.GetInt32(14);
        sd.StartingPotions = reader.GetInt32(15);
        sd.GoldBoosterActive = reader.GetBoolean(16);
        sd.SelectedSkin = reader.GetString(17);
        sd.BestSurvivalTime = (float)reader.GetDouble(18);
        sd.LastSelectedStage = reader.GetInt32(19);
        sd.LastShopRefresh = reader.GetDateTime(20);
    }

    private static async Task LoadSettingsAsync(SqlConnection conn, string username, SaveData sd)
    {
        const string sql = @"
            SELECT [ResolutionIndex], [Fullscreen], [EffectsQuality], [Vsync], [FpsCap],
                   [MasterVolume], [MusicVolume], [SfxVolume], [ShowFps], [ShowMinimap],
                   [UiScale], [Difficulty], [ScreenShake]
            FROM [dbo].[UserSettings] WHERE [Username] = @Username";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Username", username);
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            await LoadLegacySettingsFromUserProgressAsync(conn, username, sd);
            return;
        }

        sd.ResolutionIndex = reader.GetInt32(0);
        sd.Fullscreen = reader.GetBoolean(1);
        sd.EffectsQuality = reader.GetInt32(2);
        sd.Vsync = reader.GetBoolean(3);
        sd.FpsCap = reader.GetInt32(4);
        sd.MasterVolume = reader.GetInt32(5);
        sd.MusicVolume = reader.GetInt32(6);
        sd.SfxVolume = reader.GetInt32(7);
        sd.ShowFps = reader.GetBoolean(8);
        sd.ShowMinimap = reader.GetBoolean(9);
        sd.UiScale = reader.GetInt32(10);
        sd.Difficulty = reader.GetInt32(11);
        sd.ScreenShake = reader.GetBoolean(12);
    }

    private static async Task LoadLegacySettingsFromUserProgressAsync(SqlConnection conn, string username, SaveData sd)
    {
        const string checkSql = @"
            SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'UserProgress' AND COLUMN_NAME = 'ResolutionIndex';";
        using (var checkCmd = new SqlCommand(checkSql, conn))
        {
            if ((int)await checkCmd.ExecuteScalarAsync() == 0) return;
        }

        const string sql = @"
            SELECT [ResolutionIndex], [Fullscreen], [EffectsQuality], [Vsync], [FpsCap],
                   [MasterVolume], [MusicVolume], [SfxVolume], [ShowFps], [ShowMinimap],
                   [UiScale], [Difficulty], [ScreenShake]
            FROM [dbo].[UserProgress] WHERE [Username] = @Username";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Username", username);
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return;

        sd.ResolutionIndex = reader.GetInt32(0);
        sd.Fullscreen = reader.GetBoolean(1);
        sd.EffectsQuality = reader.GetInt32(2);
        sd.Vsync = reader.GetBoolean(3);
        sd.FpsCap = reader.GetInt32(4);
        sd.MasterVolume = reader.GetInt32(5);
        sd.MusicVolume = reader.GetInt32(6);
        sd.SfxVolume = reader.GetInt32(7);
        sd.ShowFps = reader.GetBoolean(8);
        sd.ShowMinimap = reader.GetBoolean(9);
        sd.UiScale = reader.GetInt32(10);
        sd.Difficulty = reader.GetInt32(11);
        sd.ScreenShake = reader.GetBoolean(12);
    }

    private static async Task LoadUnlockedSkinsAsync(SqlConnection conn, string username, SaveData sd)
    {
        sd.UnlockedSkins = new List<string>();
        const string sql = "SELECT [SkinCode] FROM [dbo].[UserUnlockedSkins] WHERE [Username] = @Username ORDER BY [SkinCode];";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Username", username);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            sd.UnlockedSkins.Add(reader.GetString(0));

        if (sd.UnlockedSkins.Count == 0)
            sd.UnlockedSkins.Add("Knight");
    }

    private static async Task LoadEquipmentAsync(SqlConnection conn, string username, SaveData sd)
    {
        sd.OwnedEquipmentIds = new List<string>();
        sd.EquippedIds = new Dictionary<string, string>
        {
            { "Head", "" }, { "Chest", "" }, { "Gauntlet", "" }, { "Leg", "" },
            { "Shoes", "" }, { "Weapon", "" }, { "Health", "" }, { "Misc", "" }
        };

        const string sql = "SELECT [EquipmentId], [IsEquipped], [EquippedSlot] FROM [dbo].[UserEquipment] WHERE [Username] = @Username";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Username", username);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            string eqId = reader.GetString(0);
            bool isEquipped = reader.GetBoolean(1);
            string slot = reader.IsDBNull(2) ? null : reader.GetString(2);
            if (isEquipped && !string.IsNullOrEmpty(slot))
                sd.EquippedIds[slot] = eqId;
            else
                sd.OwnedEquipmentIds.Add(eqId);
        }
    }

    private static async Task LoadStageRecordsAsync(SqlConnection conn, string username, SaveData sd)
    {
        sd.StageRecords = new Dictionary<string, StageRecord>();
        const string sql = "SELECT [StageIndex], [BestSurvivalTime], [BestKills], [BestGold] FROM [dbo].[UserStageRecords] WHERE [Username] = @Username";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Username", username);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            sd.StageRecords[reader.GetInt32(0).ToString()] = new StageRecord
            {
                BestSurvivalTime = (float)reader.GetDouble(1),
                BestKills = reader.GetInt32(2),
                BestGold = reader.GetInt32(3)
            };
        }
    }

    private static async Task LoadShopRollsAsync(SqlConnection conn, string username, SaveData sd)
    {
        sd.ShopRolledIds = new List<string>();
        for (int i = 0; i < 8; i++) sd.ShopRolledIds.Add("");

        const string sql = "SELECT [EquipmentId], [SlotIndex] FROM [dbo].[UserShopRolls] WHERE [Username] = @Username ORDER BY [SlotIndex] ASC";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Username", username);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            int slotIdx = reader.GetInt32(1);
            if (slotIdx >= 0 && slotIdx < 8)
                sd.ShopRolledIds[slotIdx] = reader.GetString(0);
        }
    }

    public static async Task SaveUserDataAsync(string username, SaveData sd)
    {
        if (sd == null || string.IsNullOrEmpty(username)) return;

        try
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            using var trans = conn.BeginTransaction();

            try
            {
                await SaveProgressAsync(conn, trans, username, sd);
                await SaveSettingsAsync(conn, trans, username, sd);
                await SaveUnlockedSkinsAsync(conn, trans, username, sd);
                await SaveEquipmentAsync(conn, trans, username, sd);
                await SaveStageRecordsAsync(conn, trans, username, sd);
                await SaveShopRollsAsync(conn, trans, username, sd);

                using (var touchCmd = new SqlCommand(
                    "UPDATE [dbo].[GameUsers] SET [UpdatedAt] = GETDATE() WHERE [Username] = @Username", conn, trans))
                {
                    touchCmd.Parameters.AddWithValue("@Username", username);
                    await touchCmd.ExecuteNonQueryAsync();
                }

                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving user data: {ex.Message}");
        }
    }

    private static async Task SaveProgressAsync(SqlConnection conn, SqlTransaction trans, string username, SaveData sd)
    {
        const string sql = @"
            MERGE [dbo].[UserProgress] AS target
            USING (SELECT @Username AS Username) AS source ON target.[Username] = source.[Username]
            WHEN MATCHED THEN UPDATE SET
                [Gold] = @Gold,
                [MaxHPUpgradeLevel] = @MaxHPUpgradeLevel,
                [DamageUpgradeLevel] = @DamageUpgradeLevel,
                [SpeedUpgradeLevel] = @SpeedUpgradeLevel,
                [ArmorUpgradeLevel] = @ArmorUpgradeLevel,
                [MagnetUpgradeLevel] = @MagnetUpgradeLevel,
                [CooldownUpgradeLevel] = @CooldownUpgradeLevel,
                [GrowthUpgradeLevel] = @GrowthUpgradeLevel,
                [GreedUpgradeLevel] = @GreedUpgradeLevel,
                [LuckUpgradeLevel] = @LuckUpgradeLevel,
                [RegenUpgradeLevel] = @RegenUpgradeLevel,
                [AreaUpgradeLevel] = @AreaUpgradeLevel,
                [DurationUpgradeLevel] = @DurationUpgradeLevel,
                [ProjSpeedUpgradeLevel] = @ProjSpeedUpgradeLevel,
                [ReviveCount] = @ReviveCount,
                [StartingPotions] = @StartingPotions,
                [GoldBoosterActive] = @GoldBoosterActive,
                [SelectedSkin] = @SelectedSkin,
                [BestSurvivalTime] = @BestSurvivalTime,
                [LastSelectedStage] = @LastSelectedStage,
                [LastShopRefresh] = @LastShopRefresh
            WHEN NOT MATCHED THEN INSERT (
                [Username], [Gold], [MaxHPUpgradeLevel], [DamageUpgradeLevel], [SpeedUpgradeLevel], [ArmorUpgradeLevel],
                [MagnetUpgradeLevel], [CooldownUpgradeLevel], [GrowthUpgradeLevel], [GreedUpgradeLevel], [LuckUpgradeLevel],
                [RegenUpgradeLevel], [AreaUpgradeLevel], [DurationUpgradeLevel], [ProjSpeedUpgradeLevel],
                [ReviveCount], [StartingPotions], [GoldBoosterActive], [SelectedSkin],
                [BestSurvivalTime], [LastSelectedStage], [LastShopRefresh]
            ) VALUES (
                @Username, @Gold, @MaxHPUpgradeLevel, @DamageUpgradeLevel, @SpeedUpgradeLevel, @ArmorUpgradeLevel,
                @MagnetUpgradeLevel, @CooldownUpgradeLevel, @GrowthUpgradeLevel, @GreedUpgradeLevel, @LuckUpgradeLevel,
                @RegenUpgradeLevel, @AreaUpgradeLevel, @DurationUpgradeLevel, @ProjSpeedUpgradeLevel,
                @ReviveCount, @StartingPotions, @GoldBoosterActive, @SelectedSkin,
                @BestSurvivalTime, @LastSelectedStage, @LastShopRefresh
            );";

        using var cmd = new SqlCommand(sql, conn, trans);
        cmd.Parameters.AddWithValue("@Username", username);
        cmd.Parameters.AddWithValue("@Gold", sd.Gold);
        cmd.Parameters.AddWithValue("@MaxHPUpgradeLevel", sd.MaxHPUpgradeLevel);
        cmd.Parameters.AddWithValue("@DamageUpgradeLevel", sd.DamageUpgradeLevel);
        cmd.Parameters.AddWithValue("@SpeedUpgradeLevel", sd.SpeedUpgradeLevel);
        cmd.Parameters.AddWithValue("@ArmorUpgradeLevel", sd.ArmorUpgradeLevel);
        cmd.Parameters.AddWithValue("@MagnetUpgradeLevel", sd.MagnetUpgradeLevel);
        cmd.Parameters.AddWithValue("@CooldownUpgradeLevel", sd.CooldownUpgradeLevel);
        cmd.Parameters.AddWithValue("@GrowthUpgradeLevel", sd.GrowthUpgradeLevel);
        cmd.Parameters.AddWithValue("@GreedUpgradeLevel", sd.GreedUpgradeLevel);
        cmd.Parameters.AddWithValue("@LuckUpgradeLevel", sd.LuckUpgradeLevel);
        cmd.Parameters.AddWithValue("@RegenUpgradeLevel", sd.RegenUpgradeLevel);
        cmd.Parameters.AddWithValue("@AreaUpgradeLevel", sd.AreaUpgradeLevel);
        cmd.Parameters.AddWithValue("@DurationUpgradeLevel", sd.DurationUpgradeLevel);
        cmd.Parameters.AddWithValue("@ProjSpeedUpgradeLevel", sd.ProjSpeedUpgradeLevel);
        cmd.Parameters.AddWithValue("@ReviveCount", sd.ReviveCount);
        cmd.Parameters.AddWithValue("@StartingPotions", sd.StartingPotions);
        cmd.Parameters.AddWithValue("@GoldBoosterActive", sd.GoldBoosterActive);
        cmd.Parameters.AddWithValue("@SelectedSkin", sd.SelectedSkin ?? "Knight");
        cmd.Parameters.AddWithValue("@BestSurvivalTime", (double)sd.BestSurvivalTime);
        cmd.Parameters.AddWithValue("@LastSelectedStage", sd.LastSelectedStage);
        cmd.Parameters.AddWithValue("@LastShopRefresh", sd.LastShopRefresh);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task SaveSettingsAsync(SqlConnection conn, SqlTransaction trans, string username, SaveData sd)
    {
        const string sql = @"
            MERGE [dbo].[UserSettings] AS target
            USING (SELECT @Username AS Username) AS source ON target.[Username] = source.[Username]
            WHEN MATCHED THEN UPDATE SET
                [ResolutionIndex] = @ResolutionIndex,
                [Fullscreen] = @Fullscreen,
                [EffectsQuality] = @EffectsQuality,
                [Vsync] = @Vsync,
                [FpsCap] = @FpsCap,
                [MasterVolume] = @MasterVolume,
                [MusicVolume] = @MusicVolume,
                [SfxVolume] = @SfxVolume,
                [ShowFps] = @ShowFps,
                [ShowMinimap] = @ShowMinimap,
                [UiScale] = @UiScale,
                [Difficulty] = @Difficulty,
                [ScreenShake] = @ScreenShake
            WHEN NOT MATCHED THEN INSERT (
                [Username], [ResolutionIndex], [Fullscreen], [EffectsQuality], [Vsync], [FpsCap],
                [MasterVolume], [MusicVolume], [SfxVolume], [ShowFps], [ShowMinimap], [UiScale], [Difficulty], [ScreenShake]
            ) VALUES (
                @Username, @ResolutionIndex, @Fullscreen, @EffectsQuality, @Vsync, @FpsCap,
                @MasterVolume, @MusicVolume, @SfxVolume, @ShowFps, @ShowMinimap, @UiScale, @Difficulty, @ScreenShake
            );";

        using var cmd = new SqlCommand(sql, conn, trans);
        cmd.Parameters.AddWithValue("@Username", username);
        cmd.Parameters.AddWithValue("@ResolutionIndex", sd.ResolutionIndex);
        cmd.Parameters.AddWithValue("@Fullscreen", sd.Fullscreen);
        cmd.Parameters.AddWithValue("@EffectsQuality", sd.EffectsQuality);
        cmd.Parameters.AddWithValue("@Vsync", sd.Vsync);
        cmd.Parameters.AddWithValue("@FpsCap", sd.FpsCap);
        cmd.Parameters.AddWithValue("@MasterVolume", sd.MasterVolume);
        cmd.Parameters.AddWithValue("@MusicVolume", sd.MusicVolume);
        cmd.Parameters.AddWithValue("@SfxVolume", sd.SfxVolume);
        cmd.Parameters.AddWithValue("@ShowFps", sd.ShowFps);
        cmd.Parameters.AddWithValue("@ShowMinimap", sd.ShowMinimap);
        cmd.Parameters.AddWithValue("@UiScale", sd.UiScale);
        cmd.Parameters.AddWithValue("@Difficulty", sd.Difficulty);
        cmd.Parameters.AddWithValue("@ScreenShake", sd.ScreenShake);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task SaveUnlockedSkinsAsync(SqlConnection conn, SqlTransaction trans, string username, SaveData sd)
    {
        using (var delCmd = new SqlCommand("DELETE FROM [dbo].[UserUnlockedSkins] WHERE [Username] = @Username", conn, trans))
        {
            delCmd.Parameters.AddWithValue("@Username", username);
            await delCmd.ExecuteNonQueryAsync();
        }

        var skins = sd.UnlockedSkins ?? new List<string> { "Knight" };
        if (skins.Count == 0) skins.Add("Knight");

        const string insSql = "INSERT INTO [dbo].[UserUnlockedSkins] ([Username], [SkinCode]) VALUES (@Username, @SkinCode)";
        foreach (var skin in skins)
        {
            if (string.IsNullOrWhiteSpace(skin)) continue;
            using var insCmd = new SqlCommand(insSql, conn, trans);
            insCmd.Parameters.AddWithValue("@Username", username);
            insCmd.Parameters.AddWithValue("@SkinCode", skin);
            await insCmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task SaveEquipmentAsync(SqlConnection conn, SqlTransaction trans, string username, SaveData sd)
    {
        using (var delCmd = new SqlCommand("DELETE FROM [dbo].[UserEquipment] WHERE [Username] = @Username", conn, trans))
        {
            delCmd.Parameters.AddWithValue("@Username", username);
            await delCmd.ExecuteNonQueryAsync();
        }

        if (sd.OwnedEquipmentIds != null)
        {
            const string insOwned = "INSERT INTO [dbo].[UserEquipment] ([Username], [EquipmentId], [IsEquipped], [EquippedSlot]) VALUES (@Username, @EquipmentId, 0, NULL)";
            foreach (var eqId in sd.OwnedEquipmentIds)
            {
                if (string.IsNullOrEmpty(eqId)) continue;
                using var cmd = new SqlCommand(insOwned, conn, trans);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@EquipmentId", eqId);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        if (sd.EquippedIds != null)
        {
            const string insEquipped = "INSERT INTO [dbo].[UserEquipment] ([Username], [EquipmentId], [IsEquipped], [EquippedSlot]) VALUES (@Username, @EquipmentId, 1, @EquippedSlot)";
            foreach (var kvp in sd.EquippedIds)
            {
                if (string.IsNullOrEmpty(kvp.Value)) continue;
                using var cmd = new SqlCommand(insEquipped, conn, trans);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@EquipmentId", kvp.Value);
                cmd.Parameters.AddWithValue("@EquippedSlot", kvp.Key);
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }

    private static async Task SaveStageRecordsAsync(SqlConnection conn, SqlTransaction trans, string username, SaveData sd)
    {
        using (var delCmd = new SqlCommand("DELETE FROM [dbo].[UserStageRecords] WHERE [Username] = @Username", conn, trans))
        {
            delCmd.Parameters.AddWithValue("@Username", username);
            await delCmd.ExecuteNonQueryAsync();
        }

        if (sd.StageRecords == null) return;

        const string insSql = "INSERT INTO [dbo].[UserStageRecords] ([Username], [StageIndex], [BestSurvivalTime], [BestKills], [BestGold]) VALUES (@Username, @StageIndex, @BestSurvivalTime, @BestKills, @BestGold)";
        foreach (var kvp in sd.StageRecords)
        {
            if (!int.TryParse(kvp.Key, out int stageIdx) || kvp.Value == null) continue;
            using var cmd = new SqlCommand(insSql, conn, trans);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@StageIndex", stageIdx);
            cmd.Parameters.AddWithValue("@BestSurvivalTime", (double)kvp.Value.BestSurvivalTime);
            cmd.Parameters.AddWithValue("@BestKills", kvp.Value.BestKills);
            cmd.Parameters.AddWithValue("@BestGold", kvp.Value.BestGold);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task SaveShopRollsAsync(SqlConnection conn, SqlTransaction trans, string username, SaveData sd)
    {
        using (var delCmd = new SqlCommand("DELETE FROM [dbo].[UserShopRolls] WHERE [Username] = @Username", conn, trans))
        {
            delCmd.Parameters.AddWithValue("@Username", username);
            await delCmd.ExecuteNonQueryAsync();
        }

        if (sd.ShopRolledIds == null) return;

        const string insSql = "INSERT INTO [dbo].[UserShopRolls] ([Username], [EquipmentId], [SlotIndex]) VALUES (@Username, @EquipmentId, @SlotIndex)";
        for (int i = 0; i < sd.ShopRolledIds.Count; i++)
        {
            using var cmd = new SqlCommand(insSql, conn, trans);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@EquipmentId", sd.ShopRolledIds[i] ?? "");
            cmd.Parameters.AddWithValue("@SlotIndex", i);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}

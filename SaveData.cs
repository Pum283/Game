using System;
using System.IO;
using System.Text.Json;

namespace MyGame;

public class SaveData
{
    public int Gold { get; set; } = 0;
    
    // Permanent Upgrades (max 10)
    public int MaxHPUpgradeLevel { get; set; } = 0;    // +10% Max HP per level
    public int DamageUpgradeLevel { get; set; } = 0;   // +10% Damage per level
    public int SpeedUpgradeLevel { get; set; } = 0;    // +5% Speed per level
    public int ArmorUpgradeLevel { get; set; } = 0;    // -1 flat damage per level
    public int MagnetUpgradeLevel { get; set; } = 0;   // +15% pickup range per level
    public int CooldownUpgradeLevel { get; set; } = 0; // -4% weapon cooldown per level
    public int GrowthUpgradeLevel { get; set; } = 0;   // +8% EXP gained per level
    public int GreedUpgradeLevel { get; set; } = 0;    // +10% gold collected per level
    public int LuckUpgradeLevel { get; set; } = 0;     // +10% luck per level

    // Consumables bought at Lobby for next run
    public int ReviveCount { get; set; } = 0;          // Max 3
    public int StartingPotions { get; set; } = 0;      // Max 5
    public bool GoldBoosterActive { get; set; } = false;

    // Character Skins
    public string SelectedSkin { get; set; } = "Knight";
    public System.Collections.Generic.List<string> UnlockedSkins { get; set; } = new() { "Knight" };

    // Saved Game Settings
    public int ResolutionIndex { get; set; } = 2; // Default to 1280x720
    public bool Fullscreen { get; set; } = false;
    public int EffectsQuality { get; set; } = 1;  // 0=Low, 1=Medium, 2=High
    public bool Vsync { get; set; } = true;
    public int FpsCap { get; set; } = 2;          // 0=30, 1=60, 2=120, 3=Unlimited
    public int MasterVolume { get; set; } = 8;
    public int MusicVolume { get; set; } = 6;
    public int SfxVolume { get; set; } = 8;
    public bool ShowFps { get; set; } = false;
    public bool ShowMinimap { get; set; } = true;
    public int UiScale { get; set; } = 1;
    public int Difficulty { get; set; } = 0;
    public bool ScreenShake { get; set; } = true;

    // Statistics / High Scores
    public float BestSurvivalTime { get; set; } = 0f;
    public int LastSelectedStage { get; set; } = 1;

    // Map-specific records
    public System.Collections.Generic.Dictionary<string, StageRecord> StageRecords { get; set; } = new();

    public StageRecord GetStageRecord(int stageIndex)
    {
        string key = stageIndex.ToString();
        if (StageRecords == null)
        {
            StageRecords = new System.Collections.Generic.Dictionary<string, StageRecord>();
        }
        if (!StageRecords.TryGetValue(key, out var record))
        {
            record = new StageRecord();
            StageRecords[key] = record;
        }
        return record;
    }

    public void UpdateStageRecord(int stageIndex, float survivalTime, int kills, int gold)
    {
        var record = GetStageRecord(stageIndex);
        if (survivalTime > record.BestSurvivalTime)
        {
            record.BestSurvivalTime = survivalTime;
        }
        if (kills > record.BestKills)
        {
            record.BestKills = kills;
        }
        if (gold > record.BestGold)
        {
            record.BestGold = gold;
        }
    }

    private static readonly string SaveFilePath = "save.json";

    public static SaveData Load()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                string json = File.ReadAllText(SaveFilePath);
                var data = JsonSerializer.Deserialize<SaveData>(json);
                if (data != null) return data;
            }
        }
        catch (Exception)
        {
            // Fallback if JSON is malformed
        }

        return new SaveData();
    }

    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SaveFilePath, json);
        }
        catch (Exception)
        {
            // Ignore write issues
        }
    }
}

public class StageRecord
{
    public float BestSurvivalTime { get; set; } = 0f;
    public int BestKills { get; set; } = 0;
    public int BestGold { get; set; } = 0;
}

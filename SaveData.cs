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

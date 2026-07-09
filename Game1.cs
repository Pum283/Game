using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MyGame;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    
    // Assets
    private Texture2D _pixelTexture;
    private SpriteFont _scoreFont;
    private Texture2D _playerTexture;
    private Texture2D _zombieTexture;
    private Texture2D _batTexture;
    private Texture2D _skeletonTexture;
    private Texture2D _golemTexture;
    private Texture2D _chestTexture;
    private Texture2D _coinTexture;
    private Texture2D _grassTileTexture;
    private Texture2D _sandTileTexture;
    private Texture2D _wallTileTexture;
    private Texture2D _natureTileTexture;
    private Texture2D _floorTileTexture;
    private Texture2D _waterTileTexture;
    private Texture2D _reliefTileTexture;
    private Texture2D _houseTileTexture;
    private Texture2D _elementTileTexture;
    private Texture2D _villageTileTexture;
    private Texture2D _interiorFloorTexture;
    private Texture2D _interiorWallTexture;
    private Texture2D _xpGemTexture;
    private Texture2D _wandProjTexture;
    private Texture2D _fireballProjTexture;
    private Texture2D _arrowProjTexture;
    private Texture2D _axeProjTexture;
    private Texture2D _waterSplashTexture;
    
    // NEW textures
    private Texture2D _crossProjTexture;
    private Texture2D _bibleProjTexture;
    private Texture2D _knifeProjTexture;

    // Render target for resolution upscaling
    private RenderTarget2D _mainRenderTarget;

    private struct TooltipEntry
    {
        public Rectangle Bounds;
        public string Title;
        public string Body;
    }

    private readonly List<TooltipEntry> _tooltipEntries = new();

    // Victory/Endless state
    private bool _hasWonRun = false;
    private bool _inEndlessMode = false;

    // Chest open selection state
    private bool _isChestChoiceActive = false;
    private int _selectedChestChoiceIndex = 0;
    private List<UpgradeOption> _chestChoices = new List<UpgradeOption>();
    
    // Map dimensions
    private const int MapWidth = 2000;
    private const int MapHeight = 2000;
    private byte[,] _terrainGrid;
    private int _terrainCols;
    private int _terrainRows;
    
    // Game Entities
    private Player _player;
    private List<Enemy> _enemies;
    private List<Projectile> _projectiles; // Player projectiles
    private List<Projectile> _enemyProjectiles; // Enemy projectiles (arrows)
    private List<Gem> _gems;
    private List<Chest> _chests;
    private List<Coin> _coins;
    private List<Obstacle> _obstacles;

    // Shared Static Visuals and pools (accessed by weapons)
    public static List<VisualEffect> VisualEffects = new List<VisualEffect>();
    public static List<HolyWaterPool> HolyWaterPools = new List<HolyWaterPool>();

    // Spawning logic
    private float _spawnTimer;
    private float _spawnInterval = 1.5f;
    private Random _random;

    // Game states
    private enum GameState
    {
        Lobby,
        StageSelect,
        ShopMenu,
        SkinShop,
        Settings,
        Playing,
        Paused,
        LevelUpUpgrades,
        ChestOpen,
        GameOver,
        VictoryChoice
    }
    private GameState _gameState = GameState.Lobby;
    
    // Persistent Progress & Shop
    private SaveData _saveData;
    private int _selectedLobbyIndex = 0; // 0: Play, 1: Select Stage, 2: Shop, 3: Skins, 4: Exit
    private int _selectedSkinIndex = 0;
    private int _selectedPauseIndex = 0;
    private Dictionary<string, Texture2D> _skinTextures = new Dictionary<string, Texture2D>();
    private Dictionary<string, Texture2D> _itemIcons = new Dictionary<string, Texture2D>();
    private int _selectedStageIndex = 0;
    private int _selectedShopIndex = 0;
    private int _currentStage = 1;

    private struct StageConfig
    {
        public int Index;
        public string Name;
        public string Description;
        public string LobbyName;
        public Color GroundBaseColor;
        public int TileSetType; // 1: Grass, 2: Sand, 3: Dungeon
        public int TerrainVariant; // Unique decoration pattern per stage
        public float SpawnIntervalMultiplier;
        public int SpawnBatchCount; // Enemies spawned per tick
        public float MonsterHPMultiplier;
        public float MonsterSpeedMultiplier;
        public int BossSpawnIntervalSeconds;
        public int ObstacleLayoutType;
    }

    private static readonly StageConfig[] StageConfigs = new StageConfig[]
    {
        new StageConfig {
            Index = 1,
            Name = "MAN 1: THAO NGUYEN YEN BINH",
            LobbyName = "MAN 1: THAO NGUYEN (TAN THU)",
            Description = "Map mo vo tan. Chi bui co trang tri. Quai cham, khong gian rong de thu vu khi.",
            GroundBaseColor = new Color(117, 167, 67),
            TileSetType = 1,
            TerrainVariant = 1,
            SpawnIntervalMultiplier = 1.15f,
            SpawnBatchCount = 1,
            MonsterHPMultiplier = 0.85f,
            MonsterSpeedMultiplier = 0.72f,
            BossSpawnIntervalSeconds = 300,
            ObstacleLayoutType = 1
        },
        new StageConfig {
            Index = 2,
            Name = "MAN 2: NGHIA TRANG CO",
            LobbyName = "MAN 2: NGHIA TRANG (ZIC-ZAC)",
            Description = "Bia mo chan duong, quai bay xuyen qua. Phai luon lach theo duong gap ghenh.",
            GroundBaseColor = new Color(45, 55, 45),
            TileSetType = 1,
            TerrainVariant = 2,
            SpawnIntervalMultiplier = 0.95f,
            SpawnBatchCount = 1,
            MonsterHPMultiplier = 1.0f,
            MonsterSpeedMultiplier = 1.0f,
            BossSpawnIntervalSeconds = 300,
            ObstacleLayoutType = 2
        },
        new StageConfig {
            Index = 3,
            Name = "MAN 3: THU VIEN CAM",
            LobbyName = "MAN 3: THU VIEN (HANG NGANG)",
            Description = "Hanh lang hep giua ke sach. Quai xuat hien tu trai/phai - tot cho vu khi ban thang.",
            GroundBaseColor = new Color(90, 70, 50),
            TileSetType = 3,
            TerrainVariant = 3,
            SpawnIntervalMultiplier = 0.85f,
            SpawnBatchCount = 1,
            MonsterHPMultiplier = 1.1f,
            MonsterSpeedMultiplier = 1.05f,
            BossSpawnIntervalSeconds = 270,
            ObstacleLayoutType = 3
        },
        new StageConfig {
            Index = 4,
            Name = "MAN 4: DAM LAY DOC",
            LobbyName = "MAN 4: DAM LAY (VUNG DOC)",
            Description = "Vung doc lam cham va mat mau (DoT). Cay kho va da tang chan duong.",
            GroundBaseColor = new Color(34, 76, 34),
            TileSetType = 1,
            TerrainVariant = 4,
            SpawnIntervalMultiplier = 0.8f,
            SpawnBatchCount = 1,
            MonsterHPMultiplier = 1.15f,
            MonsterSpeedMultiplier = 1.0f,
            BossSpawnIntervalSeconds = 240,
            ObstacleLayoutType = 4
        },
        new StageConfig {
            Index = 5,
            Name = "MAN 5: RUNG SUONG MU",
            LobbyName = "MAN 5: RUNG SUONG MU (HANTAM NHIN)",
            Description = "Tam nhin han che quanh nhan vat. Cay lon chan dan va nguoi choi.",
            GroundBaseColor = new Color(30, 60, 30),
            TileSetType = 1,
            TerrainVariant = 5,
            SpawnIntervalMultiplier = 0.75f,
            SpawnBatchCount = 2,
            MonsterHPMultiplier = 1.0f,
            MonsterSpeedMultiplier = 1.1f,
            BossSpawnIntervalSeconds = 210,
            ObstacleLayoutType = 5
        },
        new StageConfig {
            Index = 6,
            Name = "MAN 6: LAU DAI DO NAT",
            LobbyName = "MAN 6: LAU DAI (BAY GAI)",
            Description = "Phong vuong, tuong co the pha. Bay gai kich hoat moi 10 giay.",
            GroundBaseColor = new Color(55, 55, 65),
            TileSetType = 3,
            TerrainVariant = 6,
            SpawnIntervalMultiplier = 0.7f,
            SpawnBatchCount = 1,
            MonsterHPMultiplier = 1.3f,
            MonsterSpeedMultiplier = 1.05f,
            BossSpawnIntervalSeconds = 180,
            ObstacleLayoutType = 6
        },
        new StageConfig {
            Index = 7,
            Name = "MAN 7: HAM MO DUNG NHAM",
            LobbyName = "MAN 7: HAM MO (CUA HEP)",
            Description = "Hanh lang doc hep, dong lava ngang. Slime lua nhay tu dung nham.",
            GroundBaseColor = new Color(77, 20, 20),
            TileSetType = 3,
            TerrainVariant = 7,
            SpawnIntervalMultiplier = 0.7f,
            SpawnBatchCount = 1,
            MonsterHPMultiplier = 1.2f,
            MonsterSpeedMultiplier = 1.15f,
            BossSpawnIntervalSeconds = 180,
            ObstacleLayoutType = 7
        },
        new StageConfig {
            Index = 8,
            Name = "MAN 8: SA MAC CHET CHOC",
            LobbyName = "MAN 8: SA MAC (BAO CAT)",
            Description = "Map mo. Xuong rong gay sat thuong. Bao cat moi 3 phut day lui nguoi choi.",
            GroundBaseColor = new Color(216, 185, 125),
            TileSetType = 2,
            TerrainVariant = 8,
            SpawnIntervalMultiplier = 0.8f,
            SpawnBatchCount = 1,
            MonsterHPMultiplier = 1.1f,
            MonsterSpeedMultiplier = 1.1f,
            BossSpawnIntervalSeconds = 180,
            ObstacleLayoutType = 8
        },
        new StageConfig {
            Index = 9,
            Name = "MAN 9: CAU TREO VUC THAM",
            LobbyName = "MAN 9: CAU TREO (SINH TON)",
            Description = "Chi di tren cau hep. Quai roi tu tren xuong. Thung go pha de lay vang/mau.",
            GroundBaseColor = new Color(35, 35, 45),
            TileSetType = 3,
            TerrainVariant = 9,
            SpawnIntervalMultiplier = 0.55f,
            SpawnBatchCount = 2,
            MonsterHPMultiplier = 1.25f,
            MonsterSpeedMultiplier = 1.2f,
            BossSpawnIntervalSeconds = 150,
            ObstacleLayoutType = 9
        },
        new StageConfig {
            Index = 10,
            Name = "MAN 10: DINH NUI BANG GIA",
            LobbyName = "MAN 10: DINH BANG (CUC KHO)",
            Description = "San bang tron truot co quan tinh. Khoi bang che tam nhin. Quai dong bang 1 giay.",
            GroundBaseColor = new Color(173, 216, 230),
            TileSetType = 2,
            TerrainVariant = 10,
            SpawnIntervalMultiplier = 0.5f,
            SpawnBatchCount = 2,
            MonsterHPMultiplier = 1.7f,
            MonsterSpeedMultiplier = 0.95f,
            BossSpawnIntervalSeconds = 120,
            ObstacleLayoutType = 10
        }
    };

    private StageConfig GetCurrentStageConfig()
    {
        int idx = MathHelper.Clamp(_currentStage - 1, 0, StageConfigs.Length - 1);
        return StageConfigs[idx];
    }

    private StageMapBuilder _mapBuilder = new StageMapBuilder();
    private StageMechanics _stageMechanics = new StageMechanics();
    private int _goldCollectedThisRun = 0;
    private string _chestOpenedMessage = "";

    // Giant Boss Spawning
    private int _lastSpawnedFiveMinBossInterval = 0;
    private string _bossWarningText = "";
    private float _bossWarningTimer = 0f;

    // ---- Settings ----
    // Display
    private int _settingsResolutionIndex = 0; // 0=800x600, 1=1024x768, 2=1280x720, 3=1600x900, 4=1920x1080
    private bool _settingsFullscreen = false;
    // Graphics
    private int _settingsEffectsQuality = 1; // 0=Low, 1=Medium, 2=High
    private bool _settingsVsync = true;
    private int _settingsFpsCap = 2; // 0=30, 1=60, 2=120, 3=Unlimited
    // Audio
    private int _settingsMasterVolume = 8;  // 0-10
    private int _settingsMusicVolume = 6;   // 0-10
    private int _settingsSfxVolume = 8;     // 0-10
    // UI
    private bool _settingsShowFps = false;
    private bool _settingsShowMinimap = true;
    private int _settingsUiScale = 1; // 0=Small, 1=Normal, 2=Large
    // Gameplay
    private int _settingsDifficulty = 0; // 0=Normal, 1=Hard, 2=Brutal
    private bool _settingsScreenShake = true;
    // Settings navigation
    private int _settingsCategoryIndex = 0;  // 0=Display, 1=Graphics, 2=Audio, 3=UI, 4=Gameplay
    private int _settingsItemIndex = 0;      // selected item within current category
    private static readonly string[] _settingsCategoryNames = { "HIEN THI", "DO HOA", "AM THANH", "GIAO DIEN", "GAMEPLAY" };
    private float _settingsFps = 0f;
    private float _settingsFpsTimer = 0f;
    private int _settingsFpsCount = 0;

    // Stats & Timers
    private float _gameTimer = 0f; // Track run time in seconds
    private int _killCount = 0;
    private KeyboardState _previousKeyboardState;
    
    // Golem boss spawns tracking
    private bool _golemSpawned1 = false;
    private bool _golemSpawned2 = false;
    private bool _golemSpawned3 = false;

    // Modular Weapons List
    private List<Weapon> _weapons;

    // Upgrade Choices storage
    private class UpgradeOption
    {
        public string Code { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
    private List<UpgradeOption> _upgradeChoices = new List<UpgradeOption>();

    // Boss Chest Reward display lines
    private List<string> _chestRewards = new List<string>();

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 600;
    }

    protected override void Initialize()
    {
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        // Load Persistent Progress
        _saveData = SaveData.Load();

        // Apply saved settings
        _settingsResolutionIndex = _saveData.ResolutionIndex;
        _settingsFullscreen = _saveData.Fullscreen;
        _settingsEffectsQuality = _saveData.EffectsQuality;
        _settingsVsync = _saveData.Vsync;
        _settingsFpsCap = _saveData.FpsCap;
        _settingsMasterVolume = _saveData.MasterVolume;
        _settingsMusicVolume = _saveData.MusicVolume;
        _settingsSfxVolume = _saveData.SfxVolume;
        _settingsShowFps = _saveData.ShowFps;
        _settingsShowMinimap = _saveData.ShowMinimap;
        _settingsUiScale = _saveData.UiScale;
        _settingsDifficulty = _saveData.Difficulty;
        _settingsScreenShake = _saveData.ScreenShake;
        _currentStage = MathHelper.Clamp(_saveData.LastSelectedStage, 1, StageConfigs.Length);
        _selectedStageIndex = _currentStage - 1;

        // Apply resolution
        int[][] res = { 
            new[]{800,600}, new[]{1024,768}, new[]{1280,720}, new[]{1280,800},
            new[]{1366,768}, new[]{1440,900}, new[]{1600,900}, new[]{1920,1080},
            new[]{2560,1440}, new[]{3840,2160} 
        };
        _settingsResolutionIndex = Math.Clamp(_settingsResolutionIndex, 0, res.Length - 1);
        _graphics.PreferredBackBufferWidth = res[_settingsResolutionIndex][0];
        _graphics.PreferredBackBufferHeight = res[_settingsResolutionIndex][1];
        _graphics.IsFullScreen = _settingsFullscreen;
        _graphics.ApplyChanges();

        // Apply VSync and FpsCap
        IsFixedTimeStep = _settingsVsync;
        SetFpsCap(_settingsFpsCap);

        // Initialize main render target for virtual resolution scaling
        _mainRenderTarget = new RenderTarget2D(GraphicsDevice, 800, 600);

        ResetGame();

        // Start in Lobby instead of starting the game directly
        _gameState = GameState.Lobby;

        base.Initialize();
    }

    private void SetFpsCap(int capIndex)
    {
        if (capIndex == 0) // 30 FPS
        {
            TargetElapsedTime = TimeSpan.FromSeconds(1d / 30d);
            _graphics.SynchronizeWithVerticalRetrace = false;
        }
        else if (capIndex == 1) // 60 FPS
        {
            TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d);
            _graphics.SynchronizeWithVerticalRetrace = false;
        }
        else if (capIndex == 2) // 120 FPS
        {
            TargetElapsedTime = TimeSpan.FromSeconds(1d / 120d);
            _graphics.SynchronizeWithVerticalRetrace = false;
        }
        else // Unlimited
        {
            TargetElapsedTime = TimeSpan.FromSeconds(1d / 9999d);
            _graphics.SynchronizeWithVerticalRetrace = false;
        }
        _graphics.ApplyChanges();
    }

    private void ResetGame()
    {
        // Start player exactly in the middle of the large 2000x2000 map
        Vector2 startPos = new Vector2(MapWidth / 2f - 16, MapHeight / 2f - 16);
        _player = new Player(startPos, _saveData);

        _enemies = new List<Enemy>();
        _projectiles = new List<Projectile>();
        _enemyProjectiles = new List<Projectile>();
        _gems = new List<Gem>();
        _chests = new List<Chest>();
        _coins = new List<Coin>();
        _obstacles = new List<Obstacle>();
        
        VisualEffects.Clear();
        HolyWaterPools.Clear();

        // Reset victory/endless state
        _hasWonRun = false;
        _inEndlessMode = false;

        // Generate terrain and map obstacles based on stage
        _mapBuilder.Build(_currentStage, MapWidth, MapHeight);
        _terrainGrid = _mapBuilder.Terrain;
        _terrainCols = _mapBuilder.Cols;
        _terrainRows = _mapBuilder.Rows;
        _obstacles = _mapBuilder.Obstacles;
        _stageMechanics.Reset(_currentStage, _terrainGrid);

        // Initialize weapons modularly (now with 9 weapons)
        _weapons = new List<Weapon>
        {
            new MagicWandWeapon(),
            new GarlicWeapon(),
            new FireballWeapon(),
            new AxeWeapon(),
            new LightningWeapon(),
            new HolyWaterWeapon(),
            new CrossWeapon(),
            new BibleWeapon(),
            new KnifeWeapon()
        };

        _random = new Random();
        _spawnTimer = 0f;
        _gameTimer = 0f;
        _spawnInterval = 2.2f; // Increased from 1.5f to reduce early game spawn rate
        
        _golemSpawned1 = false;
        _golemSpawned2 = false;
        _golemSpawned3 = false;
        _lastSpawnedFiveMinBossInterval = 0;
        _bossWarningTimer = 0f;
        _bossWarningText = "";
        _killCount = 0;
        _goldCollectedThisRun = 0;

        // Spawn 5 natural chests at start
        var rand = new Random();
        for (int i = 0; i < 5; i++)
        {
            int cx = rand.Next(300, MapWidth - 300);
            int cy = rand.Next(300, MapHeight - 300);
            
            // Avoid placing too close to the starting center position
            if (Vector2.Distance(new Vector2(cx, cy), new Vector2(MapWidth / 2f, MapHeight / 2f)) > 200f)
            {
                Vector2 chestPos = FindNearestWalkablePosition(new Vector2(cx, cy), 28, 28);
                if (IsWalkableAndFree(chestPos, 28, 28))
                    _chests.Add(new Chest(chestPos));
            }
        }
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _scoreFont = Content.Load<SpriteFont>("ScoreFont");
        
        // Load Sprite Textures
        _playerTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Actor/Character/Knight/SeparateAnim/Walk");
        _zombieTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Actor/Monster/Slime/Slime");
        _batTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Actor/Monster/YellowsBat/SpriteSheet");
        _skeletonTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Actor/Character/Skeleton/SeparateAnim/Walk");
        _golemTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Actor/Character/DemonRed/SeparateAnim/Walk");
        
        _chestTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Items/Treasure/BigTreasureChest");
        _coinTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Items/Treasure/GoldCoin");
        _xpGemTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Items/Resource/GemGreen");
        
        _wandProjTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/FX/Projectile/EnergyBall");
        _fireballProjTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/FX/Projectile/Fireball");
        _arrowProjTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/FX/Projectile/Arrow");
        _axeProjTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Items/Weapons/Axe/Sprite");
        _waterSplashTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/FX/Elemental/Water/SpriteSheet");
        
        // Load new projectile textures
        _crossProjTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/FX/Projectile/BigShuriken");
        _bibleProjTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Items/Object/Book");
        _knifeProjTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/FX/Projectile/BigKunai");
        
        // Load Floor Tilesets
        _grassTileTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Backgrounds/Tilesets/TilesetField");
        _sandTileTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Backgrounds/Tilesets/TilesetDesert");
        _wallTileTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Backgrounds/Tilesets/TilesetDungeon");
        _natureTileTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Backgrounds/Tilesets/TilesetNature");
        _floorTileTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Backgrounds/Tilesets/TilesetFloor");
        _waterTileTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Backgrounds/Tilesets/TilesetWater");
        _reliefTileTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Backgrounds/Tilesets/TilesetRelief");
        _houseTileTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Backgrounds/Tilesets/TilesetHouse");
        _elementTileTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Backgrounds/Tilesets/TilesetElement");
        _villageTileTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Backgrounds/Tilesets/TilesetVillageAbandoned");
        _interiorFloorTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Backgrounds/Tilesets/Interior/TilesetInteriorFloor");
        _interiorWallTexture = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Backgrounds/Tilesets/Interior/TilesetWallSimple");

        // Load skin textures
        _skinTextures["Knight"] = _playerTexture;
        _skinTextures["NinjaBlue"] = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Actor/Character/NinjaBlue/SeparateAnim/Walk");
        _skinTextures["NinjaRed"] = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Actor/Character/NinjaRed/SeparateAnim/Walk");
        _skinTextures["Samurai"] = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Actor/Character/Samurai/SeparateAnim/Walk");
        _skinTextures["Monk"] = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Actor/Character/Monk/SeparateAnim/Walk");
        _skinTextures["KnightGold"] = Content.Load<Texture2D>("Ninja Adventure - Asset Pack/Actor/Character/KnightGold/SeparateAnim/Walk");

        // Load item icons
        string[] iconPaths = {
            "WAND:Ui/Skill Icon/Spell/MagicWeapon",
            "GARLIC:Ui/Skill Icon/Spell/Camouflage",
            "FIREBALL:Ui/Skill Icon/Spell/Fireball",
            "LIGHTNING:Ui/Skill Icon/Spell/OrbLight",
            "HOLYWATER:Ui/Skill Icon/Spell/OrbWater",
            "CROSS:Ui/Skill Icon/Items & Weapon/Shuriken",
            "BIBLE:Ui/Skill Icon/Spell/BookLight",
            "KNIFE:Ui/Skill Icon/Items & Weapon/Kunai",
            "BOOTS:Ui/Skill Icon/Items & Weapon/Boot",
            "REGEN:Ui/Skill Icon/Items & Weapon/Ring",
            "SHIELD:Ui/Skill Icon/Items & Weapon/Armor",
            "SPINACH:Ui/Skill Icon/Spell/AttackUpgrade",
            "MAGNET:Ui/Skill Icon/Items & Weapon/Hook",
            "DUPLICATOR:Ui/Skill Icon/Spell/Permutation",
            "CLOVER:Ui/Skill Icon/Spell/LuckUpgrade",
            "HEART:Ui/Skill Icon/Spell/Heal",
            "PUMMAROLA:Ui/Skill Icon/Spell/DefenseUpgrade",
            "BRACER:Ui/Skill Icon/Items & Weapon/Arrow",
            "SPELLBINDER:Ui/Skill Icon/Spell/Vision",
            "TOME:Ui/Skill Icon/Items & Weapon/Scroll",
            "POTION:Ui/Skill Icon/Job & Action/Potion",
            "REROLL:Ui/Skill Icon/Spell/Upgrade",
            "HEAL:Ui/Skill Icon/Spell/Heal"
        };
        foreach (var ip in iconPaths)
        {
            var parts = ip.Split(':');
            _itemIcons[parts[0]] = Content.Load<Texture2D>($"Ninja Adventure - Asset Pack/{parts[1]}");
        }
    }

    // Static helpers to add visuals/pools from weapon logic
    public static void AddVisualEffect(VisualEffect effect)
    {
        VisualEffects.Add(effect);
    }

    public static void AddHolyWaterPool(HolyWaterPool pool)
    {
        HolyWaterPools.Add(pool);
    }

    private Weapon GetWeapon(string name)
    {
        return _weapons.Find(w => w.Name == name);
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();

        // ESC key checks for pausing and menu exits
        if (keyboardState.IsKeyDown(Keys.Escape) && _previousKeyboardState.IsKeyUp(Keys.Escape))
        {
            if (_gameState == GameState.Playing)
            {
                _selectedPauseIndex = 0;
                _gameState = GameState.Paused;
            }
            else if (_gameState == GameState.Paused)
            {
                _gameState = GameState.Playing; // Resume
            }
            else if (_gameState == GameState.GameOver)
            {
                // Save collected gold
                _saveData.Gold += _goldCollectedThisRun;
                _saveData.Save();
                _gameState = GameState.Lobby;
            }
            else if (_gameState == GameState.ShopMenu || _gameState == GameState.StageSelect || _gameState == GameState.SkinShop)
            {
                _gameState = GameState.Lobby;
            }
            else if (_gameState == GameState.Lobby)
            {
                Exit();
            }
        }

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (_gameState)
        {
            case GameState.Lobby:
                UpdateLobby(keyboardState);
                break;

            case GameState.StageSelect:
                UpdateStageSelect(keyboardState);
                break;

            case GameState.ShopMenu:
                UpdateShopMenu(keyboardState);
                break;

            case GameState.SkinShop:
                UpdateSkinShop(keyboardState);
                break;

            case GameState.Settings:
                UpdateSettings(keyboardState, gameTime);
                break;

            case GameState.Playing:
                UpdateGameplay(gameTime, deltaTime);
                break;

            case GameState.Paused:
                UpdatePaused(keyboardState);
                break;

            case GameState.LevelUpUpgrades:
                UpdateUpgrades(keyboardState);
                break;

            case GameState.ChestOpen:
                UpdateChestOpen(keyboardState);
                break;

            case GameState.VictoryChoice:
                UpdateVictoryChoice(keyboardState);
                break;

            case GameState.GameOver:
                if (keyboardState.IsKeyDown(Keys.Enter) || keyboardState.IsKeyDown(Keys.Space))
                {
                    ResetGame();
                    _gameState = GameState.Playing;
                }
                break;
        }

        _previousKeyboardState = keyboardState;
        base.Update(gameTime);
    }

    private void UpdateLobby(KeyboardState keyboardState)
    {
        if ((keyboardState.IsKeyDown(Keys.W) && _previousKeyboardState.IsKeyUp(Keys.W)) ||
            (keyboardState.IsKeyDown(Keys.Up) && _previousKeyboardState.IsKeyUp(Keys.Up)))
        {
            _selectedLobbyIndex = (_selectedLobbyIndex - 1 + 6) % 6;
        }
        if ((keyboardState.IsKeyDown(Keys.S) && _previousKeyboardState.IsKeyUp(Keys.S)) ||
            (keyboardState.IsKeyDown(Keys.Down) && _previousKeyboardState.IsKeyUp(Keys.Down)))
        {
            _selectedLobbyIndex = (_selectedLobbyIndex + 1) % 6;
        }

        if ((keyboardState.IsKeyDown(Keys.Enter) && _previousKeyboardState.IsKeyUp(Keys.Enter)) ||
            (keyboardState.IsKeyDown(Keys.Space) && _previousKeyboardState.IsKeyUp(Keys.Space)))
        {
            if (_selectedLobbyIndex == 0) // Start Play
            {
                ResetGame();
                _gameState = GameState.Playing;
            }
            else if (_selectedLobbyIndex == 1) // Select Stage
            {
                _selectedStageIndex = _currentStage - 1;
                _gameState = GameState.StageSelect;
            }
            else if (_selectedLobbyIndex == 2) // Shop upgrades
            {
                _selectedShopIndex = 0;
                _gameState = GameState.ShopMenu;
            }
            else if (_selectedLobbyIndex == 3) // Skin Shop
            {
                _selectedSkinIndex = 0;
                _gameState = GameState.SkinShop;
            }
            else if (_selectedLobbyIndex == 4) // Settings
            {
                _settingsCategoryIndex = 0;
                _settingsItemIndex = 0;
                _gameState = GameState.Settings;
            }
            else if (_selectedLobbyIndex == 5) // Exit
            {
                Exit();
            }
        }
    }

    private void UpdateStageSelect(KeyboardState keyboardState)
    {
        int totalStages = StageConfigs.Length;
        _selectedStageIndex = Math.Clamp(_selectedStageIndex, 0, totalStages - 1);

        if ((keyboardState.IsKeyDown(Keys.W) && _previousKeyboardState.IsKeyUp(Keys.W)) ||
            (keyboardState.IsKeyDown(Keys.Up) && _previousKeyboardState.IsKeyUp(Keys.Up)))
        {
            _selectedStageIndex = Math.Max(0, _selectedStageIndex - 1);
        }
        if ((keyboardState.IsKeyDown(Keys.S) && _previousKeyboardState.IsKeyUp(Keys.S)) ||
            (keyboardState.IsKeyDown(Keys.Down) && _previousKeyboardState.IsKeyUp(Keys.Down)))
        {
            _selectedStageIndex = Math.Min(totalStages - 1, _selectedStageIndex + 1);
        }
        if ((keyboardState.IsKeyDown(Keys.A) && _previousKeyboardState.IsKeyUp(Keys.A)) ||
            (keyboardState.IsKeyDown(Keys.Left) && _previousKeyboardState.IsKeyUp(Keys.Left)))
        {
            int row = _selectedStageIndex % 5;
            if (_selectedStageIndex >= 5)
                _selectedStageIndex = row;
        }
        if ((keyboardState.IsKeyDown(Keys.D) && _previousKeyboardState.IsKeyUp(Keys.D)) ||
            (keyboardState.IsKeyDown(Keys.Right) && _previousKeyboardState.IsKeyUp(Keys.Right)))
        {
            int row = _selectedStageIndex % 5;
            if (_selectedStageIndex < 5 && row + 5 < totalStages)
                _selectedStageIndex = row + 5;
        }

        if ((keyboardState.IsKeyDown(Keys.Enter) && _previousKeyboardState.IsKeyUp(Keys.Enter)) ||
            (keyboardState.IsKeyDown(Keys.Space) && _previousKeyboardState.IsKeyUp(Keys.Space)))
        {
            _currentStage = _selectedStageIndex + 1;
            _saveData.LastSelectedStage = _currentStage;
            _saveData.Save();
            _gameState = GameState.Lobby;
        }
    }

    private void UpdateShopMenu(KeyboardState keyboardState)
    {
        int totalItems = 13;
        bool up = (keyboardState.IsKeyDown(Keys.W) && _previousKeyboardState.IsKeyUp(Keys.W)) ||
                  (keyboardState.IsKeyDown(Keys.Up) && _previousKeyboardState.IsKeyUp(Keys.Up));
        bool down = (keyboardState.IsKeyDown(Keys.S) && _previousKeyboardState.IsKeyUp(Keys.S)) ||
                    (keyboardState.IsKeyDown(Keys.Down) && _previousKeyboardState.IsKeyUp(Keys.Down));
        bool left = (keyboardState.IsKeyDown(Keys.A) && _previousKeyboardState.IsKeyUp(Keys.A)) ||
                    (keyboardState.IsKeyDown(Keys.Left) && _previousKeyboardState.IsKeyUp(Keys.Left));
        bool right = (keyboardState.IsKeyDown(Keys.D) && _previousKeyboardState.IsKeyUp(Keys.D)) ||
                     (keyboardState.IsKeyDown(Keys.Right) && _previousKeyboardState.IsKeyUp(Keys.Right));

        if (up)
        {
            _selectedShopIndex = (_selectedShopIndex - 1 + totalItems) % totalItems;
        }
        if (down)
        {
            _selectedShopIndex = (_selectedShopIndex + 1) % totalItems;
        }
        
        // Column switching: left column (0-8) and right column (9-12)
        if (left)
        {
            if (_selectedShopIndex >= 9)
            {
                // Jump to left column
                _selectedShopIndex = Math.Clamp(_selectedShopIndex - 9, 0, 8);
            }
        }
        if (right)
        {
            if (_selectedShopIndex <= 8)
            {
                // Jump to right column
                _selectedShopIndex = Math.Clamp(_selectedShopIndex + 9, 9, 12);
            }
        }

        if ((keyboardState.IsKeyDown(Keys.Enter) && _previousKeyboardState.IsKeyUp(Keys.Enter)) ||
            (keyboardState.IsKeyDown(Keys.Space) && _previousKeyboardState.IsKeyUp(Keys.Space)))
        {
            if (_selectedShopIndex <= 8)
            {
                BuyPermanentUpgrade(_selectedShopIndex);
            }
            else
            {
                switch (_selectedShopIndex)
                {
                    case 9: // Revive Potion (100 gold, max 3)
                        if (_saveData.Gold >= 100 && _saveData.ReviveCount < 3)
                        {
                            _saveData.Gold -= 100;
                            _saveData.ReviveCount++;
                            _saveData.Save();
                        }
                        break;

                    case 10: // HP Potion (30 gold, max 5)
                        if (_saveData.Gold >= 30 && _saveData.StartingPotions < 5)
                        {
                            _saveData.Gold -= 30;
                            _saveData.StartingPotions++;
                            _saveData.Save();
                        }
                        break;

                    case 11: // Gold Booster (80 gold, single use)
                        if (_saveData.Gold >= 80 && !_saveData.GoldBoosterActive)
                        {
                            _saveData.Gold -= 80;
                            _saveData.GoldBoosterActive = true;
                            _saveData.Save();
                        }
                        break;

                    case 12: // Exit Shop
                        _gameState = GameState.Lobby;
                        break;
                }
            }
        }
    }

    private void BuyPermanentUpgrade(int shopIndex)
    {
        int cost = 0;
        switch (shopIndex)
        {
            case 0:
                cost = GetUpgradeCost(_saveData.MaxHPUpgradeLevel);
                if (cost > 0 && _saveData.Gold >= cost && _saveData.MaxHPUpgradeLevel < 10)
                {
                    _saveData.Gold -= cost;
                    _saveData.MaxHPUpgradeLevel++;
                    _saveData.Save();
                }
                break;
            case 1:
                cost = GetUpgradeCost(_saveData.DamageUpgradeLevel);
                if (cost > 0 && _saveData.Gold >= cost && _saveData.DamageUpgradeLevel < 10)
                {
                    _saveData.Gold -= cost;
                    _saveData.DamageUpgradeLevel++;
                    _saveData.Save();
                }
                break;
            case 2:
                cost = GetUpgradeCost(_saveData.SpeedUpgradeLevel);
                if (cost > 0 && _saveData.Gold >= cost && _saveData.SpeedUpgradeLevel < 10)
                {
                    _saveData.Gold -= cost;
                    _saveData.SpeedUpgradeLevel++;
                    _saveData.Save();
                }
                break;
            case 3:
                cost = GetUpgradeCost(_saveData.ArmorUpgradeLevel);
                if (cost > 0 && _saveData.Gold >= cost && _saveData.ArmorUpgradeLevel < 10)
                {
                    _saveData.Gold -= cost;
                    _saveData.ArmorUpgradeLevel++;
                    _saveData.Save();
                }
                break;
            case 4:
                cost = GetUpgradeCost(_saveData.MagnetUpgradeLevel);
                if (cost > 0 && _saveData.Gold >= cost && _saveData.MagnetUpgradeLevel < 10)
                {
                    _saveData.Gold -= cost;
                    _saveData.MagnetUpgradeLevel++;
                    _saveData.Save();
                }
                break;
            case 5:
                cost = GetUpgradeCost(_saveData.CooldownUpgradeLevel);
                if (cost > 0 && _saveData.Gold >= cost && _saveData.CooldownUpgradeLevel < 10)
                {
                    _saveData.Gold -= cost;
                    _saveData.CooldownUpgradeLevel++;
                    _saveData.Save();
                }
                break;
            case 6:
                cost = GetUpgradeCost(_saveData.GrowthUpgradeLevel);
                if (cost > 0 && _saveData.Gold >= cost && _saveData.GrowthUpgradeLevel < 10)
                {
                    _saveData.Gold -= cost;
                    _saveData.GrowthUpgradeLevel++;
                    _saveData.Save();
                }
                break;
            case 7:
                cost = GetUpgradeCost(_saveData.GreedUpgradeLevel);
                if (cost > 0 && _saveData.Gold >= cost && _saveData.GreedUpgradeLevel < 10)
                {
                    _saveData.Gold -= cost;
                    _saveData.GreedUpgradeLevel++;
                    _saveData.Save();
                }
                break;
            case 8:
                cost = GetUpgradeCost(_saveData.LuckUpgradeLevel);
                if (cost > 0 && _saveData.Gold >= cost && _saveData.LuckUpgradeLevel < 10)
                {
                    _saveData.Gold -= cost;
                    _saveData.LuckUpgradeLevel++;
                    _saveData.Save();
                }
                break;
        }
    }

    private int GetUpgradeCost(int currentLevel)
    {
        if (currentLevel >= 10) return 0;
        return 20 + currentLevel * 30; // 20, 50, 80, 110, 140, 170, 200, 230, 260, 290 gold
    }

    private void UpdateGameplay(GameTime gameTime, float deltaTime)
    {
        _gameTimer += deltaTime;

        // Decrease boss alert timer
        if (_bossWarningTimer > 0f)
        {
            _bossWarningTimer -= deltaTime;
        }

        // Stage mechanics (poison, fog, sandstorm, lava spawn, sky drop, spikes)
        _stageMechanics.Update(deltaTime, _player, _terrainGrid, _enemies, _random, SpawnStageEnemy);

        // 1. Update player (with obstacles collision)
        _player.Update(gameTime, MapWidth, MapHeight, _obstacles, _terrainGrid);

        ApplyObstacleHazards(deltaTime);

        // Potion manual use check (Key H)
        var kbState = Keyboard.GetState();
        if (kbState.IsKeyDown(Keys.H) && _previousKeyboardState.IsKeyUp(Keys.H))
        {
            if (_player.CurrentPotions > 0 && _player.HP < _player.MaxHP)
            {
                _player.CurrentPotions--;
                _player.HP = MathHelper.Min(_player.HP + 30f, _player.MaxHP);
                VisualEffects.Add(new VisualEffect
                {
                    Position = _player.Position - new Vector2(10, 10),
                    Width = 52,
                    Height = 52,
                    Color = Color.LimeGreen * 0.7f,
                    Duration = 0.3f
                });
            }
        }

        // Game Over & Revive condition
        if (_player.HP <= 0)
        {
            if (_player.CurrentRevives > 0)
            {
                _player.CurrentRevives--;
                _player.HP = _player.MaxHP * 0.5f; // Revive with 50% HP
                VisualEffects.Add(new VisualEffect
                {
                    Position = _player.Position - new Vector2(20, 20),
                    Width = 72,
                    Height = 72,
                    Color = Color.Gold * 0.9f,
                    Duration = 0.8f
                });
            }
            else
            {
                // Save gold collected this run (scaled by permanent Greed upgrade)
                int earnedGold = (int)(_goldCollectedThisRun * _player.PermanentGreedBoost);
                _saveData.Gold += earnedGold;

                // Save survival time high score
                if (_gameTimer > _saveData.BestSurvivalTime)
                {
                    _saveData.BestSurvivalTime = _gameTimer;
                }
                _saveData.Save();
                
                _gameState = GameState.GameOver;
                return;
            }
        }

        // 2. Passive: Regen Ring (Heals every 1 second)
        if (_player.RegenRingLevel > 0)
        {
            _regenTimer += deltaTime;
            if (_regenTimer >= 1.0f)
            {
                float heal = 0;
                if (_player.RegenRingLevel == 1) heal = 0.5f;
                else if (_player.RegenRingLevel == 2) heal = 1.0f;
                else if (_player.RegenRingLevel == 3) heal = 2.0f;
                else if (_player.RegenRingLevel == 4) heal = 3.0f;
                else if (_player.RegenRingLevel == 5) heal = 5.0f;

                _player.HP = MathHelper.Min(_player.HP + heal, _player.MaxHP);
                _regenTimer = 0f;
            }
        }

        // 2.5 Passive: Pummarola HP recovery (+0.5 HP/s per level)
        if (_player.PummarolaLevel > 0)
        {
            _player.HP = MathHelper.Min(_player.HP + (_player.PummarolaLevel * 0.5f) * deltaTime, _player.MaxHP);
        }

        // 2.6 Giant Boss Spawn check (Stage-dependent interval)
        int bossInt = GetCurrentStageConfig().BossSpawnIntervalSeconds;
        int currentBossInterval = (int)(_gameTimer / bossInt);
        if (currentBossInterval > _lastSpawnedFiveMinBossInterval)
        {
            SpawnSuperBoss(currentBossInterval);
            _lastSpawnedFiveMinBossInterval = currentBossInterval;
        }

        // 2.7 30-Minute Victory check
        if (_gameTimer >= 1800f && !_hasWonRun)
        {
            _gameState = GameState.VictoryChoice;
        }

        // 3. Update Weapons (Only those unlocked/lvl > 0, scaling with shop damage multiplier)
        foreach (var weapon in _weapons)
        {
            if (weapon.Level > 0)
            {
                // We wrap update logic. Weapon damage will be multiplied in bullet collision or logic.
                weapon.Update(gameTime, _player, _enemies, _projectiles, _random);
            }
        }

        // Apply Player Damage multiplier to newly shot projectiles
        foreach (var proj in _projectiles)
        {
            if (proj.Damage > 0 && !proj.IsEnemyProjectile && proj.PenetrationCount > 0)
            {
                // Multiply only once by using a tag or flag if needed, but since it's already set on spawn,
                // we actually scale weapons damage in the weapon update or during projectile creation.
                // In Weapon.cs, we calculate dmg = baseDmg * spinach. Let's multiply it by player.DamageMultiplier!
            }
        }

        // 4. Update Spawning based on run time & Selected Stage
        _spawnTimer += deltaTime;
        var config = GetCurrentStageConfig();
        float currentInterval = _spawnInterval * config.SpawnIntervalMultiplier;
        if (_spawnTimer >= currentInterval)
        {
            for (int i = 0; i < config.SpawnBatchCount; i++)
            {
                SpawnTimeBasedEnemy();
            }
            _spawnTimer = 0f;
            _spawnInterval = MathHelper.Max(_spawnInterval - 0.008f, 0.5f); // Slower difficulty progression
        }

        // Boss spawns checking based on stage index
        if (config.Index <= 3) // Easy: 1 boss at 2:00 (120s)
        {
            if (_gameTimer >= 120f && !_golemSpawned1)
            {
                SpawnGolemBoss();
                _golemSpawned1 = true;
            }
        }
        else if (config.Index <= 6) // Medium: 2 bosses (at 1:30 and 3:00)
        {
            if (_gameTimer >= 90f && !_golemSpawned1)
            {
                SpawnGolemBoss();
                _golemSpawned1 = true;
            }
            if (_gameTimer >= 180f && !_golemSpawned2)
            {
                SpawnGolemBoss();
                _golemSpawned2 = true;
            }
        }
        else // Hard: 3 bosses (at 1:00, 2:00, and 3:00)
        {
            if (_gameTimer >= 60f && !_golemSpawned1)
            {
                SpawnGolemBoss();
                _golemSpawned1 = true;
            }
            if (_gameTimer >= 120f && !_golemSpawned2)
            {
                SpawnGolemBoss();
                _golemSpawned2 = true;
            }
            if (_gameTimer >= 180f && !_golemSpawned3)
            {
                SpawnGolemBoss();
                _golemSpawned3 = true;
            }
        }

        // 5. Update Holy Water Pools
        for (int i = HolyWaterPools.Count - 1; i >= 0; i--)
        {
            var pool = HolyWaterPools[i];
            pool.Duration -= deltaTime;

            // Damage enemies inside every 0.25 seconds
            pool.TickTimer += deltaTime;
            if (pool.TickTimer >= 0.25f)
            {
                foreach (var enemy in _enemies)
                {
                    if (Vector2.Distance(pool.Position, enemy.Position) <= pool.Radius)
                    {
                        enemy.HP -= pool.Damage * _player.DamageMultiplier;
                    }
                }
                pool.TickTimer = 0f;
            }

            if (pool.Duration <= 0)
            {
                HolyWaterPools.RemoveAt(i);
            }
        }

        // 6. Update Visual Effects
        for (int i = VisualEffects.Count - 1; i >= 0; i--)
        {
            var effect = VisualEffects[i];
            effect.Duration -= deltaTime;
            if (effect.Duration <= 0)
            {
                VisualEffects.RemoveAt(i);
            }
        }

        // 7. Update Player Projectiles (apply damage multiplier)
        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            var proj = _projectiles[i];
            if (_stageMechanics.SandstormActive && _currentStage == 8)
                proj.Velocity += _stageMechanics.SandstormDirection * 40f * deltaTime;

            proj.Update(gameTime, _player.Center, _player);

            if (!proj.IsEnemyProjectile)
                CheckProjectileObstacleCollision(proj);

            for (int j = _enemies.Count - 1; j >= 0; j--)
            {
                var enemy = _enemies[j];
                if (proj.Bounds.Intersects(enemy.Bounds))
                {
                    // Scale damage with permanent shop multiplier!
                    enemy.HP -= proj.Damage * _player.DamageMultiplier;
                    proj.PenetrationCount--;
                    
                    if (proj.PenetrationCount <= 0)
                    {
                        proj.IsDestroyed = true;
                        break;
                    }
                }
            }

            if (proj.IsDestroyed)
            {
                _projectiles.RemoveAt(i);
            }
        }

        // 8. Update Enemy Projectiles
        for (int i = _enemyProjectiles.Count - 1; i >= 0; i--)
        {
            var proj = _enemyProjectiles[i];
            proj.Update(gameTime, _player.Center, _player);

            // Collide with Player
            if (proj.Bounds.Intersects(_player.Bounds))
            {
                float finalDamage = proj.Damage;
                if (_player.ShieldLevel > 0)
                {
                    float reduction = MathHelper.Min(_player.ShieldLevel * 0.1f, 0.4f);
                    finalDamage *= (1f - reduction);
                }
                // Apply permanent armor flat reduction
                finalDamage = MathHelper.Max(1f, finalDamage - _player.PermanentArmor);
                _player.HP -= finalDamage;
                proj.IsDestroyed = true;
            }

            if (proj.IsDestroyed)
            {
                _enemyProjectiles.RemoveAt(i);
            }
        }

        // 9. Update Enemies
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            var enemy = _enemies[i];
            enemy.Update(gameTime, _player.Center, _obstacles, _terrainGrid);

            // Ranged enemy shoots arrow check
            if (enemy.ShouldShoot)
            {
                _enemyProjectiles.Add(new Projectile(
                    enemy.Position,
                    _player.Center,
                    speed: 240f,
                    damage: 10f,
                    isFireball: false,
                    isAxe: false,
                    hasGravity: false,
                    penetrationCount: 1,
                    isEnemyProjectile: true
                ));
                enemy.ShouldShoot = false;
            }

            // Contact damage with Player
            if (_player.Bounds.Intersects(enemy.Bounds))
            {
                float finalDamage = enemy.Damage;
                if (_player.ShieldLevel > 0)
                {
                    float reduction = MathHelper.Min(_player.ShieldLevel * 0.1f, 0.4f);
                    finalDamage *= (1f - reduction);
                }
                
                finalDamage = MathHelper.Max(1f, finalDamage - _player.PermanentArmor);
                _player.HP -= finalDamage * deltaTime;
                if (_player.HP < 0) _player.HP = 0;

                if (_currentStage == 10 && (enemy.Type == EnemyType.Skeleton || enemy.Type == EnemyType.Bat))
                {
                    if (_random.Next(100) < 12)
                        _player.FreezeTimer = 1.0f;
                }

                // Reflection damage (Shield level 4 & 5)
                if (_player.ShieldLevel >= 4)
                {
                    float reflectionRate = _player.ShieldLevel == 4 ? 0.15f : 0.30f;
                    enemy.HP -= enemy.Damage * reflectionRate * deltaTime;
                }
            }

            // If enemy died
            if (enemy.IsDead)
            {
                if (enemy.Type == EnemyType.GolemBoss)
                {
                    // Spawn Boss Chest and 15 Gold coins!
                    Vector2 dropPos = FindNearestWalkablePosition(enemy.Position, 28, 28);
                    _chests.Add(new Chest(dropPos));
                    _coins.Add(new Coin(dropPos, 15));
                }
                else
                {
                    // Spawn normal XP Gem
                    Vector2 dropPos = FindNearestWalkablePosition(enemy.Position, 20, 20);
                    _gems.Add(new Gem(dropPos, 2f));
                    
                    // 40% chance to drop 1 Gold coin
                    if (_random.Next(100) < 40)
                    {
                        _coins.Add(new Coin(dropPos, 1));
                    }
                }
                
                _killCount++;
                _enemies.RemoveAt(i);
            }
        }

        // 10. Update Gems
        float magnetRange = 120f * (1f + _player.AttractorbLevel * 0.25f);
        for (int i = _gems.Count - 1; i >= 0; i--)
        {
            var gem = _gems[i];
            gem.Update(gameTime, _player.Center, magnetRange);

            if (_player.Bounds.Intersects(gem.Bounds))
            {
                _player.XP += gem.XPValue;
                gem.IsCollected = true;

                if (_player.XP >= _player.TargetXP)
                {
                    _player.XP -= _player.TargetXP;
                    _player.Level++;
                    _player.TargetXP = (int)(_player.TargetXP * 1.5f);
                    
                    GenerateUpgradeChoices();
                    _gameState = GameState.LevelUpUpgrades;
                }
            }

            if (gem.IsCollected)
            {
                _gems.RemoveAt(i);
            }
        }

        // 11. Update Gold Coins (attracted by magnet too!)
        for (int i = _coins.Count - 1; i >= 0; i--)
        {
            var coin = _coins[i];
            coin.Update(gameTime, _player.Center, magnetRange);

            if (_player.Bounds.Intersects(coin.Bounds))
            {
                _goldCollectedThisRun += coin.Value;
                coin.IsCollected = true;
            }

            if (coin.IsCollected)
            {
                _coins.RemoveAt(i);
            }
        }

        // 12. Update Chests (Intersects checking)
        for (int i = _chests.Count - 1; i >= 0; i--)
        {
            var chest = _chests[i];
            if (_player.Bounds.Intersects(chest.Bounds))
            {
                OpenChest(chest);
                _chests.RemoveAt(i);
                break; // Pause gameplay and open chest rewards screen
            }
        }
    }

    private void GenerateUpgradeChoices()
    {
        _upgradeChoices.Clear();
        List<UpgradeOption> allOptions = new List<UpgradeOption>();

        // Add Active Weapons
        if (GetWeapon("Magic Wand").Level < 5)
        {
            int nextLvl = GetWeapon("Magic Wand").Level + 1;
            string desc = nextLvl == 2 ? "+20% Sat thuong truong phep" :
                          nextLvl == 3 ? "Ban them +1 tia phep cung luc" :
                          nextLvl == 4 ? "-20% Thoi gian hoi truong phep" : "Tia phep co the xuyen thau 1 quai";
            allOptions.Add(new UpgradeOption { Code = "WAND", Title = $"Magic Wand (Cap {nextLvl})", Description = desc });
        }
        
        if (GetWeapon("Garlic Aura").Level < 5)
        {
            int nextLvl = GetWeapon("Garlic Aura").Level + 1;
            string desc = nextLvl == 1 ? "Mo khoa: Hao quang toi bao ve quanh nguoi" :
                          nextLvl == 2 ? "+20% Ban kinh vong bao ve" :
                          nextLvl == 3 ? "+30% Sat thuong hao quang" :
                          nextLvl == 4 ? "Day lui nhe quai cham vao hao quang" : "Tang gap doi sat thuong hao quang";
            allOptions.Add(new UpgradeOption { Code = "GARLIC", Title = nextLvl == 1 ? "Unlock Garlic Aura" : $"Garlic Aura (Cap {nextLvl})", Description = desc });
        }

        if (GetWeapon("Fireball").Level < 5)
        {
            int nextLvl = GetWeapon("Fireball").Level + 1;
            string desc = nextLvl == 1 ? "Mo khoa: Cau lua no cuc manh xuyen thau 3 quai" :
                          nextLvl == 2 ? "Tang toc do bay qua cau lua" :
                          nextLvl == 3 ? "+40% Sat thuong no dien rong" :
                          nextLvl == 4 ? "Cau lua phat no lan rong khi bien mat" : "Ban 2 cau lua cung luc ngau nhien";
            allOptions.Add(new UpgradeOption { Code = "FIREBALL", Title = nextLvl == 1 ? "Unlock Fireball" : $"Fireball (Cap {nextLvl})", Description = desc });
        }

        if (GetWeapon("Axe").Level < 5)
        {
            int nextLvl = GetWeapon("Axe").Level + 1;
            string desc = nextLvl == 1 ? "Mo khoa: Quang chiec Riu xoay tron bay cau vong" :
                          nextLvl == 2 ? "+20% Sat thuong riu quang" :
                          nextLvl == 3 ? "Quang 2 chiec Riu doi xung nhau" :
                          nextLvl == 4 ? "Riu co kha nang xuyen thau vo tan" : "Quang 3 chiec Riu cuc manh cung luc";
            allOptions.Add(new UpgradeOption { Code = "AXE", Title = nextLvl == 1 ? "Unlock Axe" : $"Axe (Cap {nextLvl})", Description = desc });
        }

        if (GetWeapon("Lightning Ring").Level < 5)
        {
            int nextLvl = GetWeapon("Lightning Ring").Level + 1;
            string desc = nextLvl == 1 ? "Mo khoa: Set giat ngau nhien tu troi xuong quai" :
                          nextLvl == 2 ? "+10 Sat thuong sam set" :
                          nextLvl == 3 ? "Set danh dong thoi 2 muc tieu" :
                          nextLvl == 4 ? "-25% Thoi gian hoi sam set" : "Set danh dong thoi 3 muc tieu";
            allOptions.Add(new UpgradeOption { Code = "LIGHTNING", Title = nextLvl == 1 ? "Unlock Lightning Ring" : $"Lightning Ring (Cap {nextLvl})", Description = desc });
        }

        if (GetWeapon("Holy Water").Level < 5)
        {
            int nextLvl = GetWeapon("Holy Water").Level + 1;
            string desc = nextLvl == 1 ? "Mo khoa: Nem lo nuoc tao vung sat thuong lien tuc" :
                          nextLvl == 2 ? "+10px Ban kinh vung nuoc thanh" :
                          nextLvl == 3 ? "+1 Giay thoi gian ton tai vung nuoc" :
                          nextLvl == 4 ? "Nem dong thoi 2 lo nuoc thanh" : "Tang +30% Sat thuong nuoc thanh";
            allOptions.Add(new UpgradeOption { Code = "HOLYWATER", Title = nextLvl == 1 ? "Unlock Holy Water" : $"Holy Water (Cap {nextLvl})", Description = desc });
        }

        if (GetWeapon("Cross").Level < 5)
        {
            int nextLvl = GetWeapon("Cross").Level + 1;
            string desc = nextLvl == 1 ? "Mo khoa: Chiec Thap Tu bay nhu Boomerang xuyen thau" :
                          nextLvl == 2 ? "+20% Sat thuong" :
                          nextLvl == 3 ? "Ban 2 chiec Thap Tu cung luc" :
                          nextLvl == 4 ? "Tang ban kinh bay cua Thap Tu" : "Ban 3 chiec Thap Tu cuc manh";
            allOptions.Add(new UpgradeOption { Code = "CROSS", Title = nextLvl == 1 ? "Unlock Cross" : $"Cross (Cap {nextLvl})", Description = desc });
        }

        if (GetWeapon("King Bible").Level < 5)
        {
            int nextLvl = GetWeapon("King Bible").Level + 1;
            string desc = nextLvl == 1 ? "Mo khoa: Sach Kinh Thanh xoay tron bao ve quanh nguoi" :
                          nextLvl == 2 ? "Tang +1 chiec sach quyen nang" :
                          nextLvl == 3 ? "+30% Thoi gian ton tai sach" :
                          nextLvl == 4 ? "Tang +1 chiec sach quyen nang" : "Tang gap doi sat thuong sach";
            allOptions.Add(new UpgradeOption { Code = "BIBLE", Title = nextLvl == 1 ? "Unlock King Bible" : $"King Bible (Cap {nextLvl})", Description = desc });
        }

        if (GetWeapon("Knife").Level < 5)
        {
            int nextLvl = GetWeapon("Knife").Level + 1;
            string desc = nextLvl == 1 ? "Mo khoa: Phong dao lien tuc ve huong di chuyen" :
                          nextLvl == 2 ? "-20% Cooldown ban dao" :
                          nextLvl == 3 ? "Phong 2 dao cung luc" :
                          nextLvl == 4 ? "Dao co the xuyen thau 1 quai" : "Phong 3 dao cung luc cuc nhanh";
            allOptions.Add(new UpgradeOption { Code = "KNIFE", Title = nextLvl == 1 ? "Unlock Knife" : $"Knife (Cap {nextLvl})", Description = desc });
        }

        // Add Passives
        if (_player.SpeedBootsLevel < 5)
            allOptions.Add(new UpgradeOption { Code = "BOOTS", Title = $"Speed Boots (Cap {_player.SpeedBootsLevel + 1})", Description = "+15% Toc do di chuyen nhan vat" });

        if (_player.RegenRingLevel < 5)
        {
            string desc = "";
            int nextLvl = _player.RegenRingLevel + 1;
            if (nextLvl == 1) desc = "Tu dong hoi 1 HP moi 2 giay";
            else if (nextLvl == 2) desc = "Hoi 1 HP moi 1 giay";
            else if (nextLvl == 3) desc = "Hoi 2 HP moi 1 giay";
            else if (nextLvl == 4) desc = "Hoi 3 HP moi 1 giay";
            else if (nextLvl == 5) desc = "Hoi 5 HP moi 1 giay";
            allOptions.Add(new UpgradeOption { Code = "REGEN", Title = nextLvl == 1 ? "Unlock Regen Ring" : $"Regen Ring (Cap {nextLvl})", Description = desc });
        }

        if (_player.ShieldLevel < 5)
        {
            string desc = "";
            int nextLvl = _player.ShieldLevel + 1;
            if (nextLvl == 1) desc = "Giam 10% sat thuong quai va cham";
            else if (nextLvl == 2) desc = "Giam 20% sat thuong nhan vao";
            else if (nextLvl == 3) desc = "Giam 30% sat thuong nhan vao";
            else if (nextLvl == 4) desc = "Phan lai 15% sat thuong nhan vao len quai";
            else if (nextLvl == 5) desc = "Giam 40% sat thuong & Phan 30% sat thuong";
            allOptions.Add(new UpgradeOption { Code = "SHIELD", Title = nextLvl == 1 ? "Unlock Shield" : $"Shield (Cap {nextLvl})", Description = desc });
        }

        if (_player.SpinachLevel < 5)
            allOptions.Add(new UpgradeOption { Code = "SPINACH", Title = $"Spinach (Cap {_player.SpinachLevel + 1})", Description = "+10% Sat thuong cho toan bo vu khi chu dong" });

        if (_player.AttractorbLevel < 5)
            allOptions.Add(new UpgradeOption { Code = "MAGNET", Title = $"Attractorb (Cap {_player.AttractorbLevel + 1})", Description = "+25% Ban kinh hut ngoc kinh nghiem" });

        if (_player.DuplicatorLevel < 5)
            allOptions.Add(new UpgradeOption { Code = "DUPLICATOR", Title = $"Duplicator (Cap {_player.DuplicatorLevel + 1})", Description = "+1 So luong tia/dan cho tat ca vu khi ban" });

        if (_player.CloverLevel < 5)
            allOptions.Add(new UpgradeOption { Code = "CLOVER", Title = $"Clover (Cap {_player.CloverLevel + 1})", Description = "+15% May man (Tang co hoi lay do nang cap tot, vang)" });

        if (_player.HollowHeartLevel < 5)
            allOptions.Add(new UpgradeOption { Code = "HEART", Title = $"Hollow Heart (Cap {_player.HollowHeartLevel + 1})", Description = "+20% Mau toi da nhan vat" });

        if (_player.PummarolaLevel < 5)
            allOptions.Add(new UpgradeOption { Code = "PUMMAROLA", Title = $"Pummarola (Cap {_player.PummarolaLevel + 1})", Description = "+0.5 HP tu dong phuc hoi moi giay" });

        if (_player.BracerLevel < 5)
            allOptions.Add(new UpgradeOption { Code = "BRACER", Title = $"Bracer (Cap {_player.BracerLevel + 1})", Description = "+15% Toc do bay cua tat ca cac loai dan/riu" });

        if (_player.SpellbinderLevel < 5)
            allOptions.Add(new UpgradeOption { Code = "SPELLBINDER", Title = $"Spellbinder (Cap {_player.SpellbinderLevel + 1})", Description = "+15% Thoi gian ton tai & Kich thuoc hieu ung" });

        if (_player.EmptyTomeLevel < 5)
            allOptions.Add(new UpgradeOption { Code = "TOME", Title = $"Empty Tome (Cap {_player.EmptyTomeLevel + 1})", Description = "-8% Cooldown (Thoi gian cho) toan bo vu khi" });

        // Clover / Luck increases choices options count!
        float totalLuck = _player.PermanentLuckBoost + _player.CloverLevel * 0.15f;
        int luckyChoices = (int)(totalLuck - 1f) * 2; // +2 options if very lucky
        int choicesCount = Math.Min(Math.Min(2 + _currentStage + luckyChoices, 6), allOptions.Count);
        
        for (int i = 0; i < choicesCount; i++)
        {
            int idx = _random.Next(allOptions.Count);
            _upgradeChoices.Add(allOptions[idx]);
            allOptions.RemoveAt(idx);
        }

        // Add Reroll option if player is at Stage 2+ and has Clover
        if (_currentStage >= 2 && _upgradeChoices.Count > 0 && totalLuck >= 1.15f)
        {
            _upgradeChoices.Add(new UpgradeOption { Code = "REROLL", Title = "REROLL CHOICES", Description = "Xao bai lam lai de chon cac nang cap khac" });
        }

        if (_upgradeChoices.Count == 0)
        {
            _upgradeChoices.Add(new UpgradeOption { Code = "HEAL", Title = "Heal to Full", Description = "Hoi phuc 100% thanh mau hien tai" });
        }
    }

    private void UpdateUpgrades(KeyboardState keyboardState)
    {
        bool choice1 = keyboardState.IsKeyDown(Keys.D1) && _previousKeyboardState.IsKeyUp(Keys.D1);
        bool choice2 = keyboardState.IsKeyDown(Keys.D2) && _previousKeyboardState.IsKeyUp(Keys.D2);
        bool choice3 = keyboardState.IsKeyDown(Keys.D3) && _previousKeyboardState.IsKeyUp(Keys.D3);
        bool choice4 = keyboardState.IsKeyDown(Keys.D4) && _previousKeyboardState.IsKeyUp(Keys.D4);
        bool choice5 = keyboardState.IsKeyDown(Keys.D5) && _previousKeyboardState.IsKeyUp(Keys.D5);
        bool choice6 = keyboardState.IsKeyDown(Keys.D6) && _previousKeyboardState.IsKeyUp(Keys.D6);
        bool choice7 = keyboardState.IsKeyDown(Keys.D7) && _previousKeyboardState.IsKeyUp(Keys.D7);

        UpgradeOption selected = null;
        if (choice1 && _upgradeChoices.Count >= 1) selected = _upgradeChoices[0];
        else if (choice2 && _upgradeChoices.Count >= 2) selected = _upgradeChoices[1];
        else if (choice3 && _upgradeChoices.Count >= 3) selected = _upgradeChoices[2];
        else if (choice4 && _upgradeChoices.Count >= 4) selected = _upgradeChoices[3];
        else if (choice5 && _upgradeChoices.Count >= 5) selected = _upgradeChoices[4];
        else if (choice6 && _upgradeChoices.Count >= 6) selected = _upgradeChoices[5];
        else if (choice7 && _upgradeChoices.Count >= 7) selected = _upgradeChoices[6];

        if (selected != null)
        {
            if (selected.Code == "REROLL")
            {
                // Re-generate choices (Reroll)
                GenerateUpgradeChoices();
            }
            else
            {
                ApplyUpgrade(selected.Code);
                _gameState = GameState.Playing;
            }
        }
    }

    private void ApplyUpgrade(string code)
    {
        switch (code)
        {
            case "WAND": GetWeapon("Magic Wand").Level++; break;
            case "GARLIC": GetWeapon("Garlic Aura").Level++; break;
            case "FIREBALL": GetWeapon("Fireball").Level++; break;
            case "AXE": GetWeapon("Axe").Level++; break;
            case "LIGHTNING": GetWeapon("Lightning Ring").Level++; break;
            case "HOLYWATER": GetWeapon("Holy Water").Level++; break;
            case "CROSS": GetWeapon("Cross").Level++; break;
            case "BIBLE": GetWeapon("King Bible").Level++; break;
            case "KNIFE": GetWeapon("Knife").Level++; break;
            
            case "BOOTS": _player.SpeedBootsLevel++; break;
            case "REGEN": _player.RegenRingLevel++; break;
            case "SHIELD": _player.ShieldLevel++; break;
            case "SPINACH": _player.SpinachLevel++; break;
            case "MAGNET": _player.AttractorbLevel++; break;
            case "DUPLICATOR": _player.DuplicatorLevel++; break;
            case "CLOVER": _player.CloverLevel++; break;
            case "HEART": 
                _player.HollowHeartLevel++;
                float oldMax = _player.MaxHP;
                _player.MaxHP = 100f * (1f + _saveData.MaxHPUpgradeLevel * 0.10f) * (1f + _player.HollowHeartLevel * 0.20f);
                _player.HP += (_player.MaxHP - oldMax);
                break;
            case "PUMMAROLA": _player.PummarolaLevel++; break;
            case "BRACER": _player.BracerLevel++; break;
            case "SPELLBINDER": _player.SpellbinderLevel++; break;
            case "TOME": _player.EmptyTomeLevel++; break;

            case "HEAL":
                float healAmt = 20f + _currentStage * 10f;
                _player.HP = Math.Min(_player.HP + healAmt, _player.MaxHP);
                break;
        }
    }

    private void OpenChest(Chest chest)
    {
        _isChestChoiceActive = true;
        _selectedChestChoiceIndex = 0;
        _chestChoices.Clear();

        // 1. Choice: Gold & Heal (scaled by Greed permanent upgrade)
        int goldAmt = 35 + _random.Next(26);
        goldAmt = (int)(goldAmt * _player.PermanentGreedBoost);
        if (_player.GoldBoosterActive) goldAmt *= 2; // double gold coins boost!
        int healAmt = (int)(_player.MaxHP * 0.35f);
        _chestChoices.Add(new UpgradeOption { 
            Code = $"GOLD_HEAL_{goldAmt}_{healAmt}", 
            Title = "Vang & Tri Lieu", 
            Description = $"+{goldAmt} Vang & Hoi phuc +{healAmt} HP (35% Max)" 
        });

        // 2. Choice: EXP Bundle (scaled by Growth permanent upgrade)
        int expAmt = 20 + _player.Level * 4;
        expAmt = (int)(expAmt * _player.PermanentGrowthBoost);
        _chestChoices.Add(new UpgradeOption { 
            Code = $"EXP_{expAmt}", 
            Title = "Tri Thuc Co Dai", 
            Description = $"+{expAmt} Kinh Nghiem giup tang cap nhanh hon" 
        });

        // 3. Choice: Specific Item Upgrade (pick a random active weapon/passive upgrade or new unlock)
        List<UpgradeOption> upgradeOptionsList = new List<UpgradeOption>();
        foreach (var w in _weapons)
        {
            if (w.Level > 0 && w.Level < 5)
            {
                upgradeOptionsList.Add(new UpgradeOption { 
                    Code = $"WEAPON_{w.Name}", 
                    Title = $"Nang cap {w.Name}", 
                    Description = $"Nang cap vu khi {w.Name} len Cap {w.Level + 1}" 
                });
            }
            else if (w.Level == 0)
            {
                upgradeOptionsList.Add(new UpgradeOption { 
                    Code = $"WEAPON_{w.Name}", 
                    Title = $"Mo khoa {w.Name}", 
                    Description = $"So huu them vu khi {w.Name} ban tu dong" 
                });
            }
        }
        
        Action<string, string, int> addPassiveOpt = (code, name, lvl) =>
        {
            if (lvl > 0 && lvl < 5)
            {
                upgradeOptionsList.Add(new UpgradeOption { 
                    Code = $"PASSIVE_{code}", 
                    Title = $"Nang cap {name}", 
                    Description = $"Tang vat pham {name} len Cap {lvl + 1}" 
                });
            }
            else if (lvl == 0)
            {
                upgradeOptionsList.Add(new UpgradeOption { 
                    Code = $"PASSIVE_{code}", 
                    Title = $"Mo khoa {name}", 
                    Description = $"So huu ho tro {name} tang vinh vien chi so run" 
                });
            }
        };

        addPassiveOpt("BOOTS", "Speed Boots", _player.SpeedBootsLevel);
        addPassiveOpt("REGEN", "Regen Ring", _player.RegenRingLevel);
        addPassiveOpt("SHIELD", "Shield", _player.ShieldLevel);
        addPassiveOpt("SPINACH", "Spinach", _player.SpinachLevel);
        addPassiveOpt("MAGNET", "Attractorb", _player.AttractorbLevel);
        addPassiveOpt("DUPLICATOR", "Duplicator", _player.DuplicatorLevel);
        addPassiveOpt("CLOVER", "Clover", _player.CloverLevel);
        addPassiveOpt("HEART", "Hollow Heart", _player.HollowHeartLevel);
        addPassiveOpt("PUMMAROLA", "Pummarola", _player.PummarolaLevel);
        addPassiveOpt("BRACER", "Bracer", _player.BracerLevel);
        addPassiveOpt("SPELLBINDER", "Spellbinder", _player.SpellbinderLevel);
        addPassiveOpt("TOME", "Empty Tome", _player.EmptyTomeLevel);

        if (upgradeOptionsList.Count > 0)
        {
            int rIdx = _random.Next(upgradeOptionsList.Count);
            _chestChoices.Add(upgradeOptionsList[rIdx]);
        }
        else
        {
            _chestChoices.Add(new UpgradeOption { 
                Code = "GOLD_150", 
                Title = "Tieu Diet Thuong Gia", 
                Description = "+150 Vang (Tat ca cac vat pham da dat max!)" 
            });
        }

        // 4. Choice: HP Potion Refill
        _chestChoices.Add(new UpgradeOption { 
            Code = "POTION", 
            Title = "Binh Mau Tiep Te", 
            Description = "+1 Binh Mau vao hanh trang sinh ton (Phim H)" 
        });

        // 5. Clover/Luck 5th Option
        float totalLuck = _player.PermanentLuckBoost + _player.CloverLevel * 0.15f;
        if (totalLuck >= 1.2f && _random.Next(100) < (totalLuck - 1.0f) * 100f)
        {
            int sGold = (int)(200 * _player.PermanentGreedBoost);
            if (_player.GoldBoosterActive) sGold *= 2;
            _chestChoices.Add(new UpgradeOption { 
                Code = "SUPER_GOLD", 
                Title = "MAY MAN THIEN AN! PHAT LOC", 
                Description = $"+{sGold} Vang & Tieu diet ke thu yeu xung quanh!" 
            });
        }

        _gameState = GameState.ChestOpen;
    }

    private void SpawnStageEnemy(Vector2 spawnPos, EnemyType type)
    {
        spawnPos = FindNearestWalkablePosition(spawnPos, 32, 32);
        var enemy = new Enemy(spawnPos, type);
        enemy.Position = FindNearestWalkablePosition(enemy.Position, enemy.Width, enemy.Height);
        if (!IsWalkableAndFree(enemy.Position, enemy.Width, enemy.Height))
            return;
        ApplyStageEnemyScaling(enemy);
        _enemies.Add(enemy);
    }

    private bool IntersectsBlockingObstacle(Rectangle rect)
    {
        foreach (var obs in _obstacles)
        {
            if (obs.IsDestroyed || !obs.BlocksMovement) continue;
            if (rect.Intersects(obs.Bounds)) return true;
        }
        return false;
    }

    private bool IsWalkableAndFree(Vector2 pos, int width, int height)
    {
        if (!MapTerrain.IsWalkableAt(_terrainGrid, pos, width, height)) return false;
        return !IntersectsBlockingObstacle(new Rectangle((int)pos.X, (int)pos.Y, width, height));
    }

    private Vector2 FindNearestWalkablePosition(Vector2 origin, int width, int height)
    {
        origin.X = MathHelper.Clamp(origin.X, 0, MapWidth - width);
        origin.Y = MathHelper.Clamp(origin.Y, 0, MapHeight - height);
        if (IsWalkableAndFree(origin, width, height))
            return origin;

        int step = MapTerrain.CellSize;
        for (int ring = 1; ring <= 18; ring++)
        {
            for (int dx = -ring; dx <= ring; dx++)
            {
                for (int dy = -ring; dy <= ring; dy++)
                {
                    if (Math.Abs(dx) != ring && Math.Abs(dy) != ring) continue;
                    Vector2 probe = new Vector2(origin.X + dx * step, origin.Y + dy * step);
                    probe.X = MathHelper.Clamp(probe.X, 0, MapWidth - width);
                    probe.Y = MathHelper.Clamp(probe.Y, 0, MapHeight - height);
                    if (IsWalkableAndFree(probe, width, height))
                        return probe;
                }
            }
        }

        Vector2 fallback = new Vector2(_player.Position.X, _player.Position.Y);
        fallback.X = MathHelper.Clamp(fallback.X, 0, MapWidth - width);
        fallback.Y = MathHelper.Clamp(fallback.Y, 0, MapHeight - height);
        if (IsWalkableAndFree(fallback, width, height))
            return fallback;

        return origin;
    }

    private void ApplyStageEnemyScaling(Enemy enemy)
    {
        var config = GetCurrentStageConfig();
        float diffMult = 1f + (_gameTimer / 60f) * 0.15f;
        float diffSettingMult = _settingsDifficulty == 0 ? 1.0f : (_settingsDifficulty == 1 ? 1.35f : 1.70f);

        enemy.HP *= diffMult * diffSettingMult * config.MonsterHPMultiplier;
        enemy.MaxHP = enemy.HP;
        enemy.Damage *= diffMult * diffSettingMult;
        enemy.Speed *= (1f + (diffMult - 1f) * 0.18f) * config.MonsterSpeedMultiplier;
    }

    private void ApplyObstacleHazards(float deltaTime)
    {
        foreach (var obs in _obstacles)
        {
            if (obs.IsDestroyed || !obs.DealsContactDamage) continue;
            if (!_player.Bounds.Intersects(obs.Bounds)) continue;
            _player.HP = MathHelper.Max(0f, _player.HP - 6f * deltaTime);
        }
    }

    private void CheckProjectileObstacleCollision(Projectile proj)
    {
        if (proj.IsDestroyed) return;

        foreach (var obs in _obstacles)
        {
            if (obs.IsDestroyed || !obs.BlocksProjectiles) continue;
            if (!proj.Bounds.Intersects(obs.Bounds)) continue;

            if (obs.IsDestructible)
            {
                obs.HP -= proj.Damage * _player.DamageMultiplier;
                if (obs.HP <= 0f)
                    DestroyObstacle(obs);
            }

            proj.PenetrationCount--;
            if (proj.PenetrationCount <= 0)
            {
                proj.IsDestroyed = true;
                return;
            }
        }
    }

    private void DestroyObstacle(Obstacle obs)
    {
        if (obs.IsDestroyed) return;
        obs.IsDestroyed = true;
        obs.BlocksMovement = false;
        obs.BlocksProjectiles = false;

        if (obs.Loot == ObstacleLoot.Gold)
        {
            Vector2 dropPos = FindNearestWalkablePosition(obs.Position, 20, 20);
            _coins.Add(new Coin(dropPos, obs.LootAmount));
            _goldCollectedThisRun += obs.LootAmount;
        }
        else if (obs.Loot == ObstacleLoot.Health)
        {
            _player.HP = MathHelper.Min(_player.HP + obs.LootAmount, _player.MaxHP);
            VisualEffects.Add(new VisualEffect
            {
                Position = obs.Position,
                Width = 40,
                Height = 40,
                Color = Color.LimeGreen * 0.7f,
                Duration = 0.35f
            });
        }
    }

    private void SpawnTimeBasedEnemy()
    {
        var config = GetCurrentStageConfig();
        EnemyType typeToSpawn = PickEnemyTypeForStage(config.Index);
        Vector2 spawnPos = GetSpawnPositionForStage(config.Index);
        var enemy = new Enemy(spawnPos, typeToSpawn);
        enemy.Position = FindNearestWalkablePosition(enemy.Position, enemy.Width, enemy.Height);
        if (!IsWalkableAndFree(enemy.Position, enemy.Width, enemy.Height))
            return;
        ApplyStageEnemyScaling(enemy);
        _enemies.Add(enemy);
    }

    private EnemyType PickEnemyTypeForStage(int stage)
    {
        int roll = _random.Next(100);
        return stage switch
        {
            1 => EnemyType.Zombie,
            2 => roll < 35 ? EnemyType.Bat : roll < 65 ? EnemyType.Skeleton : EnemyType.Zombie,
            3 => roll < 55 ? EnemyType.Skeleton : EnemyType.Zombie,
            4 => roll < 40 ? EnemyType.Bat : EnemyType.Zombie,
            5 => roll < 30 ? EnemyType.Bat : roll < 55 ? EnemyType.Skeleton : EnemyType.Zombie,
            6 => roll < 35 ? EnemyType.Golem : roll < 65 ? EnemyType.Skeleton : EnemyType.Zombie,
            7 => roll < 50 ? EnemyType.Zombie : roll < 75 ? EnemyType.Bat : EnemyType.Skeleton,
            8 => roll < 45 ? EnemyType.Skeleton : roll < 70 ? EnemyType.Bat : EnemyType.Zombie,
            9 => roll < 50 ? EnemyType.Bat : EnemyType.Zombie,
            10 => roll < 40 ? EnemyType.Skeleton : roll < 70 ? EnemyType.Golem : EnemyType.Bat,
            _ => EnemyType.Zombie
        };
    }

    private Vector2 GetSpawnPositionForStage(int stage)
    {
        if (stage == 3)
        {
            bool fromLeft = _random.Next(2) == 0;
            float x = fromLeft ? 80f : MapWidth - 80f;
            float y = _player.Center.Y + _random.Next(-120, 121);
            return new Vector2(x, MathHelper.Clamp(y, 120f, MapHeight - 120f));
        }

        if (stage == 9)
        {
            float x = _player.Center.X + _random.Next(-80, 81);
            return new Vector2(MathHelper.Clamp(x, 80f, MapWidth - 80f), _player.Center.Y - 380f);
        }

        double angle = _random.NextDouble() * Math.PI * 2;
        float dist = stage == 1 ? 560f : 500f;
        return _player.Center + new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
    }

    private void SpawnGolemBoss()
    {
        var config = GetCurrentStageConfig();
        double angle = _random.NextDouble() * Math.PI * 2;
        Vector2 spawnPos = _player.Center + new Vector2(
            (float)Math.Cos(angle) * 450f,
            (float)Math.Sin(angle) * 450f
        );

        float diffMult = 1f + (_gameTimer / 60f) * 0.15f;
        float diffSettingMult = _settingsDifficulty == 0 ? 1.0f : (_settingsDifficulty == 1 ? 1.35f : 1.70f);

        var boss = new Enemy(spawnPos, EnemyType.GolemBoss);
        boss.Position = FindNearestWalkablePosition(boss.Position, boss.Width, boss.Height);
        if (!IsWalkableAndFree(boss.Position, boss.Width, boss.Height))
            return;
        float stageHPMult = config.MonsterHPMultiplier;
        float stageSpeedMult = config.MonsterSpeedMultiplier;

        boss.HP *= (diffMult * diffSettingMult * 1.5f * stageHPMult);
        boss.MaxHP = boss.HP;
        boss.Damage *= (diffMult * diffSettingMult * 1.25f);
        boss.Speed *= stageSpeedMult;

        _enemies.Add(boss);
    }

    private void SpawnSuperBoss(int multiplier)
    {
        var config = GetCurrentStageConfig();
        double angle = _random.NextDouble() * Math.PI * 2;
        Vector2 spawnPos = _player.Center + new Vector2(
            (float)Math.Cos(angle) * 450f,
            (float)Math.Sin(angle) * 450f
        );

        float diffMult = 1f + (_gameTimer / 60f) * 0.15f;
        float diffSettingMult = _settingsDifficulty == 0 ? 1.0f : (_settingsDifficulty == 1 ? 1.35f : 1.70f);

        var superBoss = new Enemy(spawnPos, EnemyType.GolemBoss)
        {
            Width = 96,
            Height = 96
        };
        superBoss.Position = FindNearestWalkablePosition(superBoss.Position, superBoss.Width, superBoss.Height);
        if (!IsWalkableAndFree(superBoss.Position, superBoss.Width, superBoss.Height))
            return;
        float stageHPMult = config.MonsterHPMultiplier;
        float stageSpeedMult = config.MonsterSpeedMultiplier;

        // Giant Golem: 5x HP multiplier * five-minute interval multiplier * stage HP multiplier
        superBoss.HP = 200f * diffMult * diffSettingMult * 5f * multiplier * stageHPMult;
        superBoss.MaxHP = superBoss.HP;
        superBoss.Damage = 25f * diffMult * diffSettingMult * 1.5f;
        superBoss.Speed = 35f * 1.15f * stageSpeedMult;

        _enemies.Add(superBoss);

        // Flash screen effect or alert text
        _bossWarningText = "CANH BAO: THU LINH DAI VUONG CAP " + multiplier + " XUAT HIEN!";
        _bossWarningTimer = 4f;
    }

    protected override void Draw(GameTime gameTime)
    {
        if (!_mapshotsDone && Environment.GetCommandLineArgs().Length > 1 &&
            Environment.GetCommandLineArgs()[1] == "--mapshots")
        {
            _mapshotsDone = true;
            RenderMapshots();
            Exit();
            return;
        }

        _tooltipEntries.Clear();

        // 1. Render all game states to the virtual 800x600 RenderTarget
        GraphicsDevice.SetRenderTarget(_mainRenderTarget);
        GraphicsDevice.Clear(new Color(15, 15, 15));

        int screenWidth = 800;
        int screenHeight = 600;

        float cameraX = MathHelper.Clamp(_player.Center.X, screenWidth / 2f, MapWidth - screenWidth / 2f);
        float cameraY = MathHelper.Clamp(_player.Center.Y, screenHeight / 2f, MapHeight - screenHeight / 2f);
        
        Matrix cameraMatrix = Matrix.CreateTranslation(-cameraX, -cameraY, 0) *
                              Matrix.CreateTranslation(screenWidth / 2f, screenHeight / 2f, 0);

        switch (_gameState)
        {
            case GameState.Lobby:
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                DrawLobby(gameTime);
                DrawTooltips();
                _spriteBatch.End();
                break;

            case GameState.StageSelect:
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                DrawStageSelect();
                DrawTooltips();
                _spriteBatch.End();
                break;

            case GameState.ShopMenu:
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                DrawShopMenu();
                DrawTooltips();
                _spriteBatch.End();
                break;

            case GameState.SkinShop:
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                DrawSkinShop(gameTime);
                DrawTooltips();
                _spriteBatch.End();
                break;

            case GameState.Settings:
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                DrawSettings(gameTime);
                DrawTooltips();
                _spriteBatch.End();
                break;

            case GameState.Playing:
                _spriteBatch.Begin(transformMatrix: cameraMatrix, samplerState: SamplerState.PointClamp);
                DrawWorld(gameTime);
                _spriteBatch.End();

                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                DrawHUD(gameTime);
                DrawMiniMap(gameTime);
                if (_currentStage == 5)
                    _stageMechanics.DrawFog(_spriteBatch, _pixelTexture, new Vector2(400, 300), screenWidth, screenHeight);
                _stageMechanics.DrawSandstorm(_spriteBatch, _pixelTexture, screenWidth, screenHeight);
                DrawTooltips();
                _spriteBatch.End();
                break;

            case GameState.Paused:
                _spriteBatch.Begin(transformMatrix: cameraMatrix, samplerState: SamplerState.PointClamp);
                DrawWorld(gameTime);
                _spriteBatch.End();

                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                DrawPausedScreen(gameTime);
                DrawTooltips();
                _spriteBatch.End();
                break;

            case GameState.LevelUpUpgrades:
                _spriteBatch.Begin(transformMatrix: cameraMatrix, samplerState: SamplerState.PointClamp);
                DrawWorld(gameTime);
                _spriteBatch.End();

                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                DrawHUD(gameTime);
                DrawUpgradeMenu();
                DrawTooltips();
                _spriteBatch.End();
                break;

            case GameState.ChestOpen:
                _spriteBatch.Begin(transformMatrix: cameraMatrix, samplerState: SamplerState.PointClamp);
                DrawWorld(gameTime);
                _spriteBatch.End();

                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                DrawHUD(gameTime);
                if (_isChestChoiceActive)
                    DrawChestChoiceMenu();
                else
                    DrawChestOpenMenu();
                DrawTooltips();
                _spriteBatch.End();
                break;

            case GameState.GameOver:
                _spriteBatch.Begin(transformMatrix: cameraMatrix, samplerState: SamplerState.PointClamp);
                DrawWorld(gameTime);
                _spriteBatch.End();

                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                DrawHUD(gameTime);
                DrawGameOverScreen();
                DrawTooltips();
                _spriteBatch.End();
                break;

            case GameState.VictoryChoice:
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                DrawVictoryChoiceScreen();
                DrawTooltips();
                _spriteBatch.End();
                break;
        }

        // 2. Set render target back to physical screen and upscale centered
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        int physicalW = GraphicsDevice.Viewport.Width;
        int physicalH = GraphicsDevice.Viewport.Height;
        float targetAspect = 800f / 600f;
        float physicalAspect = (float)physicalW / physicalH;

        int destW = physicalW;
        int destH = physicalH;
        int destX = 0;
        int destY = 0;

        if (physicalAspect > targetAspect)
        {
            destW = (int)(physicalH * targetAspect);
            destH = physicalH;
            destX = (physicalW - destW) / 2;
        }
        else if (physicalAspect < targetAspect)
        {
            destW = physicalW;
            destH = (int)(physicalW / targetAspect);
            destY = (physicalH - destH) / 2;
        }

        Rectangle destRect = new Rectangle(destX, destY, destW, destH);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_mainRenderTarget, destRect, Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void UpdateSettings(KeyboardState keyboardState, GameTime gameTime)
    {
        _settingsFpsTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        _settingsFpsCount++;
        if (_settingsFpsTimer >= 0.5f) { _settingsFps = _settingsFpsCount / _settingsFpsTimer; _settingsFpsTimer = 0; _settingsFpsCount = 0; }

        bool up    = (keyboardState.IsKeyDown(Keys.W)     && _previousKeyboardState.IsKeyUp(Keys.W))
                  || (keyboardState.IsKeyDown(Keys.Up)    && _previousKeyboardState.IsKeyUp(Keys.Up));
        bool down  = (keyboardState.IsKeyDown(Keys.S)     && _previousKeyboardState.IsKeyUp(Keys.S))
                  || (keyboardState.IsKeyDown(Keys.Down)  && _previousKeyboardState.IsKeyUp(Keys.Down));
        bool left  = (keyboardState.IsKeyDown(Keys.A)     && _previousKeyboardState.IsKeyUp(Keys.A))
                  || (keyboardState.IsKeyDown(Keys.Left)  && _previousKeyboardState.IsKeyUp(Keys.Left));
        bool right = (keyboardState.IsKeyDown(Keys.D)     && _previousKeyboardState.IsKeyUp(Keys.D))
                  || (keyboardState.IsKeyDown(Keys.Right) && _previousKeyboardState.IsKeyUp(Keys.Right));
        bool esc   = keyboardState.IsKeyDown(Keys.Escape) && _previousKeyboardState.IsKeyUp(Keys.Escape);
        bool tabFwd= keyboardState.IsKeyDown(Keys.Tab)    && _previousKeyboardState.IsKeyUp(Keys.Tab);
        bool tabBwd= keyboardState.IsKeyDown(Keys.Q)      && _previousKeyboardState.IsKeyUp(Keys.Q);

        if (esc)
        {
            _saveData.ResolutionIndex = _settingsResolutionIndex;
            _saveData.Fullscreen = _settingsFullscreen;
            _saveData.EffectsQuality = _settingsEffectsQuality;
            _saveData.Vsync = _settingsVsync;
            _saveData.FpsCap = _settingsFpsCap;
            _saveData.MasterVolume = _settingsMasterVolume;
            _saveData.MusicVolume = _settingsMusicVolume;
            _saveData.SfxVolume = _settingsSfxVolume;
            _saveData.ShowFps = _settingsShowFps;
            _saveData.ShowMinimap = _settingsShowMinimap;
            _saveData.UiScale = _settingsUiScale;
            _saveData.Difficulty = _settingsDifficulty;
            _saveData.ScreenShake = _settingsScreenShake;
            _saveData.Save();
            _gameState = GameState.Lobby;
            return;
        }
        if (tabFwd) { _settingsCategoryIndex = (_settingsCategoryIndex + 1) % 5; _settingsItemIndex = 0; }
        if (tabBwd) { _settingsCategoryIndex = (_settingsCategoryIndex - 1 + 5) % 5; _settingsItemIndex = 0; }

        int[] itemCounts = { 2, 3, 3, 3, 2 };
        int maxItems = itemCounts[_settingsCategoryIndex];
        if (up)   _settingsItemIndex = (_settingsItemIndex - 1 + maxItems) % maxItems;
        if (down) _settingsItemIndex = (_settingsItemIndex + 1) % maxItems;

        if (left || right)
        {
            int d = right ? 1 : -1;
            switch (_settingsCategoryIndex)
            {
                case 0:
                    if (_settingsItemIndex == 0) { _settingsResolutionIndex = Math.Clamp(_settingsResolutionIndex + d, 0, 4); int[][] res = { new[]{800,600}, new[]{1024,768}, new[]{1280,720}, new[]{1600,900}, new[]{1920,1080} }; _graphics.PreferredBackBufferWidth = res[_settingsResolutionIndex][0]; _graphics.PreferredBackBufferHeight = res[_settingsResolutionIndex][1]; _graphics.ApplyChanges(); }
                    if (_settingsItemIndex == 1) { _settingsFullscreen = !_settingsFullscreen; _graphics.IsFullScreen = _settingsFullscreen; _graphics.ApplyChanges(); }
                    break;
                case 1:
                    if (_settingsItemIndex == 0) _settingsEffectsQuality = Math.Clamp(_settingsEffectsQuality + d, 0, 2);
                    if (_settingsItemIndex == 1) { _settingsVsync = !_settingsVsync; IsFixedTimeStep = _settingsVsync; }
                    if (_settingsItemIndex == 2) _settingsFpsCap = Math.Clamp(_settingsFpsCap + d, 0, 3);
                    break;
                case 2:
                    if (_settingsItemIndex == 0) _settingsMasterVolume = Math.Clamp(_settingsMasterVolume + d, 0, 10);
                    if (_settingsItemIndex == 1) _settingsMusicVolume   = Math.Clamp(_settingsMusicVolume   + d, 0, 10);
                    if (_settingsItemIndex == 2) _settingsSfxVolume     = Math.Clamp(_settingsSfxVolume     + d, 0, 10);
                    break;
                case 3:
                    if (_settingsItemIndex == 0) _settingsShowFps     = !_settingsShowFps;
                    if (_settingsItemIndex == 1) _settingsShowMinimap = !_settingsShowMinimap;
                    if (_settingsItemIndex == 2) _settingsUiScale     = Math.Clamp(_settingsUiScale + d, 0, 2);
                    break;
                case 4:
                    if (_settingsItemIndex == 0) _settingsDifficulty  = Math.Clamp(_settingsDifficulty + d, 0, 2);
                    if (_settingsItemIndex == 1) _settingsScreenShake = !_settingsScreenShake;
                    break;
            }
        }
    }

    private string SettingsVolBar(int vol)
    {
        string bar = "[";
        for (int i = 0; i < 10; i++) bar += i < vol ? "|" : " ";
        return bar + $"] {vol}/10";
    }

    private void DrawSettings(GameTime gameTime)
    {
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, 800, 600), new Color(12, 18, 35));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, 800, 65), new Color(20, 32, 55));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 63, 800, 3), Color.CornflowerBlue);
        string title = "CAI DAT (SETTINGS)";
        Vector2 titleSz = _scoreFont.MeasureString(title);
        _spriteBatch.DrawString(_scoreFont, title, new Vector2(400 - titleSz.X / 2, 18), Color.Gold);
        string navHint = "Q/Tab: Danh muc | W/S: Chon | A/D: Doi | ESC: Lai";
        Vector2 navSz = _scoreFont.MeasureString(navHint);
        _spriteBatch.DrawString(_scoreFont, navHint, new Vector2(400 - navSz.X / 2, 44), Color.Gray);

        string[] catNames = { "HIEN THI", "DO HOA", "AM THANH", "GIAO DIEN", "GAMEPLAY" };
        int tabX = 10;
        int[] tabW = { 110, 95, 110, 120, 110 };
        for (int c = 0; c < 5; c++)
        {
            bool sel = c == _settingsCategoryIndex;
            _spriteBatch.Draw(_pixelTexture, new Rectangle(tabX, 72, tabW[c], 28), sel ? new Color(40, 80, 150) : new Color(25, 35, 60));
            _spriteBatch.Draw(_pixelTexture, new Rectangle(tabX, 72, tabW[c], 2), sel ? Color.CornflowerBlue : Color.Gray * 0.4f);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(tabX, 72, 2, 28), sel ? Color.CornflowerBlue : Color.Gray * 0.4f);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(tabX + tabW[c] - 2, 72, 2, 28), sel ? Color.CornflowerBlue : Color.Gray * 0.4f);
            Vector2 cSz = _scoreFont.MeasureString(catNames[c]);
            _spriteBatch.DrawString(_scoreFont, catNames[c], new Vector2(tabX + tabW[c] / 2f - cSz.X / 2f, 78), sel ? Color.White : Color.Gray);
            RegisterTooltip(new Rectangle(tabX, 72, tabW[c], 28), catNames[c], "Danh muc cai dat: " + catNames[c]);
            tabX += tabW[c] + 5;
        }

        _spriteBatch.Draw(_pixelTexture, new Rectangle(10, 100, 780, 450), new Color(18, 28, 50));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(10, 100, 780, 2), Color.CornflowerBlue * 0.5f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(10, 548, 780, 2), Color.CornflowerBlue * 0.5f);

        int startY = 118;
        int rowH = 68;

        Action<int, string, string, string> DrawRow = (idx, label, value, subhint) =>
        {
            bool rowSel = idx == _settingsItemIndex;
            int ry = startY + idx * rowH;
            if (rowSel) _spriteBatch.Draw(_pixelTexture, new Rectangle(15, ry, 770, rowH - 6), new Color(30, 55, 100, 160));
            _spriteBatch.Draw(_pixelTexture, new Rectangle(15, ry + rowH - 7, 770, 1), Color.DimGray * 0.4f);
            string pfx = rowSel ? "> " : "  ";
            _spriteBatch.DrawString(_scoreFont, pfx + label, new Vector2(30, ry + 8), rowSel ? Color.White : Color.LightGray);
            string valStr = rowSel ? "< " + value + " >" : value;
            Vector2 vs = _scoreFont.MeasureString(valStr);
            _spriteBatch.DrawString(_scoreFont, valStr, new Vector2(775 - vs.X, ry + 8), rowSel ? Color.Yellow : Color.Cyan);
            if (rowSel && subhint.Length > 0)
                _spriteBatch.DrawString(_scoreFont, subhint, new Vector2(30, ry + 34), Color.DimGray);
            RegisterTooltip(new Rectangle(15, ry, 770, rowH - 6), label, string.IsNullOrEmpty(subhint) ? value : value + "\n" + subhint);
        };

        string[] resOpts   = { "800x600 (4:3)", "1024x768 (4:3)", "1280x720 (16:9)", "1600x900 (16:9)", "1920x1080 FullHD" };
        string[] qualOpts  = { "Thap", "Trung Binh", "Cao" };
        string[] fpsOpts   = { "30 FPS", "60 FPS", "120 FPS", "Khong gioi han" };
        string[] scaleOpts = { "Nho", "Binh Thuong", "Lon" };
        string[] diffOpts  = { "Binh Thuong", "Kho", "Tan Bao" };

        switch (_settingsCategoryIndex)
        {
            case 0:
                DrawRow(0, "Do phan giai / Khung hinh",    resOpts[_settingsResolutionIndex],  "Thay doi ap dung ngay");
                DrawRow(1, "Toan man hinh (Fullscreen)",    _settingsFullscreen ? "BAT" : "TAT","A/D de bat/tat");
                break;
            case 1:
                DrawRow(0, "Chat luong hieu ung (Particle)",qualOpts[_settingsEffectsQuality], "High = dep hon nhung giam FPS");
                DrawRow(1, "VSync (dong bo man hinh)",       _settingsVsync ? "BAT" : "TAT",   "Giam xe hinh, tang input lag nhe");
                DrawRow(2, "Gioi han FPS",                  fpsOpts[_settingsFpsCap],           "A/D chon muc FPS mong muon");
                break;
            case 2:
                DrawRow(0, "Am luong tong the (Master)", SettingsVolBar(_settingsMasterVolume), "A/D tang/giam (0-10)");
                DrawRow(1, "Am luong nhac nen (Music)",  SettingsVolBar(_settingsMusicVolume),  "A/D tang/giam (0-10)");
                DrawRow(2, "Am luong hieu ung (SFX)",    SettingsVolBar(_settingsSfxVolume),    "A/D tang/giam (0-10)");
                break;
            case 3:
                DrawRow(0, "Hien thi dong ho FPS",      _settingsShowFps     ? "BAT" : "TAT", "Goc tren phai trong tran");
                DrawRow(1, "Hien thi Minimap",          _settingsShowMinimap ? "BAT" : "TAT", "Ban do thu nho goc phai");
                DrawRow(2, "Ty le giao dien (UI Scale)",scaleOpts[_settingsUiScale],           "Anh huong co chu HUD");
                break;
            case 4:
                DrawRow(0, "Do kho mac dinh",  diffOpts[_settingsDifficulty],   "Ap dung khi vao tran moi");
                DrawRow(1, "Rung man hinh",    _settingsScreenShake ? "BAT" : "TAT", "Rung khi bi damage / boss");
                break;
        }

        if (_settingsShowFps || _settingsCategoryIndex == 1)
        {
            string fps = "FPS: " + ((int)_settingsFps).ToString();
            Vector2 fv = _scoreFont.MeasureString(fps);
            _spriteBatch.DrawString(_scoreFont, fps, new Vector2(780 - fv.X, 553), Color.LimeGreen);
        }

        string back = "ESC - Quay lai Sanh Cho";
        Vector2 bv = _scoreFont.MeasureString(back);
        _spriteBatch.DrawString(_scoreFont, back, new Vector2(400 - bv.X / 2, 553), Color.Gray * 0.7f);
    }

    private void UpdateChestOpen(KeyboardState keyboardState)
    {
        if (!_isChestChoiceActive)
        {
            if ((keyboardState.IsKeyDown(Keys.Enter) && _previousKeyboardState.IsKeyUp(Keys.Enter)) ||
                (keyboardState.IsKeyDown(Keys.Space) && _previousKeyboardState.IsKeyUp(Keys.Space)))
            {
                _gameState = GameState.Playing;
            }
            return;
        }

        int count = _chestChoices.Count;
        if (count == 0)
        {
            _gameState = GameState.Playing;
            return;
        }

        bool up = (keyboardState.IsKeyDown(Keys.W) && _previousKeyboardState.IsKeyUp(Keys.W)) ||
                  (keyboardState.IsKeyDown(Keys.Up) && _previousKeyboardState.IsKeyUp(Keys.Up));
        bool down = (keyboardState.IsKeyDown(Keys.S) && _previousKeyboardState.IsKeyUp(Keys.S)) ||
                    (keyboardState.IsKeyDown(Keys.Down) && _previousKeyboardState.IsKeyUp(Keys.Down));

        if (up)
        {
            _selectedChestChoiceIndex = (_selectedChestChoiceIndex - 1 + count) % count;
        }
        if (down)
        {
            _selectedChestChoiceIndex = (_selectedChestChoiceIndex + 1) % count;
        }

        if ((keyboardState.IsKeyDown(Keys.Enter) && _previousKeyboardState.IsKeyUp(Keys.Enter)) ||
            (keyboardState.IsKeyDown(Keys.Space) && _previousKeyboardState.IsKeyUp(Keys.Space)))
        {
            var sel = _chestChoices[_selectedChestChoiceIndex];
            _isChestChoiceActive = false;
            _gameState = GameState.Playing;
            ApplyChestChoice(sel.Code);
        }
    }

    private void ApplyChestChoice(string code)
    {
        if (code.StartsWith("GOLD_HEAL_"))
        {
            string[] parts = code.Split('_');
            int gold = int.Parse(parts[2]);
            int heal = int.Parse(parts[3]);
            _goldCollectedThisRun += gold;
            _saveData.Gold += gold;
            _saveData.Save();
            _player.HP = MathHelper.Min(_player.HP + heal, _player.MaxHP);
        }
        else if (code.StartsWith("EXP_"))
        {
            string[] parts = code.Split('_');
            int exp = int.Parse(parts[1]);
            _player.XP += exp;
            if (_player.XP >= _player.TargetXP)
            {
                _player.XP -= _player.TargetXP;
                _player.Level++;
                _player.TargetXP = (int)(_player.TargetXP * 1.5f);
                GenerateUpgradeChoices();
                _gameState = GameState.LevelUpUpgrades;
            }
        }
        else if (code.StartsWith("WEAPON_"))
        {
            string name = code.Substring(7);
            var weapon = GetWeapon(name);
            if (weapon != null)
            {
                weapon.Level++;
            }
        }
        else if (code.StartsWith("PASSIVE_"))
        {
            string pName = code.Substring(8);
            switch (pName)
            {
                case "BOOTS": _player.SpeedBootsLevel++; break;
                case "REGEN": _player.RegenRingLevel++; break;
                case "SHIELD": _player.ShieldLevel++; break;
                case "SPINACH": _player.SpinachLevel++; break;
                case "MAGNET": _player.AttractorbLevel++; break;
                case "DUPLICATOR": _player.DuplicatorLevel++; break;
                case "CLOVER": _player.CloverLevel++; break;
                case "HEART": 
                    _player.HollowHeartLevel++;
                    float oldMax = _player.MaxHP;
                    _player.MaxHP = 100f * (1f + _saveData.MaxHPUpgradeLevel * 0.10f) * (1f + _player.HollowHeartLevel * 0.20f);
                    _player.HP += (_player.MaxHP - oldMax);
                    break;
                case "PUMMAROLA": _player.PummarolaLevel++; break;
                case "BRACER": _player.BracerLevel++; break;
                case "SPELLBINDER": _player.SpellbinderLevel++; break;
                case "TOME": _player.EmptyTomeLevel++; break;
            }
        }
        else if (code == "GOLD_150")
        {
            _goldCollectedThisRun += 150;
            _saveData.Gold += 150;
            _saveData.Save();
        }
        else if (code == "POTION")
        {
            _player.CurrentPotions = MathHelper.Min(_player.CurrentPotions + 1, 5);
        }
        else if (code.StartsWith("SUPER_GOLD"))
        {
            int gold = (int)(200 * _player.PermanentGreedBoost);
            if (_player.GoldBoosterActive) gold *= 2;
            _goldCollectedThisRun += gold;
            _saveData.Gold += gold;
            _saveData.Save();
            
            // Defeat surrounding weak enemies
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                if (Vector2.Distance(_player.Center, _enemies[i].Position) < 280f && _enemies[i].Type != EnemyType.GolemBoss)
                {
                    _enemies[i].HP = 0;
                }
            }
        }
    }

    private void DrawChestChoiceMenu()
    {
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, 800, 600), new Color(0, 0, 0, 220));

        string title = "★ CHON PHAN THUONG RUONG BAU ★";
        Vector2 titleSize = _scoreFont.MeasureString(title);
        _spriteBatch.DrawString(_scoreFont, title, new Vector2(400 - titleSize.X / 2, 70), Color.Gold);

        string subTitle = "Dung phim W/S hoac Mui Ten de chon. Nhan ENTER/SPACE de nhan.";
        Vector2 subSize = _scoreFont.MeasureString(subTitle);
        _spriteBatch.DrawString(_scoreFont, subTitle, new Vector2(400 - subSize.X / 2, 110), Color.Gray);

        int count = _chestChoices.Count;
        int cardW = 560;
        int cardH = 70;
        int startY = 150;
        int gap = 10;

        for (int i = 0; i < count; i++)
        {
            var choice = _chestChoices[i];
            bool sel = i == _selectedChestChoiceIndex;
            int cardY = startY + i * (cardH + gap);

            Color bgColor = sel ? new Color(40, 50, 90) : new Color(25, 25, 30);
            Color borderColor = sel ? Color.Yellow : Color.Gold * 0.4f;

            _spriteBatch.Draw(_pixelTexture, new Rectangle(120, cardY, cardW, cardH), bgColor);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(120, cardY, cardW, 2), borderColor);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(120, cardY + cardH - 2, cardW, 2), borderColor);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(120, cardY, 2, cardH), borderColor);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(120 + cardW - 2, cardY, 2, cardH), borderColor);

            // Draw selection pointer
            string pointer = sel ? "▶" : " ";
            Color textColor = sel ? Color.Yellow : Color.White;
            _spriteBatch.DrawString(_scoreFont, pointer, new Vector2(138, cardY + 24), Color.Yellow);

            // Draw Icon Box Frame
            Rectangle iconBox = new Rectangle(165, cardY + 11, 48, 48);
            _spriteBatch.Draw(_pixelTexture, iconBox, new Color(15, 15, 20));
            _spriteBatch.Draw(_pixelTexture, new Rectangle(iconBox.X, iconBox.Y, iconBox.Width, 1), Color.Gold * 0.3f);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(iconBox.X, iconBox.Bottom - 1, iconBox.Width, 1), Color.Gold * 0.3f);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(iconBox.X, iconBox.Y, 1, iconBox.Height), Color.Gold * 0.3f);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(iconBox.Right - 1, iconBox.Y, 1, iconBox.Height), Color.Gold * 0.3f);

            // Draw the Icon
            Rectangle iconDest = new Rectangle(iconBox.X + 6, iconBox.Y + 6, 36, 36);
            DrawItemIcon(_spriteBatch, choice.Code, iconDest);

            // Draw Title and Description
            DrawTruncatedString(choice.Title, new Vector2(230, cardY + 12), 390f, textColor);
            DrawTruncatedString(choice.Description, new Vector2(230, cardY + 38), 390f, Color.LightGray);
            var chestTip = GetItemTooltipInfo(choice.Code);
            RegisterTooltip(new Rectangle(120, cardY, cardW, cardH), choice.Title, choice.Description + "\n\n" + chestTip.body);

            if (sel)
            {
                _spriteBatch.Draw(_pixelTexture, new Rectangle(630, cardY + 20, 26, 26), Color.Yellow * 0.2f);
                _spriteBatch.DrawString(_scoreFont, "*", new Vector2(638, cardY + 24), Color.Yellow);
            }
        }
    }

    private void UpdateVictoryChoice(KeyboardState keyboardState)
    {
        if (keyboardState.IsKeyDown(Keys.D1) && _previousKeyboardState.IsKeyUp(Keys.D1))
        {
            int earnedGold = (int)(_goldCollectedThisRun * _player.PermanentGreedBoost);
            _saveData.Gold += earnedGold;
            if (_gameTimer > _saveData.BestSurvivalTime)
            {
                _saveData.BestSurvivalTime = _gameTimer;
            }
            _saveData.Save();
            _gameState = GameState.Lobby;
        }
        else if (keyboardState.IsKeyDown(Keys.D2) && _previousKeyboardState.IsKeyUp(Keys.D2))
        {
            _inEndlessMode = true;
            _hasWonRun = true;
            _gameState = GameState.Playing;
        }
    }

    private void DrawVictoryChoiceScreen()
    {
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, 800, 600), new Color(10, 25, 15, 230));

        string title = "★ VUOT AI CHIEN THANG ★";
        Vector2 titleSize = _scoreFont.MeasureString(title);
        _spriteBatch.DrawString(_scoreFont, title, new Vector2(400 - titleSize.X / 2, 110), Color.Gold);

        string timeText = "Thoi gian sinh ton: 30:00 (MUC TIEU HOAN THANH)";
        Vector2 timeSize = _scoreFont.MeasureString(timeText);
        _spriteBatch.DrawString(_scoreFont, timeText, new Vector2(400 - timeSize.X / 2, 170), Color.LightGreen);

        string goldText = $"Vang tich luy duoc: {_goldCollectedThisRun} Vang";
        Vector2 goldSize = _scoreFont.MeasureString(goldText);
        _spriteBatch.DrawString(_scoreFont, goldText, new Vector2(400 - goldSize.X / 2, 210), Color.Gold);

        int cardY1 = 280;
        int cardY2 = 380;

        // Choice 1
        _spriteBatch.Draw(_pixelTexture, new Rectangle(120, cardY1, 560, 75), new Color(20, 45, 25));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(120, cardY1, 560, 2), Color.LimeGreen);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(120, cardY1 + 73, 560, 2), Color.LimeGreen);
        _spriteBatch.DrawString(_scoreFont, "Nhan Phim [1] - RUT LUI AN TOAN", new Vector2(150, cardY1 + 14), Color.White);
        _spriteBatch.DrawString(_scoreFont, "Nhan toan bo Vang va ket thuc run de mua nang cap.", new Vector2(150, cardY1 + 42), Color.LightGray);
        RegisterTooltip(new Rectangle(120, cardY1, 560, 75), "Rut Lui An Toan",
            "Nhan toan bo Vang da thu thap va ket thuc run. Dung Vang de mua nang cap vinh vien tai Shop.");

        // Choice 2
        _spriteBatch.Draw(_pixelTexture, new Rectangle(120, cardY2, 560, 75), new Color(45, 35, 20));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(120, cardY2, 560, 2), Color.Orange);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(120, cardY2 + 73, 560, 2), Color.Orange);
        _spriteBatch.DrawString(_scoreFont, "Nhan Phim [2] - SINH TON VO HAN", new Vector2(150, cardY2 + 14), Color.White);
        _spriteBatch.DrawString(_scoreFont, "Tiep tuc de lap ky luc moi (do kho tang tien moi phut)!", new Vector2(150, cardY2 + 42), Color.LightGray);
        RegisterTooltip(new Rectangle(120, cardY2, 560, 75), "Sinh Ton Vo Han",
            "Tiep tuc choi sau khi hoan thanh muc tieu 30 phut. Do kho va so luong quai tang dan theo thoi gian.");
    }

    private void DrawLobby(GameTime gameTime)
    {
        // 1. Draw sảnh chờ background (Deep dark blue/purple gradient panel)
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, 800, 600), new Color(15, 23, 42));

        // Draw elegant glowing header panel
        _spriteBatch.Draw(_pixelTexture, new Rectangle(50, 40, 700, 70), new Color(30, 41, 59));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(50, 40, 700, 3), Color.Gold);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(50, 107, 700, 3), Color.Gold);

        string title = "== PIXEL SURVIVORS - KHU VUC SANH ==";
        Vector2 titleSize = _scoreFont.MeasureString(title);
        _spriteBatch.DrawString(_scoreFont, title, new Vector2(400 - titleSize.X / 2, 60), Color.Gold);

        // 2. Draw Permanent Stats & Gold Panel (top-left glass frame)
        int px = 50;
        int py = 130;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(px, py, 330, 95), new Color(30, 41, 59, 150));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(px, py, 330, 2), Color.Gold * 0.4f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(px, py + 93, 330, 2), Color.Gold * 0.4f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(px, py, 2, 95), Color.Gold * 0.4f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(px + 328, py, 2, 95), Color.Gold * 0.4f);

        _spriteBatch.DrawString(_scoreFont, $"VANG TICH LUY: {_saveData.Gold} Vang", new Vector2(px + 15, py + 10), Color.Gold);
        string statsSummary = $"HP Lvl {_saveData.MaxHPUpgradeLevel}/10 | Dmg {_saveData.DamageUpgradeLevel}/10 | Spd {_saveData.SpeedUpgradeLevel}/10";
        DrawTruncatedString(statsSummary, new Vector2(px + 15, py + 38), 300f, Color.LightGray);
        
        int bestMin = (int)_saveData.BestSurvivalTime / 60;
        int bestSec = (int)_saveData.BestSurvivalTime % 60;
        string bestTime = $"KY LUC SINH TON: {bestMin:00}:{bestSec:00}";
        _spriteBatch.DrawString(_scoreFont, bestTime, new Vector2(px + 15, py + 66), Color.GreenYellow);
        RegisterTooltip(new Rectangle(px, py, 330, 95), "Thong Ke Vinh Vien",
            $"Vang: {_saveData.Gold} | HP Lvl {_saveData.MaxHPUpgradeLevel}/10 | Dmg {_saveData.DamageUpgradeLevel}/10 | Spd {_saveData.SpeedUpgradeLevel}/10\nKy luc sinh ton: {bestMin:00}:{bestSec:00}");

        // 3. Draw Selected Map Panel (top-right glass frame)
        int rx = 420;
        int ry = 130;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(rx, ry, 330, 95), new Color(30, 41, 59, 150));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(rx, ry, 330, 2), Color.Gold * 0.4f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(rx, ry + 93, 330, 2), Color.Gold * 0.4f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(rx, ry, 2, 95), Color.Gold * 0.4f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(rx + 328, ry, 2, 95), Color.Gold * 0.4f);

        string currentStageName = TruncateText(GetCurrentStageConfig().LobbyName, 300f);
        _spriteBatch.DrawString(_scoreFont, "BAN DO HIEN TAI:", new Vector2(rx + 15, ry + 10), Color.Khaki);
        _spriteBatch.DrawString(_scoreFont, currentStageName, new Vector2(rx + 15, ry + 38), Color.LimeGreen);

        // Display pre-purchased consumables if any!
        string consText = $"Hoi Sinh: {_saveData.ReviveCount}/3 | Binh Mau: {_saveData.StartingPotions}/5" + (_saveData.GoldBoosterActive ? " | Bua Vang" : "");
        DrawTruncatedString(consText, new Vector2(rx + 15, ry + 66), 300f, Color.LightSkyBlue);
        RegisterTooltip(new Rectangle(rx, ry, 330, 95), "Ban Do Hien Tai", GetStageTooltipBody(GetCurrentStageConfig()));

        // 4. Draw Lobby Options (on the left side)
        string[] options = { 
            "1. BAT DAU CHOI (START RUN)", 
            "2. CHON MAN CHOI (SELECT STAGE)", 
            "3. SHOP NANG CAP CHI SO", 
            "4. SHOP NGOAI HINH (SKINS)",
            "5. CAI DAT (SETTINGS)",
            "6. THOAT GAME (EXIT)" 
        };
        for (int i = 0; i < options.Length; i++)
        {
            Color color = i == _selectedLobbyIndex ? Color.Yellow : Color.White;
            string prefix = i == _selectedLobbyIndex ? "> " : "  ";
            string text = prefix + options[i];
            
            _spriteBatch.DrawString(_scoreFont, text, new Vector2(70, 240 + i * 46), color);
            RegisterTooltip(new Rectangle(65, 235 + i * 46, 360, 40), options[i], GetLobbyOptionTooltip(i));
        }

        // 5. Draw Skin Preview Box (on the right side)
        int bx = 470;
        int by = 245;
        int bw = 240;
        int bh = 240;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(bx, by, bw, bh), new Color(30, 41, 59, 200));
        // Golden frame
        _spriteBatch.Draw(_pixelTexture, new Rectangle(bx, by, bw, 2), Color.Gold * 0.6f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(bx, by + bh - 2, bw, 2), Color.Gold * 0.6f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(bx, by, 2, bh), Color.Gold * 0.6f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(bx + bw - 2, by, 2, bh), Color.Gold * 0.6f);

        _spriteBatch.DrawString(_scoreFont, "NHÂN VẬT ĐANG CHỌN", new Vector2(bx + 12, by + 12), Color.Khaki);

        // Draw walking player sprite inside
        int walkFrame = (int)(gameTime.TotalGameTime.TotalSeconds * 6.0) % 4;
        Rectangle sourceRect = new Rectangle(walkFrame * 16, 0, 16, 16); // walking down
        Texture2D previewTex = _skinTextures.ContainsKey(_saveData.SelectedSkin) ? _skinTextures[_saveData.SelectedSkin] : _playerTexture;
        
        // Draw 96x96 centered walk animation
        _spriteBatch.Draw(previewTex, new Rectangle(bx + (bw - 96) / 2, by + 45, 96, 96), sourceRect, Color.White);

        string skinNameDisplay = _saveData.SelectedSkin == "Knight" ? "HIỆP SĨ SẮT" :
                                 _saveData.SelectedSkin == "NinjaBlue" ? "NINJA XANH" :
                                 _saveData.SelectedSkin == "NinjaRed" ? "NINJA HỎA LONG" :
                                 _saveData.SelectedSkin == "Samurai" ? "SAMURAI HOÀNG GIA" :
                                 _saveData.SelectedSkin == "Monk" ? "NHÀ SƯ HUYỀN THOẠI" : "HIỆP SĨ HOÀNG KIM";

        Vector2 skinTextSize = _scoreFont.MeasureString(skinNameDisplay);
        _spriteBatch.DrawString(_scoreFont, skinNameDisplay, new Vector2(bx + bw / 2 - skinTextSize.X / 2, by + bh - 40), Color.Gold);
        RegisterTooltip(new Rectangle(bx, by, bw, bh), skinNameDisplay, "Ngoai hinh nhan vat dang duoc chon. Vao Shop Ngoai Hinh de doi skin.");

        // 6. Navigation guide at the bottom
        string guide = "WASD/Mui Ten | ENTER/SPACE";
        Vector2 guideSize = _scoreFont.MeasureString(guide);
        _spriteBatch.DrawString(_scoreFont, guide, new Vector2(400 - guideSize.X / 2, 530), Color.Gray);
    }

    private void DrawStageSelect()
    {
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, 800, 600), new Color(20, 15, 20));

        string title = "CHON MAN CHOI - THU THACH TANG DAN";
        Vector2 titleSize = _scoreFont.MeasureString(title);
        _spriteBatch.DrawString(_scoreFont, title, new Vector2(400 - titleSize.X / 2, 28), Color.Gold);

        // Left panel: stage list (2 columns, fixed width)
        Rectangle listPanel = new Rectangle(18, 72, 372, 290);
        _spriteBatch.Draw(_pixelTexture, listPanel, new Color(28, 22, 28, 220));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(listPanel.X, listPanel.Y, listPanel.Width, 2), Color.Gold * 0.5f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(listPanel.X, listPanel.Y + listPanel.Height - 2, listPanel.Width, 2), Color.Gold * 0.5f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(listPanel.X, listPanel.Y, 2, listPanel.Height), Color.Gold * 0.5f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(listPanel.X + listPanel.Width - 2, listPanel.Y, 2, listPanel.Height), Color.Gold * 0.5f);

        int listStartY = listPanel.Y + 14;
        int rowHeight = 32;
        int col1X = listPanel.X + 14;
        int col2X = listPanel.X + 188;
        float colMaxWidth = 158f;

        for (int i = 0; i < StageConfigs.Length; i++)
        {
            int col = i / 5;
            int row = i % 5;
            int listX = col == 0 ? col1X : col2X;
            Color color = i == _selectedStageIndex ? Color.GreenYellow : Color.White;
            string prefix = i == _selectedStageIndex ? "> " : "  ";
            string text = prefix + TruncateText(GetStageShortLabel(StageConfigs[i]), colMaxWidth);

            _spriteBatch.DrawString(_scoreFont, text, new Vector2(listX, listStartY + row * rowHeight), color);
            RegisterTooltip(new Rectangle(listX - 4, listStartY + row * rowHeight - 2, 168, rowHeight), StageConfigs[i].Name, GetStageTooltipBody(StageConfigs[i]));
        }

        // Right panel: stage details
        var config = StageConfigs[_selectedStageIndex];
        Rectangle detailBox = new Rectangle(405, 72, 377, 400);
        int detailPad = 14;
        float detailTextWidth = detailBox.Width - detailPad * 2;

        _spriteBatch.Draw(_pixelTexture, detailBox, new Color(40, 30, 40) * 0.9f);

        int borderThick = 2;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(detailBox.X, detailBox.Y, detailBox.Width, borderThick), Color.Gold);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(detailBox.X, detailBox.Y + detailBox.Height - borderThick, detailBox.Width, borderThick), Color.Gold);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(detailBox.X, detailBox.Y, borderThick, detailBox.Height), Color.Gold);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(detailBox.X + detailBox.Width - borderThick, detailBox.Y, borderThick, detailBox.Height), Color.Gold);

        int contentY = detailBox.Y + 12;
        DrawTruncatedString(config.Name, new Vector2(detailBox.X + detailPad, contentY), detailTextWidth, Color.Gold);
        contentY += 24;
        _spriteBatch.DrawString(_scoreFont, "-------------------------", new Vector2(detailBox.X + detailPad, contentY), Color.DarkKhaki);
        contentY += 18;

        string wrappedDesc = WrapText(config.Description, detailTextWidth);
        _spriteBatch.DrawString(_scoreFont, wrappedDesc, new Vector2(detailBox.X + detailPad, contentY), Color.LightGray);
        contentY += (int)GetWrappedTextHeight(wrappedDesc, detailTextWidth) + 14;

        float spdRate = config.MonsterSpeedMultiplier;
        float hpRate = config.MonsterHPMultiplier;
        float spawnRate = 1f / config.SpawnIntervalMultiplier;
        int bossSec = config.BossSpawnIntervalSeconds;
        int batchCount = config.SpawnBatchCount;

        _spriteBatch.DrawString(_scoreFont, "THONG SO THU THACH:", new Vector2(detailBox.X + detailPad, contentY), Color.Khaki);
        contentY += 24;
        _spriteBatch.DrawString(_scoreFont, $"+ Toc do quai: x{spdRate:0.00}", new Vector2(detailBox.X + detailPad, contentY), Color.Tomato);
        contentY += 22;
        _spriteBatch.DrawString(_scoreFont, $"+ HP quai vat: x{hpRate:0.00}", new Vector2(detailBox.X + detailPad, contentY), Color.Tomato);
        contentY += 22;
        _spriteBatch.DrawString(_scoreFont, $"+ Tan suat quai: x{spawnRate:0.00}", new Vector2(detailBox.X + detailPad, contentY), Color.Tomato);
        contentY += 22;
        _spriteBatch.DrawString(_scoreFont, $"+ Quai moi dot: x{batchCount}", new Vector2(detailBox.X + detailPad, contentY), Color.Tomato);
        contentY += 22;
        _spriteBatch.DrawString(_scoreFont, $"+ Boss dai vuong: moi {bossSec}s", new Vector2(detailBox.X + detailPad, contentY), Color.Tomato);
        RegisterTooltip(detailBox, config.Name, GetStageTooltipBody(config));

        string backLine1 = "W/S: Chon man | A/D: Cot";
        string backLine2 = "ENTER: Xac nhan | ESC: Quay lai";
        Vector2 back1Size = _scoreFont.MeasureString(backLine1);
        Vector2 back2Size = _scoreFont.MeasureString(backLine2);
        _spriteBatch.DrawString(_scoreFont, backLine1, new Vector2(400 - back1Size.X / 2, 500), Color.Gray);
        _spriteBatch.DrawString(_scoreFont, backLine2, new Vector2(400 - back2Size.X / 2, 522), Color.Gray);
    }

    private void DrawShopMenu()
    {
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, 800, 600), new Color(12, 16, 25));

        string title = "CUA HANG NANG CAP & VAT PHAM BO TRO";
        Vector2 titleSize = _scoreFont.MeasureString(title);
        _spriteBatch.DrawString(_scoreFont, title, new Vector2(400 - titleSize.X / 2, 30), Color.Gold);

        string goldText = $"Vang dang co: {_saveData.Gold} Vang";
        Vector2 goldSize = _scoreFont.MeasureString(goldText);
        _spriteBatch.DrawString(_scoreFont, goldText, new Vector2(400 - goldSize.X / 2, 65), Color.LimeGreen);

        // Column 1 Frame: Permanent Stats (X=40, Y=105, W=340, H=385)
        int col1X = 40;
        int colY = 105;
        int colW = 340;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(col1X, colY, colW, 385), new Color(25, 30, 45, 180));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(col1X, colY, colW, 2), Color.Gold * 0.4f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(col1X, colY + 383, colW, 2), Color.Gold * 0.4f);
        _spriteBatch.DrawString(_scoreFont, "NANG CAP VINH VIEN (L1-10)", new Vector2(col1X + 15, colY + 12), Color.Gold);

        string[] permNames = {
            "Max HP",
            "Sat Thuong",
            "Toc Do",
            "Giap",
            "Nam Cham",
            "Hoi Chieu",
            "Tang Truong",
            "Greed (Vang)",
            "Luck (May Man)"
        };

        int[] permLvls = {
            _saveData.MaxHPUpgradeLevel,
            _saveData.DamageUpgradeLevel,
            _saveData.SpeedUpgradeLevel,
            _saveData.ArmorUpgradeLevel,
            _saveData.MagnetUpgradeLevel,
            _saveData.CooldownUpgradeLevel,
            _saveData.GrowthUpgradeLevel,
            _saveData.GreedUpgradeLevel,
            _saveData.LuckUpgradeLevel
        };

        for (int i = 0; i < 9; i++)
        {
            int idx = i;
            bool sel = idx == _selectedShopIndex;
            int itemY = colY + 45 + i * 36;

            if (sel) _spriteBatch.Draw(_pixelTexture, new Rectangle(col1X + 5, itemY - 2, colW - 10, 30), new Color(45, 65, 100));

            Color color = sel ? Color.Yellow : Color.White;
            string pfx = sel ? "> " : "  ";
            string text = pfx + permNames[i];
            DrawTruncatedString(text, new Vector2(col1X + 10, itemY + 2), colW - 110f, color);

            string status = permLvls[i] >= 10 ? "MAX" : $"{permLvls[i]}/10 [{GetUpgradeCost(permLvls[i])}g]";
            Color statusColor = permLvls[i] >= 10 ? Color.Orange : (sel ? Color.Yellow : Color.Cyan);
            Vector2 sz = _scoreFont.MeasureString(status);
            _spriteBatch.DrawString(_scoreFont, status, new Vector2(col1X + colW - sz.X - 15, itemY + 2), statusColor);
            RegisterTooltip(new Rectangle(col1X + 5, itemY - 2, colW - 10, 30), permNames[i], GetShopItemTooltip(idx));
        }

        // Column 2 Frame: Consumables & Exit (X=420, Y=105, W=340, H=385)
        int col2X = 420;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(col2X, colY, colW, 385), new Color(25, 30, 45, 180));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(col2X, colY, colW, 2), Color.Gold * 0.4f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(col2X, colY + 383, colW, 2), Color.Gold * 0.4f);
        _spriteBatch.DrawString(_scoreFont, "VAT PHAM HO TRO TRAN DAU", new Vector2(col2X + 15, colY + 12), Color.Gold);

        // Consumables: index 9 to 11
        string[] consNames = { "Hoi Sinh (Revive)", "Binh Mau (Potion)", "Bua Vang (Booster)" };
        int[] consCounts = { _saveData.ReviveCount, _saveData.StartingPotions, _saveData.GoldBoosterActive ? 1 : 0 };
        int[] consMax = { 3, 5, 1 };
        int[] consPrices = { 100, 30, 80 };

        for (int i = 0; i < 3; i++)
        {
            int idx = 9 + i;
            bool sel = idx == _selectedShopIndex;
            int itemY = colY + 48 + i * 55;

            if (sel) _spriteBatch.Draw(_pixelTexture, new Rectangle(col2X + 5, itemY - 2, colW - 10, 48), new Color(45, 65, 100));

            Color color = sel ? Color.Yellow : Color.White;
            string pfx = sel ? "> " : "  ";
            DrawTruncatedString(pfx + consNames[i], new Vector2(col2X + 10, itemY + 4), colW - 120f, color);

            string desc = i == 0 ? "Hoi sinh voi 50% HP (Max 3)" :
                          i == 1 ? "Hoi +30 HP trong tran (Max 5)" : "Nhan doi vang rot ra tran toi";
            DrawTruncatedString("  " + desc, new Vector2(col2X + 10, itemY + 26), colW - 20f, Color.Gray);

            string status = consCounts[i] >= consMax[i] ? "MAX" : $"{consCounts[i]}/{consMax[i]} [{consPrices[i]}g]";
            Color statusColor = consCounts[i] >= consMax[i] ? Color.Orange : (sel ? Color.Yellow : Color.Cyan);
            Vector2 sz = _scoreFont.MeasureString(status);
            _spriteBatch.DrawString(_scoreFont, status, new Vector2(col2X + colW - sz.X - 15, itemY + 14), statusColor);
            RegisterTooltip(new Rectangle(col2X + 5, itemY - 2, colW - 10, 48), consNames[i], GetShopItemTooltip(idx));
        }

        // Back button (index 12) at the bottom right
        bool backSel = _selectedShopIndex == 12;
        int backY = colY + 310;
        if (backSel) _spriteBatch.Draw(_pixelTexture, new Rectangle(col2X + 20, backY, colW - 40, 50), new Color(80, 40, 40));
        else _spriteBatch.Draw(_pixelTexture, new Rectangle(col2X + 20, backY, colW - 40, 50), new Color(40, 20, 20));
        
        _spriteBatch.Draw(_pixelTexture, new Rectangle(col2X + 20, backY, colW - 40, 2), Color.Red * 0.5f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(col2X + 20, backY + 48, colW - 40, 2), Color.Red * 0.5f);

        string backText = "QUAY LAI SANH CHO (BACK)";
        Vector2 backSz = _scoreFont.MeasureString(backText);
        _spriteBatch.DrawString(_scoreFont, backText, new Vector2(col2X + colW / 2 - backSz.X / 2, backY + 14), backSel ? Color.Yellow : Color.White);
        RegisterTooltip(new Rectangle(col2X + 20, backY, colW - 40, 50), "Quay Lai Sanh Cho", GetShopItemTooltip(12));

        // Description / Detail Panel at the bottom (X=40, Y=505, W=720, H=50)
        _spriteBatch.Draw(_pixelTexture, new Rectangle(40, 505, 720, 50), new Color(20, 25, 38, 220));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(40, 505, 720, 2), Color.Gold * 0.4f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(40, 553, 720, 2), Color.Gold * 0.4f);

        string itemDesc = "";
        switch (_selectedShopIndex)
        {
            case 0: itemDesc = "Max HP: Tang +10% mau toi da vinh vien cho nhan vat."; break;
            case 1: itemDesc = "Sat Thuong: Tang +10% sat thuong gay ra cho quai vinh vien."; break;
            case 2: itemDesc = "Toc Do: Tang +5% toc do di chuyen trong tran vinh vien."; break;
            case 3: itemDesc = "Giap: Giam -1 sat thuong vat ly nhan vao tren moi cap vinh vien."; break;
            case 4: itemDesc = "Nam Cham: Tang +15% ban kinh hut ngoc va vang vinh vien."; break;
            case 5: itemDesc = "Hoi Chieu: Giam -4% thoi gian cho giua moi lan ra chieu vinh vien."; break;
            case 6: itemDesc = "Tang Truong: Tang +8% diem kinh nghiem (EXP) thu thap duoc vinh vien."; break;
            case 7: itemDesc = "Greed: Tang +10% luong vang thu thap duoc trong tran vinh vien."; break;
            case 8: itemDesc = "Luck: Tang +10% may man mo o qua thu 5 va xao bai vinh vien."; break;
            case 9: itemDesc = "Hoi Sinh: Tu dong hoi sinh 50% HP khi het mau trong tran (Max 3)."; break;
            case 10: itemDesc = "Binh Mau: Mang theo binh tiep te giup hoi +30 HP khi bam H (Max 5)."; break;
            case 11: itemDesc = "Bua Vang: Nhan doi toan bo luong vang nhan duoc trong tran dau ke."; break;
            case 12: itemDesc = "Thoat cua hang va quay tro lai sanh cho chinh."; break;
        }

        string wrappedItemDesc = WrapText(itemDesc, 680f);
        Vector2 descSz = _scoreFont.MeasureString(wrappedItemDesc);
        _spriteBatch.DrawString(_scoreFont, wrappedItemDesc, new Vector2(400 - descSz.X / 2, 512), Color.LimeGreen);

        string guide = "W/S/A/D hoac Phim Mui Ten de chon  |  ENTER/SPACE: Mua hang";
        Vector2 guideSize = _scoreFont.MeasureString(guide);
        _spriteBatch.DrawString(_scoreFont, guide, new Vector2(400 - guideSize.X / 2, 568), Color.Gray * 0.7f);
    }

    private void DrawWorld(GameTime gameTime)
    {
        DrawGrid();

        // 1. Draw Obstacles (walls/pillars)
        foreach (var obs in _obstacles)
        {
            if (obs.IsDestroyed) continue;
            obs.Draw(_spriteBatch, _wallTileTexture, _natureTileTexture, _reliefTileTexture, _sandTileTexture,
                _houseTileTexture, _elementTileTexture, _villageTileTexture, _waterTileTexture);
        }

        _stageMechanics.DrawActiveSpikes(_spriteBatch, _pixelTexture, _terrainGrid);

        // 2. Draw Holy Water pools — elliptical water puddle with animated ripple rings
        foreach (var pool in HolyWaterPools)
        {
            float t = (float)gameTime.TotalGameTime.TotalSeconds;
            int cx = (int)pool.Position.X;
            int cy = (int)pool.Position.Y;
            int rx = (int)pool.Radius;        // horizontal radius
            int ry = (int)(pool.Radius * 0.45f); // vertical radius (flat ellipse)

            // Fade out when less than 1s remaining
            float alpha = pool.Duration < 1.0f ? pool.Duration : 1.0f;

            // --- Filled ellipse body: draw horizontal scanlines ---
            Color waterDeep   = new Color(20,  100, 180) * (0.55f * alpha);
            Color waterShallow = new Color(80, 180, 255) * (0.35f * alpha);
            for (int dy = -ry; dy <= ry; dy++)
            {
                float frac = (float)dy / ry;
                float halfW = (float)Math.Sqrt(Math.Max(0, 1.0 - frac * frac)) * rx;
                int x0 = cx - (int)halfW;
                int w  = (int)(halfW * 2);
                if (w < 1) continue;
                // Blend deeper color in center, shallower at edge
                Color lineColor = Color.Lerp(waterShallow, waterDeep, 1f - Math.Abs(frac));
                _spriteBatch.Draw(_pixelTexture, new Rectangle(x0, cy + dy, w, 1), lineColor);
            }

            // --- Ripple rings: two animated expanding circles ---
            for (int ring = 0; ring < 2; ring++)
            {
                float phase = (t * 0.9f + ring * 0.5f) % 1.0f;
                float ringRx = rx * (0.3f + phase * 0.7f);
                float ringRy = ry * (0.3f + phase * 0.7f);
                float ringAlpha = (1f - phase) * 0.7f * alpha;
                Color ringColor = new Color(160, 220, 255) * ringAlpha;

                int segments = 48;
                for (int i = 0; i < segments; i++)
                {
                    double a0 = (double)i / segments * Math.PI * 2;
                    double a1 = (double)(i + 1) / segments * Math.PI * 2;
                    int px = cx + (int)(Math.Cos(a0) * ringRx);
                    int py = cy + (int)(Math.Sin(a0) * ringRy);
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(px - 1, py - 1, 2, 2), ringColor);
                }
            }

            // --- Highlight shimmer (small bright dot that moves) ---
            float shimX = cx + (float)Math.Cos(t * 2.0) * rx * 0.3f;
            float shimY = cy + (float)Math.Sin(t * 2.0) * ry * 0.3f;
            _spriteBatch.Draw(_pixelTexture, new Rectangle((int)shimX - 2, (int)shimY - 2, 5, 3),
                new Color(220, 240, 255) * (0.6f * alpha));
        }

        // 3. Draw Gems (Pulsing halo & cross flare)
        foreach (var gem in _gems)
        {
            gem.Draw(_spriteBatch, _xpGemTexture, _pixelTexture, gameTime);
        }

        // 4. Draw Coins (Pulsing halo & cross flare)
        foreach (var coin in _coins)
        {
            coin.Draw(_spriteBatch, _coinTexture, _pixelTexture, gameTime);
        }

        // 5. Draw Chests (Pulsing halo & cross flare)
        foreach (var chest in _chests)
        {
            chest.Draw(_spriteBatch, _chestTexture, _pixelTexture, gameTime);
        }

        // 6. Draw Projectiles (Player)
        foreach (var proj in _projectiles)
        {
            Texture2D texture = _wandProjTexture;
            if (proj.IsFireball) texture = _fireballProjTexture;
            else if (proj.IsAxe) texture = _axeProjTexture;
            
            proj.Draw(_spriteBatch, texture, gameTime);
        }

        // 7. Draw Projectiles (Enemy arrows)
        foreach (var proj in _enemyProjectiles)
        {
            proj.Draw(_spriteBatch, _arrowProjTexture, gameTime);
        }

        // 8. Draw Enemies (Pass correct animation sheet)
        foreach (var enemy in _enemies)
        {
            Texture2D texture = _zombieTexture;
            if (enemy.Type == EnemyType.Bat) texture = _batTexture;
            else if (enemy.Type == EnemyType.Skeleton) texture = _skeletonTexture;
            else if (enemy.Type == EnemyType.Golem || enemy.Type == EnemyType.GolemBoss) texture = _golemTexture;
            
            enemy.Draw(_spriteBatch, texture, _pixelTexture);
        }

        // 9. Draw Player
        _player.Draw(_spriteBatch, _playerTexture);

        // 10. Draw Garlic Aura
        foreach (var weapon in _weapons)
        {
            if (weapon.Level > 0)
            {
                weapon.Draw(_spriteBatch, _pixelTexture, _player);
            }
        }

        // 11. Draw Lightning bolts visual effects
        foreach (var effect in VisualEffects)
        {
            _spriteBatch.Draw(_pixelTexture, new Rectangle((int)effect.Position.X, (int)effect.Position.Y, effect.Width, effect.Height), effect.Color);
        }
    }

    private bool _mapshotsDone;

    // Che do debug: chup toan bo 10 map ra file PNG roi thoat (chay voi --mapshots)
    private void RenderMapshots()
    {
        int savedStage = _currentStage;
        using var rt = new RenderTarget2D(GraphicsDevice, MapWidth, MapHeight);
        System.IO.Directory.CreateDirectory("mapshots");
        for (int s = 1; s <= StageConfigs.Length; s++)
        {
            _currentStage = s;
            ResetGame();
            GraphicsDevice.SetRenderTarget(rt);
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawGrid();
            foreach (var obs in _obstacles)
            {
                if (obs.IsDestroyed) continue;
                obs.Draw(_spriteBatch, _wallTileTexture, _natureTileTexture, _reliefTileTexture, _sandTileTexture,
                    _houseTileTexture, _elementTileTexture, _villageTileTexture, _waterTileTexture);
            }
            _spriteBatch.End();
            GraphicsDevice.SetRenderTarget(null);
            using var fs = System.IO.File.Create($"mapshots/stage{s}.png");
            rt.SaveAsPng(fs, MapWidth, MapHeight);
        }
        _currentStage = savedStage;
    }

    private void DrawGrid()
    {
        if (_terrainGrid == null) return;

        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, MapWidth, MapHeight), new Color(8, 10, 18));

        for (int tx = 0; tx < _terrainCols; tx++)
        {
            for (int ty = 0; ty < _terrainRows; ty++)
            {
                DrawTerrainCell(tx, ty, _terrainGrid[tx, ty]);
            }
        }

        DrawWalkabilityBorders();
        DrawTerrainDecorations();
    }

    // Ve duong vien de phan tach ro o di duoc / khong di duoc.
    // Muc tieu thuần UI: nguoi choi nhin map de doc duong de hon.
    private void DrawWalkabilityBorders()
    {
        int t = 3; // border thickness
        Color edge = new Color(235, 220, 155) * 0.52f;
        Color edgeOuter = new Color(35, 28, 12) * 0.55f;

        for (int tx = 0; tx < _terrainCols; tx++)
        {
            for (int ty = 0; ty < _terrainRows; ty++)
            {
                byte c = _terrainGrid[tx, ty];
                if (!MapTerrain.IsWalkable(c)) continue;

                int x = tx * MapTerrain.CellSize;
                int y = ty * MapTerrain.CellSize;
                int s = MapTerrain.CellSize;

                bool n = !MapTerrain.IsWalkable(GetTerrainSafe(tx, ty - 1));
                bool sdn = !MapTerrain.IsWalkable(GetTerrainSafe(tx, ty + 1));
                bool w = !MapTerrain.IsWalkable(GetTerrainSafe(tx - 1, ty));
                bool e = !MapTerrain.IsWalkable(GetTerrainSafe(tx + 1, ty));

                if (n)
                {
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, s, t), edge);
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y + t, s, 1), edgeOuter);
                }
                if (sdn)
                {
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y + s - t, s, t), edge);
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y + s - t - 1, s, 1), edgeOuter);
                }
                if (w)
                {
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, t, s), edge);
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(x + t, y, 1, s), edgeOuter);
                }
                if (e)
                {
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(x + s - t, y, t, s), edge);
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(x + s - t - 1, y, 1, s), edgeOuter);
                }

                // Cham sang o 4 goc de duong vien lien mach, de nhin khi map cong/cheo.
                if ((n && w) || (n && e) || (sdn && w) || (sdn && e))
                {
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 12, y + 12, 8, 8), edge * 0.55f);
                }
            }
        }
    }

    private bool IsTerrainLiquidAt(int tx, int ty)
    {
        if (tx < 0 || ty < 0 || tx >= _terrainCols || ty >= _terrainRows) return false;
        return MapTerrain.IsLiquid(_terrainGrid[tx, ty]);
    }

    // Toa do tile da duoc kiem chung truc tiep tren anh tileset:
    //  TilesetField:  dirt(16,16) grassL(16,64) grassD(16,112) pink(16,160) snow(16,208)
    //  TilesetFloor:  sand(16,16)/(80,16) pale(208,16) pathL(16,144)/(80,144) pathD(272,144)
    //                 snow(16,272)/(80,272) mud(208,272)/(272,272) ice(16,352)/(64,352) ember(256,352)
    //  TilesetWater:  water(16,16) frozen(272,16) poison(208,144)/(272,144) plank(16,208)
    //  InteriorFloor: paleBrick(16,16) tan(208,16) tanOrnate(272,64) orangeBrick(16,144)
    //                 greenTile(208,144) goldCobble(16,208) darkCobble(208,208) darkOrnate(272,208)
    //  TilesetDungeon: spike(64,16)   TilesetRelief: greenCliff(0/16/32,0) orangeCliff(192/208,96)
    private void DrawTerrainCell(int tx, int ty, byte cell)
    {
        int stage = GetCurrentStageConfig().TerrainVariant;
        int x = tx * MapTerrain.CellSize;
        int y = ty * MapTerrain.CellSize;
        Rectangle dest = new Rectangle(x, y, MapTerrain.CellSize, MapTerrain.CellSize);
        int hash = MapTerrain.Hash(tx, ty, 17);

        if (cell == MapTerrain.Void)
        {
            Color voidColor = new Color(12 + hash % 8, 14 + hash % 6, 22 + hash % 10);
            _spriteBatch.Draw(_pixelTexture, dest, voidColor);
            if (hash % 9 == 0)
                _spriteBatch.Draw(_pixelTexture, dest, Color.Black * 0.25f);
            return;
        }

        if (cell == MapTerrain.Water)
        {
            if (stage == 10)
                _spriteBatch.Draw(_waterTileTexture, dest, new Rectangle(272, 16, 16, 16), Color.White);
            else
                _spriteBatch.Draw(_waterTileTexture, dest, new Rectangle(16, 16, 16, 16), Color.White);
            if (hash % 7 == 0)
                _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 6 + hash % 10, y + 8 + hash % 8, 6, 3), Color.White * 0.25f);
            return;
        }

        if (cell == MapTerrain.Lava)
        {
            Color lavaBase = hash % 3 == 0 ? new Color(200, 55, 12) : new Color(160, 36, 8);
            _spriteBatch.Draw(_pixelTexture, dest, lavaBase);
            _spriteBatch.Draw(_floorTileTexture, dest, new Rectangle(256, 352, 16, 16), new Color(255, 130, 40) * 0.55f);
            if (hash % 4 == 0)
                _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 6, y + 6, 20, 20), new Color(255, 210, 70) * 0.35f);
            return;
        }

        if (cell == MapTerrain.Poison)
        {
            Rectangle poisonSrc = new Rectangle(hash % 2 == 0 ? 208 : 272, 144, 16, 16);
            _spriteBatch.Draw(_waterTileTexture, dest, poisonSrc, Color.White);
            if (hash % 3 == 0)
                _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 8 + hash % 12, y + 8 + hash % 10, 5, 5), new Color(220, 130, 255) * 0.45f);
            return;
        }

        if (cell == MapTerrain.Spike)
        {
            _spriteBatch.Draw(_interiorFloorTexture, dest, new Rectangle(208, 208, 16, 16), new Color(150, 145, 150));
            _spriteBatch.Draw(_wallTileTexture, dest, new Rectangle(64, 16, 16, 16), Color.White);
            if (_stageMechanics.SpikesActive)
                _spriteBatch.Draw(_pixelTexture, dest, new Color(255, 50, 50) * 0.45f);
            return;
        }

        if (cell == MapTerrain.Ice)
        {
            Rectangle iceSrc = new Rectangle(hash % 3 == 0 ? 64 : 16, 352, 16, 16);
            _spriteBatch.Draw(_floorTileTexture, dest, iceSrc, Color.White);
            if (hash % 5 == 0)
                _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 4 + hash % 12, y + 4 + hash % 10, 8, 2), Color.White * 0.35f);
            return;
        }

        if (cell == MapTerrain.Cliff)
        {
            if (stage == 3)
            {
                // Thu vien: tuong go am
                Rectangle wallSrc = new Rectangle(hash % 5 == 0 ? 144 : 80, 16, 16, 16);
                _spriteBatch.Draw(_interiorWallTexture, dest, wallSrc, new Color(200, 175, 150));
                _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y + dest.Height - 4, dest.Width, 4), Color.Black * 0.3f);
                return;
            }

            bool vertical = !MapTerrain.IsWalkable(GetTerrainSafe(tx, ty - 1)) ||
                            !MapTerrain.IsWalkable(GetTerrainSafe(tx, ty + 1));
            bool useOrange = stage == 2 || stage == 6 || stage == 7 || stage == 8;
            Rectangle cliffSrc;
            Color cliffTint = Color.White;
            if (useOrange)
            {
                cliffSrc = new Rectangle(192 + (hash % 2) * 16, 96, 16, 16);
                if (stage == 6) cliffTint = new Color(190, 185, 200);
                if (stage == 2) cliffTint = new Color(170, 175, 165);
            }
            else
            {
                cliffSrc = vertical ? new Rectangle(0, 0, 16, 16)
                                    : new Rectangle(16 + (hash % 2) * 16, 0, 16, 16);
                if (stage == 10) cliffTint = new Color(215, 235, 255);
            }
            _spriteBatch.Draw(_reliefTileTexture, dest, cliffSrc, cliffTint);
            return;
        }

        Texture2D tex;
        Rectangle src;
        Color tint = Color.White;

        switch (cell)
        {
            case MapTerrain.Sand:
                tex = _floorTileTexture;
                src = hash % 6 == 0 ? new Rectangle(208, 16, 16, 16)
                    : new Rectangle(hash % 3 == 0 ? 80 : 16, 16, 16, 16);
                break;

            case MapTerrain.Dirt:
                if (stage == 2 || stage == 4)
                {
                    tex = _floorTileTexture;
                    src = new Rectangle(hash % 2 == 0 ? 208 : 272, 272, 16, 16);
                }
                else
                {
                    tex = _grassTileTexture;
                    src = new Rectangle(16, 16, 16, 16);
                }
                break;

            case MapTerrain.Stone:
                tex = _interiorFloorTexture;
                switch (stage)
                {
                    case 2:  src = new Rectangle(208, 144, 16, 16); tint = new Color(200, 210, 200); break;
                    case 7:  src = new Rectangle(16, 144, 16, 16);  tint = new Color(235, 200, 175); break;
                    case 8:  src = new Rectangle(16, 16, 16, 16);   break;
                    case 10: src = new Rectangle(16, 16, 16, 16);   tint = new Color(200, 220, 245); break;
                    default: src = new Rectangle(16, 208, 16, 16);  break;
                }
                break;

            case MapTerrain.Snow:
                if (hash % 4 == 0)
                {
                    tex = _floorTileTexture;
                    src = new Rectangle(hash % 2 == 0 ? 16 : 80, 272, 16, 16);
                }
                else
                {
                    tex = _grassTileTexture;
                    src = new Rectangle(16, 208, 16, 16);
                }
                break;

            case MapTerrain.Dungeon:
                tex = _interiorFloorTexture;
                src = hash % 9 == 0 ? new Rectangle(272, 208, 16, 16)
                                    : new Rectangle(208, 208, 16, 16);
                if (stage == 3) tint = new Color(215, 200, 190);
                break;

            case MapTerrain.Path:
                tex = _floorTileTexture;
                switch (stage)
                {
                    case 2:
                    case 5:  src = new Rectangle(272, 144, 16, 16); break;
                    case 6:  tex = _interiorFloorTexture; src = new Rectangle(208, 144, 16, 16); break;
                    case 7:  tex = _interiorFloorTexture; src = new Rectangle(16, 208, 16, 16); break;
                    default: src = new Rectangle(hash % 3 == 0 ? 80 : 16, 144, 16, 16); break;
                }
                break;

            case MapTerrain.WoodFloor:
                if (stage == 3)
                {
                    tex = _interiorFloorTexture;
                    src = hash % 11 == 0 ? new Rectangle(272, 64, 16, 16)
                                         : new Rectangle(208, 16, 16, 16);
                }
                else
                {
                    tex = _waterTileTexture;
                    src = new Rectangle(16, 208, 16, 16);
                }
                break;

            case MapTerrain.Moss:
                tex = _grassTileTexture;
                src = new Rectangle(16, 112, 16, 16);
                tint = stage switch
                {
                    4 => new Color(165, 190, 140),
                    2 => new Color(175, 185, 165),
                    _ => new Color(200, 225, 175)
                };
                break;

            case MapTerrain.Grass2:
                tex = _grassTileTexture;
                src = new Rectangle(16, 112, 16, 16);
                if (stage == 4) tint = new Color(190, 205, 160);
                break;

            default: // Grass
                tex = _grassTileTexture;
                src = new Rectangle(16, 64, 16, 16);
                break;
        }

        _spriteBatch.Draw(tex, dest, src, tint);
        DrawShoreBlend(tx, ty, dest);
    }

    private byte GetTerrainSafe(int tx, int ty)
    {
        if (tx < 0 || ty < 0 || tx >= _terrainCols || ty >= _terrainRows) return MapTerrain.Void;
        return _terrainGrid[tx, ty];
    }

    private void DrawShoreBlend(int tx, int ty, Rectangle dest)
    {
        // Vien sang nhe o mep nuoc/lava de bo dat va chat long hoa vao nhau
        bool n = IsTerrainLiquidAt(tx, ty - 1);
        bool s = IsTerrainLiquidAt(tx, ty + 1);
        bool e = IsTerrainLiquidAt(tx + 1, ty);
        bool w = IsTerrainLiquidAt(tx - 1, ty);
        if (!(n || s || e || w)) return;

        Color edge = Color.White * 0.30f;
        int t = 4;
        if (n) _spriteBatch.Draw(_pixelTexture, new Rectangle(dest.X, dest.Y, dest.Width, t), edge);
        if (s) _spriteBatch.Draw(_pixelTexture, new Rectangle(dest.X, dest.Bottom - t, dest.Width, t), edge);
        if (w) _spriteBatch.Draw(_pixelTexture, new Rectangle(dest.X, dest.Y, t, dest.Height), edge);
        if (e) _spriteBatch.Draw(_pixelTexture, new Rectangle(dest.Right - t, dest.Y, t, dest.Height), edge);
    }

    private void DrawTerrainDecorations()
    {
        var config = GetCurrentStageConfig();
        int step = 4;
        for (int tx = 2; tx < _terrainCols - 2; tx += step)
        {
            for (int ty = 2; ty < _terrainRows - 2; ty += step)
            {
                byte cell = _terrainGrid[tx, ty];
                if (!MapTerrain.IsWalkable(cell) || MapTerrain.IsLiquid(cell)) continue;

                int hash  = MapTerrain.Hash(tx, ty, config.Index + 500);
                int hash2 = MapTerrain.Hash(tx, ty, config.Index + 999);
                int x = tx * MapTerrain.CellSize;
                int y = ty * MapTerrain.CellSize;

                // Decor tile da xac minh (TilesetNature): hoa huong duong (0/16,176),
                // co 3 la (32,176), tulip (48,176), duong xi (64/80,176), cuc trang (96,176),
                // da nho nau/cam (64/80/96,208), pha le (0=cam,16=xanh,32=tim,48=luc ,224),
                // da reu hoa (64/80,256)
                switch (config.TerrainVariant)
                {
                    // MAP 1: THAO NGUYEN -- hoa dai ruc ro tren dong co
                    case 1:
                        if (hash % 9 == 0 && cell == MapTerrain.Grass)
                            _spriteBatch.Draw(_natureTileTexture, new Rectangle(x + 4, y + 4, 24, 24), new Rectangle((hash2 % 2) * 16, 176, 16, 16), Color.White);
                        else if (hash % 13 == 0 && cell == MapTerrain.Grass2)
                            _spriteBatch.Draw(_natureTileTexture, new Rectangle(x + 6, y + 6, 20, 20), new Rectangle(hash2 % 2 == 0 ? 48 : 96, 176, 16, 16), Color.White);
                        else if (hash % 17 == 0 && cell == MapTerrain.Moss)
                            _spriteBatch.Draw(_natureTileTexture, new Rectangle(x + 4, y + 4, 24, 24), new Rectangle(32, 176, 16, 16), Color.White);
                        break;

                    // MAP 2: NGHIA TRANG -- duong xi heo, da xam, anh nen ma mi
                    case 2:
                        if (hash % 11 == 0 && cell == MapTerrain.Dirt)
                            _spriteBatch.Draw(_natureTileTexture, new Rectangle(x + 6, y + 8, 20, 20), new Rectangle(64 + (hash2 % 2) * 16, 176, 16, 16), new Color(150, 150, 130));
                        else if (hash % 16 == 0 && (cell == MapTerrain.Dirt || cell == MapTerrain.Moss))
                            _spriteBatch.Draw(_natureTileTexture, new Rectangle(x + 8, y + 10, 16, 16), new Rectangle(64, 208, 16, 16), new Color(170, 170, 175));
                        else if (hash2 % 21 == 0 && cell != MapTerrain.Stone)
                            _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 10, y + 10, 12, 12), new Color(140, 220, 160) * 0.14f);
                        break;

                    // MAP 3: THU VIEN -- tham hoa van, anh nen sach am ap
                    case 3:
                        if (hash % 14 == 0 && cell == MapTerrain.WoodFloor)
                            _spriteBatch.Draw(_interiorFloorTexture, new Rectangle(x, y, 32, 32), new Rectangle(272, 64, 16, 16), Color.White * 0.85f);
                        else if (hash % 19 == 0)
                            _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 8, y + 8, 16, 16), new Color(255, 215, 130) * 0.16f);
                        break;

                    // MAP 4: DAM LAY DOC -- da reu hoa, duong xi, hoi doc tim
                    case 4:
                        if (hash % 10 == 0 && cell == MapTerrain.Moss)
                            _spriteBatch.Draw(_natureTileTexture, new Rectangle(x + 4, y + 4, 24, 24), new Rectangle(64 + (hash2 % 2) * 16, 256, 16, 16), Color.White);
                        else if (hash % 15 == 0 && (cell == MapTerrain.Grass2 || cell == MapTerrain.Moss))
                            _spriteBatch.Draw(_natureTileTexture, new Rectangle(x + 6, y + 6, 20, 20), new Rectangle(64 + (hash2 % 2) * 16, 176, 16, 16), new Color(170, 200, 150));
                        else if (hash2 % 13 == 0)
                            _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 8 + (hash % 10), y + 8 + (hash2 % 8), 6, 6), new Color(170, 80, 230) * 0.22f);
                        break;

                    // MAP 5: RUNG SUONG MU -- duong xi day, pha le xanh phat sang
                    case 5:
                        if (hash % 8 == 0 && (cell == MapTerrain.Grass2 || cell == MapTerrain.Moss))
                            _spriteBatch.Draw(_natureTileTexture, new Rectangle(x + 4, y + 4, 24, 24), new Rectangle(64 + (hash2 % 2) * 16, 176, 16, 16), Color.White);
                        else if (hash % 18 == 0 && cell == MapTerrain.Grass)
                            _spriteBatch.Draw(_natureTileTexture, new Rectangle(x + 6, y + 6, 20, 20), new Rectangle(32, 176, 16, 16), Color.White);
                        else if (hash2 % 23 == 0 && cell == MapTerrain.Moss)
                            _spriteBatch.Draw(_natureTileTexture, new Rectangle(x + 8, y + 6, 16, 20), new Rectangle(16, 224, 16, 16), new Color(190, 255, 230));
                        else if (hash % 26 == 0)
                            _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 6, y + 6, 20, 20), new Color(100, 200, 130) * 0.15f);
                        break;

                    // MAP 6: LAU DAI DO NAT -- da vo, anh duoc am
                    case 6:
                        if (hash % 12 == 0 && cell == MapTerrain.Dungeon)
                            _spriteBatch.Draw(_natureTileTexture, new Rectangle(x + 8, y + 10, 16, 16), new Rectangle(64, 208, 16, 16), new Color(160, 160, 170));
                        else if (hash % 20 == 0 && cell == MapTerrain.Path)
                            _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 8, y + 8, 16, 16), new Color(255, 190, 100) * 0.18f);
                        break;

                    // MAP 7: HAM DUNG NHAM -- da nong, pha le cam, tia lua
                    case 7:
                        if (hash % 10 == 0 && cell == MapTerrain.Stone)
                            _spriteBatch.Draw(_natureTileTexture, new Rectangle(x + 8, y + 10, 16, 16), new Rectangle(80 + (hash2 % 2) * 16, 208, 16, 16), Color.White);
                        else if (hash % 21 == 0 && cell == MapTerrain.Stone)
                            _spriteBatch.Draw(_natureTileTexture, new Rectangle(x + 8, y + 8, 16, 20), new Rectangle(0, 224, 16, 16), Color.White);
                        else if (hash % 7 == 0)
                            _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 4, y + 4, 24, 24), new Color(255, 140, 40) * 0.14f);
                        else if (hash2 % 11 == 0)
                            _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 12 + (hash % 4), y + 12, 4, 7), new Color(255, 220, 80) * 0.45f);
                        break;

                    // MAP 8: SA MAC -- xuong khô, da nau, nang chay
                    case 8:
                        if (hash % 12 == 0 && cell == MapTerrain.Sand)
                            _spriteBatch.Draw(_natureTileTexture, new Rectangle(x + 8, y + 10, 16, 16), new Rectangle(64, 208, 16, 16), new Color(230, 210, 170));
                        else if (hash % 25 == 0 && cell == MapTerrain.Sand)
                            _spriteBatch.Draw(_sandTileTexture, new Rectangle(x + 2, y + 2, 28, 28), new Rectangle(256, 160, 32, 32), Color.White);
                        else if (hash2 % 13 == 0)
                            _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 6, y + 6, 20, 20), new Color(255, 230, 140) * 0.15f);
                        break;

                    // MAP 9: CAU TREO -- anh vuc xanh, hat bui sang
                    case 9:
                        if (hash % 15 == 0 && cell == MapTerrain.Stone)
                            _spriteBatch.Draw(_natureTileTexture, new Rectangle(x + 8, y + 10, 16, 16), new Rectangle(64, 208, 16, 16), new Color(180, 180, 190));
                        else if (hash2 % 14 == 0 && cell == MapTerrain.WoodFloor)
                            _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 8, y + 8, 16, 16), new Color(80, 140, 220) * 0.12f);
                        break;

                    // MAP 10: DINH BANG -- pha le xanh, tuyet lap lanh
                    case 10:
                        if (hash % 12 == 0 && cell == MapTerrain.Snow)
                            _spriteBatch.Draw(_natureTileTexture, new Rectangle(x + 8, y + 6, 16, 20), new Rectangle(16, 224, 16, 16), new Color(220, 240, 255));
                        else if (hash % 17 == 0 && cell == MapTerrain.Ice)
                            _spriteBatch.Draw(_natureTileTexture, new Rectangle(x + 8, y + 10, 16, 16), new Rectangle(64, 192, 16, 16), Color.White);
                        else if (hash2 % 9 == 0)
                            _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 14, y + 14, 4, 4), Color.White * 0.45f);
                        break;
                }
            }
        }
    }

    private void DrawHUD(GameTime gameTime)
    {
        // 1. Draw HP bar
        _spriteBatch.Draw(_pixelTexture, new Rectangle(20, 20, 200, 15), Color.DarkRed);
        float hpPercentage = Math.Max(0f, _player.HP / _player.MaxHP);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(20, 20, (int)(200 * hpPercentage), 15), Color.Red);
        _spriteBatch.DrawString(_scoreFont, $"HP: {(int)_player.HP}/{(int)_player.MaxHP}", new Vector2(25, 18), Color.White);

        // 2. Draw XP bar
        _spriteBatch.Draw(_pixelTexture, new Rectangle(20, 45, 200, 10), Color.DimGray);
        float xpPercentage = Math.Max(0f, _player.XP / _player.TargetXP);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(20, 45, (int)(200 * xpPercentage), 10), Color.Cyan);
        _spriteBatch.DrawString(_scoreFont, $"LVL {_player.Level}", new Vector2(230, 40), Color.Cyan);

        // HP Potions and Revive tokens remaining
        string consText = $"Hoi Sinh: {_player.CurrentRevives} | Binh Mau [H]: {_player.CurrentPotions}";
        DrawTruncatedString(consText, new Vector2(20, 95), 280f, Color.LightSkyBlue);

        // 3. Draw Game Timer and Stage Name
        int minutes = (int)_gameTimer / 60;
        int seconds = (int)_gameTimer % 60;
        string timeText = $"{minutes:00}:{seconds:00}";
        Vector2 timeTextSize = _scoreFont.MeasureString(timeText);
        _spriteBatch.DrawString(_scoreFont, timeText, new Vector2(400 - timeTextSize.X / 2, 20), Color.White);

        DrawTruncatedString(GetStageShortLabel(GetCurrentStageConfig()), new Vector2(20, 70), 260f, Color.Khaki);

        // Endless mode survival label
        if (_inEndlessMode)
        {
            string endlessText = "SINH TON VO HAN";
            Vector2 endlessSize = _scoreFont.MeasureString(endlessText);
            _spriteBatch.DrawString(_scoreFont, endlessText, new Vector2(400 - endlessSize.X / 2, 50), Color.Orange);
        }

        // Blinking Boss Warning alert
        if (_bossWarningTimer > 0f && !string.IsNullOrEmpty(_bossWarningText))
        {
            float pulse = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 12.0);
            if (pulse > 0)
            {
                string warnText = TruncateText(_bossWarningText, 760f);
                Vector2 warnSize = _scoreFont.MeasureString(warnText);
                _spriteBatch.DrawString(_scoreFont, warnText, new Vector2(400 - warnSize.X / 2, 260), Color.Red);
            }
        }

        // 4. Draw Score / Kill Count / Gold Collected (below minimap)
        string killText = $"Kills: {_killCount} | Vang: {_goldCollectedThisRun}";
        DrawTruncatedString(killText, new Vector2(630, 180), 155f, Color.Gold);

        // 5. Draw Active Weapons Slots (bottom left)
        _spriteBatch.DrawString(_scoreFont, "VŨ KHÍ KHẢ DỤNG:", new Vector2(20, 435), Color.Khaki);
        int weaponSlotIndex = 0;
        foreach (var weapon in _weapons)
        {
            if (weapon.Level > 0)
            {
                int sx = 20 + weaponSlotIndex * (42 + 10);
                int sy = 458;
                Rectangle slotRect = new Rectangle(sx, sy, 42, 42);

                // Slot Panel Background & Golden Border
                _spriteBatch.Draw(_pixelTexture, slotRect, new Color(30, 30, 35, 180));
                _spriteBatch.Draw(_pixelTexture, new Rectangle(sx, sy, 42, 2), Color.Gold * 0.4f);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(sx, sy + 40, 42, 2), Color.Gold * 0.4f);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(sx, sy, 2, 42), Color.Gold * 0.4f);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(sx + 40, sy, 2, 42), Color.Gold * 0.4f);

                // Draw Icon
                string code = WeaponNameToCode(weapon.Name);
                DrawItemIcon(_spriteBatch, code, slotRect);

                // Level Pips
                DrawLevelPips(_spriteBatch, weapon.Level, slotRect);
                var weaponTip = GetItemTooltipInfo(code, weapon.Level);
                RegisterTooltip(slotRect, weaponTip.title, weaponTip.body);

                weaponSlotIndex++;
            }
        }

        // 6. Draw Active Passives Slots (bottom left)
        _spriteBatch.DrawString(_scoreFont, "TRANG BỊ HỖ TRỢ:", new Vector2(20, 505), Color.Khaki);
        int passiveSlotIndex = 0;
        
        Action<string, int> drawPassiveSlot = (code, level) =>
        {
            if (level > 0)
            {
                int row = passiveSlotIndex / 6;
                int col = passiveSlotIndex % 6;
                int sx = 20 + col * (42 + 10);
                int sy = 528 + row * (34 + 6);
                
                Rectangle slotRect = new Rectangle(sx, sy, 34, 34);

                _spriteBatch.Draw(_pixelTexture, slotRect, new Color(30, 30, 35, 180));
                _spriteBatch.Draw(_pixelTexture, new Rectangle(sx, sy, 34, 2), Color.Gold * 0.3f);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(sx, sy + 32, 34, 2), Color.Gold * 0.3f);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(sx, sy, 2, 34), Color.Gold * 0.3f);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(sx + 32, sy, 2, 34), Color.Gold * 0.3f);

                DrawItemIcon(_spriteBatch, code, slotRect);

                // Level Pips
                DrawLevelPips(_spriteBatch, level, slotRect);
                var passiveTip = GetItemTooltipInfo(code, level);
                RegisterTooltip(slotRect, passiveTip.title, passiveTip.body);

                passiveSlotIndex++;
            }
        };

        drawPassiveSlot("BOOTS", _player.SpeedBootsLevel);
        drawPassiveSlot("REGEN", _player.RegenRingLevel);
        drawPassiveSlot("SHIELD", _player.ShieldLevel);
        drawPassiveSlot("SPINACH", _player.SpinachLevel);
        drawPassiveSlot("MAGNET", _player.AttractorbLevel);
        drawPassiveSlot("DUPLICATOR", _player.DuplicatorLevel);
        drawPassiveSlot("CLOVER", _player.CloverLevel);
        drawPassiveSlot("HEART", _player.HollowHeartLevel);
        drawPassiveSlot("PUMMAROLA", _player.PummarolaLevel);
        drawPassiveSlot("BRACER", _player.BracerLevel);
        drawPassiveSlot("SPELLBINDER", _player.SpellbinderLevel);
        drawPassiveSlot("TOME", _player.EmptyTomeLevel);
    }

    private void DrawItemIcon(SpriteBatch spriteBatch, string itemCode, Rectangle destRect)
    {
        string lookupCode = itemCode;
        if (itemCode.StartsWith("WEAPON_")) lookupCode = itemCode.Substring(7);
        else if (itemCode.StartsWith("PASSIVE_")) lookupCode = itemCode.Substring(8);
        else if (itemCode.StartsWith("GOLD_HEAL_")) lookupCode = "GOLD";
        else if (itemCode.StartsWith("EXP_")) lookupCode = "EXP";
        else if (itemCode == "GOLD_150" || itemCode == "SUPER_GOLD") lookupCode = "GOLD";

        if (lookupCode == "AXE")
        {
            spriteBatch.Draw(_axeProjTexture, new Rectangle(destRect.X + (destRect.Width - 22) / 2, destRect.Y + (destRect.Height - 24) / 2, 22, 24), Color.White);
        }
        else if (lookupCode == "GOLD")
        {
            spriteBatch.Draw(_coinTexture, destRect, Color.White);
        }
        else if (lookupCode == "EXP")
        {
            spriteBatch.Draw(_xpGemTexture, destRect, Color.White);
        }
        else if (_itemIcons.TryGetValue(lookupCode, out var texture))
        {
            spriteBatch.Draw(texture, destRect, Color.White);
        }
        else
        {
            spriteBatch.Draw(_pixelTexture, destRect, Color.Gray);
        }
    }

    private void DrawLevelPips(SpriteBatch spriteBatch, int level, Rectangle slotRect)
    {
        int maxLevel = 5;
        int pipW = 4;
        int pipH = 5;
        int gap = 2;
        int totalW = maxLevel * pipW + (maxLevel - 1) * gap;
        int startX = slotRect.X + (slotRect.Width - totalW) / 2;
        int startY = slotRect.Bottom - pipH - 3;

        for (int i = 0; i < maxLevel; i++)
        {
            Color pipColor;
            if (level >= 5)
            {
                pipColor = Color.Orange;
            }
            else if (i < level)
            {
                pipColor = Color.Yellow;
            }
            else
            {
                pipColor = Color.Gray * 0.4f;
            }

            spriteBatch.Draw(_pixelTexture, new Rectangle(startX + i * (pipW + gap), startY, pipW, pipH), pipColor);
        }
    }

    private string GetStageUpgradeHint()
    {
        int choices = Math.Min(2 + _currentStage, 6);
        string hint = $"Man {_currentStage}: {choices} lua chon";
        if (_currentStage >= 2)
            hint += ", xao tron";
        if (_currentStage >= 4)
            hint += ", quai dong";
        if (_currentStage >= 7)
            hint += ", boss nhieu";
        return hint;
    }

    private void DrawMiniMap(GameTime gameTime)
    {
        if (!_settingsShowMinimap)
            return;
        // Place Mini Map on top-right (X=630, Y=20, W=150, H=150)
        int mx = 630;
        int my = 20;
        int mw = 150;
        int mh = 150;
        float scale = 150f / 2000f; // Scale factor

        // Semi-transparent black background
        _spriteBatch.Draw(_pixelTexture, new Rectangle(mx, my, mw, mh), Color.Black * 0.5f);
        
        // Golden border frame
        _spriteBatch.Draw(_pixelTexture, new Rectangle(mx, my, mw, 2), Color.Gold * 0.6f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(mx, my + mh - 2, mw, 2), Color.Gold * 0.6f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(mx, my, 2, mh), Color.Gold * 0.6f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(mx + mw - 2, my, 2, mh), Color.Gold * 0.6f);

        // 2. Draw ruins/wall obstacles (grey dots/lines)
        foreach (var obs in _obstacles)
        {
            int ox = mx + (int)(obs.Position.X * scale);
            int oy = my + (int)(obs.Position.Y * scale);
            int ow = (int)(obs.Width * scale);
            int oh = (int)(obs.Height * scale);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(ox, oy, Math.Max(2, ow), Math.Max(2, oh)), Color.Gray * 0.7f);
        }

        // 3. Draw Chests (Pulsing gold dots)
        float pulse = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 8.0) * 1f;
        foreach (var chest in _chests)
        {
            int cx = mx + (int)(chest.Position.X * scale);
            int cy = my + (int)(chest.Position.Y * scale);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(cx - 2 - (int)(pulse/2), cy - 2 - (int)(pulse/2), 4 + (int)pulse, 4 + (int)pulse), Color.Yellow);
        }

        // 4. Draw Normal Enemies & Boss Golem
        foreach (var enemy in _enemies)
        {
            int ex = mx + (int)(enemy.Position.X * scale);
            int ey = my + (int)(enemy.Position.Y * scale);
            if (enemy.Type == EnemyType.GolemBoss)
            {
                float flash = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 12.0);
                Color bossColor = flash > 0 ? Color.Red : Color.DarkRed;
                _spriteBatch.Draw(_pixelTexture, new Rectangle(ex - 3, ey - 3, 6, 6), bossColor);
            }
            else
            {
                _spriteBatch.Draw(_pixelTexture, new Rectangle(ex - 1, ey - 1, 2, 2), Color.Red * 0.8f);
            }
        }

        // 5. Draw Player (Pulsing green dot)
        int px = mx + (int)(_player.Position.X * scale);
        int py = my + (int)(_player.Position.Y * scale);
        float pPulse = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 10.0) * 1f;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(px - 2 - (int)(pPulse/2), py - 2 - (int)(pPulse/2), 5 + (int)pPulse, 5 + (int)pPulse), Color.LimeGreen);
    }

    private static string SanitizeFontText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text
            .Replace('—', '-')
            .Replace('–', '-')
            .Replace('“', '"')
            .Replace('”', '"')
            .Replace('’', '\'');
    }

    private string WrapText(string text, float maxLineWidth)
    {
        text = SanitizeFontText(text);
        string[] words = text.Split(' ');
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        float lineWidth = 0f;
        float spaceWidth = _scoreFont.MeasureString(" ").X;

        foreach (string word in words)
        {
            Vector2 size = _scoreFont.MeasureString(word);
            if (lineWidth + size.X > maxLineWidth)
            {
                sb.Append("\n");
                sb.Append(word);
                lineWidth = size.X;
            }
            else
            {
                if (sb.Length > 0 && sb[sb.Length - 1] != '\n')
                {
                    sb.Append(" ");
                    lineWidth += spaceWidth;
                }
                sb.Append(word);
                lineWidth += size.X;
            }
        }
        return sb.ToString();
    }

    private string TruncateText(string text, float maxWidth)
    {
        text = SanitizeFontText(text);
        if (string.IsNullOrEmpty(text) || _scoreFont.MeasureString(text).X <= maxWidth)
            return text;

        const string ellipsis = "...";
        float ellipsisWidth = _scoreFont.MeasureString(ellipsis).X;
        for (int len = text.Length - 1; len > 0; len--)
        {
            string candidate = text.Substring(0, len) + ellipsis;
            if (_scoreFont.MeasureString(candidate).X <= maxWidth)
                return candidate;
        }

        return ellipsis;
    }

    private float GetWrappedTextHeight(string text, float maxLineWidth)
    {
        return _scoreFont.MeasureString(WrapText(text, maxLineWidth)).Y;
    }

    private void RegisterTooltip(Rectangle bounds, string title, string body)
    {
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
            return;
        _tooltipEntries.Add(new TooltipEntry
        {
            Bounds = bounds,
            Title = SanitizeFontText(title ?? string.Empty),
            Body = SanitizeFontText(body ?? string.Empty)
        });
    }

    private Point GetUiMousePosition()
    {
        var mouse = Mouse.GetState();
        int physicalW = GraphicsDevice.Viewport.Width;
        int physicalH = GraphicsDevice.Viewport.Height;
        float targetAspect = 800f / 600f;
        float physicalAspect = (float)physicalW / physicalH;

        int destW = physicalW;
        int destH = physicalH;
        int destX = 0;
        int destY = 0;

        if (physicalAspect > targetAspect)
        {
            destW = (int)(physicalH * targetAspect);
            destH = physicalH;
            destX = (physicalW - destW) / 2;
        }
        else if (physicalAspect < targetAspect)
        {
            destW = physicalW;
            destH = (int)(physicalW / targetAspect);
            destY = (physicalH - destH) / 2;
        }

        if (mouse.X < destX || mouse.X >= destX + destW || mouse.Y < destY || mouse.Y >= destY + destH)
            return new Point(-1, -1);

        float scaleX = 800f / destW;
        float scaleY = 600f / destH;
        return new Point((int)((mouse.X - destX) * scaleX), (int)((mouse.Y - destY) * scaleY));
    }

    private void DrawTooltips()
    {
        if (_tooltipEntries.Count == 0)
            return;

        Point mouse = GetUiMousePosition();
        if (mouse.X < 0)
            return;

        TooltipEntry? hovered = null;
        for (int i = _tooltipEntries.Count - 1; i >= 0; i--)
        {
            if (_tooltipEntries[i].Bounds.Contains(mouse))
            {
                hovered = _tooltipEntries[i];
                break;
            }
        }

        if (!hovered.HasValue)
            return;

        var entry = hovered.Value;
        const float maxTextWidth = 260f;
        const int pad = 10;
        const int titleGap = 6;

        string wrappedBody = string.IsNullOrWhiteSpace(entry.Body) ? string.Empty : WrapText(entry.Body, maxTextWidth);
        Vector2 titleSize = string.IsNullOrWhiteSpace(entry.Title) ? Vector2.Zero : _scoreFont.MeasureString(entry.Title);
        Vector2 bodySize = string.IsNullOrWhiteSpace(wrappedBody) ? Vector2.Zero : _scoreFont.MeasureString(wrappedBody);

        int boxW = (int)Math.Max(140f, Math.Max(titleSize.X, bodySize.X)) + pad * 2;
        int boxH = pad + (int)titleSize.Y + (string.IsNullOrWhiteSpace(wrappedBody) ? 0 : titleGap + (int)bodySize.Y) + pad;

        int boxX = mouse.X + 14;
        int boxY = mouse.Y + 14;
        if (boxX + boxW > 796) boxX = mouse.X - boxW - 14;
        if (boxY + boxH > 596) boxY = mouse.Y - boxH - 14;
        boxX = Math.Max(4, boxX);
        boxY = Math.Max(4, boxY);

        var box = new Rectangle(boxX, boxY, boxW, boxH);
        _spriteBatch.Draw(_pixelTexture, box, new Color(12, 18, 32, 245));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(box.X, box.Y, box.Width, 2), Color.Gold * 0.85f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(box.X, box.Bottom - 2, box.Width, 2), Color.Gold * 0.85f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(box.X, box.Y, 2, box.Height), Color.Gold * 0.85f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(box.Right - 2, box.Y, 2, box.Height), Color.Gold * 0.85f);

        int textY = box.Y + pad;
        if (!string.IsNullOrWhiteSpace(entry.Title))
        {
            _spriteBatch.DrawString(_scoreFont, entry.Title, new Vector2(box.X + pad, textY), Color.Gold);
            textY += (int)titleSize.Y + titleGap;
        }
        if (!string.IsNullOrWhiteSpace(wrappedBody))
            _spriteBatch.DrawString(_scoreFont, wrappedBody, new Vector2(box.X + pad, textY), Color.LightGray);
    }

    private static string WeaponNameToCode(string name)
    {
        return name switch
        {
            "Magic Wand" => "WAND",
            "Garlic Aura" => "GARLIC",
            "Fireball" => "FIREBALL",
            "Axe" => "AXE",
            "Lightning Ring" => "LIGHTNING",
            "Holy Water" => "HOLYWATER",
            "Cross" => "CROSS",
            "King Bible" => "BIBLE",
            _ => "KNIFE"
        };
    }

    private (string title, string body) GetItemTooltipInfo(string code, int level = 0)
    {
        string lookup = code;
        if (code.StartsWith("WEAPON_")) lookup = code.Substring(7);
        else if (code.StartsWith("PASSIVE_")) lookup = code.Substring(8);
        else if (code.StartsWith("GOLD_HEAL_")) lookup = "GOLD";
        else if (code.StartsWith("EXP_")) lookup = "EXP";
        else if (code == "GOLD_150" || code == "SUPER_GOLD") lookup = "GOLD";

        string title = lookup switch
        {
            "WAND" => "Magic Wand",
            "GARLIC" => "Garlic Aura",
            "FIREBALL" => "Fireball",
            "AXE" => "Axe",
            "LIGHTNING" => "Lightning Ring",
            "HOLYWATER" => "Holy Water",
            "CROSS" => "Cross",
            "BIBLE" => "King Bible",
            "KNIFE" => "Knife",
            "BOOTS" => "Speed Boots",
            "REGEN" => "Regen Ring",
            "SHIELD" => "Armor Shield",
            "SPINACH" => "Spinach",
            "MAGNET" => "Attractorb",
            "DUPLICATOR" => "Duplicator",
            "CLOVER" => "Clover",
            "HEART" => "Hollow Heart",
            "PUMMAROLA" => "Pummarola",
            "BRACER" => "Bracer",
            "SPELLBINDER" => "Spellbinder",
            "TOME" => "Empty Tome",
            "HEAL" => "Hoi Mau",
            "REROLL" => "Xao Bai",
            "GOLD" => "Vang",
            "EXP" => "Kinh Nghiem",
            _ => lookup
        };

        string body = lookup switch
        {
            "WAND" => "Ban tia phep theo huong di chuyen. Tang cap tang sat thuong, so tia va toc do ban.",
            "GARLIC" => "Hao quang bao ve quanh nhan vat, gay sat thuong lien tuc cho quai o gan.",
            "FIREBALL" => "Ban cau lua no manh, gay sat thuong dien rong va xuyen thau nhieu quai.",
            "AXE" => "Quang riu xoay tron bay theo quy dao, danh trung nhieu muc tieu.",
            "LIGHTNING" => "Goi sam set tu tren troi xuong quai ngau nhien trong ban kinh.",
            "HOLYWATER" => "Nem lo nuoc thanh tao vung sat thuong lien tuc tren mat dat.",
            "CROSS" => "Phong thap tu bay nhu boomerang, xuyen thau quai tren duong di.",
            "BIBLE" => "Sach kinh xoay quanh nhan vat, bao ve va gay sat thuong lien tuc.",
            "KNIFE" => "Phong dao nhanh ve huong di chuyen, phu hop danh quai mong.",
            "BOOTS" => "Tang toc do di chuyen, giup ne tranh va quet map nhanh hon.",
            "REGEN" => "Tu dong hoi HP theo thoi gian, rat huu ich trong tran dai.",
            "SHIELD" => "Giam sat thuong nhan vao; cap cao co the phan sat thuong.",
            "SPINACH" => "Tang sat thuong toan bo vu khi va hieu ung tan cong.",
            "MAGNET" => "Tang ban kinh hut ngoc kinh nghiem va vang tu xa.",
            "DUPLICATOR" => "Co hoi nhan doi phan thuong hoac hieu ung khi giet quai.",
            "CLOVER" => "Tang may man, giup nhan phan thuong tot hon tu ruong.",
            "HEART" => "Tang HP toi da, giup song sot lau hon truoc dong quai.",
            "PUMMAROLA" => "Tang toc do hoi HP tu nhien va hieu qua hoi mau.",
            "BRACER" => "Giam thoi gian hoi chieu vu khi, ban chieu nhanh hon.",
            "SPELLBINDER" => "Tang thoi gian ton tai cua vu khi va hieu ung xoay quanh.",
            "TOME" => "Giam thoi gian hoi chieu toan bo, tang tan suat tan cong.",
            "HEAL" => "Hoi ngay mot luong HP lon de tiep tuc tran dau.",
            "REROLL" => "Xao lai danh sach nang cap de tim lua chon tot hon.",
            "GOLD" => "Nhan them vang trong tran, dung de mua nang cap vinh vien.",
            "EXP" => "Nhan them kinh nghiem, len cap nhanh hon trong tran.",
            _ => "Vat pham ho tro trong tran dau."
        };

        if (level > 0)
            body += $"\nCap hien tai: {level}/5";
        else if (level == 0 && IsWeaponOrPassiveCode(lookup))
            body += "\nChua so huu hoac chua mo khoa.";

        return (title, body);
    }

    private static bool IsWeaponOrPassiveCode(string code)
    {
        return code is "WAND" or "GARLIC" or "FIREBALL" or "AXE" or "LIGHTNING" or "HOLYWATER"
            or "CROSS" or "BIBLE" or "KNIFE" or "BOOTS" or "REGEN" or "SHIELD" or "SPINACH"
            or "MAGNET" or "DUPLICATOR" or "CLOVER" or "HEART" or "PUMMAROLA" or "BRACER"
            or "SPELLBINDER" or "TOME";
    }

    private string GetShopItemTooltip(int index)
    {
        return index switch
        {
            0 => "Max HP: Tang +10% mau toi da vinh vien cho nhan vat.",
            1 => "Sat Thuong: Tang +10% sat thuong gay ra cho quai vinh vien.",
            2 => "Toc Do: Tang +5% toc do di chuyen trong tran vinh vien.",
            3 => "Giap: Giam -1 sat thuong vat ly nhan vao tren moi cap vinh vien.",
            4 => "Nam Cham: Tang +15% ban kinh hut ngoc va vang vinh vien.",
            5 => "Hoi Chieu: Giam -4% thoi gian cho giua moi lan ra chieu vinh vien.",
            6 => "Tang Truong: Tang +8% diem kinh nghiem (EXP) thu thap duoc vinh vien.",
            7 => "Greed: Tang +10% luong vang thu thap duoc trong tran vinh vien.",
            8 => "Luck: Tang +10% may man mo o qua thu 5 va xao bai vinh vien.",
            9 => "Hoi Sinh: Tu dong hoi sinh 50% HP khi het mau trong tran (Max 3).",
            10 => "Binh Mau: Mang theo binh tiep te giup hoi +30 HP khi bam H (Max 5).",
            11 => "Bua Vang: Nhan doi toan bo luong vang nhan duoc trong tran dau ke.",
            12 => "Thoat cua hang va quay tro lai sanh cho chinh.",
            _ => string.Empty
        };
    }

    private string GetLobbyOptionTooltip(int index)
    {
        return index switch
        {
            0 => "Bat dau tran dau tai man hien tai voi trang bi va vat pham da mua.",
            1 => "Chon 1 trong 10 man choi, do kho va co che dac biet tang dan.",
            2 => "Mua nang cap vinh vien: HP, sat thuong, toc do, giap, nam cham...",
            3 => "Doi ngoai hinh nhan vat. Mot so skin can mua bang vang.",
            4 => "Cai dat do hoa, am thanh, giao dien va do kho.",
            5 => "Thoat game ve man hinh nen.",
            _ => string.Empty
        };
    }

    private string GetSkinTooltip(SkinOption skin)
    {
        bool unlocked = _saveData.UnlockedSkins.Contains(skin.Code);
        string status = unlocked ? "Da so huu." : $"Gia: {skin.Cost} Vang.";
        string action = _saveData.SelectedSkin == skin.Code ? "Dang su dung."
            : unlocked ? "ENTER de chon lam nhan vat."
            : "ENTER de mua va mo khoa.";
        return $"Skin trang tri nhan vat. {status} {action}";
    }

    private string GetStageTooltipBody(StageConfig config)
    {
        float spdRate = config.MonsterSpeedMultiplier;
        float hpRate = config.MonsterHPMultiplier;
        float spawnRate = 1f / config.SpawnIntervalMultiplier;
        return config.Description + "\n\n"
            + $"Toc do quai: x{spdRate:0.00} | HP quai: x{hpRate:0.00}\n"
            + $"Tan suat quai: x{spawnRate:0.00} | Quai/dot: x{config.SpawnBatchCount}\n"
            + $"Boss dai vuong: moi {config.BossSpawnIntervalSeconds}s";
    }

    private void DrawTruncatedString(string text, Vector2 position, float maxWidth, Color color)
    {
        _spriteBatch.DrawString(_scoreFont, TruncateText(text, maxWidth), position, color);
    }

    private string GetStageShortLabel(StageConfig config)
    {
        int colon = config.Name.IndexOf(':');
        string shortName = colon >= 0 ? config.Name.Substring(colon + 1).Trim() : config.Name;
        return $"M{config.Index} {shortName}";
    }

    private void DrawUpgradeMenu()
    {
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, 800, 600), new Color(0, 0, 0, 200));

        // Header
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, 800, 70), new Color(20, 30, 50));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 68, 800, 3), Color.Gold);

        string title = "LEN CAP! CHON NANG CAP MOI";
        string stageHint = TruncateText(GetStageUpgradeHint(), 760f);
        Vector2 titleSize = _scoreFont.MeasureString(title);
        _spriteBatch.DrawString(_scoreFont, title, new Vector2(400 - titleSize.X / 2, 20), Color.Gold);
        Vector2 hintSize = _scoreFont.MeasureString(stageHint);
        _spriteBatch.DrawString(_scoreFont, stageHint, new Vector2(400 - hintSize.X / 2, 46), Color.LightSkyBlue * 0.8f);

        // Card layout: 2 columns if more than 4 cards, else single column
        int count = _upgradeChoices.Count;
        bool twoCol = count > 4;
        int cardW = twoCol ? 385 : 560;
        int cardH = 75;
        int startX_col1 = twoCol ? 10 : 120;
        int startX_col2 = 405;
        int startY = 85;
        int gap = 8;

        for (int i = 0; i < count; i++)
        {
            var choice = _upgradeChoices[i];

            int col = twoCol ? i % 2 : 0;
            int row = twoCol ? i / 2 : i;
            int cardX = col == 0 ? startX_col1 : startX_col2;
            int cardY = startY + row * (cardH + gap);

            // Special card colors
            Color bgColor = choice.Code == "HEAL"    ? new Color(30, 50, 30) :
                            choice.Code == "REROLL"  ? new Color(50, 30, 50) :
                                                       new Color(25, 35, 55);
            Color borderColor = choice.Code == "HEAL"   ? Color.LimeGreen * 0.7f :
                                choice.Code == "REROLL" ? Color.Violet * 0.7f :
                                                          Color.Gold * 0.5f;

            _spriteBatch.Draw(_pixelTexture, new Rectangle(cardX, cardY, cardW, cardH), bgColor);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(cardX, cardY, cardW, 2), borderColor);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(cardX, cardY + cardH - 2, cardW, 2), borderColor);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(cardX, cardY, 2, cardH), borderColor);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(cardX + cardW - 2, cardY, 2, cardH), borderColor);

            // Key badge
            string keyText = $"[{i + 1}]";
            Color keyColor = choice.Code == "HEAL"   ? Color.LimeGreen :
                             choice.Code == "REROLL" ? Color.Violet : Color.Yellow;
            _spriteBatch.Draw(_pixelTexture, new Rectangle(cardX + 5, cardY + 22, 26, 26), new Color(10, 10, 20));
            Vector2 keySize = _scoreFont.MeasureString(keyText);
            _spriteBatch.DrawString(_scoreFont, keyText, new Vector2(cardX + 5 + 13 - keySize.X / 2, cardY + 22 + 6), keyColor);

            // Icon
            Rectangle iconRect = new Rectangle(cardX + 36, cardY + 10, 36, 36);
            DrawItemIcon(_spriteBatch, choice.Code, iconRect);

            // Title & Description
            Color titleColor = choice.Code == "HEAL"   ? Color.LightGreen :
                               choice.Code == "REROLL" ? Color.Plum : Color.LimeGreen;
            DrawTruncatedString(choice.Title, new Vector2(cardX + 78, cardY + 10), cardW - 88f, titleColor);

            string wrappedDesc = WrapText(choice.Description, cardW - 88f);
            _spriteBatch.DrawString(_scoreFont, wrappedDesc, new Vector2(cardX + 78, cardY + 34), Color.LightGray);
            var upgradeTip = GetItemTooltipInfo(choice.Code);
            RegisterTooltip(new Rectangle(cardX, cardY, cardW, cardH), choice.Title, choice.Description + "\n\n" + upgradeTip.body);
        }

        // Footer guide
        string guide = count <= 3 ? "Nhan [1] [2] [3] de chon" :
                       count <= 5 ? "Nhan [1]-[5] de chon" : "Nhan [1]-[7] de chon";
        Vector2 guideSize = _scoreFont.MeasureString(guide);
        _spriteBatch.DrawString(_scoreFont, guide, new Vector2(400 - guideSize.X / 2, 575), Color.DimGray);
    }

    private void DrawChestOpenMenu()
    {
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, 800, 600), new Color(0, 0, 0, 220));

        string title = "★ PHẦN THƯỞNG RƯƠNG BÁU ★";
        Vector2 titleSize = _scoreFont.MeasureString(title);
        _spriteBatch.DrawString(_scoreFont, title, new Vector2(400 - titleSize.X / 2, 120), Color.Gold);

        string subTitle = "Bạn mở rương và tìm thấy:";
        Vector2 subSize = _scoreFont.MeasureString(subTitle);
        _spriteBatch.DrawString(_scoreFont, subTitle, new Vector2(400 - subSize.X / 2, 180), Color.White);

        // Render granted rewards
        string reward = _chestOpenedMessage;
        int yPos = 250;

        _spriteBatch.Draw(_pixelTexture, new Rectangle(150, yPos, 500, 60), new Color(40, 40, 50));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(150, yPos, 500, 2), Color.Gold * 0.5f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(150, yPos + 58, 500, 2), Color.Gold * 0.5f);

        Vector2 rSize = _scoreFont.MeasureString(TruncateText(reward, 470f));
        _spriteBatch.DrawString(_scoreFont, TruncateText(reward, 470f), new Vector2(400 - rSize.X / 2, yPos + 18), Color.LimeGreen);
        RegisterTooltip(new Rectangle(150, yPos, 500, 60), "Phan Thuong Ruong", _chestOpenedMessage);

        string closeText = "Nhấn ENTER hoặc SPACE để Tiếp Tục";
        Vector2 closeSize = _scoreFont.MeasureString(closeText);
        _spriteBatch.DrawString(_scoreFont, closeText, new Vector2(400 - closeSize.X / 2, 450), Color.Khaki);
    }

    private void DrawGameOverScreen()
    {
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, 800, 600), new Color(80, 0, 0, 150));

        string title = "KẾT THÚC TRẬN ĐẤU";
        string scoreText = $"Tổng tiêu diệt: {_killCount} | Cấp đạt: {_player.Level}";
        string goldText = $"Số Vàng tích lũy thêm: +{_goldCollectedThisRun} Vàng";
        string restart = "Nhấn SPACE hoặc ENTER để Chơi Lại";
        string exitText = "Nhấn ESC để quay lại Sảnh Chờ";

        Vector2 titleSize = _scoreFont.MeasureString(title);
        Vector2 scoreSize = _scoreFont.MeasureString(scoreText);
        Vector2 goldSize = _scoreFont.MeasureString(goldText);
        Vector2 restartSize = _scoreFont.MeasureString(restart);
        Vector2 exitSize = _scoreFont.MeasureString(exitText);

        _spriteBatch.DrawString(_scoreFont, title, new Vector2(400 - titleSize.X / 2, 160), Color.Red);
        _spriteBatch.DrawString(_scoreFont, scoreText, new Vector2(400 - scoreSize.X / 2, 230), Color.White);
        _spriteBatch.DrawString(_scoreFont, goldText, new Vector2(400 - goldSize.X / 2, 280), Color.Gold);
        _spriteBatch.DrawString(_scoreFont, restart, new Vector2(400 - restartSize.X / 2, 360), Color.LimeGreen);
        _spriteBatch.DrawString(_scoreFont, exitText, new Vector2(400 - exitSize.X / 2, 410), Color.LightGray);
    }

    private float _regenTimer = 0f;

    // --- NEW SKIN SHOP AND PAUSE SCREEN SYSTEMS ---
    private class SkinOption
    {
        public string Code { get; set; }
        public string DisplayName { get; set; }
        public int Cost { get; set; }
    }
    private List<SkinOption> _skinOptions = new List<SkinOption>
    {
        new SkinOption { Code = "Knight", DisplayName = "Hiep Si Sat (Default)", Cost = 0 },
        new SkinOption { Code = "NinjaBlue", DisplayName = "Ninja Xanh Lam", Cost = 200 },
        new SkinOption { Code = "NinjaRed", DisplayName = "Ninja Hoa Long", Cost = 400 },
        new SkinOption { Code = "Samurai", DisplayName = "Samurai Hoang Gia", Cost = 600 },
        new SkinOption { Code = "Monk", DisplayName = "Nha Su Huyen Thoai", Cost = 800 },
        new SkinOption { Code = "KnightGold", DisplayName = "Hiep Si Hoang Kim", Cost = 1000 }
    };

    private void UpdateSkinShop(KeyboardState keyboardState)
    {
        int count = _skinOptions.Count + 1; // plus 1 for the "Back" option
        if ((keyboardState.IsKeyDown(Keys.W) && _previousKeyboardState.IsKeyUp(Keys.W)) ||
            (keyboardState.IsKeyDown(Keys.Up) && _previousKeyboardState.IsKeyUp(Keys.Up)))
        {
            _selectedSkinIndex = (_selectedSkinIndex - 1 + count) % count;
        }
        if ((keyboardState.IsKeyDown(Keys.S) && _previousKeyboardState.IsKeyUp(Keys.S)) ||
            (keyboardState.IsKeyDown(Keys.Down) && _previousKeyboardState.IsKeyUp(Keys.Down)))
        {
            _selectedSkinIndex = (_selectedSkinIndex + 1) % count;
        }

        if ((keyboardState.IsKeyDown(Keys.Enter) && _previousKeyboardState.IsKeyUp(Keys.Enter)) ||
            (keyboardState.IsKeyDown(Keys.Space) && _previousKeyboardState.IsKeyUp(Keys.Space)))
        {
            if (_selectedSkinIndex == _skinOptions.Count) // Back option
            {
                _gameState = GameState.Lobby;
            }
            else
            {
                var skin = _skinOptions[_selectedSkinIndex];
                bool isUnlocked = _saveData.UnlockedSkins.Contains(skin.Code);
                
                if (isUnlocked)
                {
                    _saveData.SelectedSkin = skin.Code;
                    _saveData.Save();
                }
                else
                {
                    if (_saveData.Gold >= skin.Cost)
                    {
                        _saveData.Gold -= skin.Cost;
                        _saveData.UnlockedSkins.Add(skin.Code);
                        _saveData.SelectedSkin = skin.Code;
                        _saveData.Save();
                    }
                }
            }
        }
    }

    private void UpdatePaused(KeyboardState keyboardState)
    {
        if ((keyboardState.IsKeyDown(Keys.W) && _previousKeyboardState.IsKeyUp(Keys.W)) ||
            (keyboardState.IsKeyDown(Keys.Up) && _previousKeyboardState.IsKeyUp(Keys.Up)))
        {
            _selectedPauseIndex = (_selectedPauseIndex - 1 + 3) % 3;
        }
        if ((keyboardState.IsKeyDown(Keys.S) && _previousKeyboardState.IsKeyUp(Keys.S)) ||
            (keyboardState.IsKeyDown(Keys.Down) && _previousKeyboardState.IsKeyUp(Keys.Down)))
        {
            _selectedPauseIndex = (_selectedPauseIndex + 1) % 3;
        }

        if ((keyboardState.IsKeyDown(Keys.Enter) && _previousKeyboardState.IsKeyUp(Keys.Enter)) ||
            (keyboardState.IsKeyDown(Keys.Space) && _previousKeyboardState.IsKeyUp(Keys.Space)))
        {
            if (_selectedPauseIndex == 0) // Resume
            {
                _gameState = GameState.Playing;
            }
            else if (_selectedPauseIndex == 1) // Exit Stage (returns to Lobby and saves current gold)
            {
                _saveData.Gold += _goldCollectedThisRun;
                _saveData.Save();
                _gameState = GameState.Lobby;
            }
            else if (_selectedPauseIndex == 2) // Exit Game
            {
                _saveData.Gold += _goldCollectedThisRun;
                _saveData.Save();
                Exit();
            }
        }
    }

    private void DrawSkinShop(GameTime gameTime)
    {
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, 800, 600), new Color(15, 23, 42));

        // Header
        _spriteBatch.Draw(_pixelTexture, new Rectangle(50, 40, 700, 70), new Color(30, 41, 59));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(50, 40, 700, 3), Color.Gold);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(50, 107, 700, 3), Color.Gold);

        string title = "== CUA HANG NGOAI HINH - SKINS SHOP ==";
        Vector2 titleSize = _scoreFont.MeasureString(title);
        _spriteBatch.DrawString(_scoreFont, title, new Vector2(400 - titleSize.X / 2, 60), Color.Gold);

        // Gold
        string goldText = $"So Vang Hien Co: {_saveData.Gold} Vang";
        Vector2 goldSize = _scoreFont.MeasureString(goldText);
        _spriteBatch.DrawString(_scoreFont, goldText, new Vector2(400 - goldSize.X / 2, 130), Color.Yellow);

        // Options List (Left)
        int sx = 50;
        int sy = 180;
        for (int i = 0; i < _skinOptions.Count; i++)
        {
            var skin = _skinOptions[i];
            bool isUnlocked = _saveData.UnlockedSkins.Contains(skin.Code);
            bool isSelected = _saveData.SelectedSkin == skin.Code;

            string status = isSelected ? "[DANG CHON]" : isUnlocked ? "[DA MO KHOA]" : $"[GIA: {skin.Cost} VANG]";
            Color statusColor = isSelected ? Color.LimeGreen : isUnlocked ? Color.Cyan : Color.Gold;

            Color itemColor = i == _selectedSkinIndex ? Color.Yellow : Color.White;
            string prefix = i == _selectedSkinIndex ? "> " : "  ";
            
            _spriteBatch.DrawString(_scoreFont, $"{prefix}{TruncateText(skin.DisplayName, 175f)}", new Vector2(sx, sy + i * 48), itemColor);
            _spriteBatch.DrawString(_scoreFont, TruncateText(status, 120f), new Vector2(sx + 200, sy + i * 48), statusColor);
            RegisterTooltip(new Rectangle(sx, sy + i * 48 - 2, 330, 44), skin.DisplayName, GetSkinTooltip(skin));
        }

        // Back Option
        Color backColor = _selectedSkinIndex == _skinOptions.Count ? Color.Yellow : Color.White;
        string backPrefix = _selectedSkinIndex == _skinOptions.Count ? "> " : "  ";
        _spriteBatch.DrawString(_scoreFont, $"{backPrefix}QUAY LAI SANH CHO", new Vector2(sx, sy + _skinOptions.Count * 48), backColor);
        RegisterTooltip(new Rectangle(sx, sy + _skinOptions.Count * 48 - 2, 330, 44), "Quay Lai Sanh Cho", "Thoat cua hang skin va quay ve sanh cho.");

        // Preview Box (Right)
        int bx = 490;
        int by = 180;
        int bw = 240;
        int bh = 290;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(bx, by, bw, bh), new Color(30, 41, 59, 200));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(bx, by, bw, 2), Color.Gold * 0.6f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(bx, by + bh - 2, bw, 2), Color.Gold * 0.6f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(bx, by, 2, bh), Color.Gold * 0.6f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(bx + bw - 2, by, 2, bh), Color.Gold * 0.6f);

        _spriteBatch.DrawString(_scoreFont, "ANH XEM TRUOC", new Vector2(bx + 12, by + 12), Color.Khaki);

        Texture2D previewTex = _playerTexture;
        string previewCode = "Knight";
        if (_selectedSkinIndex < _skinOptions.Count) previewCode = _skinOptions[_selectedSkinIndex].Code;
        else previewCode = _saveData.SelectedSkin;
        
        if (_skinTextures.ContainsKey(previewCode)) previewTex = _skinTextures[previewCode];

        int walkFrame = (int)(gameTime.TotalGameTime.TotalSeconds * 6.0) % 4;
        Rectangle sourceRect = new Rectangle(walkFrame * 16, 0, 16, 16); // walking down
        _spriteBatch.Draw(previewTex, new Rectangle(bx + (bw - 96) / 2, by + 65, 96, 96), sourceRect, Color.White);

        if (_selectedSkinIndex < _skinOptions.Count)
        {
            var skin = _skinOptions[_selectedSkinIndex];
            string unlockedStatusText = _saveData.UnlockedSkins.Contains(skin.Code) ? "DA SO HUU" : "CHUA SO HUU";
            Color unlockedColor = _saveData.UnlockedSkins.Contains(skin.Code) ? Color.LimeGreen : Color.LightGray;
            
            Vector2 uSize = _scoreFont.MeasureString(unlockedStatusText);
            _spriteBatch.DrawString(_scoreFont, unlockedStatusText, new Vector2(bx + bw / 2 - uSize.X / 2, by + bh - 70), unlockedColor);

            string actionText = _saveData.SelectedSkin == skin.Code ? "DANG SU DUNG" : _saveData.UnlockedSkins.Contains(skin.Code) ? "ENTER DE CHON" : "ENTER DE MUA";
            Vector2 aSize = _scoreFont.MeasureString(actionText);
            _spriteBatch.DrawString(_scoreFont, actionText, new Vector2(bx + bw / 2 - aSize.X / 2, by + bh - 35), Color.Gold);
        }
        RegisterTooltip(new Rectangle(bx, by, bw, bh), "Anh Xem Truoc", "Xem truoc ngoai hinh skin truoc khi mua hoac chon.");

        string guide = "W/S: Di chuyen | ENTER/SPACE: Chon | ESC: Ve Sanh";
        Vector2 guideSize = _scoreFont.MeasureString(guide);
        _spriteBatch.DrawString(_scoreFont, guide, new Vector2(400 - guideSize.X / 2, 515), Color.Gray);
    }

    private void DrawPausedScreen(GameTime gameTime)
    {
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, 800, 600), new Color(15, 23, 42, 220));

        // Header Panel
        _spriteBatch.Draw(_pixelTexture, new Rectangle(50, 40, 700, 65), new Color(30, 41, 59));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(50, 40, 700, 3), Color.Gold);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(50, 102, 700, 3), Color.Gold);

        string title = "★ TRẬN ĐẤU TẠM DỪNG - PAUSED ★";
        Vector2 titleSize = _scoreFont.MeasureString(title);
        _spriteBatch.DrawString(_scoreFont, title, new Vector2(400 - titleSize.X / 2, 58), Color.Gold);

        // Stats Panel (Left)
        int lx = 50;
        int ly = 130;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(lx, ly, 330, 250), new Color(30, 41, 59, 150));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(lx, ly, 330, 2), Color.Gold * 0.4f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(lx, ly + 248, 330, 2), Color.Gold * 0.4f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(lx, ly, 2, 250), Color.Gold * 0.4f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(lx + 328, ly, 2, 250), Color.Gold * 0.4f);

        _spriteBatch.DrawString(_scoreFont, "THÔNG SỐ CHI TIẾT", new Vector2(lx + 15, ly + 15), Color.Khaki);
        _spriteBatch.DrawString(_scoreFont, $"HP: {(int)_player.HP} / {(int)_player.MaxHP}", new Vector2(lx + 15, ly + 55), Color.Tomato);
        
        _spriteBatch.Draw(_pixelTexture, new Rectangle(lx + 15, ly + 85, 200, 10), Color.DarkRed);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(lx + 15, ly + 85, (int)(200 * (_player.HP / _player.MaxHP)), 10), Color.Red);

        _spriteBatch.DrawString(_scoreFont, $"Cấp Độ: {_player.Level} (XP: {(int)_player.XP}/{(int)_player.TargetXP})", new Vector2(lx + 15, ly + 105), Color.Cyan);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(lx + 15, ly + 135, 200, 8), Color.DimGray);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(lx + 15, ly + 135, (int)(200 * (_player.XP / _player.TargetXP)), 8), Color.Cyan);

        _spriteBatch.DrawString(_scoreFont, $"Mạng tiêu diệt: {_killCount} Kills", new Vector2(lx + 15, ly + 155), Color.LightGray);
        _spriteBatch.DrawString(_scoreFont, $"Vàng trong trận: {_goldCollectedThisRun} Vàng", new Vector2(lx + 15, ly + 185), Color.Gold);
        
        int minutes = (int)_gameTimer / 60;
        int seconds = (int)_gameTimer % 60;
        _spriteBatch.DrawString(_scoreFont, $"Thời gian trôi qua: {minutes:00}:{seconds:00}", new Vector2(lx + 15, ly + 215), Color.White);

        // Equipment Panel (Right)
        int rx = 420;
        int ry = 130;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(rx, ry, 330, 250), new Color(30, 41, 59, 150));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(rx, ry, 330, 2), Color.Gold * 0.4f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(rx, ry + 248, 330, 2), Color.Gold * 0.4f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(rx, ry, 2, 250), Color.Gold * 0.4f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(rx + 328, ry, 2, 250), Color.Gold * 0.4f);

        _spriteBatch.DrawString(_scoreFont, "TRANG BỊ SỞ HỮU", new Vector2(rx + 15, ry + 15), Color.Khaki);

        _spriteBatch.DrawString(_scoreFont, "VŨ KHÍ:", new Vector2(rx + 15, ry + 50), Color.LightGray);
        int weaponIdx = 0;
        foreach (var weapon in _weapons)
        {
            if (weapon.Level > 0)
            {
                int sx = rx + 15 + weaponIdx * 48;
                int sy = ry + 75;
                Rectangle slotRect = new Rectangle(sx, sy, 36, 36);
                
                _spriteBatch.Draw(_pixelTexture, slotRect, new Color(30, 30, 35));
                _spriteBatch.Draw(_pixelTexture, new Rectangle(sx, sy, 36, 2), Color.Gold * 0.4f);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(sx, sy + 34, 36, 2), Color.Gold * 0.4f);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(sx, sy, 2, 36), Color.Gold * 0.4f);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(sx + 34, sy, 2, 36), Color.Gold * 0.4f);

                string code = WeaponNameToCode(weapon.Name);
                
                DrawItemIcon(_spriteBatch, code, slotRect);
                _spriteBatch.DrawString(_scoreFont, $"L{weapon.Level}", new Vector2(sx + 2, sy + 18), Color.Yellow);
                var pauseWeaponTip = GetItemTooltipInfo(code, weapon.Level);
                RegisterTooltip(slotRect, pauseWeaponTip.title, pauseWeaponTip.body);

                weaponIdx++;
            }
        }

        _spriteBatch.DrawString(_scoreFont, "HỖ TRỢ:", new Vector2(rx + 15, ry + 130), Color.LightGray);
        int passiveIdx = 0;
        Action<string, int> drawPausePassive = (code, level) =>
        {
            if (level > 0)
            {
                int row = passiveIdx / 6;
                int col = passiveIdx % 6;
                int sx = rx + 15 + col * 40;
                int sy = ry + 155 + row * 40;
                Rectangle slotRect = new Rectangle(sx, sy, 34, 34);

                _spriteBatch.Draw(_pixelTexture, slotRect, new Color(30, 30, 35));
                _spriteBatch.Draw(_pixelTexture, new Rectangle(sx, sy, 34, 2), Color.Gold * 0.3f);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(sx, sy + 32, 34, 2), Color.Gold * 0.3f);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(sx, sy, 2, 34), Color.Gold * 0.3f);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(sx + 32, sy, 2, 34), Color.Gold * 0.3f);

                DrawItemIcon(_spriteBatch, code, slotRect);
                _spriteBatch.DrawString(_scoreFont, $"L{level}", new Vector2(sx + 2, sy + 18), Color.Yellow);
                var pausePassiveTip = GetItemTooltipInfo(code, level);
                RegisterTooltip(slotRect, pausePassiveTip.title, pausePassiveTip.body);

                passiveIdx++;
            }
        };

        drawPausePassive("BOOTS", _player.SpeedBootsLevel);
        drawPausePassive("REGEN", _player.RegenRingLevel);
        drawPausePassive("SHIELD", _player.ShieldLevel);
        drawPausePassive("SPINACH", _player.SpinachLevel);
        drawPausePassive("MAGNET", _player.AttractorbLevel);
        drawPausePassive("DUPLICATOR", _player.DuplicatorLevel);
        drawPausePassive("CLOVER", _player.CloverLevel);
        drawPausePassive("HEART", _player.HollowHeartLevel);
        drawPausePassive("PUMMAROLA", _player.PummarolaLevel);
        drawPausePassive("BRACER", _player.BracerLevel);
        drawPausePassive("SPELLBINDER", _player.SpellbinderLevel);
        drawPausePassive("TOME", _player.EmptyTomeLevel);

        // Options
        string[] options = { "CHƠI TIẾP (RESUME)", "THOÁT TRẬN VỀ SẢNH (EXIT STAGE)", "THOÁT GAME (EXIT GAME)" };
        int oy = 405;
        for (int i = 0; i < options.Length; i++)
        {
            Color color = i == _selectedPauseIndex ? Color.Yellow : Color.White;
            string prefix = i == _selectedPauseIndex ? "▶ " : "  ";
            string text = prefix + options[i];
            
            Vector2 textSize = _scoreFont.MeasureString(text);
            _spriteBatch.DrawString(_scoreFont, text, new Vector2(400 - textSize.X / 2, oy + i * 45), color);
            string pauseTip = i == 0 ? "Tiep tuc tran dau tu cho nay."
                : i == 1 ? "Thoat tran, giu Vang da thu thap va quay ve Sanh Cho."
                : "Thoat hoan toan khoi game.";
            RegisterTooltip(new Rectangle(200, oy + i * 45 - 4, 400, 40), options[i], pauseTip);
        }

        string guide = "W/S: Di chuyển | ENTER/SPACE: Chọn | ESC: Resume";
        Vector2 guideSize = _scoreFont.MeasureString(guide);
        _spriteBatch.DrawString(_scoreFont, guide, new Vector2(400 - guideSize.X / 2, 545), Color.Gray);
    }
}

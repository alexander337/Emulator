using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using Dapper;

namespace SroNexus.Services
{
    /// <summary>
    /// Real Database Service Implementation for SQL Server
    /// Handles all database operations for SroNexus
    /// </summary>
    public interface IDatabaseService
    {
        Task<bool> InitializeDatabase();
        Task<bool> TestConnection();
        Task<T> ExecuteScalarAsync<T>(string query, object parameters = null);
        Task<IEnumerable<T>> QueryAsync<T>(string query, object parameters = null);
        Task<int> ExecuteAsync(string query, object parameters = null);
        Task<bool> ExecuteTransactionAsync(List<(string query, object parameters)> commands);
        IDbConnection GetConnection();
        Task<bool> BackupDatabase(string backupPath);
        Task<bool> RestoreDatabase(string backupPath);
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger = Log.ForContext<DatabaseService>();
        private readonly string _connectionString;
        private readonly string _gameServerConnectionString;
        private readonly string _masterServerConnectionString;

        public DatabaseService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _gameServerConnectionString = _configuration.GetConnectionString("GameServerConnection");
            _masterServerConnectionString = _configuration.GetConnectionString("MasterServerConnection");
        }

        /// <summary>
        /// Initializes the database and ensures all tables exist
        /// </summary>
        public async Task<bool> InitializeDatabase()
        {
            try
            {
                _logger.Information("Initializing SroNexus database...");

                // Test connection
                if (!await TestConnection())
                {
                    _logger.Error("Failed to connect to database");
                    return false;
                }

                // Check if databases exist
                await EnsureDatabasesExist();

                // Check if tables exist
                await EnsureTablesExist();

                // Load initial data if needed
                await LoadInitialData();

                _logger.Information("Database initialization completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to initialize database");
                return false;
            }
        }

        /// <summary>
        /// Tests database connection
        /// </summary>
        public async Task<bool> TestConnection()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var result = await connection.ExecuteScalarAsync<int>("SELECT 1");
                
                _logger.Information("Database connection test successful");
                return result == 1;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Database connection test failed");
                return false;
            }
        }

        /// <summary>
        /// Executes a scalar query
        /// </summary>
        public async Task<T> ExecuteScalarAsync<T>(string query, object parameters = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                return await connection.ExecuteScalarAsync<T>(query, parameters);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to execute scalar query: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// Executes a query and returns results
        /// </summary>
        public async Task<IEnumerable<T>> QueryAsync<T>(string query, object parameters = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                return await connection.QueryAsync<T>(query, parameters);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to execute query: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// Executes a command
        /// </summary>
        public async Task<int> ExecuteAsync(string query, object parameters = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                return await connection.ExecuteAsync(query, parameters);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to execute command: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// Executes multiple commands in a transaction
        /// </summary>
        public async Task<bool> ExecuteTransactionAsync(List<(string query, object parameters)> commands)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var (query, parameters) in commands)
                {
                    await connection.ExecuteAsync(query, parameters, transaction);
                }

                await transaction.CommitAsync();
                _logger.Information("Transaction completed successfully with {Count} commands", commands.Count);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.Error(ex, "Transaction failed and was rolled back");
                return false;
            }
        }

        /// <summary>
        /// Gets a database connection
        /// </summary>
        public IDbConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Backs up the database
        /// </summary>
        public async Task<bool> BackupDatabase(string backupPath)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var databases = new[] { "SroNexus", "SroNexus_GameServer", "SroNexus_MasterServer" };

                foreach (var database in databases)
                {
                    var backupFile = $"{backupPath}\\{database}_{timestamp}.bak";
                    var query = $@"
                        BACKUP DATABASE [{database}] 
                        TO DISK = @BackupPath 
                        WITH FORMAT, INIT, 
                        NAME = '{database} Full Backup', 
                        SKIP, NOREWIND, NOUNLOAD, STATS = 10";

                    using var connection = new SqlConnection(_connectionString.Replace("SroNexus", "master"));
                    await connection.ExecuteAsync(query, new { BackupPath = backupFile });
                    
                    _logger.Information("Database {Database} backed up to {Path}", database, backupFile);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to backup database");
                return false;
            }
        }

        /// <summary>
        /// Restores the database from backup
        /// </summary>
        public async Task<bool> RestoreDatabase(string backupPath)
        {
            try
            {
                var query = $@"
                    RESTORE DATABASE [SroNexus] 
                    FROM DISK = @BackupPath 
                    WITH REPLACE, RECOVERY";

                using var connection = new SqlConnection(_connectionString.Replace("SroNexus", "master"));
                await connection.ExecuteAsync(query, new { BackupPath = backupPath });
                
                _logger.Information("Database restored from {Path}", backupPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to restore database");
                return false;
            }
        }

        /// <summary>
        /// Ensures all required databases exist
        /// </summary>
        private async Task EnsureDatabasesExist()
        {
            var databases = new[] { "SroNexus", "SroNexus_GameServer", "SroNexus_MasterServer" };
            
            using var connection = new SqlConnection(_connectionString.Replace("SroNexus", "master"));
            
            foreach (var database in databases)
            {
                var exists = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM sys.databases WHERE name = @Database",
                    new { Database = database });

                if (exists == 0)
                {
                    _logger.Warning("Database {Database} does not exist. Creating...", database);
                    await connection.ExecuteAsync($"CREATE DATABASE [{database}]");
                    _logger.Information("Database {Database} created", database);
                }
            }
        }

        /// <summary>
        /// Ensures all required tables exist
        /// </summary>
        private async Task EnsureTablesExist()
        {
            // Check main database tables
            await EnsureTableExists("SroNexus", "SRN_Features", @"
                CREATE TABLE SRN_Features (
                    FeatureID INT IDENTITY(1,1) PRIMARY KEY,
                    FeatureCode NVARCHAR(100) UNIQUE NOT NULL,
                    FeatureName NVARCHAR(200) NOT NULL,
                    FeatureCategory NVARCHAR(100) NOT NULL,
                    IsEnabled BIT DEFAULT 0,
                    Configuration NVARCHAR(MAX),
                    Description NVARCHAR(500),
                    CreatedDate DATETIME DEFAULT GETDATE(),
                    ModifiedDate DATETIME DEFAULT GETDATE()
                )");

            await EnsureTableExists("SroNexus", "SRN_ServerConfig", @"
                CREATE TABLE SRN_ServerConfig (
                    ConfigID INT IDENTITY(1,1) PRIMARY KEY,
                    ConfigKey NVARCHAR(100) UNIQUE NOT NULL,
                    ConfigValue NVARCHAR(MAX) NOT NULL,
                    ConfigType NVARCHAR(50) NOT NULL,
                    Description NVARCHAR(500),
                    ModifiedDate DATETIME DEFAULT GETDATE()
                )");

            // Check game server tables
            await EnsureTableExists("SroNexus_GameServer", "SRN_Items", @"
                CREATE TABLE SRN_Items (
                    ItemID INT IDENTITY(1,1) PRIMARY KEY,
                    ItemCode NVARCHAR(100) UNIQUE NOT NULL,
                    ItemName NVARCHAR(200) NOT NULL,
                    DisplayName NVARCHAR(200) NOT NULL,
                    ItemType NVARCHAR(50) NOT NULL,
                    ItemTypeDisplay NVARCHAR(100) NOT NULL,
                    LevelRequired INT DEFAULT 1,
                    PhysicalAttackMin INT DEFAULT 0,
                    PhysicalAttackMax INT DEFAULT 0,
                    MagicalAttackMin INT DEFAULT 0,
                    MagicalAttackMax INT DEFAULT 0,
                    PhysicalDefense INT DEFAULT 0,
                    MagicalDefense INT DEFAULT 0,
                    HitRatePercent DECIMAL(5,2) DEFAULT 0,
                    ParryRatePercent DECIMAL(5,2) DEFAULT 0,
                    CriticalRatePercent DECIMAL(5,2) DEFAULT 0,
                    StrRequired INT DEFAULT 0,
                    IntRequired INT DEFAULT 0,
                    RaceRestriction NVARCHAR(50) DEFAULT 'Any',
                    GenderRestriction NVARCHAR(50) DEFAULT 'Any',
                    JobRestriction NVARCHAR(100) DEFAULT 'Any',
                    BuyPrice BIGINT DEFAULT 0,
                    SellPrice BIGINT DEFAULT 0,
                    RepairCostBase BIGINT DEFAULT 0,
                    RepairCostMultiplier DECIMAL(5,2) DEFAULT 1.0,
                    MarketValue BIGINT DEFAULT 0,
                    IsActive BIT DEFAULT 1,
                    IsCustom BIT DEFAULT 0,
                    CreatedDate DATETIME DEFAULT GETDATE(),
                    ModifiedDate DATETIME DEFAULT GETDATE(),
                    CreatedBy NVARCHAR(100) DEFAULT 'System',
                    ModifiedBy NVARCHAR(100) DEFAULT 'System'
                )");

            await EnsureTableExists("SroNexus_GameServer", "SRN_Monsters", @"
                CREATE TABLE SRN_Monsters (
                    MonsterID INT IDENTITY(1,1) PRIMARY KEY,
                    MonsterCode NVARCHAR(100) UNIQUE NOT NULL,
                    MonsterName NVARCHAR(200) NOT NULL,
                    DisplayName NVARCHAR(200) NOT NULL,
                    MonsterType NVARCHAR(50) NOT NULL,
                    Level INT DEFAULT 1,
                    HealthPoints BIGINT DEFAULT 100,
                    ManaPoints BIGINT DEFAULT 0,
                    PhysicalAttackMin INT DEFAULT 0,
                    PhysicalAttackMax INT DEFAULT 0,
                    MagicalAttackMin INT DEFAULT 0,
                    MagicalAttackMax INT DEFAULT 0,
                    PhysicalDefense INT DEFAULT 0,
                    MagicalDefense INT DEFAULT 0,
                    HitRatePercent DECIMAL(5,2) DEFAULT 85.00,
                    CriticalRatePercent DECIMAL(5,2) DEFAULT 5.00,
                    AggressionType NVARCHAR(50) DEFAULT 'Passive',
                    DetectionRange INT DEFAULT 15,
                    ChaseRange INT DEFAULT 30,
                    RespawnTime INT DEFAULT 60,
                    MovementSpeed NVARCHAR(50) DEFAULT 'Normal',
                    IsPackHunter BIT DEFAULT 0,
                    IsBerserker BIT DEFAULT 0,
                    IsNightHunter BIT DEFAULT 0,
                    IsMagicResistant BIT DEFAULT 0,
                    ExpReward BIGINT DEFAULT 0,
                    SkillExpReward BIGINT DEFAULT 0,
                    GoldMin BIGINT DEFAULT 0,
                    GoldMax BIGINT DEFAULT 0,
                    IsActive BIT DEFAULT 1,
                    IsCustom BIT DEFAULT 0,
                    CreatedDate DATETIME DEFAULT GETDATE(),
                    ModifiedDate DATETIME DEFAULT GETDATE()
                )");

            // Check master server tables
            await EnsureTableExists("SroNexus_MasterServer", "Accounts", @"
                CREATE TABLE Accounts (
                    AccountID INT IDENTITY(1,1) PRIMARY KEY,
                    Username NVARCHAR(50) UNIQUE NOT NULL,
                    Password NVARCHAR(255) NOT NULL,
                    Email NVARCHAR(255) UNIQUE NOT NULL,
                    SecurityQuestion NVARCHAR(500),
                    SecurityAnswer NVARCHAR(255),
                    IsActive BIT DEFAULT 1,
                    IsBanned BIT DEFAULT 0,
                    BanReason NVARCHAR(500),
                    BanExpireDate DATETIME,
                    IsVIP BIT DEFAULT 0,
                    VIPLevel INT DEFAULT 0,
                    VIPExpireDate DATETIME,
                    LastLoginDate DATETIME,
                    LastLoginIP NVARCHAR(50),
                    HardwareID NVARCHAR(255),
                    FailedLoginAttempts INT DEFAULT 0,
                    CreatedDate DATETIME DEFAULT GETDATE(),
                    ModifiedDate DATETIME DEFAULT GETDATE()
                )");

            await EnsureTableExists("SroNexus_MasterServer", "Characters", @"
                CREATE TABLE Characters (
                    CharacterID INT IDENTITY(1,1) PRIMARY KEY,
                    AccountID INT FOREIGN KEY REFERENCES Accounts(AccountID),
                    CharacterName NVARCHAR(50) UNIQUE NOT NULL,
                    Level INT DEFAULT 1,
                    Experience BIGINT DEFAULT 0,
                    SkillPoints INT DEFAULT 0,
                    StatPoints INT DEFAULT 0,
                    Strength INT DEFAULT 10,
                    Intelligence INT DEFAULT 10,
                    CurrentHP INT DEFAULT 100,
                    MaxHP INT DEFAULT 100,
                    CurrentMP INT DEFAULT 50,
                    MaxMP INT DEFAULT 50,
                    WorldID INT DEFAULT 1,
                    RegionID INT DEFAULT 1,
                    PosX FLOAT DEFAULT 0,
                    PosY FLOAT DEFAULT 0,
                    PosZ FLOAT DEFAULT 0,
                    Gold BIGINT DEFAULT 0,
                    SilkOwned INT DEFAULT 0,
                    SilkGift INT DEFAULT 0,
                    JobType NVARCHAR(50) DEFAULT 'None',
                    JobLevel INT DEFAULT 0,
                    JobExp BIGINT DEFAULT 0,
                    GuildID INT,
                    GuildRank NVARCHAR(50),
                    IsOnline BIT DEFAULT 0,
                    IsDeleted BIT DEFAULT 0,
                    DeleteDate DATETIME,
                    CreatedDate DATETIME DEFAULT GETDATE(),
                    LastPlayedDate DATETIME DEFAULT GETDATE()
                )");
        }

        /// <summary>
        /// Ensures a specific table exists
        /// </summary>
        private async Task EnsureTableExists(string database, string tableName, string createScript)
        {
            var connectionString = database switch
            {
                "SroNexus" => _connectionString,
                "SroNexus_GameServer" => _gameServerConnectionString,
                "SroNexus_MasterServer" => _masterServerConnectionString,
                _ => _connectionString
            };

            using var connection = new SqlConnection(connectionString);
            
            var exists = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
                  WHERE TABLE_NAME = @TableName",
                new { TableName = tableName });

            if (exists == 0)
            {
                _logger.Warning("Table {Table} does not exist in {Database}. Creating...", tableName, database);
                await connection.ExecuteAsync(createScript);
                _logger.Information("Table {Table} created in {Database}", tableName, database);
            }
        }

        /// <summary>
        /// Loads initial data if tables are empty
        /// </summary>
        private async Task LoadInitialData()
        {
            using var connection = new SqlConnection(_connectionString);
            
            // Check if features table is empty
            var featureCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM SRN_Features");

            if (featureCount == 0)
            {
                _logger.Information("Loading initial feature data...");
                
                var features = GetDefaultFeatures();
                foreach (var feature in features)
                {
                    await connection.ExecuteAsync(@"
                        INSERT INTO SRN_Features (FeatureCode, FeatureName, FeatureCategory, IsEnabled, Description)
                        VALUES (@FeatureCode, @FeatureName, @FeatureCategory, @IsEnabled, @Description)",
                        feature);
                }
                
                _logger.Information("Loaded {Count} default features", features.Count);
            }

            // Check if server config is empty
            var configCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM SRN_ServerConfig");

            if (configCount == 0)
            {
                _logger.Information("Loading initial server configuration...");
                
                var configs = GetDefaultServerConfig();
                foreach (var config in configs)
                {
                    await connection.ExecuteAsync(@"
                        INSERT INTO SRN_ServerConfig (ConfigKey, ConfigValue, ConfigType, Description)
                        VALUES (@ConfigKey, @ConfigValue, @ConfigType, @Description)",
                        config);
                }
                
                _logger.Information("Loaded {Count} default configurations", configs.Count);
            }
        }

        /// <summary>
        /// Gets default features
        /// </summary>
        private List<dynamic> GetDefaultFeatures()
        {
            return new List<dynamic>
            {
                new { FeatureCode = "AUTO_HUNT", FeatureName = "Auto-Hunt System", FeatureCategory = "Gameplay", IsEnabled = true, Description = "Automatic hunting with AI assistance" },
                new { FeatureCode = "AUTO_POTION", FeatureName = "Auto-Potion System", FeatureCategory = "Gameplay", IsEnabled = true, Description = "Automatic potion usage" },
                new { FeatureCode = "WEB_MARKET", FeatureName = "Web-Based Market", FeatureCategory = "Economy", IsEnabled = true, Description = "Online marketplace accessible via web" },
                new { FeatureCode = "BOT_PROTECTION", FeatureName = "Bot Protection", FeatureCategory = "Security", IsEnabled = true, Description = "Advanced bot detection and prevention" },
                new { FeatureCode = "PVP_ARENA", FeatureName = "PvP Arena System", FeatureCategory = "PvP", IsEnabled = true, Description = "Structured PvP combat arena" },
                new { FeatureCode = "GUILD_ALLIANCE", FeatureName = "Guild Alliance", FeatureCategory = "Social", IsEnabled = true, Description = "Alliance system between guilds" },
                new { FeatureCode = "ACHIEVEMENT_SYSTEM", FeatureName = "Achievement System", FeatureCategory = "Progression", IsEnabled = true, Description = "Player achievement tracking" },
                new { FeatureCode = "DAILY_QUESTS", FeatureName = "Daily Quests", FeatureCategory = "Quests", IsEnabled = true, Description = "Daily quest system with rewards" },
                new { FeatureCode = "FORTRESS_WARS", FeatureName = "Fortress Wars", FeatureCategory = "Territory", IsEnabled = true, Description = "Enhanced fortress war system" },
                new { FeatureCode = "ALCHEMY_ENHANCED", FeatureName = "Enhanced Alchemy", FeatureCategory = "Enhancement", IsEnabled = true, Description = "Improved alchemy success rates" },
                new { FeatureCode = "MULTI_CLIENT", FeatureName = "Multi-Client Support", FeatureCategory = "Gameplay", IsEnabled = false, Description = "Allow multiple clients per PC" },
                new { FeatureCode = "CUSTOM_DUNGEONS", FeatureName = "Custom Dungeons", FeatureCategory = "Content", IsEnabled = false, Description = "Custom dungeon instances" },
                new { FeatureCode = "BATTLE_ROYALE", FeatureName = "Battle Royale Mode", FeatureCategory = "PvP", IsEnabled = false, Description = "Last man standing PvP mode" },
                new { FeatureCode = "PET_EVOLUTION", FeatureName = "Pet Evolution System", FeatureCategory = "Gameplay", IsEnabled = true, Description = "Evolve pets to higher tiers" },
                new { FeatureCode = "WEATHER_EFFECTS", FeatureName = "Weather Effects", FeatureCategory = "Visual", IsEnabled = true, Description = "Dynamic weather system" },
                new { FeatureCode = "DAY_NIGHT_CYCLE", FeatureName = "Day/Night Cycle", FeatureCategory = "Visual", IsEnabled = true, Description = "Realistic day and night cycle" },
                new { FeatureCode = "AUCTION_HOUSE", FeatureName = "Auction House", FeatureCategory = "Economy", IsEnabled = true, Description = "Advanced auction system" },
                new { FeatureCode = "HONOR_POINTS", FeatureName = "Honor Point System", FeatureCategory = "PvP", IsEnabled = true, Description = "PvP honor rewards" },
                new { FeatureCode = "TITLE_SYSTEM", FeatureName = "Title System", FeatureCategory = "Progression", IsEnabled = true, Description = "Achievement-based titles" },
                new { FeatureCode = "DISCORD_INTEGRATION", FeatureName = "Discord Integration", FeatureCategory = "Social", IsEnabled = true, Description = "Discord bot integration" }
            };
        }

        /// <summary>
        /// Gets default server configuration
        /// </summary>
        private List<dynamic> GetDefaultServerConfig()
        {
            return new List<dynamic>
            {
                new { ConfigKey = "MAX_LEVEL", ConfigValue = "140", ConfigType = "Integer", Description = "Maximum character level" },
                new { ConfigKey = "EXP_RATE", ConfigValue = "10", ConfigType = "Integer", Description = "Experience multiplier" },
                new { ConfigKey = "GOLD_RATE", ConfigValue = "5", ConfigType = "Integer", Description = "Gold drop multiplier" },
                new { ConfigKey = "DROP_RATE", ConfigValue = "3", ConfigType = "Integer", Description = "Item drop multiplier" },
                new { ConfigKey = "MAX_PLUS_LEVEL", ConfigValue = "20", ConfigType = "Integer", Description = "Maximum plus enhancement level" },
                new { ConfigKey = "FORTRESS_WAR_TIME", ConfigValue = "20:00", ConfigType = "Time", Description = "Daily fortress war start time" },
                new { ConfigKey = "MAINTENANCE_MODE", ConfigValue = "false", ConfigType = "Boolean", Description = "Server maintenance mode" },
                new { ConfigKey = "WELCOME_MESSAGE", ConfigValue = "Welcome to SroNexus Server!", ConfigType = "String", Description = "Server welcome message" },
                new { ConfigKey = "MAX_PLAYERS", ConfigValue = "1000", ConfigType = "Integer", Description = "Maximum concurrent players" },
                new { ConfigKey = "PVP_ENABLED", ConfigValue = "true", ConfigType = "Boolean", Description = "Enable PvP globally" },
                new { ConfigKey = "TRADE_ENABLED", ConfigValue = "true", ConfigType = "Boolean", Description = "Enable trading" },
                new { ConfigKey = "STALL_ENABLED", ConfigValue = "true", ConfigType = "Boolean", Description = "Enable stall system" },
                new { ConfigKey = "GUILD_MAX_MEMBERS", ConfigValue = "50", ConfigType = "Integer", Description = "Maximum guild members" },
                new { ConfigKey = "PARTY_MAX_MEMBERS", ConfigValue = "8", ConfigType = "Integer", Description = "Maximum party members" },
                new { ConfigKey = "INVENTORY_SLOTS", ConfigValue = "120", ConfigType = "Integer", Description = "Inventory slot count" },
                new { ConfigKey = "STORAGE_SLOTS", ConfigValue = "200", ConfigType = "Integer", Description = "Storage slot count" },
                new { ConfigKey = "SKILL_POINT_RATE", ConfigValue = "1", ConfigType = "Integer", Description = "Skill point gain rate" },
                new { ConfigKey = "STAT_POINT_RATE", ConfigValue = "1", ConfigType = "Integer", Description = "Stat point gain rate" },
                new { ConfigKey = "RESPAWN_TIME", ConfigValue = "30", ConfigType = "Integer", Description = "Player respawn time in seconds" },
                new { ConfigKey = "TELEPORT_COST_MULTIPLIER", ConfigValue = "1.0", ConfigType = "Float", Description = "Teleport cost multiplier" }
            };
        }
    }
}

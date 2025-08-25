-- =============================================
-- SroNexus SQL Server Database Setup Script
-- Version: 1.0.0
-- Database: SQL Server 2019 or later
-- =============================================

-- Create databases
USE master;
GO

-- Drop existing databases if they exist (BE CAREFUL IN PRODUCTION!)
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'SroNexus')
    DROP DATABASE SroNexus;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'SroNexus_GameServer')
    DROP DATABASE SroNexus_GameServer;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'SroNexus_MasterServer')
    DROP DATABASE SroNexus_MasterServer;
GO

-- Create Main Database
CREATE DATABASE SroNexus
ON PRIMARY 
(
    NAME = N'SroNexus',
    FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\SroNexus.mdf',
    SIZE = 100MB,
    MAXSIZE = UNLIMITED,
    FILEGROWTH = 10MB
)
LOG ON 
(
    NAME = N'SroNexus_log',
    FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\SroNexus_log.ldf',
    SIZE = 50MB,
    MAXSIZE = 2048GB,
    FILEGROWTH = 10MB
);
GO

-- Create Game Server Database
CREATE DATABASE SroNexus_GameServer
ON PRIMARY 
(
    NAME = N'SroNexus_GameServer',
    FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\SroNexus_GameServer.mdf',
    SIZE = 500MB,
    MAXSIZE = UNLIMITED,
    FILEGROWTH = 50MB
)
LOG ON 
(
    NAME = N'SroNexus_GameServer_log',
    FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\SroNexus_GameServer_log.ldf',
    SIZE = 100MB,
    MAXSIZE = 2048GB,
    FILEGROWTH = 25MB
);
GO

-- Create Master Server Database
CREATE DATABASE SroNexus_MasterServer
ON PRIMARY 
(
    NAME = N'SroNexus_MasterServer',
    FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\SroNexus_MasterServer.mdf',
    SIZE = 100MB,
    MAXSIZE = UNLIMITED,
    FILEGROWTH = 10MB
)
LOG ON 
(
    NAME = N'SroNexus_MasterServer_log',
    FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\SroNexus_MasterServer_log.ldf',
    SIZE = 50MB,
    MAXSIZE = 2048GB,
    FILEGROWTH = 10MB
);
GO

-- =============================================
-- SroNexus Main Database Tables
-- =============================================
USE SroNexus;
GO

-- Features Configuration Table
CREATE TABLE SRN_Features (
    FeatureID INT IDENTITY(1,1) PRIMARY KEY,
    FeatureCode NVARCHAR(100) UNIQUE NOT NULL,
    FeatureName NVARCHAR(200) NOT NULL,
    FeatureCategory NVARCHAR(100) NOT NULL,
    IsEnabled BIT DEFAULT 0,
    Configuration NVARCHAR(MAX), -- JSON configuration
    Description NVARCHAR(500),
    CreatedDate DATETIME DEFAULT GETDATE(),
    ModifiedDate DATETIME DEFAULT GETDATE()
);
GO

-- Server Configuration Table
CREATE TABLE SRN_ServerConfig (
    ConfigID INT IDENTITY(1,1) PRIMARY KEY,
    ConfigKey NVARCHAR(100) UNIQUE NOT NULL,
    ConfigValue NVARCHAR(MAX) NOT NULL,
    ConfigType NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500),
    ModifiedDate DATETIME DEFAULT GETDATE()
);
GO

-- =============================================
-- Game Server Database Tables
-- =============================================
USE SroNexus_GameServer;
GO

-- Items Table with User-Friendly Names
CREATE TABLE SRN_Items (
    ItemID INT IDENTITY(1,1) PRIMARY KEY,
    ItemCode NVARCHAR(100) UNIQUE NOT NULL,
    ItemName NVARCHAR(200) NOT NULL,
    DisplayName NVARCHAR(200) NOT NULL,
    ItemType NVARCHAR(50) NOT NULL,
    ItemTypeDisplay NVARCHAR(100) NOT NULL,
    LevelRequired INT DEFAULT 1,
    
    -- Combat Stats (User-friendly)
    PhysicalAttackMin INT DEFAULT 0,
    PhysicalAttackMax INT DEFAULT 0,
    MagicalAttackMin INT DEFAULT 0,
    MagicalAttackMax INT DEFAULT 0,
    PhysicalDefense INT DEFAULT 0,
    MagicalDefense INT DEFAULT 0,
    HitRatePercent DECIMAL(5,2) DEFAULT 0, -- Stored as 85.50 not 0.8550
    ParryRatePercent DECIMAL(5,2) DEFAULT 0,
    CriticalRatePercent DECIMAL(5,2) DEFAULT 0,
    
    -- Requirements
    StrRequired INT DEFAULT 0,
    IntRequired INT DEFAULT 0,
    RaceRestriction NVARCHAR(50) DEFAULT 'Any',
    GenderRestriction NVARCHAR(50) DEFAULT 'Any',
    JobRestriction NVARCHAR(100) DEFAULT 'Any',
    
    -- Economic Properties
    BuyPrice BIGINT DEFAULT 0,
    SellPrice BIGINT DEFAULT 0,
    RepairCostBase BIGINT DEFAULT 0,
    RepairCostMultiplier DECIMAL(5,2) DEFAULT 1.0,
    MarketValue BIGINT DEFAULT 0,
    
    -- Metadata
    IsActive BIT DEFAULT 1,
    IsCustom BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    ModifiedDate DATETIME DEFAULT GETDATE(),
    CreatedBy NVARCHAR(100) DEFAULT 'System',
    ModifiedBy NVARCHAR(100) DEFAULT 'System'
);
GO

-- Alchemy Options Table
CREATE TABLE SRN_ItemAlchemy (
    AlchemyID INT IDENTITY(1,1) PRIMARY KEY,
    ItemID INT FOREIGN KEY REFERENCES SRN_Items(ItemID),
    OptionName NVARCHAR(100) NOT NULL,
    OptionDisplayName NVARCHAR(200) NOT NULL,
    IsEnabled BIT DEFAULT 1,
    MinValue INT DEFAULT 1,
    MaxValue INT DEFAULT 10,
    SuccessRatePercent DECIMAL(5,2) DEFAULT 85.00,
    CreatedDate DATETIME DEFAULT GETDATE()
);
GO

-- Plus System Configuration
CREATE TABLE SRN_ItemPlusSystem (
    PlusID INT IDENTITY(1,1) PRIMARY KEY,
    ItemID INT FOREIGN KEY REFERENCES SRN_Items(ItemID),
    MaxPlusLevel INT DEFAULT 20,
    SuccessRate_1_7 DECIMAL(5,2) DEFAULT 95.00,
    SuccessRate_8_12 DECIMAL(5,2) DEFAULT 85.00,
    SuccessRate_13_15 DECIMAL(5,2) DEFAULT 75.00,
    SuccessRate_16_18 DECIMAL(5,2) DEFAULT 65.00,
    SuccessRate_19_20 DECIMAL(5,2) DEFAULT 55.00,
    DestroyOnFail BIT DEFAULT 1,
    PhysicalBonusPerPlus INT DEFAULT 15,
    MagicalBonusPerPlus INT DEFAULT 10,
    CreatedDate DATETIME DEFAULT GETDATE()
);
GO

-- Monsters Table
CREATE TABLE SRN_Monsters (
    MonsterID INT IDENTITY(1,1) PRIMARY KEY,
    MonsterCode NVARCHAR(100) UNIQUE NOT NULL,
    MonsterName NVARCHAR(200) NOT NULL,
    DisplayName NVARCHAR(200) NOT NULL,
    MonsterType NVARCHAR(50) NOT NULL,
    Level INT DEFAULT 1,
    
    -- Stats
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
    
    -- Behavior
    AggressionType NVARCHAR(50) DEFAULT 'Passive',
    DetectionRange INT DEFAULT 15,
    ChaseRange INT DEFAULT 30,
    RespawnTime INT DEFAULT 60,
    MovementSpeed NVARCHAR(50) DEFAULT 'Normal',
    
    -- Special Behaviors
    IsPackHunter BIT DEFAULT 0,
    IsBerserker BIT DEFAULT 0,
    IsNightHunter BIT DEFAULT 0,
    IsMagicResistant BIT DEFAULT 0,
    
    -- Rewards
    ExpReward BIGINT DEFAULT 0,
    SkillExpReward BIGINT DEFAULT 0,
    GoldMin BIGINT DEFAULT 0,
    GoldMax BIGINT DEFAULT 0,
    
    -- Metadata
    IsActive BIT DEFAULT 1,
    IsCustom BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    ModifiedDate DATETIME DEFAULT GETDATE()
);
GO

-- Monster Drops Table
CREATE TABLE SRN_MonsterDrops (
    DropID INT IDENTITY(1,1) PRIMARY KEY,
    MonsterID INT FOREIGN KEY REFERENCES SRN_Monsters(MonsterID),
    ItemID INT FOREIGN KEY REFERENCES SRN_Items(ItemID),
    DropRatePercent DECIMAL(8,4) DEFAULT 0.01, -- 0.01% precision
    MinQuantity INT DEFAULT 1,
    MaxQuantity INT DEFAULT 1,
    IsRare BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE()
);
GO

-- Skills Table
CREATE TABLE SRN_Skills (
    SkillID INT IDENTITY(1,1) PRIMARY KEY,
    SkillCode NVARCHAR(100) UNIQUE NOT NULL,
    SkillName NVARCHAR(200) NOT NULL,
    DisplayName NVARCHAR(200) NOT NULL,
    SkillType NVARCHAR(50) NOT NULL,
    MasteryType NVARCHAR(50) NOT NULL,
    RequiredLevel INT DEFAULT 1,
    RequiredSkillPoints INT DEFAULT 1,
    
    -- Effects
    DamagePercent DECIMAL(8,2) DEFAULT 100.00,
    CooldownSeconds DECIMAL(5,2) DEFAULT 1.0,
    ManaCost INT DEFAULT 0,
    CastTime DECIMAL(5,2) DEFAULT 0,
    Range INT DEFAULT 1,
    AreaOfEffect INT DEFAULT 0,
    
    -- Metadata
    IsActive BIT DEFAULT 1,
    IsCustom BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    ModifiedDate DATETIME DEFAULT GETDATE()
);
GO

-- =============================================
-- Master Server Database Tables
-- =============================================
USE SroNexus_MasterServer;
GO

-- Accounts Table
CREATE TABLE Accounts (
    AccountID INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) UNIQUE NOT NULL,
    Password NVARCHAR(255) NOT NULL, -- Hashed
    Email NVARCHAR(255) UNIQUE NOT NULL,
    SecurityQuestion NVARCHAR(500),
    SecurityAnswer NVARCHAR(255), -- Hashed
    
    -- Account Status
    IsActive BIT DEFAULT 1,
    IsBanned BIT DEFAULT 0,
    BanReason NVARCHAR(500),
    BanExpireDate DATETIME,
    
    -- VIP/Premium
    IsVIP BIT DEFAULT 0,
    VIPLevel INT DEFAULT 0,
    VIPExpireDate DATETIME,
    
    -- Security
    LastLoginDate DATETIME,
    LastLoginIP NVARCHAR(50),
    HardwareID NVARCHAR(255),
    FailedLoginAttempts INT DEFAULT 0,
    
    -- Metadata
    CreatedDate DATETIME DEFAULT GETDATE(),
    ModifiedDate DATETIME DEFAULT GETDATE()
);
GO

-- Characters Table
CREATE TABLE Characters (
    CharacterID INT IDENTITY(1,1) PRIMARY KEY,
    AccountID INT FOREIGN KEY REFERENCES Accounts(AccountID),
    CharacterName NVARCHAR(50) UNIQUE NOT NULL,
    Level INT DEFAULT 1,
    Experience BIGINT DEFAULT 0,
    SkillPoints INT DEFAULT 0,
    StatPoints INT DEFAULT 0,
    
    -- Stats
    Strength INT DEFAULT 10,
    Intelligence INT DEFAULT 10,
    CurrentHP INT DEFAULT 100,
    MaxHP INT DEFAULT 100,
    CurrentMP INT DEFAULT 50,
    MaxMP INT DEFAULT 50,
    
    -- Location
    WorldID INT DEFAULT 1,
    RegionID INT DEFAULT 1,
    PosX FLOAT DEFAULT 0,
    PosY FLOAT DEFAULT 0,
    PosZ FLOAT DEFAULT 0,
    
    -- Currency
    Gold BIGINT DEFAULT 0,
    SilkOwned INT DEFAULT 0,
    SilkGift INT DEFAULT 0,
    
    -- Job
    JobType NVARCHAR(50) DEFAULT 'None',
    JobLevel INT DEFAULT 0,
    JobExp BIGINT DEFAULT 0,
    
    -- Guild
    GuildID INT,
    GuildRank NVARCHAR(50),
    
    -- Status
    IsOnline BIT DEFAULT 0,
    IsDeleted BIT DEFAULT 0,
    DeleteDate DATETIME,
    
    -- Metadata
    CreatedDate DATETIME DEFAULT GETDATE(),
    LastPlayedDate DATETIME DEFAULT GETDATE()
);
GO

-- =============================================
-- Insert Default Data
-- =============================================

-- Insert default features
USE SroNexus;
GO

INSERT INTO SRN_Features (FeatureCode, FeatureName, FeatureCategory, IsEnabled, Description) VALUES
('AUTO_HUNT', 'Auto-Hunt System', 'Gameplay', 1, 'Automatic hunting with AI assistance'),
('AUTO_POTION', 'Auto-Potion System', 'Gameplay', 1, 'Automatic potion usage'),
('WEB_MARKET', 'Web-Based Market', 'Economy', 1, 'Online marketplace accessible via web'),
('BOT_PROTECTION', 'Bot Protection', 'Security', 1, 'Advanced bot detection and prevention'),
('PVP_ARENA', 'PvP Arena System', 'PvP', 1, 'Structured PvP combat arena'),
('GUILD_ALLIANCE', 'Guild Alliance', 'Social', 1, 'Alliance system between guilds'),
('ACHIEVEMENT_SYSTEM', 'Achievement System', 'Progression', 1, 'Player achievement tracking'),
('DAILY_QUESTS', 'Daily Quests', 'Quests', 1, 'Daily quest system with rewards'),
('FORTRESS_WARS', 'Fortress Wars', 'Territory', 1, 'Enhanced fortress war system'),
('ALCHEMY_ENHANCED', 'Enhanced Alchemy', 'Enhancement', 1, 'Improved alchemy success rates');
GO

-- Insert default server configuration
INSERT INTO SRN_ServerConfig (ConfigKey, ConfigValue, ConfigType, Description) VALUES
('MAX_LEVEL', '140', 'Integer', 'Maximum character level'),
('EXP_RATE', '10', 'Integer', 'Experience multiplier'),
('GOLD_RATE', '5', 'Integer', 'Gold drop multiplier'),
('DROP_RATE', '3', 'Integer', 'Item drop multiplier'),
('MAX_PLUS_LEVEL', '20', 'Integer', 'Maximum plus enhancement level'),
('FORTRESS_WAR_TIME', '20:00', 'Time', 'Daily fortress war start time'),
('MAINTENANCE_MODE', 'false', 'Boolean', 'Server maintenance mode'),
('WELCOME_MESSAGE', 'Welcome to SroNexus Server!', 'String', 'Server welcome message');
GO

-- Create indexes for performance
USE SroNexus_GameServer;
GO

CREATE INDEX IX_Items_ItemType ON SRN_Items(ItemType);
CREATE INDEX IX_Items_Level ON SRN_Items(LevelRequired);
CREATE INDEX IX_Items_Active ON SRN_Items(IsActive);
CREATE INDEX IX_Monsters_Level ON SRN_Monsters(Level);
CREATE INDEX IX_Monsters_Type ON SRN_Monsters(MonsterType);
CREATE INDEX IX_MonsterDrops_Monster ON SRN_MonsterDrops(MonsterID);
CREATE INDEX IX_MonsterDrops_Item ON SRN_MonsterDrops(ItemID);
GO

USE SroNexus_MasterServer;
GO

CREATE INDEX IX_Accounts_Username ON Accounts(Username);
CREATE INDEX IX_Accounts_Email ON Accounts(Email);
CREATE INDEX IX_Characters_Account ON Characters(AccountID);
CREATE INDEX IX_Characters_Name ON Characters(CharacterName);
CREATE INDEX IX_Characters_Guild ON Characters(GuildID);
GO

PRINT 'SroNexus SQL Server database setup completed successfully!';
GO

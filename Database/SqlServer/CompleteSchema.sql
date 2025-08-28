-- Complete SQL Server Schema for SroNexus
-- This file contains all tables needed for a fully functional server

USE [SRO_VT_SHARD]
GO

-- =============================================
-- ITEM AND EQUIPMENT TABLES
-- =============================================

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='items' AND xtype='U')
BEGIN
    CREATE TABLE items (
        id INT IDENTITY(1,1) PRIMARY KEY,
        code_name NVARCHAR(100) UNIQUE NOT NULL,
        type INT NOT NULL,
        degree INT DEFAULT 1,
        max_stack INT DEFAULT 1,
        req_level INT DEFAULT 1,
        req_str INT DEFAULT 0,
        req_int INT DEFAULT 0,
        physical_min INT DEFAULT 0,
        physical_max INT DEFAULT 0,
        magical_min INT DEFAULT 0,
        magical_max INT DEFAULT 0,
        physical_defense INT DEFAULT 0,
        magical_defense INT DEFAULT 0,
        hit_rate INT DEFAULT 0,
        parry_rate INT DEFAULT 0,
        critical INT DEFAULT 0,
        block_rate INT DEFAULT 0,
        durability INT DEFAULT 100,
        price BIGINT DEFAULT 0,
        sell_price BIGINT DEFAULT 0,
        sox_rate FLOAT DEFAULT 0,
        created_at DATETIME DEFAULT GETDATE()
    )
END
GO

-- =============================================
-- SKILL TABLES
-- =============================================

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='skills' AND xtype='U')
BEGIN
    CREATE TABLE skills (
        id INT IDENTITY(1,1) PRIMARY KEY,
        code_name NVARCHAR(100) UNIQUE NOT NULL,
        mastery_id INT NOT NULL,
        level_required INT DEFAULT 1,
        skill_point_required INT DEFAULT 1,
        weapon_required INT DEFAULT 0,
        mana_cost INT DEFAULT 0,
        stamina_cost INT DEFAULT 0,
        cast_time FLOAT DEFAULT 0,
        cooldown FLOAT DEFAULT 0,
        range FLOAT DEFAULT 0,
        target_type INT DEFAULT 0, -- 0=self, 1=target, 2=area
        damage_type INT DEFAULT 0, -- 0=physical, 1=magical
        damage_min INT DEFAULT 0,
        damage_max INT DEFAULT 0,
        description NVARCHAR(MAX)
    )
END
GO

-- =============================================
-- NPC AND MOB TABLES
-- =============================================

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='npcs' AND xtype='U')
BEGIN
    CREATE TABLE npcs (
        id INT IDENTITY(1,1) PRIMARY KEY,
        code_name NVARCHAR(100) UNIQUE NOT NULL,
        name NVARCHAR(100) NOT NULL,
        type INT NOT NULL, -- 0=merchant, 1=quest, 2=guard, 3=mob
        level INT DEFAULT 1,
        hp INT DEFAULT 100,
        mp INT DEFAULT 100,
        physical_defense INT DEFAULT 0,
        magical_defense INT DEFAULT 0,
        physical_attack_min INT DEFAULT 0,
        physical_attack_max INT DEFAULT 0,
        magical_attack_min INT DEFAULT 0,
        magical_attack_max INT DEFAULT 0,
        movement_speed FLOAT DEFAULT 1.0,
        attack_speed FLOAT DEFAULT 1.0,
        sight_range FLOAT DEFAULT 10.0,
        attack_range FLOAT DEFAULT 2.0,
        experience INT DEFAULT 0,
        skill_points INT DEFAULT 0,
        respawn_time INT DEFAULT 60
    )
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='spawn_points' AND xtype='U')
BEGIN
    CREATE TABLE spawn_points (
        id INT IDENTITY(1,1) PRIMARY KEY,
        npc_id INT NOT NULL,
        x FLOAT NOT NULL,
        y FLOAT NOT NULL,
        z FLOAT NOT NULL,
        region INT NOT NULL,
        radius FLOAT DEFAULT 5.0,
        max_count INT DEFAULT 1,
        respawn_time INT DEFAULT 60,
        is_active BIT DEFAULT 1,
        FOREIGN KEY (npc_id) REFERENCES npcs(id)
    )
END
GO

-- =============================================
-- QUEST TABLES
-- =============================================

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='quests' AND xtype='U')
BEGIN
    CREATE TABLE quests (
        id INT IDENTITY(1,1) PRIMARY KEY,
        code_name NVARCHAR(100) UNIQUE NOT NULL,
        name NVARCHAR(200) NOT NULL,
        description NVARCHAR(MAX),
        type INT DEFAULT 0, -- 0=main, 1=side, 2=daily, 3=event
        min_level INT DEFAULT 1,
        max_level INT DEFAULT 999,
        prerequisite_quest_id INT,
        reward_experience BIGINT DEFAULT 0,
        reward_gold BIGINT DEFAULT 0,
        reward_skill_points INT DEFAULT 0,
        reward_item_id INT,
        reward_item_quantity INT DEFAULT 1,
        is_repeatable BIT DEFAULT 0,
        cooldown_hours INT DEFAULT 0
    )
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='quest_objectives' AND xtype='U')
BEGIN
    CREATE TABLE quest_objectives (
        id INT IDENTITY(1,1) PRIMARY KEY,
        quest_id INT NOT NULL,
        objective_type INT NOT NULL, -- 0=kill, 1=collect, 2=talk, 3=escort
        target_id INT,
        target_quantity INT DEFAULT 1,
        description NVARCHAR(500),
        FOREIGN KEY (quest_id) REFERENCES quests(id)
    )
END
GO

-- =============================================
-- MARKET AND ECONOMY TABLES
-- =============================================

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='market_items' AND xtype='U')
BEGIN
    CREATE TABLE market_items (
        id INT IDENTITY(1,1) PRIMARY KEY,
        seller_id INT NOT NULL,
        item_id INT NOT NULL,
        quantity INT DEFAULT 1,
        price BIGINT NOT NULL,
        enchant_level INT DEFAULT 0,
        variance BIGINT DEFAULT 0,
        listed_at DATETIME DEFAULT GETDATE(),
        expires_at DATETIME,
        status INT DEFAULT 0, -- 0=active, 1=sold, 2=expired, 3=cancelled
        buyer_id INT,
        sold_at DATETIME,
        FOREIGN KEY (seller_id) REFERENCES characters(id),
        FOREIGN KEY (item_id) REFERENCES items(id)
    )
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='stalls' AND xtype='U')
BEGIN
    CREATE TABLE stalls (
        id INT IDENTITY(1,1) PRIMARY KEY,
        owner_id INT NOT NULL,
        title NVARCHAR(100),
        x FLOAT NOT NULL,
        y FLOAT NOT NULL,
        z FLOAT NOT NULL,
        region INT NOT NULL,
        opened_at DATETIME DEFAULT GETDATE(),
        is_active BIT DEFAULT 1,
        FOREIGN KEY (owner_id) REFERENCES characters(id)
    )
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='stall_items' AND xtype='U')
BEGIN
    CREATE TABLE stall_items (
        id INT IDENTITY(1,1) PRIMARY KEY,
        stall_id INT NOT NULL,
        item_id INT NOT NULL,
        quantity INT DEFAULT 1,
        price BIGINT NOT NULL,
        FOREIGN KEY (stall_id) REFERENCES stalls(id),
        FOREIGN KEY (item_id) REFERENCES character_items(id)
    )
END
GO

-- =============================================
-- FORTRESS AND TERRITORY TABLES
-- =============================================

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='fortresses' AND xtype='U')
BEGIN
    CREATE TABLE fortresses (
        id INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(100) NOT NULL,
        region INT NOT NULL,
        owner_guild_id INT,
        tax_rate INT DEFAULT 0,
        defense_level INT DEFAULT 1,
        last_war_time DATETIME,
        next_war_time DATETIME,
        created_at DATETIME DEFAULT GETDATE(),
        FOREIGN KEY (owner_guild_id) REFERENCES guilds(id)
    )
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='fortress_structures' AND xtype='U')
BEGIN
    CREATE TABLE fortress_structures (
        id INT IDENTITY(1,1) PRIMARY KEY,
        fortress_id INT NOT NULL,
        structure_type INT NOT NULL, -- 0=gate, 1=tower, 2=wall, 3=command_post
        hp INT NOT NULL,
        max_hp INT NOT NULL,
        x FLOAT NOT NULL,
        y FLOAT NOT NULL,
        z FLOAT NOT NULL,
        is_destroyed BIT DEFAULT 0,
        FOREIGN KEY (fortress_id) REFERENCES fortresses(id)
    )
END
GO

-- =============================================
-- EVENT AND TOURNAMENT TABLES
-- =============================================

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='events' AND xtype='U')
BEGIN
    CREATE TABLE events (
        id INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(200) NOT NULL,
        type INT NOT NULL, -- 0=pvp, 1=pve, 2=race, 3=collection
        start_time DATETIME NOT NULL,
        end_time DATETIME NOT NULL,
        min_level INT DEFAULT 1,
        max_level INT DEFAULT 999,
        max_participants INT DEFAULT 100,
        reward_item_id INT,
        reward_gold BIGINT DEFAULT 0,
        is_active BIT DEFAULT 1
    )
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='event_participants' AND xtype='U')
BEGIN
    CREATE TABLE event_participants (
        id INT IDENTITY(1,1) PRIMARY KEY,
        event_id INT NOT NULL,
        character_id INT NOT NULL,
        score INT DEFAULT 0,
        rank INT,
        joined_at DATETIME DEFAULT GETDATE(),
        FOREIGN KEY (event_id) REFERENCES events(id),
        FOREIGN KEY (character_id) REFERENCES characters(id),
        UNIQUE(event_id, character_id)
    )
END
GO

-- =============================================
-- LOGGING AND ANALYTICS TABLES
-- =============================================

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='login_history' AND xtype='U')
BEGIN
    CREATE TABLE login_history (
        id INT IDENTITY(1,1) PRIMARY KEY,
        account_id INT NOT NULL,
        character_id INT,
        ip_address NVARCHAR(50),
        login_time DATETIME DEFAULT GETDATE(),
        logout_time DATETIME,
        session_duration INT, -- in seconds
        FOREIGN KEY (account_id) REFERENCES accounts(id)
    )
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='trade_logs' AND xtype='U')
BEGIN
    CREATE TABLE trade_logs (
        id INT IDENTITY(1,1) PRIMARY KEY,
        from_character_id INT NOT NULL,
        to_character_id INT NOT NULL,
        item_id INT,
        quantity INT DEFAULT 1,
        gold_amount BIGINT DEFAULT 0,
        trade_time DATETIME DEFAULT GETDATE(),
        FOREIGN KEY (from_character_id) REFERENCES characters(id),
        FOREIGN KEY (to_character_id) REFERENCES characters(id)
    )
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='chat_logs' AND xtype='U')
BEGIN
    CREATE TABLE chat_logs (
        id INT IDENTITY(1,1) PRIMARY KEY,
        character_id INT NOT NULL,
        chat_type INT NOT NULL, -- 0=all, 1=party, 2=guild, 3=whisper, 4=trade
        message NVARCHAR(500),
        target_character_id INT,
        sent_at DATETIME DEFAULT GETDATE(),
        FOREIGN KEY (character_id) REFERENCES characters(id)
    )
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='anti_cheat_logs' AND xtype='U')
BEGIN
    CREATE TABLE anti_cheat_logs (
        id INT IDENTITY(1,1) PRIMARY KEY,
        character_id INT NOT NULL,
        violation_type INT NOT NULL, -- 0=speed, 1=teleport, 2=damage, 3=packet
        details NVARCHAR(MAX),
        detected_at DATETIME DEFAULT GETDATE(),
        action_taken INT DEFAULT 0, -- 0=logged, 1=warned, 2=kicked, 3=banned
        FOREIGN KEY (character_id) REFERENCES characters(id)
    )
END
GO

-- =============================================
-- INDEXES FOR PERFORMANCE
-- =============================================

CREATE INDEX idx_character_items_character ON character_items(character_id);
CREATE INDEX idx_spawn_points_region ON spawn_points(region);
CREATE INDEX idx_market_items_status ON market_items(status);
CREATE INDEX idx_market_items_seller ON market_items(seller_id);
CREATE INDEX idx_stall_items_stall ON stall_items(stall_id);
CREATE INDEX idx_fortress_owner ON fortresses(owner_guild_id);
CREATE INDEX idx_event_participants_event ON event_participants(event_id);
CREATE INDEX idx_login_history_account ON login_history(account_id);
CREATE INDEX idx_trade_logs_from ON trade_logs(from_character_id);
CREATE INDEX idx_trade_logs_to ON trade_logs(to_character_id);
CREATE INDEX idx_chat_logs_character ON chat_logs(character_id);
CREATE INDEX idx_anti_cheat_character ON anti_cheat_logs(character_id);
GO

-- =============================================
-- STORED PROCEDURES
-- =============================================

CREATE OR ALTER PROCEDURE sp_CreateCharacter
    @AccountId INT,
    @Name NVARCHAR(50),
    @Model INT,
    @Level INT = 1,
    @Gold BIGINT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check if name already exists
    IF EXISTS (SELECT 1 FROM characters WHERE name = @Name)
    BEGIN
        RETURN -1; -- Name already taken
    END
    
    -- Insert character
    INSERT INTO characters (account_id, name, model, level, gold)
    VALUES (@AccountId, @Name, @Model, @Level, @Gold);
    
    RETURN SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE sp_GetCharacterList
    @AccountId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT id, name, model, level, gold, position_x, position_y, position_z
    FROM characters
    WHERE account_id = @AccountId AND deleted = 0
    ORDER BY created_at;
END
GO

CREATE OR ALTER PROCEDURE sp_SaveCharacterPosition
    @CharacterId INT,
    @X FLOAT,
    @Y FLOAT,
    @Z FLOAT,
    @Region INT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE characters
    SET position_x = @X, position_y = @Y, position_z = @Z, region = @Region,
        updated_at = GETDATE()
    WHERE id = @CharacterId;
END
GO

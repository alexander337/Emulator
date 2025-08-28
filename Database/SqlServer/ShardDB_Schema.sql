-- SQL Server schema for Shard Database
USE [SRO_VT_SHARD]
GO

-- Characters table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='characters' AND xtype='U')
BEGIN
    CREATE TABLE characters (
        id INT IDENTITY(1,1) PRIMARY KEY,
        account_id INT NOT NULL,
        name NVARCHAR(50) UNIQUE NOT NULL,
        model INT DEFAULT 0,
        volume INT DEFAULT 0,
        level INT DEFAULT 1,
        experience BIGINT DEFAULT 0,
        strength INT DEFAULT 20,
        intellect INT DEFAULT 20,
        stat_points INT DEFAULT 0,
        skill_points INT DEFAULT 0,
        hp INT DEFAULT 100,
        mp INT DEFAULT 100,
        gold BIGINT DEFAULT 0,
        gold_stored BIGINT DEFAULT 0,
        position_x FLOAT DEFAULT 0,
        position_y FLOAT DEFAULT 0,
        position_z FLOAT DEFAULT 0,
        region INT DEFAULT 0,
        return_point INT DEFAULT 0,
        pk_level INT DEFAULT 0,
        pk_points INT DEFAULT 0,
        created_at DATETIME DEFAULT GETDATE(),
        updated_at DATETIME DEFAULT GETDATE(),
        deleted BIT DEFAULT 0
    )
END
GO

-- Character items table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='character_items' AND xtype='U')
BEGIN
    CREATE TABLE character_items (
        id INT IDENTITY(1,1) PRIMARY KEY,
        character_id INT NOT NULL,
        item_id INT NOT NULL,
        position INT NOT NULL,
        quantity INT DEFAULT 1,
        durability INT,
        enchant_level INT DEFAULT 0,
        variance BIGINT DEFAULT 0,
        data VARBINARY(MAX),
        created_at DATETIME DEFAULT GETDATE(),
        FOREIGN KEY (character_id) REFERENCES characters(id)
    )
END
GO

-- Character skills table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='character_skills' AND xtype='U')
BEGIN
    CREATE TABLE character_skills (
        id INT IDENTITY(1,1) PRIMARY KEY,
        character_id INT NOT NULL,
        skill_id INT NOT NULL,
        level INT DEFAULT 1,
        FOREIGN KEY (character_id) REFERENCES characters(id),
        UNIQUE(character_id, skill_id)
    )
END
GO

-- Character masteries table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='character_masteries' AND xtype='U')
BEGIN
    CREATE TABLE character_masteries (
        id INT IDENTITY(1,1) PRIMARY KEY,
        character_id INT NOT NULL,
        mastery_id INT NOT NULL,
        level INT DEFAULT 0,
        FOREIGN KEY (character_id) REFERENCES characters(id),
        UNIQUE(character_id, mastery_id)
    )
END
GO

-- Character quests table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='character_quests' AND xtype='U')
BEGIN
    CREATE TABLE character_quests (
        id INT IDENTITY(1,1) PRIMARY KEY,
        character_id INT NOT NULL,
        quest_id INT NOT NULL,
        status INT DEFAULT 0, -- 0=active, 1=completed
        progress NVARCHAR(MAX),
        completed_at DATETIME,
        FOREIGN KEY (character_id) REFERENCES characters(id)
    )
END
GO

-- Guild table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='guilds' AND xtype='U')
BEGIN
    CREATE TABLE guilds (
        id INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(50) UNIQUE NOT NULL,
        level INT DEFAULT 1,
        points INT DEFAULT 0,
        master_id INT NOT NULL,
        notice NVARCHAR(MAX),
        created_at DATETIME DEFAULT GETDATE(),
        FOREIGN KEY (master_id) REFERENCES characters(id)
    )
END
GO

-- Guild members table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='guild_members' AND xtype='U')
BEGIN
    CREATE TABLE guild_members (
        id INT IDENTITY(1,1) PRIMARY KEY,
        guild_id INT NOT NULL,
        character_id INT NOT NULL,
        rank INT DEFAULT 0,
        contribution INT DEFAULT 0,
        joined_at DATETIME DEFAULT GETDATE(),
        FOREIGN KEY (guild_id) REFERENCES guilds(id),
        FOREIGN KEY (character_id) REFERENCES characters(id),
        UNIQUE(character_id)
    )
END
GO

-- Friends table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='friends' AND xtype='U')
BEGIN
    CREATE TABLE friends (
        id INT IDENTITY(1,1) PRIMARY KEY,
        character_id INT NOT NULL,
        friend_id INT NOT NULL,
        group_id INT DEFAULT 0,
        created_at DATETIME DEFAULT GETDATE(),
        FOREIGN KEY (character_id) REFERENCES characters(id),
        FOREIGN KEY (friend_id) REFERENCES characters(id),
        UNIQUE(character_id, friend_id)
    )
END
GO

-- Block list table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='character_blocks' AND xtype='U')
BEGIN
    CREATE TABLE character_blocks (
        id INT IDENTITY(1,1) PRIMARY KEY,
        character_id INT NOT NULL,
        blocked_id INT NOT NULL,
        created_at DATETIME DEFAULT GETDATE(),
        FOREIGN KEY (character_id) REFERENCES characters(id),
        FOREIGN KEY (blocked_id) REFERENCES characters(id),
        UNIQUE(character_id, blocked_id)
    )
END
GO

-- Create indexes
CREATE INDEX idx_characters_account ON characters(account_id);
CREATE INDEX idx_characters_name ON characters(name);
CREATE INDEX idx_character_items_char ON character_items(character_id);
CREATE INDEX idx_character_skills_char ON character_skills(character_id);
CREATE INDEX idx_guild_members_guild ON guild_members(guild_id);
CREATE INDEX idx_guild_members_char ON guild_members(character_id);
CREATE INDEX idx_friends_char ON friends(character_id);
GO

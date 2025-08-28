-- SQL Server schema for Account Database
USE [SRO_VT_ACCOUNT]
GO

-- Accounts table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='accounts' AND xtype='U')
BEGIN
    CREATE TABLE accounts (
        id INT IDENTITY(1,1) PRIMARY KEY,
        username NVARCHAR(50) UNIQUE NOT NULL,
        password NVARCHAR(100) NOT NULL,
        email NVARCHAR(100),
        last_login_ip NVARCHAR(50),
        last_login_time DATETIME,
        is_logged BIT DEFAULT 0,
        access_level INT DEFAULT 0,
        silk_own INT DEFAULT 0,
        silk_gift INT DEFAULT 0,
        silk_point INT DEFAULT 0,
        created_at DATETIME DEFAULT GETDATE(),
        updated_at DATETIME DEFAULT GETDATE()
    )
END
GO

-- Activation codes table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='activation' AND xtype='U')
BEGIN
    CREATE TABLE activation (
        id INT IDENTITY(1,1) PRIMARY KEY,
        code NVARCHAR(50) UNIQUE NOT NULL,
        account_id INT,
        used BIT DEFAULT 0,
        used_date DATETIME,
        created_at DATETIME DEFAULT GETDATE(),
        FOREIGN KEY (account_id) REFERENCES accounts(id)
    )
END
GO

-- Name filter table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='name_filter' AND xtype='U')
BEGIN
    CREATE TABLE name_filter (
        id INT IDENTITY(1,1) PRIMARY KEY,
        pattern NVARCHAR(100) NOT NULL,
        type NVARCHAR(20) NOT NULL, -- 'banned', 'reserved'
        created_at DATETIME DEFAULT GETDATE()
    )
END
GO

-- News table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='news' AND xtype='U')
BEGIN
    CREATE TABLE news (
        id INT IDENTITY(1,1) PRIMARY KEY,
        title NVARCHAR(200) NOT NULL,
        content NVARCHAR(MAX),
        author NVARCHAR(50),
        category NVARCHAR(50),
        is_active BIT DEFAULT 1,
        created_at DATETIME DEFAULT GETDATE(),
        updated_at DATETIME DEFAULT GETDATE()
    )
END
GO

-- Create indexes
CREATE INDEX idx_accounts_username ON accounts(username);
CREATE INDEX idx_accounts_email ON accounts(email);
CREATE INDEX idx_activation_code ON activation(code);
GO

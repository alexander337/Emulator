-- Initial data for SQL Server
USE [SRO_VT_ACCOUNT]
GO

-- Insert default admin account (password: admin)
IF NOT EXISTS (SELECT * FROM accounts WHERE username = 'admin')
BEGIN
    INSERT INTO accounts (username, password, email, access_level)
    VALUES ('admin', 'admin', 'admin@sronexus.com', 100)
END
GO

-- Insert test accounts
IF NOT EXISTS (SELECT * FROM accounts WHERE username = 'test1')
BEGIN
    INSERT INTO accounts (username, password, email)
    VALUES ('test1', 'test1', 'test1@sronexus.com')
END
GO

IF NOT EXISTS (SELECT * FROM accounts WHERE username = 'test2')
BEGIN
    INSERT INTO accounts (username, password, email)
    VALUES ('test2', 'test2', 'test2@sronexus.com')
END
GO

-- Insert default news
IF NOT EXISTS (SELECT * FROM news WHERE title = 'Welcome to SroNexus')
BEGIN
    INSERT INTO news (title, content, author, category)
    VALUES (
        'Welcome to SroNexus',
        'Welcome to the unified SroNexus server! This is a Windows-optimized monolith combining Login and Game servers.',
        'System',
        'Announcement'
    )
END
GO

USE [SRO_VT_SHARD]
GO

-- No initial shard data needed - characters will be created by players

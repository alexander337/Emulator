# 📊 SroNexus SQL Server Database Setup Guide

## Prerequisites

- **SQL Server 2019** or later (Express edition is fine for development)
- **SQL Server Management Studio (SSMS)** 18.0 or later
- **Windows Authentication** or **SQL Server Authentication** enabled
- At least **2GB free space** for databases

## Installation Steps

### 1. Install SQL Server

If you don't have SQL Server installed:

1. Download [SQL Server 2022 Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
2. Run the installer and choose "Basic" installation
3. Accept the license terms
4. Choose installation location
5. Wait for installation to complete

### 2. Install SQL Server Management Studio (SSMS)

1. Download [SSMS](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms)
2. Run the installer
3. Follow the installation wizard
4. Restart your computer if prompted

### 3. Configure SQL Server

1. Open **SQL Server Configuration Manager**
2. Enable **SQL Server Browser** service
3. Enable **TCP/IP** protocol:
   - SQL Server Network Configuration → Protocols → TCP/IP → Enable
   - Right-click TCP/IP → Properties → IP Addresses
   - Set TCP Port to 1433 for all IPs
4. Restart SQL Server service

### 4. Setup Mixed Mode Authentication

1. Open SSMS
2. Connect to your server
3. Right-click server → Properties → Security
4. Select **SQL Server and Windows Authentication mode**
5. Click OK and restart SQL Server

### 5. Create Database Login

Run this in SSMS:

```sql
-- Create a login for SroNexus
CREATE LOGIN SroNexusUser 
WITH PASSWORD = 'YourStrongPassword123!',
DEFAULT_DATABASE = master,
CHECK_EXPIRATION = OFF,
CHECK_POLICY = OFF;
GO

-- Grant necessary permissions
ALTER SERVER ROLE sysadmin ADD MEMBER SroNexusUser;
GO
```

### 6. Run Database Setup Script

1. Open SSMS
2. Connect to your SQL Server
3. Open `SroNexus/Database/SQL_Server_Setup.sql`
4. Execute the script (F5)
5. Verify all databases were created:
   - SroNexus
   - SroNexus_GameServer
   - SroNexus_MasterServer

### 7. Import eSRO Tables (if needed)

If you have existing eSRO database:

```sql
-- Import existing tables
USE SroNexus_GameServer;
GO

-- Import from existing eSRO database
-- Adjust paths and database names as needed
INSERT INTO SRN_Items (ItemCode, ItemName, DisplayName, ItemType, LevelRequired)
SELECT 
    CodeName128,
    Name,
    Name,
    CASE 
        WHEN TypeID1 = 3 THEN 'Weapon'
        WHEN TypeID1 = 4 THEN 'Armor'
        ELSE 'Other'
    END,
    ReqLevel1
FROM [eSRO_Database].[dbo].[RefObjCommon]
WHERE Service = 1;
GO
```

## Connection Configuration

### Update appsettings.json

Edit `SroNexus/SroNexusApp/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SroNexus;User Id=SroNexusUser;Password=YourStrongPassword123!;TrustServerCertificate=True;"
  }
}
```

### Connection String Options

#### Windows Authentication:
```
Server=localhost;Database=SroNexus;Integrated Security=True;TrustServerCertificate=True;
```

#### SQL Server Authentication:
```
Server=localhost;Database=SroNexus;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

#### Remote Server:
```
Server=192.168.1.100,1433;Database=SroNexus;User Id=SroNexusUser;Password=YourPassword;TrustServerCertificate=True;
```

#### Named Instance:
```
Server=localhost\SQLEXPRESS;Database=SroNexus;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

## Database Structure

### Main Databases

1. **SroNexus** - Configuration and features
   - SRN_Features - Feature toggles
   - SRN_ServerConfig - Server settings

2. **SroNexus_GameServer** - Game data
   - SRN_Items - Item definitions
   - SRN_Monsters - Monster data
   - SRN_Skills - Skill information
   - SRN_ItemAlchemy - Alchemy options
   - SRN_ItemPlusSystem - Enhancement settings

3. **SroNexus_MasterServer** - Account data
   - Accounts - User accounts
   - Characters - Player characters

## Maintenance

### Backup Databases

```sql
-- Backup all databases
BACKUP DATABASE SroNexus 
TO DISK = 'C:\Backups\SroNexus.bak'
WITH FORMAT, INIT;

BACKUP DATABASE SroNexus_GameServer 
TO DISK = 'C:\Backups\SroNexus_GameServer.bak'
WITH FORMAT, INIT;

BACKUP DATABASE SroNexus_MasterServer 
TO DISK = 'C:\Backups\SroNexus_MasterServer.bak'
WITH FORMAT, INIT;
```

### Restore Databases

```sql
-- Restore from backup
RESTORE DATABASE SroNexus 
FROM DISK = 'C:\Backups\SroNexus.bak'
WITH REPLACE;
```

### Performance Optimization

```sql
-- Update statistics
USE SroNexus_GameServer;
EXEC sp_updatestats;
GO

-- Rebuild indexes
ALTER INDEX ALL ON SRN_Items REBUILD;
ALTER INDEX ALL ON SRN_Monsters REBUILD;
GO

-- Check database integrity
DBCC CHECKDB('SroNexus_GameServer');
GO
```

## Troubleshooting

### Cannot Connect to SQL Server

1. Check SQL Server service is running
2. Verify TCP/IP is enabled
3. Check firewall allows port 1433
4. Test with `telnet localhost 1433`

### Login Failed

1. Verify username and password
2. Check authentication mode (Windows/SQL)
3. Ensure user has permissions
4. Check default database exists

### Database Already Exists

Drop existing databases first:
```sql
DROP DATABASE IF EXISTS SroNexus;
DROP DATABASE IF EXISTS SroNexus_GameServer;
DROP DATABASE IF EXISTS SroNexus_MasterServer;
```

### Performance Issues

1. Check indexes are created
2. Update statistics regularly
3. Monitor query execution plans
4. Consider increasing memory allocation

## Security Best Practices

1. **Use strong passwords** for SQL accounts
2. **Limit permissions** - don't use 'sa' in production
3. **Enable SSL/TLS** for connections
4. **Regular backups** - automate daily backups
5. **Monitor access** - enable SQL Server audit
6. **Keep updated** - apply SQL Server updates

## Support

For database issues:
1. Check SQL Server error logs
2. Review Windows Event Viewer
3. Enable SQL Profiler for debugging
4. Post in GitHub issues with error details

---

**Your SQL Server database is now ready for SroNexus!** 🚀

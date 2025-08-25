# 🔨 SroNexus Build Instructions

## Prerequisites

### Required Software
- **Visual Studio 2022** (Community Edition or higher)
  - Workloads needed:
    - .NET Desktop Development
    - Desktop Development with C++
    - Windows SDK
- **.NET 6.0 SDK** or later
- **SQL Server 2019** or **MySQL 8.0**
- **Git** (for cloning repository)

### System Requirements
- Windows 10/11 (64-bit)
- 8GB RAM minimum (16GB recommended)
- 10GB free disk space
- Administrator privileges (for server operations)

## Step-by-Step Build Process

### 1. Clone the Repository
```bash
git clone https://github.com/Alexander0305/eSRO.git
cd eSRO/SroNexus
```

### 2. Setup Database

#### For SQL Server:
```sql
-- Open SQL Server Management Studio
-- Run these scripts in order:
1. Database/create_databases.sql
2. Database/GameServer/create_tables.sql
3. Database/GameServer/ContentData/*.sql
4. Database/GameServer/WorldData/*.sql
5. Database/MasterServer/*.sql
```

#### For MySQL:
```bash
mysql -u root -p < Database/create_databases.sql
mysql -u root -p sronexus < Database/GameServer/create_tables.sql
# Import all other SQL files
```

### 3. Build eSRO Server Components

#### Build AgentServer:
```bash
cd AgentServer
# If using Qt Creator:
qmake AgentServer.pro
make

# Or using CMake:
mkdir build && cd build
cmake ..
make
```

#### Build GatewayServer:
```bash
cd GatewayServer
qmake GatewayServer.pro
make
```

#### Build MasterServer:
```bash
cd MasterServer
qmake MasterServer.pro
make
```

### 4. Build SroNexus Management System

1. **Open Visual Studio 2022**
2. **Open Solution**: `File → Open → Project/Solution`
3. **Select**: `SroNexus/SroNexus.sln`
4. **Restore NuGet Packages**: Right-click solution → Restore NuGet Packages
5. **Set Configuration**: 
   - Configuration: `Release`
   - Platform: `x64`
6. **Build Solution**: `Build → Build Solution` (Ctrl+Shift+B)

### 5. Configure the Server

Edit `SroNexus/SroNexusApp/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SroNexus;User Id=sa;Password=YourPassword;"
  },
  "ServerSettings": {
    "PublicIP": "YOUR_PUBLIC_IP",
    "AutoStart": false,
    "MaxPlayers": 1000
  }
}
```

### 6. Configure Server Files

#### AgentServer Configuration:
Edit `AgentServer/serv.conf`:
```ini
[DATABASE]
Host=localhost
Database=SroNexus
User=sa
Password=YourPassword

[NETWORK]
Port=15780
MaxConnections=1000
```

#### GatewayServer Configuration:
Edit `GatewayServer/serv.conf`:
```ini
[DATABASE]
Host=localhost
Database=SroNexus
User=sa
Password=YourPassword

[NETWORK]
Port=15779
```

#### MasterServer Configuration:
Edit `MasterServer/serv.conf`:
```ini
[DATABASE]
Host=localhost
Database=SroNexus
User=sa
Password=YourPassword

[NETWORK]
Port=15781
```

## Running the Server

### Method 1: Using SroNexus Manager (Recommended)

1. **Run SroNexus.exe** from `SroNexus/SroNexusApp/bin/Release/net6.0-windows/`
2. Click **"Start All Servers"** button
3. Monitor the dashboard for server status
4. Check logs for any errors

### Method 2: Manual Start

1. Start MasterServer:
```bash
cd MasterServer
./MasterServer.exe
```

2. Start GatewayServer:
```bash
cd GatewayServer
./GatewayServer.exe
```

3. Start AgentServer:
```bash
cd AgentServer
./AgentServer.exe
```

4. Run SroNexus Manager:
```bash
cd SroNexus/SroNexusApp/bin/Release/net6.0-windows/
./SroNexus.exe
```

## Client Configuration

### Modify Client to Connect to Your Server

1. **Find your Silkroad Online client**
2. **Edit the client** to point to your server:
   - Use a hex editor or client patcher
   - Change the IP address to your server's IP
   - Default port: 15779

### Create a Launcher (Optional)

Create `launcher.bat`:
```batch
@echo off
set SERVER_IP=YOUR_SERVER_IP
set SERVER_PORT=15779
start sro_client.exe %SERVER_IP% %SERVER_PORT%
```

## Troubleshooting

### Common Issues

#### 1. Database Connection Failed
- Check SQL Server/MySQL is running
- Verify connection string in appsettings.json
- Ensure database user has proper permissions

#### 2. Servers Won't Start
- Check if ports are already in use
- Run as Administrator
- Check Windows Firewall settings
- Verify all DLL dependencies are present

#### 3. Build Errors
- Ensure all NuGet packages are restored
- Check .NET 6.0 SDK is installed
- Verify Visual C++ Redistributables are installed

#### 4. Client Can't Connect
- Check server IP in client configuration
- Ensure ports are open in firewall
- Verify server is actually running
- Check for antivirus blocking connections

### Log Files

Check these locations for debugging:
- `SroNexus/logs/` - Application logs
- `AgentServer/logs/` - Game server logs
- `GatewayServer/logs/` - Gateway logs
- `MasterServer/logs/` - Master server logs

## Testing the Server

### 1. Create Test Account
```sql
INSERT INTO accounts (username, password, email) 
VALUES ('test', 'hashedpassword', 'test@example.com');
```

### 2. Connect with Client
1. Start your modified client
2. Login with test account
3. Create a character
4. Enter the game world

### 3. Verify Features
- Check if all 500+ features are working
- Test item creation/editing
- Verify monster spawns
- Test PvP systems
- Check economy features

## Production Deployment

### Security Considerations
1. Change all default passwords
2. Use strong database passwords
3. Enable firewall rules
4. Set up DDoS protection
5. Regular backups
6. Monitor server resources

### Performance Optimization
1. Adjust max player limits based on hardware
2. Configure database connection pooling
3. Enable caching where appropriate
4. Monitor and optimize slow queries
5. Use SSD for database storage

## Support

If you encounter issues:
1. Check the logs first
2. Search existing GitHub issues
3. Create a new issue with:
   - Error messages
   - Log files
   - System specifications
   - Steps to reproduce

---

**Happy hosting with SroNexus!** 🚀

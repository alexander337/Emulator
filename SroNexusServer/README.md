# SroNexusServer

A Windows-only monolithic server combining Login (Gateway+Master) and Game (Agent) functionality into a single executable.

## Features

- **Unified Architecture**: Single process hosting both Login and Game servers
- **JSON Configuration**: Editable ports and database settings via `config/sronexus.json`
- **SQL Server Support**: Native Windows ODBC integration for SQL Server
- **Real-time Networking**: Uses SRNL/EPL framework with Boost.Asio
- **WPF Management UI**: SroNexusApp provides server control and database management

## Requirements

- Windows 10/11 or Windows Server 2019+
- Visual Studio 2022 with C++ development tools
- SQL Server 2019+ (Express edition works)
- ODBC Driver 18 for SQL Server
- Boost 1.82+ (for Boost.Asio and threading)
- .NET 6.0+ SDK (for SroNexusApp)

## Building

### Using CMake (Command Line)
```bash
mkdir build
cd build
cmake .. -G "Visual Studio 17 2022" -A x64
cmake --build . --config Release
```

### Using Visual Studio
1. Open the root CMakeLists.txt in Visual Studio 2022
2. Select x64-Release configuration
3. Build → Build All

## Configuration

Edit `config/sronexus.json`:

```json
{
  "LoginPort": 15779,
  "GamePort": 15780,
  "Database": {
    "Server": "localhost",
    "Database": "SRO_VT_SHARD",
    "Authentication": "SQL",
    "Username": "sa",
    "Password": "yourpassword",
    "TrustServerCertificate": true
  }
}
```

## Running

### Standalone (Development)
```bash
SroNexusServer.exe [config-path]
```

### With Manager (Production)
1. Launch SroNexusApp.exe
2. Go to Database → Connect to SQL Server
3. Configure and test connection
4. Click "Initialize Schema" for first-time setup
5. Go to Server → Start

## Architecture

### Modules

- **LoginModule**: Handles client authentication, version checking, and server selection
  - Inherits from `srv::IServer`
  - Listens on LoginPort (default 15779)
  - Manages Gateway and Master server responsibilities

- **GameModule**: Handles gameplay, world state, and character management
  - Inherits from `srv::IServer`
  - Listens on GamePort (default 15780)
  - Manages Agent server responsibilities

### Database

- **SqlServerPool**: ODBC-based connection pooling for SQL Server
  - Thread-safe connection management
  - Automatic reconnection handling
  - Query execution with result sets

### Threading Model

- Main thread: Configuration and lifecycle management
- Login thread: Runs LoginModule's io_service
- Game thread: Runs GameModule's io_service
- Each connection handled asynchronously within module threads

## Database Schema

### Account Database (SRO_VT_ACCOUNT)
- `accounts`: User accounts and authentication
- `activation`: Activation codes
- `name_filter`: Character name filtering
- `news`: Server announcements

### Shard Database (SRO_VT_SHARD)
- `characters`: Player characters
- `character_items`: Inventory and equipment
- `character_skills`: Learned skills
- `character_masteries`: Mastery progression
- `guilds`: Guild information
- `guild_members`: Guild membership
- `friends`: Friend lists
- `character_blocks`: Block lists

## Development

### Adding New Packet Handlers

1. Add handler to appropriate module (Login or Game)
2. Register opcode in SRNL/EPL headers
3. Implement state machine in connection class

### Extending Configuration

1. Update `sronexus.json` with new fields
2. Modify JSON parser in `main.cpp`
3. Update `DatabaseConnectionPage.xaml.cs` if database-related

## Troubleshooting

### Server won't start
- Check if ports are already in use
- Verify Windows Firewall allows the ports
- Run as Administrator if binding fails

### Database connection fails
- Verify SQL Server is running
- Check SQL Server Configuration Manager for TCP/IP protocol
- Ensure SQL Server Authentication is enabled (if using SQL auth)
- Test with SQL Server Management Studio first

### Build errors
- Ensure all dependencies are installed
- Check CMake can find Boost libraries
- Verify ODBC headers are available

## Migration from Legacy Servers

This monolith replaces:
- GatewayServer → LoginModule
- MasterServer → LoginModule
- AgentServer → GameModule

Key differences:
- No Unix/Linux support (Windows-only)
- No MySQL support (SQL Server only)
- Single process instead of three
- JSON config instead of INI files
- Integrated management UI

## License

See LICENSE file in repository root.

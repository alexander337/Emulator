# SroNexus Emulator

A modern Windows-optimized Silkroad Online server emulator featuring a unified monolithic architecture.

## Overview

SroNexus consolidates the traditional three-server architecture (Gateway, Master, Agent) into a single Windows executable with integrated SQL Server support and a WPF management interface.

## Key Features

- **Monolithic Architecture**: Single process combining Login and Game servers
- **Windows-Native**: Optimized for Windows with no Unix dependencies
- **SQL Server Integration**: Native ODBC support with automatic schema management
- **JSON Configuration**: Modern configuration format with UI management
- **WPF Control Panel**: Full-featured management application
- **Real-time Monitoring**: Live server status and logging

## Quick Start

1. **Prerequisites**
   - Windows 10/11 or Windows Server 2019+
   - SQL Server 2019+ (Express edition works)
   - Visual Studio 2022
   - .NET 6.0+ SDK

2. **Build**
   ```bash
   mkdir build && cd build
   cmake .. -G "Visual Studio 17 2022" -A x64
   cmake --build . --config Release
   ```

3. **Configure Database**
   - Launch `SroNexusApp.exe`
   - Navigate to Database → Connect to SQL Server
   - Enter connection details and click "Initialize Schema"

4. **Start Server**
   - In SroNexusApp, go to Server → Start
   - Or run standalone: `SroNexusServer.exe`

## Project Structure

```
├── SroNexusServer/     # Monolithic server (C++)
│   ├── modules/        # Login and Game modules
│   ├── db/            # SQL Server abstraction
│   └── config/        # JSON configuration
├── SroNexusApp/       # Management UI (WPF/C#)
├── Database/          # SQL schemas
│   └── SqlServer/     # T-SQL scripts
├── SRNL/             # Network library
├── EPL/              # Packet library
└── SOL/              # Game object library
```

## Documentation

- [Server Documentation](SroNexusServer/README.md)
- [Database Schema](Database/SqlServer/)
- [Configuration Guide](SroNexusServer/config/)

## Migration from eSRO

This project evolved from the eSRO emulator, modernizing it for Windows with:
- Removal of Unix/Linux dependencies
- Migration from MySQL to SQL Server
- Consolidation of three servers into one
- Addition of management UI
- JSON-based configuration

## Contributing

Contributions are welcome! Please ensure:
- Code compiles on Windows with Visual Studio 2022
- SQL scripts are SQL Server compatible
- New features include documentation

## License

GNU Affero General Public License v3.0 - See [LICENSE](LICENSE) for details.

## Support

For issues and questions:
- Open an issue on GitHub
- Check existing documentation
- Review troubleshooting guide in server README
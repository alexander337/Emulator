cmake_minimum_required(VERSION 2.6)
project(ESRO)

add_subdirectory(Blowfish)
add_subdirectory(SOL)
if (NOT WIN32)
add_subdirectory(SQL)
endif()
add_subdirectory(khamaileon)
add_subdirectory(EPL)
add_subdirectory(SRNL)

# Build legacy Unix-style servers only on non-Windows
if (NOT WIN32)
    add_subdirectory(MasterServer)
    add_subdirectory(GatewayServer)
    add_subdirectory(AgentServer)
endif()

# Windows monolith
add_subdirectory(SroNexusServer)


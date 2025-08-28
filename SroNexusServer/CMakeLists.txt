cmake_minimum_required(VERSION 2.6)

project(SroNexusServer)

# Windows-only target (no Unix daemonization)
if (WIN32)
    add_definitions(-DWIN32_LEAN_AND_MEAN)
    file(GLOB SRONEXUS_SERVER_SRC
        main.cpp
        modules/*.cpp
        db/*.cpp
    )
    
    include_directories(${ESRO_SRNL_SOURCE_DIR}/include)
    include_directories(${ESRO_EPL_SOURCE_DIR}/include)
    include_directories(${ESRO_SOL_SOURCE_DIR}/include)
    include_directories(${ESRO_BLOWFISH_SOURCE_DIR}/include)
    
    add_executable(SroNexusServer ${SRONEXUS_SERVER_SRC})

    # Link with SRNL/EPL/SOL for module wiring and ODBC for SQL Server
    target_link_libraries(SroNexusServer SRNL EPL SOL Blowfish odbc32 boost_system boost_thread)

    # Install executable and default config
    install(TARGETS SroNexusServer RUNTIME DESTINATION bin)
    install(FILES ${CMAKE_CURRENT_SOURCE_DIR}/config/sronexus.json DESTINATION bin/config)
endif()

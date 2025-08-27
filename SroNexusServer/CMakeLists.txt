cmake_minimum_required(VERSION 2.6)

project(SroNexusServer)

# Windows-only target (no Unix daemonization)
if (WIN32)
    add_definitions(-DWIN32_LEAN_AND_MEAN)
    file(GLOB SRONEXUS_SERVER_SRC
        main.cpp
        modules/*.cpp
    )
    add_executable(SroNexusServer ${SRONEXUS_SERVER_SRC})

    # Initially do not link DB libraries; we will add ODBC in a later PR.
    # Link only C++ runtime for now. We can link SRNL/EPL/SOL once modules start using them.
    # target_link_libraries(SroNexusServer SRNL EPL SOL)

    # Install executable and default config
    install(TARGETS SroNexusServer RUNTIME DESTINATION bin)
    install(FILES ${CMAKE_CURRENT_SOURCE_DIR}/config/sronexus.conf DESTINATION bin/config)
endif()

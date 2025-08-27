cmake_minimum_required(VERSION 2.6)

project(SroNexusServer)

# Windows-only target (no Unix daemonization)
if (WIN32)
    add_definitions(-DWIN32_LEAN_AND_MEAN)
endif()

include_directories(${SRNL_SOURCE_DIR}/include)
include_directories(${EPL_SOURCE_DIR}/include)
include_directories(${SOL_SOURCE_DIR}/include)

file(GLOB SRONEXUS_SERVER_SRC
    main.cpp
    modules/*.cpp
)

add_executable(SroNexusServer ${SRONEXUS_SERVER_SRC})

# Initially do not link DB libraries; we will add ODBC in a later PR.
# Link only C++ runtime for now. We can link SRNL/EPL/SOL once modules start using them.
# target_link_libraries(SroNexusServer SRNL EPL SOL)

# Installation
install(TARGETS SroNexusServer
        RUNTIME DESTINATION bin)

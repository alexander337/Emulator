#include <iostream>
#include <fstream>
#include <string>
#include <map>
#include <algorithm>
#include <cstdlib>
#include <windows.h>

#include "modules/LoginModule.hpp"
#include "modules/GameModule.hpp"

// Minimal key=value config loader for Windows
static bool read_config_file(const std::string& filename, std::map<std::string,std::string>& entries) {
    std::ifstream file(filename.c_str());
    if (!file) {
        std::cerr << "Config file not found: " << filename << std::endl;
        return false;
    }
    std::string line;
    size_t i = 0;
    while (std::getline(file, line)) {
        ++i;
        if (line.empty() || line[0] == '#' || line[0] == ';') continue;
        // strip spaces
        line.erase(std::remove(line.begin(), line.end(), ' '), line.end());
        size_t pos = line.find('=');
        if (pos == std::string::npos) continue;
        std::string key = line.substr(0, pos);
        std::string val = line.substr(pos+1);
        entries[key] = val;
    }
    return true;
}

int main(int argc, char** argv) {
    std::cout << "SroNexusServer (Windows) starting..." << std::endl;

    // Load editable ports from config
    std::string cfgPath;
    if (argc >= 2) {
        cfgPath = argv[1];
    } else {
        // Resolve relative to executable dir: exeDir/config/sronexus.conf
        char exePath[MAX_PATH] = {0};
        if (GetModuleFileNameA(nullptr, exePath, MAX_PATH)) {
            std::string exeDir = exePath;
            auto pos = exeDir.find_last_of("\\/");
            if (pos != std::string::npos) exeDir = exeDir.substr(0, pos);
            cfgPath = exeDir + "/config/sronexus.conf";
        } else {
            cfgPath = "config/sronexus.conf";
        }
    }
    std::map<std::string,std::string> cfg;
    if (!read_config_file(cfgPath, cfg)) {
        std::cout << "Config not found at '" << cfgPath << "', using defaults." << std::endl;
    }

    // Defaults if not set
    int loginPort = cfg.count("LoginPort") ? std::atoi(cfg["LoginPort"].c_str()) : 15779;
    int gamePort  = cfg.count("GamePort")  ? std::atoi(cfg["GamePort"].c_str())  : 15780;

    std::cout << "Configured ports: Login=" << loginPort << " Game=" << gamePort << std::endl;

    // Initialize modules (stubs for now)
    LoginModule login;
    GameModule game;

    if (!login.Initialize(loginPort)) {
        std::cerr << "LoginModule initialization failed" << std::endl;
        return 1;
    }
    if (!game.Initialize(gamePort)) {
        std::cerr << "GameModule initialization failed" << std::endl;
        return 1;
    }

    // Start modules (non-blocking stubs for now)
    login.Start();
    game.Start();

    std::cout << "SroNexusServer is running. Press Enter to stop." << std::endl;
    std::cin.get();

    game.Stop();
    login.Stop();

    std::cout << "SroNexusServer stopped." << std::endl;
    return 0;
}

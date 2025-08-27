#include <iostream>
#include <fstream>
#include <string>
#include <map>
#include <algorithm>
#include <cstdlib>
#include <cctype>
#include <windows.h>

#include "modules/LoginModule.hpp"
#include "modules/GameModule.hpp"

// Minimal JSON parser for config: expects keys LoginPort and GamePort as integers
static bool read_config_json(const std::string& filename, int& loginPort, int& gamePort) {
    std::ifstream file(filename.c_str());
    if (!file) {
        return false;
    }
    std::string content((std::istreambuf_iterator<char>(file)), std::istreambuf_iterator<char>());
    auto findInt = [&](const std::string& key, int& out)->bool {
        std::string pattern = "\"" + key + "\"";
        auto pos = content.find(pattern);
        if (pos == std::string::npos) return false;
        pos = content.find(':', pos);
        if (pos == std::string::npos) return false;
        // skip spaces and quotes
        while (pos < content.size() && (content[pos] == ':' || content[pos] == ' ' || content[pos] == '\t' || content[pos] == '\"')) ++pos;
        // read number (integers only)
        std::string num;
        while (pos < content.size() && isdigit(static_cast<unsigned char>(content[pos]))) {
            num.push_back(content[pos]);
            ++pos;
        }
        if (num.empty()) return false;
        out = std::atoi(num.c_str());
        return true;
    };
    int lp = loginPort, gp = gamePort;
    bool foundAny = false;
    if (findInt("LoginPort", lp)) { loginPort = lp; foundAny = true; }
    if (findInt("GamePort", gp))  { gamePort  = gp; foundAny = true; }
    return foundAny;
}

int main(int argc, char** argv) {
    std::cout << "SroNexusServer (Windows) starting..." << std::endl;

    // Load editable ports from JSON config
    std::string cfgPath;
    if (argc >= 2) {
        cfgPath = argv[1];
    } else {
        // Resolve relative to executable dir: exeDir/config/sronexus.json
        char exePath[MAX_PATH] = {0};
        if (GetModuleFileNameA(nullptr, exePath, MAX_PATH)) {
            std::string exeDir = exePath;
            auto pos = exeDir.find_last_of("\\/");
            if (pos != std::string::npos) exeDir = exeDir.substr(0, pos);
            cfgPath = exeDir + "/config/sronexus.json";
        } else {
            cfgPath = "config/sronexus.json";
        }
    }

    // Defaults
    int loginPort = 15779;
    int gamePort  = 15780;

    if (!read_config_json(cfgPath, loginPort, gamePort)) {
        std::cout << "Config not found or invalid at '" << cfgPath << "', using defaults." << std::endl;
    }

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

    std::cout << "SroNexusServer is running." << std::endl;
    // Keep process alive when launched headless from the manager
    Sleep(INFINITE);

    // Unreachable in current scaffold; manager will Kill() the process.
    game.Stop();
    login.Stop();

    std::cout << "SroNexusServer stopped." << std::endl;
    return 0;
}

#include &lt;iostream&gt;
#include &lt;fstream&gt;
#include &lt;string&gt;
#include &lt;map&gt;

#include "modules/LoginModule.hpp"
#include "modules/GameModule.hpp"

// Minimal key=value config loader for Windows
static bool read_config_file(const std::string&amp; filename, std::map&lt;std::string,std::string&gt;&amp; entries) {
    std::ifstream file(filename.c_str());
    if (!file) {
        std::cerr &lt;&lt; "Config file not found: " &lt;&lt; filename &lt;&lt; std::endl;
        return false;
    }
    std::string line;
    size_t i = 0;
    while (std::getline(file, line)) {
        ++i;
        if (line.empty() || line[0] == '#' || line[0] == ';') continue;
        // strip spaces
        line.erase(remove(line.begin(), line.end(), ' '), line.end());
        size_t pos = line.find('=');
        if (pos == std::string::npos) continue;
        std::string key = line.substr(0, pos);
        std::string val = line.substr(pos+1);
        entries[key] = val;
    }
    return true;
}

int main(int argc, char** argv) {
    std::cout &lt;&lt; "SroNexusServer (Windows) starting..." &lt;&lt; std::endl;

    // Load editable ports from config
    std::string cfgPath = "config\\sronexus.conf";
    if (argc &gt;= 2) {
        cfgPath = argv[1];
    }
    std::map&lt;std::string,std::string&gt; cfg;
    read_config_file(cfgPath, cfg);

    // Defaults if not set
    int loginPort = cfg.count("LoginPort") ? std::atoi(cfg["LoginPort"].c_str()) : 15779;
    int gamePort  = cfg.count("GamePort")  ? std::atoi(cfg["GamePort"].c_str())  : 15780;

    std::cout &lt;&lt; "Configured ports: Login=" &lt;&lt; loginPort &lt;&lt; " Game=" &lt;&lt; gamePort &lt;&lt; std::endl;

    // Initialize modules (stubs for now)
    LoginModule login;
    GameModule game;

    if (!login.Initialize(loginPort)) {
        std::cerr &lt;&lt; "LoginModule initialization failed" &lt;&lt; std::endl;
        return 1;
    }
    if (!game.Initialize(gamePort)) {
        std::cerr &lt;&lt; "GameModule initialization failed" &lt;&lt; std::endl;
        return 1;
    }

    // Start modules (non-blocking stubs for now)
    login.Start();
    game.Start();

    std::cout &lt;&lt; "SroNexusServer is running. Press Enter to stop." &lt;&lt; std::endl;
    std::cin.get();

    game.Stop();
    login.Stop();

    std::cout &lt;&lt; "SroNexusServer stopped." &lt;&lt; std::endl;
    return 0;
}

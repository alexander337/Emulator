#pragma once
#include &lt;string&gt;
#include &lt;iostream&gt;

class GameModule {
public:
    GameModule() : m_port(0), m_running(false) {}

    bool Initialize(int port) {
        m_port = port;
        // TODO: wire AgentServer logic here (SRNL/EPL/SOL), Windows-only
        return true;
    }

    void Start() {
        m_running = true;
        std::cout &lt;&lt; "[GameModule] Starting on port " &lt;&lt; m_port &lt;&lt; std::endl;
        // TODO: start acceptors and state machines
    }

    void Stop() {
        if (!m_running) return;
        m_running = false;
        std::cout &lt;&lt; "[GameModule] Stopped" &lt;&lt; std::endl;
        // TODO: stop io_service, close sockets
    }

private:
    int m_port;
    bool m_running;
};

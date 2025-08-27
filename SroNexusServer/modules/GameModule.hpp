#pragma once
#include <string>
#include <iostream>

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
        std::cout << "[GameModule] Starting on port " << m_port << std::endl;
        // TODO: start acceptors and state machines
    }

    void Stop() {
        if (!m_running) return;
        m_running = false;
        std::cout << "[GameModule] Stopped" << std::endl;
        // TODO: stop io_service, close sockets
    }

private:
    int m_port;
    bool m_running;
};

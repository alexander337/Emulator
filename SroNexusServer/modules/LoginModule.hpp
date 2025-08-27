#pragma once
#include <string>
#include <iostream>

class LoginModule {
public:
    LoginModule() : m_port(0), m_running(false) {}

    bool Initialize(int port) {
        m_port = port;
        // TODO: wire Gateway+Master logic here (SRNL/EPL), Windows-only
        return true;
    }

    void Start() {
        m_running = true;
        std::cout << "[LoginModule] Starting on port " << m_port << std::endl;
        // TODO: start acceptors and state machines
    }

    void Stop() {
        if (!m_running) return;
        m_running = false;
        std::cout << "[LoginModule] Stopped" << std::endl;
        // TODO: stop io_service, close sockets
    }

private:
    int m_port;
    bool m_running;
};

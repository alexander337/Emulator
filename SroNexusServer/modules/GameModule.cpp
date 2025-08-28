#include "GameModule.hpp"
#include <packet_authentication.hpp>
#include <opcodes_shard_client.hpp>
#include <opcodes_shard_server.hpp>

GameModule::GameModule() : srv::IServer() {
    m_running = false;
}

GameModule::~GameModule() {
    Stop();
}

bool GameModule::Initialize(int port) {
    m_port = port;
    m_locale = 0; // Default locale
    m_client_version = 0; // Will be set by client
    m_is_encryption_active = true;
    m_connection_timeout = 30;
    m_connection_max_count = 5000; // More connections for game server
    
    std::map<std::string, std::string> config;
    config["Port"] = std::to_string(port);
    
    return srv::IServer::Initialize(config);
}

void GameModule::Start() {
    if (m_running) return;
    
    m_running = true;
    std::cout << "[GameModule] Starting on port " << m_port << std::endl;
    
    // Start accepting connections
    Execute(false); // Don't run io_service here, main will handle it
    
    // Run io_service in a separate thread
    m_io_service.run();
}

void GameModule::Stop() {
    if (!m_running) return;
    
    m_running = false;
    std::cout << "[GameModule] Stopping..." << std::endl;
    
    srv::IServer::Stop();
    
    std::cout << "[GameModule] Stopped" << std::endl;
}

bool GameModule::OnInitialize() {
    std::cout << "[GameModule] Initialized for Agent/Game functionality" << std::endl;
    return true;
}

void GameModule::OnConfigure(const std::map<std::string,std::string>& config_entries) {
    auto it = config_entries.find("Port");
    if (it != config_entries.end()) {
        m_port = std::stoi(it->second);
    }
}

void GameModule::OnRemoveConnection(const uint32_t ID) {
    std::cout << "[GameModule] Connection " << ID << " removed" << std::endl;
}

void GameModule::CreateConnection() {
    m_pending_conn = std::make_shared<GameConnection>(++m_counter, m_io_service, this);
}

// GameConnection implementation
GameConnection::GameConnection(uint32_t id, boost::asio::io_service& io_service, srv::IServer* srv)
    : srv::IConnection(id, io_service, srv) {
    
    // Set up initial game state
    // This would typically set up state machines for:
    // - Character selection
    // - World entry
    // - Movement/combat
    // - Skills/items
    // - Party/guild
}

GameConnection::~GameConnection() {
}

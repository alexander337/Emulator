#include "LoginModule.hpp"
#include <packet_authentication.hpp>
#include <opcodes_global_client.hpp>
#include <opcodes_global_server.hpp>

LoginModule::LoginModule() : srv::IServer() {
    m_running = false;
}

LoginModule::~LoginModule() {
    Stop();
}

bool LoginModule::Initialize(int port) {
    m_port = port;
    m_locale = 0; // Default locale
    m_client_version = 0; // Will be set by client
    m_is_encryption_active = true;
    m_connection_timeout = 30;
    m_connection_max_count = 1000;
    
    std::map<std::string, std::string> config;
    config["Port"] = std::to_string(port);
    
    return srv::IServer::Initialize(config);
}

void LoginModule::Start() {
    if (m_running) return;
    
    m_running = true;
    std::cout << "[LoginModule] Starting on port " << m_port << std::endl;
    
    // Start accepting connections
    Execute(false); // Don't run io_service here, main will handle it
    
    // Run io_service in a separate thread
    m_io_service.run();
}

void LoginModule::Stop() {
    if (!m_running) return;
    
    m_running = false;
    std::cout << "[LoginModule] Stopping..." << std::endl;
    
    srv::IServer::Stop();
    
    std::cout << "[LoginModule] Stopped" << std::endl;
}

bool LoginModule::OnInitialize() {
    std::cout << "[LoginModule] Initialized for Gateway+Master functionality" << std::endl;
    return true;
}

void LoginModule::OnConfigure(const std::map<std::string,std::string>& config_entries) {
    auto it = config_entries.find("Port");
    if (it != config_entries.end()) {
        m_port = std::stoi(it->second);
    }
}

void LoginModule::OnRemoveConnection(const uint32_t ID) {
    std::cout << "[LoginModule] Connection " << ID << " removed" << std::endl;
}

void LoginModule::CreateConnection() {
    m_pending_conn = std::make_shared<LoginConnection>(++m_counter, m_io_service, this);
}

// LoginConnection implementation
LoginConnection::LoginConnection(uint32_t id, boost::asio::io_service& io_service, srv::IServer* srv)
    : srv::IConnection(id, io_service, srv) {
    
    // Set up initial handshake state
    // This would typically set up state machines for:
    // - Handshake
    // - Version check
    // - Login authentication
    // - Server list/shard selection
}

LoginConnection::~LoginConnection() {
}

#include "LoginModule.hpp"
#include "../db/SqlServerPool.hpp"
#include <packet_authentication.hpp>
#include <opcodes_global_client.hpp>
#include <opcodes_global_server.hpp>
#include <chrono>
#include <sstream>

LoginModule::LoginModule() : sro::ModuleBase("Login") {
}

LoginModule::~LoginModule() {
    Stop();
}

bool LoginModule::OnModuleInitialize() {
    LogInfo("Initializing Login Module for Gateway+Master functionality");
    
    // Load server list from configuration or database
    LoadServerList();
    
    // Initialize authentication cache
    m_authCache.clear();
    
    return true;
}

void LoginModule::OnModuleConfigure(const std::map<std::string,std::string>& config) {
    // Module-specific configuration
    auto it = config.find("MaxAuthCacheSize");
    if (it != config.end()) {
        // Configure auth cache size
        LogInfo("Auth cache configured");
    }
}

void LoginModule::CreateConnection() {
    // Check connection throttling
    if (m_throttlingEnabled) {
        // Implement rate limiting
        if (GetActiveConnectionCount() >= m_connection_max_count) {
            LogWarning("Connection limit reached, rejecting new connection");
            return;
        }
    }
    
    m_pending_conn = std::make_shared<LoginConnection>(++m_counter, m_io_service, this);
    
    if (m_metricsEnabled) {
        m_metrics.totalConnections++;
        m_metrics.activeConnections++;
    }
}

void LoginModule::LoadServerList() {
    // In production, load from database
    // For now, add a default game server
    ServerInfo gameServer;
    gameServer.id = 1;
    gameServer.name = "SroNexus Game Server";
    gameServer.ip = "127.0.0.1";
    gameServer.port = 15780;
    gameServer.currentUsers = 0;
    gameServer.maxUsers = 5000;
    gameServer.isOnline = true;
    
    std::lock_guard<std::mutex> lock(m_serverMutex);
    m_servers[gameServer.id] = gameServer;
    
    LogInfo("Loaded " + std::to_string(m_servers.size()) + " game servers");
}

void LoginModule::UpdateServerStatus(uint16_t serverId, uint16_t currentUsers, bool isOnline) {
    std::lock_guard<std::mutex> lock(m_serverMutex);
    
    auto it = m_servers.find(serverId);
    if (it != m_servers.end()) {
        it->second.currentUsers = currentUsers;
        it->second.isOnline = isOnline;
        
        LogInfo("Updated server " + std::to_string(serverId) + 
                " - Users: " + std::to_string(currentUsers) + 
                " Online: " + (isOnline ? "Yes" : "No"));
    }
}

bool LoginModule::ValidateCredentials(const std::string& username, const std::string& password) {
    // Check auth cache first
    {
        std::lock_guard<std::mutex> lock(m_authCacheMutex);
        auto it = m_authCache.find(username);
        if (it != m_authCache.end()) {
            auto now = std::chrono::steady_clock::now();
            auto age = std::chrono::duration_cast<std::chrono::seconds>(now - it->second.lastAccess).count();
            
            // Cache valid for 5 minutes
            if (age < 300 && it->second.passwordHash == password) {
                it->second.lastAccess = now;
                return true;
            }
        }
    }
    
    // Query database using parameterized query to prevent SQL injection
    auto& dbPool = db::SqlServerPool::Instance();
    auto conn = dbPool.GetConnection();
    if (!conn) {
        LogError("Failed to get database connection");
        return false;
    }
    
    // Use parameterized query for security
    const std::string query = "SELECT id, password, access_level FROM accounts WHERE username = ?";
    
    if (conn->PrepareStatement(query)) {
        conn->BindParameter(1, username);
        
        std::vector<std::vector<std::string>> results;
        if (conn->ExecutePreparedQuery(results)) {
            dbPool.ReturnConnection(conn);
            
            if (!results.empty() && results[0][1] == password) {
                // Update cache
                std::lock_guard<std::mutex> lock(m_authCacheMutex);
                AuthCache cache;
                cache.passwordHash = password;
                cache.accountId = std::stoul(results[0][0]);
                cache.accessLevel = std::stoul(results[0][2]);
                cache.lastAccess = std::chrono::steady_clock::now();
                m_authCache[username] = cache;
                
                return true;
            }
        }
    }
    
    dbPool.ReturnConnection(conn);
    return false;
}

// LoginConnection implementation
LoginConnection::LoginConnection(uint32_t id, boost::asio::io_service& io_service, srv::IServer* srv)
    : srv::IConnection(id, io_service, srv)
    , m_state(State::HANDSHAKE)
    , m_accountId(0)
    , m_accessLevel(0)
    , m_failedLoginAttempts(0)
    , m_isBlocked(false) {
    
    m_connectTime = std::chrono::steady_clock::now();
    
    // Set up initial handshake
    SendHandshakeResponse();
}

LoginConnection::~LoginConnection() {
}

void LoginConnection::SetState(State newState) {
    m_state = newState;
}

void LoginConnection::OnHandshakeRequest(const uint8_t* data, size_t length) {
    if (m_state != State::HANDSHAKE) {
        return;
    }
    
    // Process handshake
    SendHandshakeResponse();
    SetState(State::VERSION_CHECK);
}

void LoginConnection::OnVersionCheck(const uint8_t* data, size_t length) {
    if (m_state != State::VERSION_CHECK) {
        return;
    }
    
    // Extract version from packet
    uint32_t clientVersion = 0; // Parse from data
    
    // Check if version is acceptable
    bool versionOk = true; // Implement version check
    
    SendVersionResponse(versionOk);
    
    if (versionOk) {
        SetState(State::LOGIN_AUTH);
    } else {
        SetState(State::DISCONNECTED);
    }
}

void LoginConnection::OnLoginRequest(const uint8_t* data, size_t length) {
    if (m_state != State::LOGIN_AUTH) {
        return;
    }
    
    if (m_isBlocked) {
        SendLoginResponse(0x02); // Account blocked
        return;
    }
    
    // Parse username and password from packet
    std::string username, password;
    // ... parsing logic ...
    
    LoginModule* module = static_cast<LoginModule*>(m_server);
    if (module->ValidateCredentials(username, password)) {
        m_username = username;
        SendLoginResponse(0x01); // Success
        SetState(State::SERVER_SELECT);
        SendServerList();
    } else {
        m_failedLoginAttempts++;
        if (m_failedLoginAttempts >= 3) {
            m_isBlocked = true;
            SendLoginResponse(0x02); // Account blocked
            SetState(State::DISCONNECTED);
        } else {
            SendLoginResponse(0x03); // Invalid credentials
        }
    }
}

void LoginConnection::OnServerSelectRequest(const uint8_t* data, size_t length) {
    if (m_state != State::SERVER_SELECT) {
        return;
    }
    
    // Parse selected server ID
    uint16_t serverId = 0; // Parse from data
    
    SendTransferInfo(serverId);
    SetState(State::TRANSFER_READY);
}

void LoginConnection::SendHandshakeResponse() {
    // Send handshake response packet
}

void LoginConnection::SendVersionResponse(bool accepted) {
    // Send version check response
}

void LoginConnection::SendLoginResponse(uint8_t result) {
    // Send login result
}

void LoginConnection::SendServerList() {
    // Send list of available game servers
}

void LoginConnection::SendTransferInfo(uint16_t serverId) {
    // Send transfer information for selected server
}

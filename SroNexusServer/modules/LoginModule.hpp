#pragma once
#include "ModuleBase.hpp"
#include <string>
#include <memory>
#include <unordered_map>
#include <boost/asio.hpp>
#include <server_connection_interface.hpp>
#include <server_state_handshake_interface.hpp>

class LoginConnection;

class LoginModule : public sro::ModuleBase {
public:
    LoginModule();
    virtual ~LoginModule();

protected:
    // ModuleBase overrides
    virtual bool OnModuleInitialize() override;
    virtual void OnModuleConfigure(const std::map<std::string,std::string>& config) override;
    virtual const char* GetModuleName() const override { return "Login"; }
    virtual void CreateConnection() override;
    
private:
    // Server list management
    struct ServerInfo {
        uint16_t id;
        std::string name;
        std::string ip;
        uint16_t port;
        uint16_t currentUsers;
        uint16_t maxUsers;
        bool isOnline;
    };
    
    std::unordered_map<uint16_t, ServerInfo> m_servers;
    std::mutex m_serverMutex;
    
    // Authentication cache for performance
    struct AuthCache {
        std::string passwordHash;
        uint32_t accountId;
        uint8_t accessLevel;
        std::chrono::steady_clock::time_point lastAccess;
    };
    
    std::unordered_map<std::string, AuthCache> m_authCache;
    std::mutex m_authCacheMutex;
    
    void LoadServerList();
    void UpdateServerStatus(uint16_t serverId, uint16_t currentUsers, bool isOnline);
    bool ValidateCredentials(const std::string& username, const std::string& password);
};

// Enhanced login connection with state machine
class LoginConnection : public srv::IConnection {
public:
    enum class State {
        HANDSHAKE,
        VERSION_CHECK,
        LOGIN_AUTH,
        SERVER_SELECT,
        TRANSFER_READY,
        DISCONNECTED
    };
    
    LoginConnection(uint32_t id, boost::asio::io_service& io_service, srv::IServer* srv);
    virtual ~LoginConnection();
    
    void SetState(State newState);
    State GetState() const { return m_state; }
    
    // Packet handlers
    void OnHandshakeRequest(const uint8_t* data, size_t length);
    void OnVersionCheck(const uint8_t* data, size_t length);
    void OnLoginRequest(const uint8_t* data, size_t length);
    void OnServerSelectRequest(const uint8_t* data, size_t length);
    
private:
    State m_state;
    std::string m_username;
    uint32_t m_accountId;
    uint8_t m_accessLevel;
    std::chrono::steady_clock::time_point m_connectTime;
    
    // Security features
    uint8_t m_failedLoginAttempts;
    bool m_isBlocked;
    
    void SendHandshakeResponse();
    void SendVersionResponse(bool accepted);
    void SendLoginResponse(uint8_t result);
    void SendServerList();
    void SendTransferInfo(uint16_t serverId);
};

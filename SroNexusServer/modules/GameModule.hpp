#pragma once
#include "ModuleBase.hpp"
#include <string>
#include <memory>
#include <unordered_map>
#include <boost/asio.hpp>
#include <server_connection_interface.hpp>
#include <player.hpp>
#include <coord.hpp>

class GameConnection;
class Player;

class GameModule : public sro::ModuleBase {
public:
    GameModule();
    virtual ~GameModule();

protected:
    // ModuleBase overrides
    virtual bool OnModuleInitialize() override;
    virtual void OnModuleConfigure(const std::map<std::string,std::string>& config) override;
    virtual const char* GetModuleName() const override { return "Game"; }
    virtual void CreateConnection() override;
    
public:
    // World management
    void BroadcastToRegion(uint16_t regionId, const uint8_t* data, size_t length);
    void BroadcastToAll(const uint8_t* data, size_t length);
    
    // Player management
    bool RegisterPlayer(uint32_t connectionId, std::shared_ptr<Player> player);
    void UnregisterPlayer(uint32_t connectionId);
    std::shared_ptr<Player> GetPlayer(uint32_t connectionId);
    std::shared_ptr<Player> GetPlayerByName(const std::string& name);
    
    // World state
    uint32_t GetOnlinePlayerCount() const { return m_players.size(); }
    
private:
    // Player management
    std::unordered_map<uint32_t, std::shared_ptr<Player>> m_players;
    std::unordered_map<std::string, uint32_t> m_playerNameIndex;
    mutable std::shared_mutex m_playerMutex;
    
    // World regions
    struct Region {
        uint16_t id;
        std::vector<uint32_t> players;
        std::vector<uint32_t> npcs;
        std::vector<uint32_t> items;
    };
    
    std::unordered_map<uint16_t, Region> m_regions;
    mutable std::shared_mutex m_regionMutex;
    
    // Game systems
    void InitializeWorldSystems();
    void InitializeNPCs();
    void InitializeSpawnPoints();
    void StartWorldTimers();
    
    // Update loops
    void UpdateWorld();
    void UpdateRegions();
    void SavePlayerData();
};

// Enhanced game connection with player state
class GameConnection : public srv::IConnection {
public:
    enum class State {
        HANDSHAKE,
        CHARACTER_SELECT,
        LOADING_WORLD,
        IN_GAME,
        DISCONNECTING
    };
    
    GameConnection(uint32_t id, boost::asio::io_service& io_service, srv::IServer* srv);
    virtual ~GameConnection();
    
    void SetState(State newState);
    State GetState() const { return m_state; }
    
    // Player management
    void SetPlayer(std::shared_ptr<Player> player) { m_player = player; }
    std::shared_ptr<Player> GetPlayer() const { return m_player; }
    
    // Packet handlers
    void OnCharacterListRequest(const uint8_t* data, size_t length);
    void OnCharacterSelectRequest(const uint8_t* data, size_t length);
    void OnCharacterCreateRequest(const uint8_t* data, size_t length);
    void OnCharacterDeleteRequest(const uint8_t* data, size_t length);
    void OnMovementRequest(const uint8_t* data, size_t length);
    void OnChatMessage(const uint8_t* data, size_t length);
    void OnSkillCast(const uint8_t* data, size_t length);
    void OnItemAction(const uint8_t* data, size_t length);
    void OnTradeRequest(const uint8_t* data, size_t length);
    
private:
    State m_state;
    std::shared_ptr<Player> m_player;
    uint32_t m_accountId;
    std::chrono::steady_clock::time_point m_lastActivity;
    
    // Anti-cheat
    struct {
        uint32_t movementViolations;
        uint32_t speedViolations;
        uint32_t teleportViolations;
        std::chrono::steady_clock::time_point lastMovement;
        Coord lastPosition;
    } m_antiCheat;
    
    void SendCharacterList();
    void SendWorldData();
    void SendSpawnPlayer();
    void SendNearbyObjects();
    
    bool ValidateMovement(const Coord& newPosition);
    bool ValidateSkillCast(uint32_t skillId, uint32_t targetId);
    bool ValidateItemAction(uint32_t itemId, uint8_t action);
};

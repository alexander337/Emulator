#include "GameModule.hpp"
#include "../db/SqlServerPool.hpp"
#include <packet_authentication.hpp>
#include <opcodes_shard_client.hpp>
#include <opcodes_shard_server.hpp>
#include <player.hpp>
#include <thread>
#include <sstream>

GameModule::GameModule() : sro::ModuleBase("Game") {
}

GameModule::~GameModule() {
    Stop();
}

bool GameModule::OnModuleInitialize() {
    LogInfo("Initializing Game Module for Agent/World functionality");
    
    // Initialize world systems
    InitializeWorldSystems();
    InitializeNPCs();
    InitializeSpawnPoints();
    
    // Start world update timers
    StartWorldTimers();
    
    return true;
}

void GameModule::OnModuleConfigure(const std::map<std::string,std::string>& config) {
    // Module-specific configuration
    auto it = config.find("MaxPlayersPerRegion");
    if (it != config.end()) {
        LogInfo("Configured max players per region: " + it->second);
    }
}

void GameModule::CreateConnection() {
    // Check connection limits
    if (GetActiveConnectionCount() >= m_connection_max_count) {
        LogWarning("Game server full, rejecting new connection");
        return;
    }
    
    m_pending_conn = std::make_shared<GameConnection>(++m_counter, m_io_service, this);
    
    if (m_metricsEnabled) {
        m_metrics.totalConnections++;
        m_metrics.activeConnections++;
    }
}

void GameModule::BroadcastToRegion(uint16_t regionId, const uint8_t* data, size_t length) {
    std::shared_lock lock(m_regionMutex);
    
    auto it = m_regions.find(regionId);
    if (it != m_regions.end()) {
        for (uint32_t playerId : it->second.players) {
            auto player = GetPlayer(playerId);
            if (player) {
                // Send packet to player
                // player->SendPacket(data, length);
            }
        }
    }
}

void GameModule::BroadcastToAll(const uint8_t* data, size_t length) {
    std::shared_lock lock(m_playerMutex);
    
    for (const auto& [id, player] : m_players) {
        if (player) {
            // Send packet to player
            // player->SendPacket(data, length);
        }
    }
}

bool GameModule::RegisterPlayer(uint32_t connectionId, std::shared_ptr<Player> player) {
    if (!player) return false;
    
    std::unique_lock lock(m_playerMutex);
    
    // Check if name already exists
    if (m_playerNameIndex.find(player->get_name()) != m_playerNameIndex.end()) {
        LogWarning("Player name already exists: " + player->get_name());
        return false;
    }
    
    m_players[connectionId] = player;
    m_playerNameIndex[player->get_name()] = connectionId;
    
    LogInfo("Player registered: " + player->get_name() + " (ID: " + std::to_string(connectionId) + ")");
    return true;
}

void GameModule::UnregisterPlayer(uint32_t connectionId) {
    std::unique_lock lock(m_playerMutex);
    
    auto it = m_players.find(connectionId);
    if (it != m_players.end()) {
        auto player = it->second;
        if (player) {
            m_playerNameIndex.erase(player->get_name());
            LogInfo("Player unregistered: " + player->get_name());
        }
        m_players.erase(it);
    }
}

std::shared_ptr<Player> GameModule::GetPlayer(uint32_t connectionId) {
    std::shared_lock lock(m_playerMutex);
    
    auto it = m_players.find(connectionId);
    if (it != m_players.end()) {
        return it->second;
    }
    return nullptr;
}

std::shared_ptr<Player> GameModule::GetPlayerByName(const std::string& name) {
    std::shared_lock lock(m_playerMutex);
    
    auto it = m_playerNameIndex.find(name);
    if (it != m_playerNameIndex.end()) {
        return GetPlayer(it->second);
    }
    return nullptr;
}

void GameModule::InitializeWorldSystems() {
    LogInfo("Initializing world systems...");
    
    // Initialize regions
    for (uint16_t i = 0; i < 100; ++i) {
        Region region;
        region.id = i;
        m_regions[i] = region;
    }
    
    LogInfo("Initialized " + std::to_string(m_regions.size()) + " regions");
}

void GameModule::InitializeNPCs() {
    LogInfo("Loading NPCs from database...");
    
    auto& dbPool = db::SqlServerPool::Instance();
    std::vector<std::vector<std::string>> results;
    
    if (dbPool.ExecuteQuery("SELECT id, model, x, y, z, region FROM npcs", results)) {
        LogInfo("Loaded " + std::to_string(results.size()) + " NPCs");
    }
}

void GameModule::InitializeSpawnPoints() {
    LogInfo("Loading spawn points...");
    
    // Load spawn points from database
    auto& dbPool = db::SqlServerPool::Instance();
    std::vector<std::vector<std::string>> results;
    
    if (dbPool.ExecuteQuery("SELECT id, mob_id, x, y, z, region, respawn_time FROM spawn_points", results)) {
        LogInfo("Loaded " + std::to_string(results.size()) + " spawn points");
    }
}

void GameModule::StartWorldTimers() {
    // Start world update thread
    std::thread worldThread([this]() {
        while (m_running) {
            UpdateWorld();
            std::this_thread::sleep_for(std::chrono::milliseconds(100));
        }
    });
    worldThread.detach();
    
    // Start region update thread
    std::thread regionThread([this]() {
        while (m_running) {
            UpdateRegions();
            std::this_thread::sleep_for(std::chrono::milliseconds(500));
        }
    });
    regionThread.detach();
    
    // Start save thread
    std::thread saveThread([this]() {
        while (m_running) {
            SavePlayerData();
            std::this_thread::sleep_for(std::chrono::minutes(5));
        }
    });
    saveThread.detach();
    
    LogInfo("World timers started");
}

void GameModule::UpdateWorld() {
    // Update world state
    // Process mob AI, respawns, etc.
}

void GameModule::UpdateRegions() {
    // Update region states
    // Check for region events, weather, etc.
}

void GameModule::SavePlayerData() {
    std::shared_lock lock(m_playerMutex);
    
    for (const auto& [id, player] : m_players) {
        if (player) {
            // Save player data to database
            // ... save logic ...
        }
    }
    
    LogInfo("Saved data for " + std::to_string(m_players.size()) + " players");
}

// GameConnection implementation
GameConnection::GameConnection(uint32_t id, boost::asio::io_service& io_service, srv::IServer* srv)
    : srv::IConnection(id, io_service, srv)
    , m_state(State::HANDSHAKE)
    , m_accountId(0) {
    
    m_lastActivity = std::chrono::steady_clock::now();
    
    // Initialize anti-cheat
    m_antiCheat.movementViolations = 0;
    m_antiCheat.speedViolations = 0;
    m_antiCheat.teleportViolations = 0;
    m_antiCheat.lastMovement = std::chrono::steady_clock::now();
}

GameConnection::~GameConnection() {
    if (m_player) {
        GameModule* module = static_cast<GameModule*>(m_server);
        module->UnregisterPlayer(m_id);
    }
}

void GameConnection::SetState(State newState) {
    m_state = newState;
}

void GameConnection::OnCharacterListRequest(const uint8_t* data, size_t length) {
    if (m_state != State::CHARACTER_SELECT) {
        return;
    }
    
    SendCharacterList();
}

void GameConnection::OnCharacterSelectRequest(const uint8_t* data, size_t length) {
    if (m_state != State::CHARACTER_SELECT) {
        return;
    }
    
    // Parse character ID from packet
    uint32_t characterId = 0; // Parse from data
    
    // Load character from database
    auto& dbPool = db::SqlServerPool::Instance();
    std::vector<std::vector<std::string>> results;
    
    std::stringstream query;
    query << "SELECT * FROM characters WHERE id = " << characterId 
          << " AND account_id = " << m_accountId;
    
    if (dbPool.ExecuteQuery(query.str(), results) && !results.empty()) {
        // Create player object
        // m_player = std::make_shared<Player>();
        // ... initialize player from database ...
        
        GameModule* module = static_cast<GameModule*>(m_server);
        if (module->RegisterPlayer(m_id, m_player)) {
            SetState(State::LOADING_WORLD);
            SendWorldData();
            SendSpawnPlayer();
            SendNearbyObjects();
            SetState(State::IN_GAME);
        }
    }
}

void GameConnection::OnCharacterCreateRequest(const uint8_t* data, size_t length) {
    if (m_state != State::CHARACTER_SELECT) {
        return;
    }
    
    // Parse character creation data
    // Validate name, appearance, etc.
    // Insert into database
    // Send response
}

void GameConnection::OnCharacterDeleteRequest(const uint8_t* data, size_t length) {
    if (m_state != State::CHARACTER_SELECT) {
        return;
    }
    
    // Parse character ID
    // Verify ownership
    // Mark as deleted in database
    // Send response
}

void GameConnection::OnMovementRequest(const uint8_t* data, size_t length) {
    if (m_state != State::IN_GAME || !m_player) {
        return;
    }
    
    // Parse movement data
    Coord newPosition; // Parse from data
    
    if (ValidateMovement(newPosition)) {
        // Update player position
        // Broadcast to nearby players
        m_antiCheat.lastPosition = newPosition;
        m_antiCheat.lastMovement = std::chrono::steady_clock::now();
    } else {
        m_antiCheat.movementViolations++;
        if (m_antiCheat.movementViolations > 10) {
            // Disconnect cheater
            SetState(State::DISCONNECTING);
        }
    }
}

void GameConnection::OnChatMessage(const uint8_t* data, size_t length) {
    if (m_state != State::IN_GAME || !m_player) {
        return;
    }
    
    // Parse chat message
    // Validate content (filter, length, etc.)
    // Broadcast based on chat type (all, party, guild, whisper)
}

void GameConnection::OnSkillCast(const uint8_t* data, size_t length) {
    if (m_state != State::IN_GAME || !m_player) {
        return;
    }
    
    // Parse skill cast request
    uint32_t skillId = 0, targetId = 0; // Parse from data
    
    if (ValidateSkillCast(skillId, targetId)) {
        // Process skill cast
        // Apply effects
        // Broadcast animation
    }
}

void GameConnection::OnItemAction(const uint8_t* data, size_t length) {
    if (m_state != State::IN_GAME || !m_player) {
        return;
    }
    
    // Parse item action
    uint32_t itemId = 0;
    uint8_t action = 0; // Parse from data
    
    if (ValidateItemAction(itemId, action)) {
        // Process item action (use, equip, drop, etc.)
    }
}

void GameConnection::OnTradeRequest(const uint8_t* data, size_t length) {
    if (m_state != State::IN_GAME || !m_player) {
        return;
    }
    
    // Parse trade request
    // Validate target player
    // Send trade invitation
}

void GameConnection::SendCharacterList() {
    // Query database for characters
    // Build and send character list packet
}

void GameConnection::SendWorldData() {
    // Send world time, weather, etc.
}

void GameConnection::SendSpawnPlayer() {
    // Send player spawn packet
}

void GameConnection::SendNearbyObjects() {
    // Send nearby players, NPCs, items
}

bool GameConnection::ValidateMovement(const Coord& newPosition) {
    // Check movement speed
    auto now = std::chrono::steady_clock::now();
    auto timeDiff = std::chrono::duration_cast<std::chrono::milliseconds>(
        now - m_antiCheat.lastMovement).count();
    
    if (timeDiff < 50) { // Too fast
        m_antiCheat.speedViolations++;
        return false;
    }
    
    // Check distance
    float distance = m_antiCheat.lastPosition.distance(newPosition);
    float maxDistance = (timeDiff / 1000.0f) * 10.0f; // 10 units per second max
    
    if (distance > maxDistance) {
        m_antiCheat.teleportViolations++;
        return false;
    }
    
    return true;
}

bool GameConnection::ValidateSkillCast(uint32_t skillId, uint32_t targetId) {
    // Check if player has skill
    // Check cooldown
    // Check mana/stamina
    // Check range
    return true;
}

bool GameConnection::ValidateItemAction(uint32_t itemId, uint8_t action) {
    // Check if player has item
    // Check if action is valid for item type
    // Check requirements
    return true;
}

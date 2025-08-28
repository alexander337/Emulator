#pragma once
#include <string>
#include <vector>
#include <functional>
#include <unordered_map>
#include <memory>

namespace sro {

// Advanced Game Master tools and commands
class GMTools {
public:
    // GM permission levels
    enum class GMLevel {
        NONE = 0,
        HELPER = 1,      // Basic support
        MODERATOR = 2,   // Can mute/kick
        GAMEMASTER = 3,  // Full game control
        DEVELOPER = 4,   // Development tools
        ADMIN = 5        // Full server control
    };

    // Command structure
    struct Command {
        std::string name;
        std::string description;
        std::vector<std::string> aliases;
        GMLevel requiredLevel;
        std::vector<std::string> parameters;
        std::function<bool(uint32_t gmId, const std::vector<std::string>& args)> handler;
        std::string usage;
        std::string category;
    };

    // GM account info
    struct GMAccount {
        uint32_t accountId;
        std::string username;
        GMLevel level;
        std::vector<std::string> permissions;
        std::chrono::steady_clock::time_point lastActivity;
        std::vector<std::string> commandHistory;
        bool invisible;
        bool invulnerable;
        bool noclip;
    };

    // Investigation tools
    class InvestigationTools {
    public:
        struct PlayerInfo {
            uint32_t playerId;
            std::string characterName;
            std::string accountName;
            std::string ipAddress;
            std::string hwid;
            std::chrono::steady_clock::time_point creationDate;
            std::chrono::seconds totalPlaytime;
            std::vector<std::string> previousNames;
            std::vector<std::string> previousIPs;
            std::vector<uint32_t> alts; // Alternative characters
        };
        
        struct TradeLog {
            uint32_t fromPlayer;
            uint32_t toPlayer;
            std::vector<uint32_t> items;
            uint64_t gold;
            std::chrono::steady_clock::time_point timestamp;
        };
        
        PlayerInfo GetPlayerInfo(uint32_t playerId);
        std::vector<TradeLog> GetTradeLogs(uint32_t playerId, std::chrono::hours duration);
        std::vector<std::string> GetChatLogs(uint32_t playerId, std::chrono::hours duration);
        std::vector<uint32_t> FindDuplicateItems();
        std::vector<uint32_t> DetectBotPatterns();
        std::unordered_map<uint32_t, float> CalculateSuspicionScores();
    };

    // World manipulation
    class WorldEditor {
    public:
        void SpawnNPC(uint32_t npcId, float x, float y, float z);
        void SpawnItem(uint32_t itemId, float x, float y, float z, uint32_t quantity);
        void CreatePortal(float fromX, float fromY, float fromZ, float toX, float toY, float toZ);
        void ModifyTerrain(float x, float y, float z, float radius, const std::string& type);
        void CreateEvent(const std::string& eventType, float x, float y, float z);
        void SetWeather(const std::string& weather, uint16_t regionId);
        void SetTime(uint32_t hour, uint32_t minute);
        void TriggerScript(const std::string& scriptName, const std::vector<std::string>& params);
    };

    // Player management
    class PlayerManager {
    public:
        void Teleport(uint32_t playerId, float x, float y, float z);
        void TeleportToPlayer(uint32_t gmId, uint32_t targetId);
        void Summon(uint32_t playerId, uint32_t gmId);
        void Kick(uint32_t playerId, const std::string& reason);
        void Ban(uint32_t playerId, std::chrono::hours duration, const std::string& reason);
        void Mute(uint32_t playerId, std::chrono::minutes duration, const std::string& reason);
        void Freeze(uint32_t playerId);
        void Unfreeze(uint32_t playerId);
        void Kill(uint32_t playerId);
        void Resurrect(uint32_t playerId);
        void Heal(uint32_t playerId);
        void SetLevel(uint32_t playerId, uint32_t level);
        void GiveItem(uint32_t playerId, uint32_t itemId, uint32_t quantity);
        void GiveGold(uint32_t playerId, uint64_t amount);
        void GiveSkillPoints(uint32_t playerId, uint32_t points);
        void ResetCharacter(uint32_t playerId);
    };

    // Event management
    class EventManager {
    public:
        struct GMEvent {
            std::string name;
            std::string type;
            std::chrono::steady_clock::time_point startTime;
            std::chrono::steady_clock::time_point endTime;
            std::vector<uint32_t> participants;
            std::unordered_map<std::string, std::string> settings;
            std::vector<std::pair<uint32_t, uint32_t>> rewards; // playerId, itemId
        };
        
        void CreateEvent(const GMEvent& event);
        void StartEvent(const std::string& eventName);
        void EndEvent(const std::string& eventName);
        void AnnounceEvent(const std::string& message);
        void RegisterParticipant(const std::string& eventName, uint32_t playerId);
        void DistributeRewards(const std::string& eventName);
    };

    // Debug tools
    class DebugTools {
    public:
        void ShowCollisionMesh(bool enable);
        void ShowPathfinding(bool enable);
        void ShowAIDebug(bool enable);
        void ShowNetworkDebug(bool enable);
        void ShowPerformanceOverlay(bool enable);
        void DumpMemory(const std::string& filename);
        void ProfileFunction(const std::string& functionName);
        void SimulateLag(uint32_t milliseconds);
        void SimulatePacketLoss(float percentage);
        void ForceGarbageCollection();
        void ReloadScripts();
        void ReloadConfigs();
    };

    // Monitoring tools
    class MonitoringTools {
    public:
        struct ServerStats {
            uint32_t onlinePlayers;
            float cpuUsage;
            float memoryUsage;
            float tickRate;
            uint32_t dbConnections;
            uint64_t totalGold;
            std::unordered_map<uint32_t, uint32_t> itemCounts;
        };
        
        ServerStats GetServerStats();
        std::vector<std::pair<uint32_t, std::string>> GetOnlinePlayers();
        std::vector<std::string> GetRecentErrors();
        std::vector<std::string> GetSlowQueries();
        void WatchPlayer(uint32_t playerId);
        void UnwatchPlayer(uint32_t playerId);
    };

public:
    static GMTools& Instance() {
        static GMTools instance;
        return instance;
    }
    
    // Command registration and execution
    void RegisterCommand(const Command& command);
    bool ExecuteCommand(uint32_t gmId, const std::string& command, const std::vector<std::string>& args);
    std::vector<Command> GetCommands(GMLevel level) const;
    
    // GM account management
    void RegisterGM(const GMAccount& account);
    void UpdateGMLevel(uint32_t accountId, GMLevel level);
    GMLevel GetGMLevel(uint32_t accountId) const;
    bool HasPermission(uint32_t accountId, const std::string& permission) const;
    
    // Tool access
    InvestigationTools& GetInvestigationTools() { return m_investigationTools; }
    WorldEditor& GetWorldEditor() { return m_worldEditor; }
    PlayerManager& GetPlayerManager() { return m_playerManager; }
    EventManager& GetEventManager() { return m_eventManager; }
    DebugTools& GetDebugTools() { return m_debugTools; }
    MonitoringTools& GetMonitoringTools() { return m_monitoringTools; }
    
    // Logging
    void LogGMAction(uint32_t gmId, const std::string& action, const std::string& details);
    std::vector<std::string> GetGMLogs(uint32_t gmId, std::chrono::hours duration);
    
private:
    GMTools();
    ~GMTools() = default;
    
    // Registered commands
    std::unordered_map<std::string, Command> m_commands;
    std::mutex m_commandMutex;
    
    // GM accounts
    std::unordered_map<uint32_t, GMAccount> m_gmAccounts;
    std::mutex m_accountMutex;
    
    // Tools
    InvestigationTools m_investigationTools;
    WorldEditor m_worldEditor;
    PlayerManager m_playerManager;
    EventManager m_eventManager;
    DebugTools m_debugTools;
    MonitoringTools m_monitoringTools;
    
    // Action logging
    std::vector<std::tuple<uint32_t, std::string, std::string, std::chrono::steady_clock::time_point>> m_actionLog;
    std::mutex m_logMutex;
    
    // Built-in command handlers
    void RegisterBuiltInCommands();
    bool HandleTeleport(uint32_t gmId, const std::vector<std::string>& args);
    bool HandleGiveItem(uint32_t gmId, const std::vector<std::string>& args);
    bool HandleBan(uint32_t gmId, const std::vector<std::string>& args);
    bool HandleEvent(uint32_t gmId, const std::vector<std::string>& args);
};

} // namespace sro

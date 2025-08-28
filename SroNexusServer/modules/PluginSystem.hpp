#pragma once
#include <string>
#include <vector>
#include <unordered_map>
#include <functional>
#include <memory>
#include <windows.h>

namespace sro {

// Plugin system for extensibility
class PluginSystem {
public:
    // Plugin interface that all plugins must implement
    class IPlugin {
    public:
        virtual ~IPlugin() = default;
        
        // Lifecycle
        virtual bool OnLoad() = 0;
        virtual void OnUnload() = 0;
        virtual void OnEnable() = 0;
        virtual void OnDisable() = 0;
        
        // Metadata
        virtual std::string GetName() const = 0;
        virtual std::string GetVersion() const = 0;
        virtual std::string GetAuthor() const = 0;
        virtual std::string GetDescription() const = 0;
        
        // Dependencies
        virtual std::vector<std::string> GetDependencies() const { return {}; }
        virtual std::vector<std::string> GetConflicts() const { return {}; }
    };

    // Event system for plugins
    class EventSystem {
    public:
        using EventHandler = std::function<void(const std::string&, void*)>;
        
        void RegisterEvent(const std::string& eventName, EventHandler handler);
        void UnregisterEvent(const std::string& eventName, EventHandler handler);
        void TriggerEvent(const std::string& eventName, void* data);
        
    private:
        std::unordered_map<std::string, std::vector<EventHandler>> m_handlers;
        std::mutex m_handlerMutex;
    };

    // API exposed to plugins
    class PluginAPI {
    public:
        // Server control
        virtual void SendPacketToPlayer(uint32_t playerId, const uint8_t* data, size_t length) = 0;
        virtual void BroadcastPacket(const uint8_t* data, size_t length) = 0;
        virtual void KickPlayer(uint32_t playerId, const std::string& reason) = 0;
        virtual void BanPlayer(uint32_t playerId, uint32_t duration, const std::string& reason) = 0;
        
        // World interaction
        virtual void SpawnNPC(uint32_t npcId, float x, float y, float z) = 0;
        virtual void SpawnItem(uint32_t itemId, float x, float y, float z) = 0;
        virtual void CreateEffect(uint32_t effectId, float x, float y, float z) = 0;
        
        // Database access
        virtual bool ExecuteQuery(const std::string& query) = 0;
        virtual std::vector<std::vector<std::string>> SelectQuery(const std::string& query) = 0;
        
        // Configuration
        virtual std::string GetConfig(const std::string& key) = 0;
        virtual void SetConfig(const std::string& key, const std::string& value) = 0;
        
        // Logging
        virtual void LogInfo(const std::string& message) = 0;
        virtual void LogWarning(const std::string& message) = 0;
        virtual void LogError(const std::string& message) = 0;
        
        // Events
        virtual void RegisterEventHandler(const std::string& event, std::function<void(void*)> handler) = 0;
        virtual void TriggerEvent(const std::string& event, void* data) = 0;
    };

    // Plugin loader
    class PluginLoader {
    public:
        struct LoadedPlugin {
            std::string path;
            HMODULE handle;
            std::unique_ptr<IPlugin> instance;
            bool enabled;
            std::chrono::steady_clock::time_point loadTime;
        };
        
        bool LoadPlugin(const std::string& path);
        bool UnloadPlugin(const std::string& name);
        bool EnablePlugin(const std::string& name);
        bool DisablePlugin(const std::string& name);
        
        std::vector<std::string> GetLoadedPlugins() const;
        LoadedPlugin* GetPlugin(const std::string& name);
        
    private:
        std::unordered_map<std::string, LoadedPlugin> m_plugins;
        std::mutex m_pluginMutex;
        
        bool ValidateDependencies(const IPlugin* plugin);
        bool CheckConflicts(const IPlugin* plugin);
    };

    // Sandboxing for plugin security
    class PluginSandbox {
    public:
        struct Permissions {
            bool canAccessDatabase;
            bool canAccessNetwork;
            bool canAccessFiles;
            bool canModifyWorld;
            bool canKickPlayers;
            bool canBanPlayers;
            uint32_t maxMemoryMB;
            uint32_t maxCpuPercent;
        };
        
        void SetPermissions(const std::string& pluginName, const Permissions& perms);
        bool CheckPermission(const std::string& pluginName, const std::string& permission);
        void MonitorResourceUsage(const std::string& pluginName);
        
    private:
        std::unordered_map<std::string, Permissions> m_permissions;
        std::unordered_map<std::string, uint64_t> m_memoryUsage;
        std::unordered_map<std::string, float> m_cpuUsage;
    };

    // Hot reload support
    class HotReloader {
    public:
        void WatchDirectory(const std::string& directory);
        void CheckForChanges();
        bool ReloadPlugin(const std::string& name);
        
    private:
        std::string m_watchDirectory;
        std::unordered_map<std::string, std::chrono::system_clock::time_point> m_fileTimestamps;
    };

public:
    static PluginSystem& Instance() {
        static PluginSystem instance;
        return instance;
    }
    
    // Plugin management
    bool LoadPlugin(const std::string& path);
    bool UnloadPlugin(const std::string& name);
    bool ReloadPlugin(const std::string& name);
    void LoadAllPlugins(const std::string& directory);
    
    // Plugin control
    bool EnablePlugin(const std::string& name);
    bool DisablePlugin(const std::string& name);
    
    // Plugin discovery
    std::vector<std::string> GetAvailablePlugins() const;
    std::vector<std::string> GetLoadedPlugins() const;
    std::vector<std::string> GetEnabledPlugins() const;
    
    // API access
    void SetAPI(std::shared_ptr<PluginAPI> api) { m_api = api; }
    std::shared_ptr<PluginAPI> GetAPI() { return m_api; }
    
    // Event system
    EventSystem& GetEventSystem() { return m_eventSystem; }
    
    // Security
    void SetSandboxPermissions(const std::string& plugin, const PluginSandbox::Permissions& perms);
    
    // Hot reload
    void EnableHotReload(const std::string& directory);
    void DisableHotReload();
    
private:
    PluginSystem() = default;
    ~PluginSystem() = default;
    
    std::unique_ptr<PluginLoader> m_loader;
    std::unique_ptr<PluginSandbox> m_sandbox;
    std::unique_ptr<HotReloader> m_hotReloader;
    std::shared_ptr<PluginAPI> m_api;
    EventSystem m_eventSystem;
    
    bool m_hotReloadEnabled;
    std::thread m_hotReloadThread;
};

// Macro for plugin exports
#define PLUGIN_EXPORT extern "C" __declspec(dllexport)

// Plugin entry point
#define PLUGIN_MAIN(PluginClass) \
    PLUGIN_EXPORT sro::PluginSystem::IPlugin* CreatePlugin() { \
        return new PluginClass(); \
    } \
    PLUGIN_EXPORT void DestroyPlugin(sro::PluginSystem::IPlugin* plugin) { \
        delete plugin; \
    }

} // namespace sro

#pragma once
#include <server_interface.hpp>
#include <atomic>
#include <chrono>
#include <memory>
#include <unordered_map>
#include <mutex>

namespace sro {

// Performance metrics tracking
struct ModuleMetrics {
    std::atomic<uint64_t> totalConnections{0};
    std::atomic<uint64_t> activeConnections{0};
    std::atomic<uint64_t> packetsReceived{0};
    std::atomic<uint64_t> packetsSent{0};
    std::atomic<uint64_t> bytesReceived{0};
    std::atomic<uint64_t> bytesSent{0};
    std::chrono::steady_clock::time_point startTime;
    
    ModuleMetrics() : startTime(std::chrono::steady_clock::now()) {}
    
    double GetUptime() const {
        auto now = std::chrono::steady_clock::now();
        return std::chrono::duration<double>(now - startTime).count();
    }
};

// Base class for both Login and Game modules
class ModuleBase : public srv::IServer {
public:
    ModuleBase(const std::string& name);
    virtual ~ModuleBase();
    
    // Common initialization
    bool Initialize(int port, const std::map<std::string, std::string>& config);
    void Start();
    void Run();
    void Stop();
    
    // Metrics access
    const ModuleMetrics& GetMetrics() const { return m_metrics; }
    
    // Connection management
    void SetMaxConnections(uint32_t max) { m_connection_max_count = max; }
    uint32_t GetActiveConnectionCount() const { return m_metrics.activeConnections.load(); }
    
protected:
    // IServer overrides
    virtual bool OnInitialize() override;
    virtual void OnConfigure(const std::map<std::string,std::string>& config_entries) override;
    virtual void OnRemoveConnection(const uint32_t ID) override;
    
    // Module-specific initialization (override in derived classes)
    virtual bool OnModuleInitialize() = 0;
    virtual void OnModuleConfigure(const std::map<std::string,std::string>& config) = 0;
    virtual const char* GetModuleName() const = 0;
    
    // Advanced features
    void EnableMetrics(bool enable) { m_metricsEnabled = enable; }
    void EnableConnectionThrottling(bool enable) { m_throttlingEnabled = enable; }
    void SetConnectionRateLimit(uint32_t connectionsPerSecond);
    
    // Logging helpers
    void LogInfo(const std::string& message);
    void LogWarning(const std::string& message);
    void LogError(const std::string& message);
    
protected:
    std::string m_moduleName;
    bool m_running;
    ModuleMetrics m_metrics;
    
    // Advanced features
    bool m_metricsEnabled;
    bool m_throttlingEnabled;
    uint32_t m_connectionRateLimit;
    
    // Connection throttling
    std::chrono::steady_clock::time_point m_lastConnectionTime;
    std::mutex m_throttleMutex;
    
private:
    bool CheckConnectionThrottle();
};

} // namespace sro

#include "ModuleBase.hpp"
#include <iostream>
#include <sstream>
#include <iomanip>

namespace sro {

ModuleBase::ModuleBase(const std::string& name) 
    : srv::IServer()
    , m_moduleName(name)
    , m_running(false)
    , m_metricsEnabled(true)
    , m_throttlingEnabled(false)
    , m_connectionRateLimit(100) // 100 connections per second default
{
}

ModuleBase::~ModuleBase() {
    Stop();
}

bool ModuleBase::Initialize(int port, const std::map<std::string, std::string>& config) {
    m_port = port;
    m_locale = 0;
    m_client_version = 0;
    m_is_encryption_active = true;
    m_connection_timeout = 30;
    
    // Set connection limits based on module type
    if (m_moduleName == "Login") {
        m_connection_max_count = 1000;
    } else if (m_moduleName == "Game") {
        m_connection_max_count = 5000;
    }
    
    // Build config for base class
    std::map<std::string, std::string> baseConfig;
    baseConfig["Port"] = std::to_string(port);
    
    // Merge with provided config
    for (const auto& [key, value] : config) {
        baseConfig[key] = value;
    }
    
    // Initialize base server
    if (!srv::IServer::Initialize(baseConfig)) {
        LogError("Failed to initialize base server");
        return false;
    }
    
    // Module-specific initialization
    if (!OnModuleInitialize()) {
        LogError("Failed to initialize module");
        return false;
    }
    
    // Configure module
    OnModuleConfigure(config);
    
    LogInfo("Module initialized successfully on port " + std::to_string(port));
    return true;
}

void ModuleBase::Start() {
    if (m_running) return;
    
    m_running = true;
    m_metrics.startTime = std::chrono::steady_clock::now();
    
    LogInfo("Starting " + m_moduleName + " module on port " + std::to_string(m_port));
    
    // Start accepting connections
    Execute(false);
}

void ModuleBase::Run() {
    LogInfo(m_moduleName + " module running");
    m_io_service.run();
    LogInfo(m_moduleName + " module stopped");
}

void ModuleBase::Stop() {
    if (!m_running) return;
    
    m_running = false;
    LogInfo("Stopping " + m_moduleName + " module...");
    
    srv::IServer::Stop();
    
    // Log final metrics
    if (m_metricsEnabled) {
        std::stringstream ss;
        ss << "Final metrics for " << m_moduleName << ":\n"
           << "  Total connections: " << m_metrics.totalConnections << "\n"
           << "  Packets received: " << m_metrics.packetsReceived << "\n"
           << "  Packets sent: " << m_metrics.packetsSent << "\n"
           << "  Bytes received: " << m_metrics.bytesReceived << "\n"
           << "  Bytes sent: " << m_metrics.bytesSent << "\n"
           << "  Uptime: " << std::fixed << std::setprecision(2) << m_metrics.GetUptime() << " seconds";
        LogInfo(ss.str());
    }
}

bool ModuleBase::OnInitialize() {
    LogInfo(m_moduleName + " module initialized");
    return true;
}

void ModuleBase::OnConfigure(const std::map<std::string,std::string>& config_entries) {
    auto it = config_entries.find("Port");
    if (it != config_entries.end()) {
        m_port = std::stoi(it->second);
    }
    
    // Check for metrics configuration
    it = config_entries.find("EnableMetrics");
    if (it != config_entries.end()) {
        m_metricsEnabled = (it->second == "true" || it->second == "1");
    }
    
    // Check for throttling configuration
    it = config_entries.find("EnableThrottling");
    if (it != config_entries.end()) {
        m_throttlingEnabled = (it->second == "true" || it->second == "1");
    }
    
    it = config_entries.find("ConnectionRateLimit");
    if (it != config_entries.end()) {
        m_connectionRateLimit = std::stoul(it->second);
    }
}

void ModuleBase::OnRemoveConnection(const uint32_t ID) {
    if (m_metricsEnabled) {
        m_metrics.activeConnections--;
    }
    LogInfo("Connection " + std::to_string(ID) + " removed from " + m_moduleName);
}

void ModuleBase::SetConnectionRateLimit(uint32_t connectionsPerSecond) {
    m_connectionRateLimit = connectionsPerSecond;
    LogInfo("Connection rate limit set to " + std::to_string(connectionsPerSecond) + " per second");
}

bool ModuleBase::CheckConnectionThrottle() {
    if (!m_throttlingEnabled || m_connectionRateLimit == 0) {
        return true;
    }
    
    std::lock_guard<std::mutex> lock(m_throttleMutex);
    
    auto now = std::chrono::steady_clock::now();
    auto timeSinceLastConnection = std::chrono::duration_cast<std::chrono::milliseconds>(
        now - m_lastConnectionTime).count();
    
    // Calculate minimum time between connections (in milliseconds)
    uint32_t minTimeBetweenConnections = 1000 / m_connectionRateLimit;
    
    if (timeSinceLastConnection < minTimeBetweenConnections) {
        return false; // Throttle this connection
    }
    
    m_lastConnectionTime = now;
    return true;
}

void ModuleBase::LogInfo(const std::string& message) {
    std::cout << "[" << m_moduleName << "] INFO: " << message << std::endl;
}

void ModuleBase::LogWarning(const std::string& message) {
    std::cout << "[" << m_moduleName << "] WARNING: " << message << std::endl;
}

void ModuleBase::LogError(const std::string& message) {
    std::cerr << "[" << m_moduleName << "] ERROR: " << message << std::endl;
}

} // namespace sro

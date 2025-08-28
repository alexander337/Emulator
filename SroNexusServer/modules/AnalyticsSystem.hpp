#pragma once
#include <unordered_map>
#include <vector>
#include <chrono>
#include <atomic>
#include <mutex>
#include <thread>

namespace sro {

// Real-time analytics and monitoring system
class AnalyticsSystem {
public:
    // Metric types
    struct Metric {
        std::string name;
        std::string category;
        double value;
        std::chrono::steady_clock::time_point timestamp;
        std::unordered_map<std::string, std::string> tags;
    };

    // Time series data
    template<typename T>
    class TimeSeries {
    public:
        void Add(T value) {
            auto now = std::chrono::steady_clock::now();
            std::lock_guard<std::mutex> lock(m_mutex);
            m_data.push_back({now, value});
            
            // Keep only last hour of data
            auto cutoff = now - std::chrono::hours(1);
            m_data.erase(
                std::remove_if(m_data.begin(), m_data.end(),
                    [cutoff](const auto& item) { return item.first < cutoff; }),
                m_data.end()
            );
        }
        
        T GetLatest() const {
            std::lock_guard<std::mutex> lock(m_mutex);
            return m_data.empty() ? T{} : m_data.back().second;
        }
        
        std::vector<std::pair<std::chrono::steady_clock::time_point, T>> GetRange(
            std::chrono::steady_clock::time_point start,
            std::chrono::steady_clock::time_point end) const {
            
            std::lock_guard<std::mutex> lock(m_mutex);
            std::vector<std::pair<std::chrono::steady_clock::time_point, T>> result;
            
            for (const auto& item : m_data) {
                if (item.first >= start && item.first <= end) {
                    result.push_back(item);
                }
            }
            
            return result;
        }
        
        double GetAverage(std::chrono::seconds window) const {
            auto now = std::chrono::steady_clock::now();
            auto start = now - window;
            
            std::lock_guard<std::mutex> lock(m_mutex);
            double sum = 0;
            int count = 0;
            
            for (const auto& item : m_data) {
                if (item.first >= start) {
                    sum += item.second;
                    count++;
                }
            }
            
            return count > 0 ? sum / count : 0;
        }
        
    private:
        mutable std::mutex m_mutex;
        std::vector<std::pair<std::chrono::steady_clock::time_point, T>> m_data;
    };

    // Performance profiler
    class Profiler {
    public:
        class ScopedTimer {
        public:
            ScopedTimer(const std::string& name, Profiler* profiler)
                : m_name(name), m_profiler(profiler),
                  m_start(std::chrono::high_resolution_clock::now()) {}
            
            ~ScopedTimer() {
                auto end = std::chrono::high_resolution_clock::now();
                auto duration = std::chrono::duration_cast<std::chrono::microseconds>(end - m_start);
                m_profiler->RecordTiming(m_name, duration.count());
            }
            
        private:
            std::string m_name;
            Profiler* m_profiler;
            std::chrono::high_resolution_clock::time_point m_start;
        };
        
        void RecordTiming(const std::string& name, int64_t microseconds);
        double GetAverageTime(const std::string& name) const;
        std::vector<std::string> GetSlowOperations(double thresholdMs) const;
        
    private:
        std::unordered_map<std::string, TimeSeries<int64_t>> m_timings;
        mutable std::mutex m_mutex;
    };

    // Player behavior analytics
    struct PlayerAnalytics {
        uint32_t playerId;
        std::chrono::steady_clock::time_point sessionStart;
        std::chrono::seconds totalPlaytime;
        
        // Activity metrics
        uint32_t monstersKilled;
        uint32_t questsCompleted;
        uint32_t itemsCollected;
        uint32_t deathCount;
        uint32_t pvpKills;
        uint32_t pvpDeaths;
        
        // Economic metrics
        uint64_t goldEarned;
        uint64_t goldSpent;
        uint32_t itemsBought;
        uint32_t itemsSold;
        
        // Social metrics
        uint32_t messagesSent;
        uint32_t friendsAdded;
        uint32_t partiesJoined;
        uint32_t guildsJoined;
        
        // Progression metrics
        uint32_t levelsGained;
        uint32_t skillsLearned;
        float progressionRate;
        
        // Behavior patterns
        std::vector<std::string> frequentAreas;
        std::vector<uint32_t> preferredItems;
        std::string playstyle; // "casual", "hardcore", "social", "solo", "pvp", "pve"
    };

    // Server health monitoring
    struct ServerHealth {
        // Performance metrics
        float cpuUsage;
        float memoryUsage;
        float diskUsage;
        float networkBandwidth;
        
        // Game metrics
        uint32_t activeConnections;
        uint32_t activeThreads;
        float tickRate;
        float averageLatency;
        
        // Database metrics
        uint32_t dbConnectionsActive;
        uint32_t dbConnectionsIdle;
        float dbQueryTime;
        uint32_t dbSlowQueries;
        
        // Error metrics
        uint32_t errorCount;
        uint32_t warningCount;
        uint32_t crashCount;
        
        std::string status; // "healthy", "degraded", "critical"
    };

    // Predictive analytics
    class PredictiveAnalytics {
    public:
        // Player churn prediction
        float PredictChurnProbability(uint32_t playerId);
        std::vector<uint32_t> GetPlayersAtRisk();
        
        // Server load prediction
        float PredictServerLoad(std::chrono::hours futureTime);
        std::chrono::steady_clock::time_point PredictPeakTime();
        
        // Economy prediction
        float PredictItemPrice(uint32_t itemId, std::chrono::hours futureTime);
        float PredictInflation(std::chrono::days futureDays);
        
        // Event optimization
        std::string RecommendNextEvent();
        std::chrono::steady_clock::time_point RecommendEventTime();
        
    private:
        // Machine learning models
        void TrainChurnModel();
        void TrainLoadModel();
        void TrainEconomyModel();
    };

    // A/B testing framework
    class ABTesting {
    public:
        struct Experiment {
            std::string name;
            std::string description;
            std::vector<std::string> variants;
            std::unordered_map<uint32_t, std::string> assignments;
            std::unordered_map<std::string, double> results;
            std::chrono::steady_clock::time_point startTime;
            std::chrono::steady_clock::time_point endTime;
            bool active;
        };
        
        void CreateExperiment(const Experiment& experiment);
        std::string AssignVariant(const std::string& experiment, uint32_t playerId);
        void RecordConversion(const std::string& experiment, uint32_t playerId, double value);
        Experiment GetResults(const std::string& experiment);
        
    private:
        std::unordered_map<std::string, Experiment> m_experiments;
        std::mutex m_experimentMutex;
    };

public:
    static AnalyticsSystem& Instance() {
        static AnalyticsSystem instance;
        return instance;
    }
    
    // Metric recording
    void RecordMetric(const Metric& metric);
    void RecordEvent(const std::string& event, const std::unordered_map<std::string, std::string>& properties);
    
    // Player analytics
    void StartPlayerSession(uint32_t playerId);
    void EndPlayerSession(uint32_t playerId);
    void UpdatePlayerAnalytics(uint32_t playerId, const std::string& metric, double value);
    PlayerAnalytics GetPlayerAnalytics(uint32_t playerId) const;
    
    // Server monitoring
    ServerHealth GetServerHealth() const;
    void UpdateServerHealth(const ServerHealth& health);
    
    // Profiling
    Profiler& GetProfiler() { return m_profiler; }
    
    // Predictive analytics
    PredictiveAnalytics& GetPredictive() { return m_predictive; }
    
    // A/B testing
    ABTesting& GetABTesting() { return m_abTesting; }
    
    // Reporting
    void GenerateDailyReport();
    void GenerateWeeklyReport();
    void GenerateMonthlyReport();
    
    // Data export
    void ExportToCSV(const std::string& filename);
    void ExportToJSON(const std::string& filename);
    void StreamToKafka(const std::string& topic);
    
private:
    AnalyticsSystem();
    ~AnalyticsSystem();
    
    // Storage
    std::unordered_map<std::string, TimeSeries<double>> m_metrics;
    std::unordered_map<uint32_t, PlayerAnalytics> m_playerAnalytics;
    TimeSeries<ServerHealth> m_serverHealth;
    
    // Components
    Profiler m_profiler;
    PredictiveAnalytics m_predictive;
    ABTesting m_abTesting;
    
    // Background processing
    std::thread m_processingThread;
    std::atomic<bool> m_running;
    
    void ProcessingLoop();
    void AggregateMetrics();
    void CleanupOldData();
};

// Macros for easy profiling
#define PROFILE_SCOPE(name) \
    sro::AnalyticsSystem::Profiler::ScopedTimer _timer(name, &sro::AnalyticsSystem::Instance().GetProfiler())

#define RECORD_METRIC(name, value) \
    sro::AnalyticsSystem::Instance().RecordMetric({name, "", value, std::chrono::steady_clock::now(), {}})

} // namespace sro

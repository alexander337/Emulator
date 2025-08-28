#pragma once
#include <memory>
#include <vector>
#include <unordered_map>
#include <functional>
#include <chrono>
#include <random>

namespace sro {

// Advanced AI system for NPCs and mobs
class AISystem {
public:
    enum class AIState {
        IDLE,
        PATROL,
        CHASE,
        COMBAT,
        FLEE,
        RETURN_HOME,
        DEAD
    };

    enum class AIPersonality {
        AGGRESSIVE,    // Attacks on sight
        DEFENSIVE,     // Attacks when attacked
        PASSIVE,       // Never attacks
        TERRITORIAL,   // Attacks when territory invaded
        GUARDIAN,      // Protects specific area/NPC
        HUNTER,        // Actively hunts players
        COWARD         // Flees when health low
    };

    struct AIBehavior {
        AIPersonality personality;
        float aggroRange;
        float chaseRange;
        float attackRange;
        float fleeHealthPercent;
        float returnHomeDistance;
        uint32_t thinkInterval; // milliseconds
        
        // Advanced behaviors
        bool canCallForHelp;
        float helpCallRange;
        bool canUseSkills;
        std::vector<uint32_t> availableSkills;
        
        // Group behaviors
        bool isPackLeader;
        bool followsPackLeader;
        uint32_t packId;
        
        // Learning capabilities
        bool canLearn;
        float learningRate;
        std::unordered_map<uint32_t, float> playerThreatLevels;
    };

    class AIEntity {
    public:
        AIEntity(uint32_t id, const AIBehavior& behavior);
        virtual ~AIEntity() = default;
        
        void Update(float deltaTime);
        void SetState(AIState newState);
        AIState GetState() const { return m_state; }
        
        // Decision making
        void EvaluateSituation();
        void SelectTarget();
        void PlanAction();
        void ExecuteAction();
        
        // Learning system
        void LearnFromCombat(uint32_t playerId, bool won);
        void AdaptStrategy();
        
        // Group coordination
        void CoordinateWithPack();
        void CallForHelp();
        void RespondToHelpCall(uint32_t callerId);
        
    protected:
        uint32_t m_id;
        AIState m_state;
        AIBehavior m_behavior;
        
        // Current situation
        uint32_t m_targetId;
        float m_distanceToTarget;
        float m_healthPercent;
        std::vector<uint32_t> m_nearbyEnemies;
        std::vector<uint32_t> m_nearbyAllies;
        
        // Decision weights (learned)
        float m_aggressiveness;
        float m_caution;
        float m_teamwork;
        
        // Timers
        std::chrono::steady_clock::time_point m_lastThink;
        std::chrono::steady_clock::time_point m_lastSkillUse;
        std::chrono::steady_clock::time_point m_combatStartTime;
        
        // Pathfinding
        std::vector<std::pair<float, float>> m_currentPath;
        size_t m_pathIndex;
        
        // Neural network for advanced decision making
        class NeuralNetwork {
        public:
            NeuralNetwork(size_t inputSize, size_t hiddenSize, size_t outputSize);
            std::vector<float> Forward(const std::vector<float>& input);
            void Train(const std::vector<float>& input, const std::vector<float>& target);
            
        private:
            std::vector<std::vector<float>> m_weightsInputHidden;
            std::vector<std::vector<float>> m_weightsHiddenOutput;
            std::vector<float> m_biasHidden;
            std::vector<float> m_biasOutput;
        };
        
        std::unique_ptr<NeuralNetwork> m_brain;
    };

    // Swarm intelligence for group behaviors
    class SwarmIntelligence {
    public:
        struct SwarmBehavior {
            float separation;    // Avoid crowding
            float alignment;     // Steer towards average heading
            float cohesion;      // Steer towards average position
            float targetWeight;  // Weight for moving towards target
        };
        
        static std::pair<float, float> CalculateSwarmMovement(
            const std::vector<AIEntity*>& swarm,
            const AIEntity* entity,
            const SwarmBehavior& behavior
        );
    };

    // Tactical AI for advanced combat
    class TacticalAI {
    public:
        enum class Tactic {
            FRONTAL_ASSAULT,
            FLANKING,
            AMBUSH,
            HIT_AND_RUN,
            DEFENSIVE_STAND,
            KITING,
            FOCUS_FIRE,
            DIVIDE_AND_CONQUER
        };
        
        static Tactic SelectTactic(
            const std::vector<AIEntity*>& allies,
            const std::vector<uint32_t>& enemies,
            const std::unordered_map<uint32_t, float>& enemyStrengths
        );
        
        static std::vector<std::pair<uint32_t, uint32_t>> AssignTargets(
            const std::vector<AIEntity*>& allies,
            const std::vector<uint32_t>& enemies,
            Tactic tactic
        );
    };

public:
    static AISystem& Instance() {
        static AISystem instance;
        return instance;
    }
    
    // Entity management
    void RegisterEntity(std::shared_ptr<AIEntity> entity);
    void UnregisterEntity(uint32_t entityId);
    std::shared_ptr<AIEntity> GetEntity(uint32_t entityId);
    
    // System update
    void Update(float deltaTime);
    
    // Global AI parameters
    void SetDifficulty(float difficulty); // 0.0 to 1.0
    void SetLearningEnabled(bool enabled);
    void SetSwarmBehaviorEnabled(bool enabled);
    
    // Analytics
    struct AIStats {
        uint32_t totalDecisions;
        uint32_t combatsWon;
        uint32_t combatsLost;
        float averageReactionTime;
        float averageCombatDuration;
        std::unordered_map<AIState, uint32_t> stateDistribution;
    };
    
    AIStats GetStatistics() const { return m_stats; }
    
private:
    AISystem() = default;
    ~AISystem() = default;
    
    std::unordered_map<uint32_t, std::shared_ptr<AIEntity>> m_entities;
    std::mutex m_entityMutex;
    
    // Global settings
    float m_difficulty;
    bool m_learningEnabled;
    bool m_swarmEnabled;
    
    // Statistics
    AIStats m_stats;
    
    // Random number generation
    std::mt19937 m_rng;
    std::uniform_real_distribution<float> m_distribution;
};

// Boss AI with phases and special mechanics
class BossAI : public AISystem::AIEntity {
public:
    struct Phase {
        float healthThreshold;
        std::vector<uint32_t> skills;
        float damageMultiplier;
        float defenseMultiplier;
        std::string phaseMessage;
        std::function<void()> phaseTransition;
    };
    
    BossAI(uint32_t id, const AISystem::AIBehavior& behavior);
    
    void AddPhase(const Phase& phase);
    void UpdatePhase();
    
    // Special boss mechanics
    void Enrage();
    void SummonMinions();
    void AreaDenial();
    void PhaseTransition();
    
private:
    std::vector<Phase> m_phases;
    size_t m_currentPhase;
    bool m_isEnraged;
    std::chrono::steady_clock::time_point m_lastSpecialAbility;
};

} // namespace sro

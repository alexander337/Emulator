#pragma once

#ifdef SRONEXUSCORE_EXPORTS
#define SRONEXUS_API __declspec(dllexport)
#else
#define SRONEXUS_API __declspec(dllimport)
#endif

#include <string>
#include <vector>
#include <memory>
#include <map>

namespace SroNexus {

    // Forward declarations
    class Item;
    class Monster;
    class Skill;
    class Scroll;

    // Enums
    enum class ServerType {
        vSRO,
        SRO_R,
        cSRO_R,
        thSRO,
        jSRO,
        BlackRogue,
        eSRO,
        kSRO
    };

    enum class ItemType {
        Weapon,
        Armor,
        Accessory,
        Consumable,
        Quest,
        Other
    };

    enum class MonsterType {
        Normal,
        Champion,
        Unique,
        Giant,
        Boss
    };

    // Item structure
    class SRONEXUS_API Item {
    public:
        int id;
        std::string code;
        std::string name;
        std::string displayName;
        ItemType type;
        int level;
        int physicalAttackMin;
        int physicalAttackMax;
        int magicalAttackMin;
        int magicalAttackMax;
        int physicalDefense;
        int magicalDefense;
        float hitRate;
        float parryRate;
        float criticalRate;
        
        // Alchemy options
        struct AlchemyOption {
            bool enabled;
            int minValue;
            int maxValue;
            float successRate;
        };
        
        std::map<std::string, AlchemyOption> alchemyOptions;
        
        // Plus system
        struct PlusSystem {
            int maxLevel;
            float successRate1_7;
            float successRate8_12;
            float successRate13_15;
            float successRate16_18;
            float successRate19_20;
            bool destroyOnFail;
        } plusSystem;
        
        Item();
        ~Item();
    };

    // Monster structure
    class SRONEXUS_API Monster {
    public:
        int id;
        std::string code;
        std::string name;
        std::string displayName;
        MonsterType type;
        int level;
        long long healthPoints;
        long long manaPoints;
        int physicalAttackMin;
        int physicalAttackMax;
        int magicalAttackMin;
        int magicalAttackMax;
        int physicalDefense;
        int magicalDefense;
        float hitRate;
        float criticalRate;
        
        // Behavior
        std::string aggressionType;
        int detectionRange;
        int chaseRange;
        int respawnTime;
        
        // Special behaviors
        bool isPackHunter;
        bool isBerserker;
        bool isNightHunter;
        bool isMagicResistant;
        
        // Rewards
        long long expReward;
        long long goldMin;
        long long goldMax;
        
        Monster();
        ~Monster();
    };

    // Core Engine Interface
    class SRONEXUS_API ISroNexusCore {
    public:
        virtual ~ISroNexusCore() = default;
        
        // Initialization
        virtual bool Initialize(const std::string& configPath) = 0;
        virtual void Shutdown() = 0;
        
        // Database operations
        virtual bool ConnectDatabase(const std::string& connectionString) = 0;
        virtual void DisconnectDatabase() = 0;
        virtual bool IsDatabaseConnected() const = 0;
        
        // Item management
        virtual std::vector<Item> GetAllItems() = 0;
        virtual Item GetItem(int id) = 0;
        virtual bool CreateItem(const Item& item) = 0;
        virtual bool UpdateItem(const Item& item) = 0;
        virtual bool DeleteItem(int id) = 0;
        virtual bool BulkUpdateItems(const std::vector<Item>& items) = 0;
        
        // Monster management
        virtual std::vector<Monster> GetAllMonsters() = 0;
        virtual Monster GetMonster(int id) = 0;
        virtual bool CreateMonster(const Monster& monster) = 0;
        virtual bool UpdateMonster(const Monster& monster) = 0;
        virtual bool DeleteMonster(int id) = 0;
        
        // Import/Export
        virtual bool ImportFromFile(const std::string& filePath, ServerType serverType) = 0;
        virtual bool ExportToFile(const std::string& filePath, const std::string& format) = 0;
        virtual bool ImportFromSourceCode(const std::string& sourcePath) = 0;
        
        // Feature management
        virtual std::map<std::string, bool> GetAllFeatures() = 0;
        virtual bool SetFeatureEnabled(const std::string& featureCode, bool enabled) = 0;
        virtual bool IsFeatureEnabled(const std::string& featureCode) = 0;
        
        // Server management
        virtual bool StartServer() = 0;
        virtual bool StopServer() = 0;
        virtual bool RestartServer() = 0;
        virtual bool IsServerRunning() const = 0;
        virtual std::string GetServerStatus() const = 0;
        
        // Value translation
        virtual std::string TranslateMagicOption(long long code) = 0;
        virtual long long GetMagicOptionCode(const std::string& name) = 0;
        virtual float ConvertPercentage(float dbValue) = 0;
        virtual float ConvertToDbPercentage(float displayValue) = 0;
    };

    // Factory function
    extern "C" SRONEXUS_API ISroNexusCore* CreateSroNexusCore();
    extern "C" SRONEXUS_API void DestroySroNexusCore(ISroNexusCore* core);
}

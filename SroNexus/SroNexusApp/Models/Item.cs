using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SroNexus.Models
{
    /// <summary>
    /// Item model for SroNexus
    /// </summary>
    public class Item : INotifyPropertyChanged
    {
        private int _id;
        private string _code = string.Empty;
        private string _name = string.Empty;
        private string _displayName = string.Empty;
        private string _description = string.Empty;
        private ItemType _type;
        private string _itemTypeDisplay = string.Empty;
        private int _level;
        private int _physicalAttackMin;
        private int _physicalAttackMax;
        private int _magicalAttackMin;
        private int _magicalAttackMax;
        private int _physicalDefense;
        private int _magicalDefense;
        private double _hitRatePercent;
        private double _parryRatePercent;
        private double _criticalRatePercent;
        private bool _isActive = true;
        private bool _isCustom = false;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Code
        {
            get => _code;
            set { _code = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public ItemType Type
        {
            get => _type;
            set 
            { 
                _type = value; 
                ItemTypeDisplay = GetItemTypeDisplay(value);
                OnPropertyChanged(); 
            }
        }

        public string ItemTypeDisplay
        {
            get => _itemTypeDisplay;
            set { _itemTypeDisplay = value; OnPropertyChanged(); }
        }

        public int Level
        {
            get => _level;
            set { _level = value; OnPropertyChanged(); }
        }

        public int PhysicalAttackMin
        {
            get => _physicalAttackMin;
            set { _physicalAttackMin = value; OnPropertyChanged(); }
        }

        public int PhysicalAttackMax
        {
            get => _physicalAttackMax;
            set { _physicalAttackMax = value; OnPropertyChanged(); }
        }

        public int MagicalAttackMin
        {
            get => _magicalAttackMin;
            set { _magicalAttackMin = value; OnPropertyChanged(); }
        }

        public int MagicalAttackMax
        {
            get => _magicalAttackMax;
            set { _magicalAttackMax = value; OnPropertyChanged(); }
        }

        public int PhysicalDefense
        {
            get => _physicalDefense;
            set { _physicalDefense = value; OnPropertyChanged(); }
        }

        public int MagicalDefense
        {
            get => _magicalDefense;
            set { _magicalDefense = value; OnPropertyChanged(); }
        }

        public double HitRatePercent
        {
            get => _hitRatePercent;
            set { _hitRatePercent = value; OnPropertyChanged(); }
        }

        public double ParryRatePercent
        {
            get => _parryRatePercent;
            set { _parryRatePercent = value; OnPropertyChanged(); }
        }

        public double CriticalRatePercent
        {
            get => _criticalRatePercent;
            set { _criticalRatePercent = value; OnPropertyChanged(); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        public bool IsCustom
        {
            get => _isCustom;
            set { _isCustom = value; OnPropertyChanged(); }
        }

        // Alchemy Options
        public Dictionary<string, AlchemyOption> AlchemyOptions { get; set; } = new Dictionary<string, AlchemyOption>();

        // Plus System
        public PlusSystem PlusSystemConfig { get; set; } = new PlusSystem();

        // Requirements
        public ItemRequirements Requirements { get; set; } = new ItemRequirements();

        // Economic Properties
        public ItemEconomics Economics { get; set; } = new ItemEconomics();

        private string GetItemTypeDisplay(ItemType type)
        {
            return type switch
            {
                ItemType.Weapon => "Weapon",
                ItemType.Armor => "Armor",
                ItemType.Accessory => "Accessory",
                ItemType.Consumable => "Consumable",
                ItemType.Quest => "Quest Item",
                ItemType.Other => "Other",
                _ => "Unknown"
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum ItemType
    {
        Weapon,
        Armor,
        Accessory,
        Consumable,
        Quest,
        Other
    }

    public class AlchemyOption
    {
        public bool Enabled { get; set; }
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public double SuccessRate { get; set; }
    }

    public class PlusSystem
    {
        public int MaxLevel { get; set; } = 20;
        public double SuccessRate1_7 { get; set; } = 95.0;
        public double SuccessRate8_12 { get; set; } = 85.0;
        public double SuccessRate13_15 { get; set; } = 75.0;
        public double SuccessRate16_18 { get; set; } = 65.0;
        public double SuccessRate19_20 { get; set; } = 55.0;
        public bool DestroyOnFail { get; set; } = true;
        public int PhysicalBonusPerPlus { get; set; } = 15;
        public int MagicalBonusPerPlus { get; set; } = 10;
    }

    public class ItemRequirements
    {
        public int StrRequired { get; set; }
        public int IntRequired { get; set; }
        public string RaceRestriction { get; set; } = "Any";
        public string GenderRestriction { get; set; } = "Any";
        public string JobRestriction { get; set; } = "Any";
    }

    public class ItemEconomics
    {
        public long BuyPrice { get; set; }
        public long SellPrice { get; set; }
        public long RepairCostBase { get; set; }
        public double RepairCostMultiplier { get; set; } = 1.0;
        public long MarketValue { get; set; }
    }
}

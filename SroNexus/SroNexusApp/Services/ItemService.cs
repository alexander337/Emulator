using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using SroNexus.Models;
using SroNexus.Data;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace SroNexus.Services
{
    public interface IItemService
    {
        Task<List<Item>> GetAllItemsAsync();
        Task<Item?> GetItemAsync(int id);
        Task<bool> CreateItemAsync(Item item);
        Task<bool> UpdateItemAsync(Item item);
        Task<bool> DeleteItemAsync(int id);
        Task<Item?> DuplicateItemAsync(Item item);
        Task<bool> ImportItemsAsync(string filePath);
        Task<bool> ExportItemsAsync(string filePath, string format);
        Task<int> BulkUpdateItemsAsync(List<Item> items);
    }

    public class ItemService : IItemService
    {
        private readonly IDatabaseService _databaseService;
        private readonly INexusCore _nexusCore;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger = Log.ForContext<ItemService>();

        public ItemService(IDatabaseService databaseService, INexusCore nexusCore, IConfiguration configuration)
        {
            _databaseService = databaseService;
            _nexusCore = nexusCore;
            _configuration = configuration;
        }

        public async Task<List<Item>> GetAllItemsAsync()
        {
            try
            {
                _logger.Information("Fetching all items");
                
                // Get items from C++ core
                var coreItems = _nexusCore.GetAllItems();
                
                // Convert to C# models
                var items = new List<Item>();
                foreach (var coreItem in coreItems)
                {
                    items.Add(ConvertFromCore(coreItem));
                }
                
                return await Task.FromResult(items);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error fetching items");
                return new List<Item>();
            }
        }

        public async Task<Item?> GetItemAsync(int id)
        {
            try
            {
                var coreItem = _nexusCore.GetItem(id);
                if (coreItem != null)
                {
                    return await Task.FromResult(ConvertFromCore(coreItem));
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error fetching item {ItemId}", id);
                return null;
            }
        }

        public async Task<bool> CreateItemAsync(Item item)
        {
            try
            {
                _logger.Information("Creating new item: {ItemName}", item.Name);
                
                // Convert to core item
                var coreItem = ConvertToCore(item);
                
                // Create in core
                var success = _nexusCore.CreateItem(coreItem);
                
                if (success)
                {
                    _logger.Information("Item created successfully: {ItemName}", item.Name);
                }
                
                return await Task.FromResult(success);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error creating item");
                return false;
            }
        }

        public async Task<bool> UpdateItemAsync(Item item)
        {
            try
            {
                _logger.Information("Updating item: {ItemId} - {ItemName}", item.Id, item.Name);
                
                var coreItem = ConvertToCore(item);
                var success = _nexusCore.UpdateItem(coreItem);
                
                if (success)
                {
                    _logger.Information("Item updated successfully: {ItemId}", item.Id);
                }
                
                return await Task.FromResult(success);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error updating item {ItemId}", item.Id);
                return false;
            }
        }

        public async Task<bool> DeleteItemAsync(int id)
        {
            try
            {
                _logger.Information("Deleting item: {ItemId}", id);
                
                var success = _nexusCore.DeleteItem(id);
                
                if (success)
                {
                    _logger.Information("Item deleted successfully: {ItemId}", id);
                }
                
                return await Task.FromResult(success);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error deleting item {ItemId}", id);
                return false;
            }
        }

        public async Task<Item?> DuplicateItemAsync(Item item)
        {
            try
            {
                var newItem = new Item
                {
                    Code = item.Code + "_COPY",
                    Name = item.Name + " (Copy)",
                    DisplayName = item.DisplayName + " (Copy)",
                    Description = item.Description,
                    Type = item.Type,
                    Level = item.Level,
                    PhysicalAttackMin = item.PhysicalAttackMin,
                    PhysicalAttackMax = item.PhysicalAttackMax,
                    MagicalAttackMin = item.MagicalAttackMin,
                    MagicalAttackMax = item.MagicalAttackMax,
                    PhysicalDefense = item.PhysicalDefense,
                    MagicalDefense = item.MagicalDefense,
                    HitRatePercent = item.HitRatePercent,
                    ParryRatePercent = item.ParryRatePercent,
                    CriticalRatePercent = item.CriticalRatePercent,
                    IsCustom = true
                };
                
                // Copy alchemy options
                foreach (var alchemy in item.AlchemyOptions)
                {
                    newItem.AlchemyOptions[alchemy.Key] = new AlchemyOption
                    {
                        Enabled = alchemy.Value.Enabled,
                        MinValue = alchemy.Value.MinValue,
                        MaxValue = alchemy.Value.MaxValue,
                        SuccessRate = alchemy.Value.SuccessRate
                    };
                }
                
                // Copy plus system
                newItem.PlusSystemConfig = new PlusSystem
                {
                    MaxLevel = item.PlusSystemConfig.MaxLevel,
                    SuccessRate1_7 = item.PlusSystemConfig.SuccessRate1_7,
                    SuccessRate8_12 = item.PlusSystemConfig.SuccessRate8_12,
                    SuccessRate13_15 = item.PlusSystemConfig.SuccessRate13_15,
                    SuccessRate16_18 = item.PlusSystemConfig.SuccessRate16_18,
                    SuccessRate19_20 = item.PlusSystemConfig.SuccessRate19_20,
                    DestroyOnFail = item.PlusSystemConfig.DestroyOnFail
                };
                
                if (await CreateItemAsync(newItem))
                {
                    return newItem;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error duplicating item");
                return null;
            }
        }

        public async Task<bool> ImportItemsAsync(string filePath)
        {
            try
            {
                _logger.Information("Importing items from: {FilePath}", filePath);
                
                // Determine server type from config
                var serverType = _configuration["ServerSettings:ServerType"] ?? "vSRO";
                
                var success = _nexusCore.ImportFromFile(filePath, serverType);
                
                if (success)
                {
                    _logger.Information("Items imported successfully from: {FilePath}", filePath);
                }
                
                return await Task.FromResult(success);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error importing items from {FilePath}", filePath);
                return false;
            }
        }

        public async Task<bool> ExportItemsAsync(string filePath, string format)
        {
            try
            {
                _logger.Information("Exporting items to: {FilePath} in format: {Format}", filePath, format);
                
                var success = _nexusCore.ExportToFile(filePath, format);
                
                if (success)
                {
                    _logger.Information("Items exported successfully to: {FilePath}", filePath);
                }
                
                return await Task.FromResult(success);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error exporting items to {FilePath}", filePath);
                return false;
            }
        }

        public async Task<int> BulkUpdateItemsAsync(List<Item> items)
        {
            try
            {
                _logger.Information("Bulk updating {Count} items", items.Count);
                
                var coreItems = items.Select(ConvertToCore).ToList();
                var success = _nexusCore.BulkUpdateItems(coreItems);
                
                if (success)
                {
                    _logger.Information("Bulk update completed for {Count} items", items.Count);
                    return await Task.FromResult(items.Count);
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error bulk updating items");
                return 0;
            }
        }

        private Item ConvertFromCore(dynamic coreItem)
        {
            var item = new Item
            {
                Id = coreItem.Id,
                Code = coreItem.Code,
                Name = coreItem.Name,
                DisplayName = coreItem.DisplayName,
                Level = coreItem.Level,
                PhysicalAttackMin = coreItem.PhysicalAttackMin,
                PhysicalAttackMax = coreItem.PhysicalAttackMax,
                MagicalAttackMin = coreItem.MagicalAttackMin,
                MagicalAttackMax = coreItem.MagicalAttackMax,
                PhysicalDefense = coreItem.PhysicalDefense,
                MagicalDefense = coreItem.MagicalDefense,
                HitRatePercent = coreItem.HitRate,
                ParryRatePercent = coreItem.ParryRate,
                CriticalRatePercent = coreItem.CriticalRate
            };
            
            // Convert alchemy options
            if (coreItem.AlchemyOptions != null)
            {
                foreach (var option in coreItem.AlchemyOptions)
                {
                    item.AlchemyOptions[option.Key] = new AlchemyOption
                    {
                        Enabled = option.Value.Enabled,
                        MinValue = option.Value.MinValue,
                        MaxValue = option.Value.MaxValue,
                        SuccessRate = option.Value.SuccessRate
                    };
                }
            }
            
            // Convert plus system
            if (coreItem.PlusSystem != null)
            {
                item.PlusSystemConfig = new PlusSystem
                {
                    MaxLevel = coreItem.PlusSystem.MaxLevel,
                    SuccessRate1_7 = coreItem.PlusSystem.SuccessRate1_7,
                    SuccessRate8_12 = coreItem.PlusSystem.SuccessRate8_12,
                    SuccessRate13_15 = coreItem.PlusSystem.SuccessRate13_15,
                    SuccessRate16_18 = coreItem.PlusSystem.SuccessRate16_18,
                    SuccessRate19_20 = coreItem.PlusSystem.SuccessRate19_20,
                    DestroyOnFail = coreItem.PlusSystem.DestroyOnFail
                };
            }
            
            return item;
        }

        private dynamic ConvertToCore(Item item)
        {
            // This would convert C# Item to C++ Item structure
            // For now, returning a dynamic object that represents the structure
            return new
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                DisplayName = item.DisplayName,
                Type = (int)item.Type,
                Level = item.Level,
                PhysicalAttackMin = item.PhysicalAttackMin,
                PhysicalAttackMax = item.PhysicalAttackMax,
                MagicalAttackMin = item.MagicalAttackMin,
                MagicalAttackMax = item.MagicalAttackMax,
                PhysicalDefense = item.PhysicalDefense,
                MagicalDefense = item.MagicalDefense,
                HitRate = (float)item.HitRatePercent,
                ParryRate = (float)item.ParryRatePercent,
                CriticalRate = (float)item.CriticalRatePercent,
                AlchemyOptions = item.AlchemyOptions,
                PlusSystem = item.PlusSystemConfig
            };
        }
    }
}

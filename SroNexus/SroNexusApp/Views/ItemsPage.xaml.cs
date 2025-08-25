using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SroNexus.Models;
using SroNexus.ViewModels;

namespace SroNexus.Views
{
    /// <summary>
    /// Items management page
    /// </summary>
    public partial class ItemsPage : Page
    {
        private readonly ItemsViewModel _viewModel;
        private Item? _selectedItem;

        public ItemsPage(ItemsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            LoadItems();
        }

        private async void LoadItems()
        {
            await _viewModel.LoadItemsAsync();
            ItemsDataGrid.ItemsSource = _viewModel.Items;
            UpdateItemCount();
        }

        private void UpdateItemCount()
        {
            ItemCountText.Text = $"Total: {_viewModel.Items?.Count ?? 0} items";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterItems();
        }

        private void TypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterItems();
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            FilterItems();
        }

        private void FilterItems()
        {
            if (_viewModel.Items == null) return;

            var filtered = _viewModel.Items.AsEnumerable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                var search = SearchBox.Text.ToLower();
                filtered = filtered.Where(i => 
                    i.Name.ToLower().Contains(search) || 
                    i.DisplayName.ToLower().Contains(search) ||
                    i.Code.ToLower().Contains(search));
            }

            // Type filter
            if (TypeFilter.SelectedIndex > 0)
            {
                var type = (ItemType)(TypeFilter.SelectedIndex - 1);
                filtered = filtered.Where(i => i.Type == type);
            }

            // Level filter
            if (int.TryParse(LevelMinFilter.Text, out int minLevel))
            {
                filtered = filtered.Where(i => i.Level >= minLevel);
            }
            if (int.TryParse(LevelMaxFilter.Text, out int maxLevel))
            {
                filtered = filtered.Where(i => i.Level <= maxLevel);
            }

            ItemsDataGrid.ItemsSource = new ObservableCollection<Item>(filtered);
            UpdateItemCount();
        }

        private void ItemsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedItem = ItemsDataGrid.SelectedItem as Item;
            if (_selectedItem != null)
            {
                LoadItemToEditor(_selectedItem);
            }
        }

        private void LoadItemToEditor(Item item)
        {
            ItemNameBox.Text = item.Name;
            ItemCodeBox.Text = item.Code;
            DisplayNameBox.Text = item.DisplayName;
            LevelBox.Text = item.Level.ToString();
            ItemTypeBox.SelectedIndex = (int)item.Type;
            DescriptionBox.Text = item.Description;

            PhysicalAttackMinBox.Text = item.PhysicalAttackMin.ToString();
            PhysicalAttackMaxBox.Text = item.PhysicalAttackMax.ToString();
            MagicalAttackMinBox.Text = item.MagicalAttackMin.ToString();
            MagicalAttackMaxBox.Text = item.MagicalAttackMax.ToString();
            PhysicalDefenseBox.Text = item.PhysicalDefense.ToString();
            MagicalDefenseBox.Text = item.MagicalDefense.ToString();
            HitRateBox.Text = item.HitRatePercent.ToString("F2");
            ParryRateBox.Text = item.ParryRatePercent.ToString("F2");
            CriticalRateBox.Text = item.CriticalRatePercent.ToString("F2");

            // Load alchemy options
            AlchemyEnabledCheck.IsChecked = item.AlchemyOptions.Any(a => a.Value.Enabled);
            
            // Load plus system
            if (item.PlusSystemConfig != null)
            {
                PlusEnabledCheck.IsChecked = true;
                MaxPlusLevelBox.Text = item.PlusSystemConfig.MaxLevel.ToString();
                DestroyOnFailCheck.IsChecked = item.PlusSystemConfig.DestroyOnFail;
                SuccessRate1_7Box.Text = item.PlusSystemConfig.SuccessRate1_7.ToString("F1");
                SuccessRate8_12Box.Text = item.PlusSystemConfig.SuccessRate8_12.ToString("F1");
                SuccessRate13_15Box.Text = item.PlusSystemConfig.SuccessRate13_15.ToString("F1");
                SuccessRate16_20Box.Text = item.PlusSystemConfig.SuccessRate19_20.ToString("F1");
            }
        }

        private async void SaveItem_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null)
            {
                _selectedItem = new Item();
            }

            // Update item from editor
            _selectedItem.Name = ItemNameBox.Text;
            _selectedItem.Code = ItemCodeBox.Text;
            _selectedItem.DisplayName = DisplayNameBox.Text;
            _selectedItem.Level = int.TryParse(LevelBox.Text, out int level) ? level : 1;
            _selectedItem.Type = (ItemType)ItemTypeBox.SelectedIndex;
            _selectedItem.Description = DescriptionBox.Text;

            _selectedItem.PhysicalAttackMin = int.TryParse(PhysicalAttackMinBox.Text, out int pMin) ? pMin : 0;
            _selectedItem.PhysicalAttackMax = int.TryParse(PhysicalAttackMaxBox.Text, out int pMax) ? pMax : 0;
            _selectedItem.MagicalAttackMin = int.TryParse(MagicalAttackMinBox.Text, out int mMin) ? mMin : 0;
            _selectedItem.MagicalAttackMax = int.TryParse(MagicalAttackMaxBox.Text, out int mMax) ? mMax : 0;
            _selectedItem.PhysicalDefense = int.TryParse(PhysicalDefenseBox.Text, out int pDef) ? pDef : 0;
            _selectedItem.MagicalDefense = int.TryParse(MagicalDefenseBox.Text, out int mDef) ? mDef : 0;
            _selectedItem.HitRatePercent = double.TryParse(HitRateBox.Text, out double hit) ? hit : 0;
            _selectedItem.ParryRatePercent = double.TryParse(ParryRateBox.Text, out double parry) ? parry : 0;
            _selectedItem.CriticalRatePercent = double.TryParse(CriticalRateBox.Text, out double crit) ? crit : 0;

            // Save plus system
            if (PlusEnabledCheck.IsChecked == true)
            {
                _selectedItem.PlusSystemConfig.MaxLevel = int.TryParse(MaxPlusLevelBox.Text, out int maxPlus) ? maxPlus : 20;
                _selectedItem.PlusSystemConfig.DestroyOnFail = DestroyOnFailCheck.IsChecked == true;
                _selectedItem.PlusSystemConfig.SuccessRate1_7 = double.TryParse(SuccessRate1_7Box.Text, out double s1) ? s1 : 95;
                _selectedItem.PlusSystemConfig.SuccessRate8_12 = double.TryParse(SuccessRate8_12Box.Text, out double s2) ? s2 : 85;
                _selectedItem.PlusSystemConfig.SuccessRate13_15 = double.TryParse(SuccessRate13_15Box.Text, out double s3) ? s3 : 75;
                _selectedItem.PlusSystemConfig.SuccessRate19_20 = double.TryParse(SuccessRate16_20Box.Text, out double s4) ? s4 : 55;
            }

            await _viewModel.SaveItemAsync(_selectedItem);
            MessageBox.Show("Item saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadItems();
        }

        private async void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete '{_selectedItem.DisplayName}'?", 
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                await _viewModel.DeleteItemAsync(_selectedItem.Id);
                MessageBox.Show("Item deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ResetForm();
                LoadItems();
            }
        }

        private void ResetForm_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private void ResetForm()
        {
            _selectedItem = null;
            ItemNameBox.Clear();
            ItemCodeBox.Clear();
            DisplayNameBox.Clear();
            LevelBox.Clear();
            ItemTypeBox.SelectedIndex = 0;
            DescriptionBox.Clear();
            
            PhysicalAttackMinBox.Clear();
            PhysicalAttackMaxBox.Clear();
            MagicalAttackMinBox.Clear();
            MagicalAttackMaxBox.Clear();
            PhysicalDefenseBox.Clear();
            MagicalDefenseBox.Clear();
            HitRateBox.Clear();
            ParryRateBox.Clear();
            CriticalRateBox.Clear();
            
            AlchemyEnabledCheck.IsChecked = false;
            PlusEnabledCheck.IsChecked = false;
            
            ItemsDataGrid.SelectedItem = null;
        }

        private void CreateNew_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
            _selectedItem = new Item { IsCustom = true };
        }

        private async void Duplicate_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null)
            {
                MessageBox.Show("Please select an item to duplicate.", "No Selection", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newItem = await _viewModel.DuplicateItemAsync(_selectedItem);
            if (newItem != null)
            {
                MessageBox.Show("Item duplicated successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadItems();
            }
        }

        private void BulkEdit_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open bulk edit window
            MessageBox.Show("Bulk Edit feature coming soon!", "Feature", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Supported Files|*.txt;*.sql;*.csv;*.json;*.xml|All Files|*.*",
                Title = "Import Items"
            };

            if (dialog.ShowDialog() == true)
            {
                await _viewModel.ImportItemsAsync(dialog.FileName);
                MessageBox.Show("Items imported successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadItems();
            }
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON Files|*.json|SQL Files|*.sql|CSV Files|*.csv|All Files|*.*",
                Title = "Export Items",
                FileName = "items_export"
            };

            if (dialog.ShowDialog() == true)
            {
                await _viewModel.ExportItemsAsync(dialog.FileName);
                MessageBox.Show("Items exported successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}

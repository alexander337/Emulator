using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using MahApps.Metro.Controls;
using Microsoft.Extensions.DependencyInjection;
using SroNexus.Views;
using SroNexus.ViewModels;
using System.Windows.Threading;

namespace SroNexus
{
    /// <summary>
    /// Main Window for SroNexus Complete Server Emulator
    /// Manages both the eSRO server and the management interface
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SroNexusServerManager _serverManager;
        private readonly DispatcherTimer _statusTimer;
        private int _onlinePlayerCount = 0;

        public MainWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _serverManager = _serviceProvider.GetRequiredService<SroNexusServerManager>();
            
            // Setup status update timer
            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _statusTimer.Tick += UpdateServerStatus;
            _statusTimer.Start();
            
            // Load dashboard by default
            NavigateTo("Dashboard");
            
            // Check for auto-start
            CheckAutoStart();
        }

        private async void CheckAutoStart()
        {
            var config = _serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            if (config.GetValue<bool>("ServerSettings:AutoStart", false))
            {
                await Task.Delay(2000); // Wait for UI to initialize
                await StartServers();
            }
        }

        private void NavigationTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                string tabName = radioButton.Name.Replace("Tab", "");
                NavigateTo(tabName);
            }
        }

        private void NavigateTo(string pageName)
        {
            Page? page = pageName switch
            {
                "Dashboard" => new DashboardPage(_serviceProvider.GetRequiredService<DashboardViewModel>()),
                "Items" => new ItemsPage(_serviceProvider.GetRequiredService<ItemsViewModel>()),
                "Monsters" => new MonstersPage(_serviceProvider.GetRequiredService<MonstersViewModel>()),
                "Skills" => new SkillsPage(_serviceProvider.GetRequiredService<SkillsViewModel>()),
                "Scrolls" => new ScrollsPage(_serviceProvider.GetRequiredService<ScrollsViewModel>()),
                "Features" => new FeaturesPage(_serviceProvider.GetRequiredService<FeaturesViewModel>()),
                "ImportExport" => new ImportExportPage(_serviceProvider.GetRequiredService<ImportExportViewModel>()),
                "Server" => new ServerManagementPage(_serviceProvider.GetRequiredService<ServerManagementViewModel>()),
                _ => null
            };

            if (page != null)
            {
                MainFrame.Navigate(page);
                UpdateStatus($"Navigated to {pageName}");
            }
        }

        private async void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_serverManager);
            settingsWindow.Owner = this;
            if (settingsWindow.ShowDialog() == true)
            {
                // Reload configuration if settings were saved
                await ReloadConfiguration();
            }
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = new HelpWindow();
            helpWindow.Owner = this;
            helpWindow.ShowDialog();
        }

        private async Task ReloadConfiguration()
        {
            UpdateStatus("Reloading configuration...");
            // Reload server configuration
            if (_serverManager.AreServersRunning())
            {
                var result = MessageBox.Show(
                    "Servers are running. Do you want to restart them to apply new settings?",
                    "Restart Servers",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    await RestartServers();
                }
            }
        }

        private async Task StartServers()
        {
            try
            {
                UpdateStatus("Starting servers...");
                var success = await _serverManager.StartAllServers();
                
                if (success)
                {
                    UpdateStatus("All servers started successfully!");
                    MessageBox.Show(
                        "Servers started successfully!\n\n" +
                        "Players can now connect to:\n" +
                        $"IP: {GetServerIP()}\n" +
                        "Port: 15779\n\n" +
                        "Make sure to configure your client accordingly.",
                        "Server Started",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    UpdateStatus("Failed to start servers");
                    MessageBox.Show(
                        "Failed to start one or more servers.\n" +
                        "Please check the logs for details.",
                        "Server Start Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
                MessageBox.Show(
                    $"Error starting servers:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task StopServers()
        {
            try
            {
                UpdateStatus("Stopping servers...");
                await _serverManager.StopAllServers();
                UpdateStatus("All servers stopped");
                _onlinePlayerCount = 0;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        private async Task RestartServers()
        {
            try
            {
                UpdateStatus("Restarting servers...");
                var success = await _serverManager.RestartAllServers();
                
                if (success)
                {
                    UpdateStatus("All servers restarted successfully!");
                }
                else
                {
                    UpdateStatus("Failed to restart servers");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        private void UpdateServerStatus(object? sender, EventArgs e)
        {
            var status = _serverManager.GetServerStatus();
            
            // Update server status in UI
            if (status.MasterServerRunning && status.GatewayServerRunning && status.AgentServerRunning)
            {
                ServerStatus.Text = $"Server: Running | Players: {_onlinePlayerCount}";
                ServerStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
            else if (status.MasterServerRunning || status.GatewayServerRunning || status.AgentServerRunning)
            {
                ServerStatus.Text = "Server: Partial";
                ServerStatus.Foreground = System.Windows.Media.Brushes.Yellow;
            }
            else
            {
                ServerStatus.Text = "Server: Offline";
                ServerStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
            
            // Update feature count
            if (StatusText != null && !StatusText.Text.StartsWith("Navigated"))
            {
                StatusText.Text = $"Features: {status.EnabledFeatures}/{status.TotalFeatures} enabled";
            }
        }

        private string GetServerIP()
        {
            var config = _serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            return config.GetValue<string>("ServerSettings:PublicIP") ?? "127.0.0.1";
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }

        public void UpdateDatabaseStatus(bool connected)
        {
            DatabaseStatus.Text = connected ? "Database: Connected" : "Database: Disconnected";
            DatabaseStatus.Foreground = connected ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.Red;
        }

        public void UpdatePlayerCount(int count)
        {
            _onlinePlayerCount = count;
        }

        protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_serverManager.AreServersRunning())
            {
                var result = MessageBox.Show(
                    "Servers are still running. Do you want to stop them before closing?",
                    "Servers Running",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                
                if (result == MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    UpdateStatus("Stopping servers before exit...");
                    await _serverManager.StopAllServers();
                    await Task.Delay(2000); // Give servers time to shut down
                    Application.Current.Shutdown();
                }
            }
            
            _statusTimer?.Stop();
            base.OnClosing(e);
        }
    }
}

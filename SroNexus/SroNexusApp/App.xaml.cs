using System;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SroNexus.Services;
using SroNexus.ViewModels;
using SroNexus.Data;

namespace SroNexus
{
    /// <summary>
    /// SroNexus - Ultimate Silkroad Online Emulator Management System
    /// Windows Desktop Application
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;
        public IServiceProvider Services => _serviceProvider!;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/sronexus-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("SroNexus Application Starting...");

            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Configure services
            var services = new ServiceCollection();
            ConfigureServices(services, configuration);
            _serviceProvider = services.BuildServiceProvider();

            // Initialize database
            var dbService = _serviceProvider.GetRequiredService<IDatabaseService>();
            dbService.InitializeDatabase();

            // Show main window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Configuration
            services.AddSingleton<IConfiguration>(configuration);

            // Services
            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddSingleton<IItemService, ItemService>();
            services.AddSingleton<IMonsterService, MonsterService>();
            services.AddSingleton<ISkillService, SkillService>();
            services.AddSingleton<IScrollService, ScrollService>();
            services.AddSingleton<IFeatureService, FeatureService>();
            services.AddSingleton<IImportExportService, ImportExportService>();
            services.AddSingleton<IServerManagementService, ServerManagementService>();
            services.AddSingleton<INexusCore, NexusCore>();

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<ItemsViewModel>();
            services.AddTransient<MonstersViewModel>();
            services.AddTransient<SkillsViewModel>();
            services.AddTransient<ScrollsViewModel>();
            services.AddTransient<FeaturesViewModel>();
            services.AddTransient<ImportExportViewModel>();
            services.AddTransient<ServerManagementViewModel>();
            services.AddTransient<DashboardViewModel>();

            // Windows
            services.AddSingleton<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("SroNexus Application Shutting Down");
            Log.CloseAndFlush();
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}

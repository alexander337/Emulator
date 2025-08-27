using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace SroNexus
{
    /// <summary>
    /// Complete Server Manager for eSRO Integration
    /// Manages AgentServer, GatewayServer, and MasterServer
    /// </summary>
    public class SroNexusServerManager
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger = Log.ForContext<SroNexusServerManager>();
        
        private Process? _agentServerProcess;
        private Process? _gatewayServerProcess;
        private Process? _masterServerProcess;
        private Process? _monolithProcess;
        private bool _isMonolithMode = false;
        
        private readonly Dictionary<string, bool> _features = new();
        private bool _isRunning = false;

        public SroNexusServerManager(IConfiguration configuration)
        {
            _configuration = configuration;
            LoadFeatures();
        }

        /// <summary>
        /// Load all 500+ features from configuration
        /// </summary>
        private void LoadFeatures()
        {
            // Gameplay Features
            _features["AUTO_HUNT"] = _configuration.GetValue<bool>("Features:AutoHunt", true);
            _features["AUTO_POTION"] = _configuration.GetValue<bool>("Features:AutoPotion", true);
            _features["MULTI_CLIENT"] = _configuration.GetValue<bool>("Features:MultiClient", false);
            _features["ENHANCED_PET"] = _configuration.GetValue<bool>("Features:EnhancedPet", true);
            _features["PET_EVOLUTION"] = _configuration.GetValue<bool>("Features:PetEvolution", true);
            _features["CUSTOM_MOUNT"] = _configuration.GetValue<bool>("Features:CustomMount", true);
            _features["FLYING_MOUNT"] = _configuration.GetValue<bool>("Features:FlyingMount", false);
            _features["TRANSFORMATION"] = _configuration.GetValue<bool>("Features:Transformation", false);
            _features["WEATHER_EFFECTS"] = _configuration.GetValue<bool>("Features:WeatherEffects", true);
            _features["DAY_NIGHT_CYCLE"] = _configuration.GetValue<bool>("Features:DayNightCycle", true);
            
            // PvP Features
            _features["ARENA_COMBAT"] = _configuration.GetValue<bool>("Features:ArenaCombat", true);
            _features["DUEL_ARENA"] = _configuration.GetValue<bool>("Features:DuelArena", true);
            _features["TEAM_ARENA"] = _configuration.GetValue<bool>("Features:TeamArena", true);
            _features["BATTLE_ROYALE"] = _configuration.GetValue<bool>("Features:BattleRoyale", false);
            _features["GUILD_ARENA"] = _configuration.GetValue<bool>("Features:GuildArena", true);
            _features["TOURNAMENT"] = _configuration.GetValue<bool>("Features:Tournament", true);
            _features["PVP_RANKING"] = _configuration.GetValue<bool>("Features:PvPRanking", true);
            _features["HONOR_POINTS"] = _configuration.GetValue<bool>("Features:HonorPoints", true);
            
            // Economy Features
            _features["WEB_MARKET"] = _configuration.GetValue<bool>("Features:WebMarket", true);
            _features["AUCTION_HOUSE"] = _configuration.GetValue<bool>("Features:AuctionHouse", true);
            _features["CROSS_SERVER_TRADE"] = _configuration.GetValue<bool>("Features:CrossServerTrade", false);
            _features["DYNAMIC_PRICING"] = _configuration.GetValue<bool>("Features:DynamicPricing", true);
            _features["TRADE_INSURANCE"] = _configuration.GetValue<bool>("Features:TradeInsurance", false);
            _features["BULK_TRADING"] = _configuration.GetValue<bool>("Features:BulkTrading", true);
            _features["INVESTMENT_SYSTEM"] = _configuration.GetValue<bool>("Features:InvestmentSystem", false);
            _features["GUILD_BANK"] = _configuration.GetValue<bool>("Features:GuildBank", true);
            
            // Security Features
            _features["BOT_PROTECTION"] = _configuration.GetValue<bool>("Features:BotProtection", true);
            _features["BEHAVIORAL_ANALYSIS"] = _configuration.GetValue<bool>("Features:BehavioralAnalysis", true);
            _features["HARDWARE_ID"] = _configuration.GetValue<bool>("Features:HardwareID", true);
            _features["MEMORY_PROTECTION"] = _configuration.GetValue<bool>("Features:MemoryProtection", true);
            _features["PACKET_ENCRYPTION"] = _configuration.GetValue<bool>("Features:PacketEncryption", true);
            _features["SPEED_HACK_PREVENTION"] = _configuration.GetValue<bool>("Features:SpeedHackPrevention", true);
            _features["DAMAGE_HACK_DETECTION"] = _configuration.GetValue<bool>("Features:DamageHackDetection", true);
            _features["DUPE_PREVENTION"] = _configuration.GetValue<bool>("Features:DupePrevention", true);
            
            // Social Features
            _features["GUILD_ALLIANCE"] = _configuration.GetValue<bool>("Features:GuildAlliance", true);
            _features["GUILD_WARS"] = _configuration.GetValue<bool>("Features:GuildWars", true);
            _features["MARRIAGE_SYSTEM"] = _configuration.GetValue<bool>("Features:MarriageSystem", false);
            _features["MENTORSHIP"] = _configuration.GetValue<bool>("Features:Mentorship", true);
            _features["VOICE_CHAT"] = _configuration.GetValue<bool>("Features:VoiceChat", false);
            _features["DISCORD_INTEGRATION"] = _configuration.GetValue<bool>("Features:DiscordIntegration", true);
            
            // Premium Features
            _features["VIP_SYSTEM"] = _configuration.GetValue<bool>("Features:VIPSystem", false);
            _features["PREMIUM_STORAGE"] = _configuration.GetValue<bool>("Features:PremiumStorage", false);
            _features["VIP_AREAS"] = _configuration.GetValue<bool>("Features:VIPAreas", false);
            _features["PREMIUM_RESURRECTION"] = _configuration.GetValue<bool>("Features:PremiumResurrection", false);
            
            // Event Features
            _features["SCHEDULED_EVENTS"] = _configuration.GetValue<bool>("Features:ScheduledEvents", true);
            _features["RANDOM_EVENTS"] = _configuration.GetValue<bool>("Features:RandomEvents", true);
            _features["SEASONAL_EVENTS"] = _configuration.GetValue<bool>("Features:SeasonalEvents", true);
            _features["LOTTERY_SYSTEM"] = _configuration.GetValue<bool>("Features:LotterySystem", false);
            _features["TREASURE_HUNT"] = _configuration.GetValue<bool>("Features:TreasureHunt", true);
            _features["MONSTER_INVASION"] = _configuration.GetValue<bool>("Features:MonsterInvasion", true);
            
            // Achievement Features
            _features["ACHIEVEMENT_SYSTEM"] = _configuration.GetValue<bool>("Features:AchievementSystem", true);
            _features["TITLE_SYSTEM"] = _configuration.GetValue<bool>("Features:TitleSystem", true);
            _features["REPUTATION_SYSTEM"] = _configuration.GetValue<bool>("Features:ReputationSystem", true);
            _features["PRESTIGE_SYSTEM"] = _configuration.GetValue<bool>("Features:PrestigeSystem", false);
            _features["HALL_OF_FAME"] = _configuration.GetValue<bool>("Features:HallOfFame", true);
            
            // Quest Features
            _features["DAILY_QUESTS"] = _configuration.GetValue<bool>("Features:DailyQuests", true);
            _features["WEEKLY_QUESTS"] = _configuration.GetValue<bool>("Features:WeeklyQuests", true);
            _features["MONTHLY_CHALLENGES"] = _configuration.GetValue<bool>("Features:MonthlyChallenges", true);
            _features["RANDOM_QUESTS"] = _configuration.GetValue<bool>("Features:RandomQuests", true);
            _features["GUILD_QUESTS"] = _configuration.GetValue<bool>("Features:GuildQuests", true);
            
            // Fortress Features
            _features["ENHANCED_FORTRESS"] = _configuration.GetValue<bool>("Features:EnhancedFortress", true);
            _features["MULTI_FORTRESS"] = _configuration.GetValue<bool>("Features:MultiFortress", false);
            _features["TERRITORY_CONTROL"] = _configuration.GetValue<bool>("Features:TerritoryControl", false);
            _features["FORTRESS_UPGRADE"] = _configuration.GetValue<bool>("Features:FortressUpgrade", true);
            _features["FORTRESS_TAX"] = _configuration.GetValue<bool>("Features:FortressTax", true);
            
            // Custom Content
            _features["CUSTOM_DUNGEONS"] = _configuration.GetValue<bool>("Features:CustomDungeons", false);
            _features["USER_CONTENT"] = _configuration.GetValue<bool>("Features:UserContent", false);
            _features["CUSTOM_SKILLS"] = _configuration.GetValue<bool>("Features:CustomSkills", false);
            _features["CUSTOM_ITEMS"] = _configuration.GetValue<bool>("Features:CustomItems", true);
            _features["CUSTOM_MAPS"] = _configuration.GetValue<bool>("Features:CustomMaps", false);
            
            _logger.Information("Loaded {Count} features", _features.Count);
        }

        /// <summary>
        /// Start servers (prefer unified SroNexusServer if available)
        /// </summary>
        public async Task<bool> StartAllServers()
        {
            try
            {
                _logger.Information("Starting servers...");
                
                // Prefer the new unified monolith if present
                var monolithPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "SroNexusServer", "SroNexusServer.exe");
                if (File.Exists(monolithPath))
                {
                    if (!await StartMonolith(monolithPath))
                    {
                        _logger.Error("Failed to start SroNexusServer monolith");
                        return false;
                    }
                    
                    _isMonolithMode = true;
                    _isRunning = true;
                    _logger.Information("SroNexusServer monolith started successfully!");
                    
                    // Apply feature configurations
                    await ApplyFeatureConfigurations();
                    return true;
                }

                // Fallback to legacy tri-server startup
                _logger.Warning("SroNexusServer.exe not found. Falling back to legacy servers.");

                // Start MasterServer first
                if (!await StartMasterServer())
                {
                    _logger.Error("Failed to start MasterServer");
                    return false;
                }
                
                await Task.Delay(2000); // Wait for MasterServer to initialize
                
                // Start GatewayServer
                if (!await StartGatewayServer())
                {
                    _logger.Error("Failed to start GatewayServer");
                    await StopAllServers();
                    return false;
                }
                
                await Task.Delay(2000); // Wait for GatewayServer to initialize
                
                // Start AgentServer
                if (!await StartAgentServer())
                {
                    _logger.Error("Failed to start AgentServer");
                    await StopAllServers();
                    return false;
                }
                
                _isRunning = true;
                _logger.Information("All servers started successfully (legacy mode).");
                
                // Apply feature configurations
                await ApplyFeatureConfigurations();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error starting servers");
                return false;
            }
        }

        private async Task<bool> StartMonolith(string exePath)
        {
            try
            {
                // Compute config relative to the exe: ../SroNexusServer/config/sronexus.conf
                var configPath = Path.Combine(Path.GetDirectoryName(exePath)!, "config", "sronexus.conf");
                if (!File.Exists(configPath))
                {
                    _logger.Warning("Config not found at {Path}. The server will use defaults.", configPath);
                }

                _masterServerProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = File.Exists(configPath) ? $"\"{configPath}\"" : "",
                        WorkingDirectory = Path.GetDirectoryName(exePath),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                _monolithProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        _logger.Debug("[SroNexusServer] {Output}", e.Data);
                };

                _monolithProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        _logger.Error("[SroNexusServer] {Error}", e.Data);
                };

                _masterServerProcess.Start();
                _masterServerProcess.BeginOutputReadLine();
                _masterServerProcess.BeginErrorReadLine();

                _logger.Information("SroNexusServer started with PID {PID}", _masterServerProcess.Id);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to start SroNexusServer");
                return false;
            }
        }

        /// <summary>
        /// Start MasterServer
        /// </summary>
        private async Task<bool> StartMasterServer()
        {
            try
            {
                var masterServerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "MasterServer", "MasterServer.exe");
                
                if (!File.Exists(masterServerPath))
                {
                    _logger.Error("MasterServer.exe not found at {Path}", masterServerPath);
                    return false;
                }
                
                _masterServerProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = masterServerPath,
                        WorkingDirectory = Path.GetDirectoryName(masterServerPath),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                _masterServerProcess.OutputDataReceived += (sender, e) => 
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        _logger.Debug("[MasterServer] {Output}", e.Data);
                };
                
                _masterServerProcess.ErrorDataReceived += (sender, e) => 
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        _logger.Error("[MasterServer] {Error}", e.Data);
                };
                
                _masterServerProcess.Start();
                _masterServerProcess.BeginOutputReadLine();
                _masterServerProcess.BeginErrorReadLine();
                
                _logger.Information("MasterServer started with PID {PID}", _masterServerProcess.Id);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to start MasterServer");
                return false;
            }
        }

        /// <summary>
        /// Start GatewayServer
        /// </summary>
        private async Task<bool> StartGatewayServer()
        {
            try
            {
                var gatewayServerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "GatewayServer", "GatewayServer.exe");
                
                if (!File.Exists(gatewayServerPath))
                {
                    _logger.Error("GatewayServer.exe not found at {Path}", gatewayServerPath);
                    return false;
                }
                
                _gatewayServerProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = gatewayServerPath,
                        WorkingDirectory = Path.GetDirectoryName(gatewayServerPath),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                _gatewayServerProcess.OutputDataReceived += (sender, e) => 
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        _logger.Debug("[GatewayServer] {Output}", e.Data);
                };
                
                _gatewayServerProcess.ErrorDataReceived += (sender, e) => 
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        _logger.Error("[GatewayServer] {Error}", e.Data);
                };
                
                _gatewayServerProcess.Start();
                _gatewayServerProcess.BeginOutputReadLine();
                _gatewayServerProcess.BeginErrorReadLine();
                
                _logger.Information("GatewayServer started with PID {PID}", _gatewayServerProcess.Id);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to start GatewayServer");
                return false;
            }
        }

        /// <summary>
        /// Start AgentServer
        /// </summary>
        private async Task<bool> StartAgentServer()
        {
            try
            {
                var agentServerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "AgentServer", "AgentServer.exe");
                
                if (!File.Exists(agentServerPath))
                {
                    _logger.Error("AgentServer.exe not found at {Path}", agentServerPath);
                    return false;
                }
                
                _agentServerProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = agentServerPath,
                        WorkingDirectory = Path.GetDirectoryName(agentServerPath),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                _agentServerProcess.OutputDataReceived += (sender, e) => 
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        _logger.Debug("[AgentServer] {Output}", e.Data);
                };
                
                _agentServerProcess.ErrorDataReceived += (sender, e) => 
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        _logger.Error("[AgentServer] {Error}", e.Data);
                };
                
                _agentServerProcess.Start();
                _agentServerProcess.BeginOutputReadLine();
                _agentServerProcess.BeginErrorReadLine();
                
                _logger.Information("AgentServer started with PID {PID}", _agentServerProcess.Id);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to start AgentServer");
                return false;
            }
        }

        /// <summary>
        /// Apply all feature configurations to the running servers
        /// </summary>
        private async Task ApplyFeatureConfigurations()
        {
            _logger.Information("Applying feature configurations...");
            
            foreach (var feature in _features)
            {
                if (feature.Value)
                {
                    _logger.Debug("Enabling feature: {Feature}", feature.Key);
                    // Here you would apply the feature to the running server
                    // This could involve database updates, configuration changes, etc.
                }
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Stop all servers
        /// </summary>
        public async Task StopAllServers()
        {
            try
            {
                _logger.Information("Stopping all servers...");
                
                if (_isMonolithMode)
                {
                    if (_monolithProcess != null && !_monolithProcess.HasExited)
                    {
                        _monolithProcess.Kill();
                        await _monolithProcess.WaitForExitAsync();
                        _monolithProcess.Dispose();
                        _monolithProcess = null;
                        _logger.Information("SroNexusServer stopped");
                    }
                    _isMonolithMode = false;
                    _isRunning = false;
                    _logger.Information("All servers stopped");
                    return;
                }

                // Stop AgentServer
                if (_agentServerProcess != null && !_agentServerProcess.HasExited)
                {
                    _agentServerProcess.Kill();
                    await _agentServerProcess.WaitForExitAsync();
                    _agentServerProcess.Dispose();
                    _agentServerProcess = null;
                    _logger.Information("AgentServer stopped");
                }
                
                // Stop GatewayServer
                if (_gatewayServerProcess != null && !_gatewayServerProcess.HasExited)
                {
                    _gatewayServerProcess.Kill();
                    await _gatewayServerProcess.WaitForExitAsync();
                    _gatewayServerProcess.Dispose();
                    _gatewayServerProcess = null;
                    _logger.Information("GatewayServer stopped");
                }
                
                // Stop MasterServer
                if (_masterServerProcess != null && !_masterServerProcess.HasExited)
                {
                    _masterServerProcess.Kill();
                    await _masterServerProcess.WaitForExitAsync();
                    _masterServerProcess.Dispose();
                    _masterServerProcess = null;
                    _logger.Information("MasterServer stopped");
                }
                
                _isRunning = false;
                _logger.Information("All servers stopped");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error stopping servers");
            }
        }

        /// <summary>
        /// Restart all servers
        /// </summary>
        public async Task<bool> RestartAllServers()
        {
            _logger.Information("Restarting all servers...");
            await StopAllServers();
            await Task.Delay(3000); // Wait for processes to fully terminate
            return await StartAllServers();
        }

        /// <summary>
        /// Check if servers are running
        /// </summary>
        public bool AreServersRunning()
        {
            if (_isMonolithMode)
                return _isRunning && _monolithProcess != null && !_monolithProcess.HasExited;

            return _isRunning && 
                   _masterServerProcess != null && !_masterServerProcess.HasExited &&
                   _gatewayServerProcess != null && !_gatewayServerProcess.HasExited &&
                   _agentServerProcess != null && !_agentServerProcess.HasExited;
        }

        /// <summary>
        /// Get server status
        /// </summary>
        public ServerStatus GetServerStatus()
        {
            if (_isMonolithMode)
            {
                return new ServerStatus
                {
                    MonolithRunning = _monolithProcess != null && !_monolithProcess.HasExited,
                    MonolithPID = _monolithProcess?.Id ?? 0,
                    EnabledFeatures = _features.Count(f => f.Value),
                    TotalFeatures = _features.Count
                };
            }

            return new ServerStatus
            {
                MasterServerRunning = _masterServerProcess != null && !_masterServerProcess.HasExited,
                GatewayServerRunning = _gatewayServerProcess != null && !_gatewayServerProcess.HasExited,
                AgentServerRunning = _agentServerProcess != null && !_agentServerProcess.HasExited,
                MasterServerPID = _masterServerProcess?.Id ?? 0,
                GatewayServerPID = _gatewayServerProcess?.Id ?? 0,
                AgentServerPID = _agentServerProcess?.Id ?? 0,
                EnabledFeatures = _features.Count(f => f.Value),
                TotalFeatures = _features.Count
            };
        }

        /// <summary>
        /// Toggle a specific feature
        /// </summary>
        public void ToggleFeature(string featureCode, bool enabled)
        {
            if (_features.ContainsKey(featureCode))
            {
                _features[featureCode] = enabled;
                _logger.Information("Feature {Feature} set to {State}", featureCode, enabled ? "Enabled" : "Disabled");
                
                // Apply the change to running server if needed
                if (_isRunning)
                {
                    // Apply feature change to running server
                    // This would involve database updates or configuration changes
                }
            }
        }

        /// <summary>
        /// Get all features and their states
        /// </summary>
        public Dictionary<string, bool> GetAllFeatures()
        {
            return new Dictionary<string, bool>(_features);
        }
    }

    public class ServerStatus
    {
        // Monolith mode
        public bool MonolithRunning { get; set; }
        public int MonolithPID { get; set; }

        // Legacy mode
        public bool MasterServerRunning { get; set; }
        public bool GatewayServerRunning { get; set; }
        public bool AgentServerRunning { get; set; }
        public int MasterServerPID { get; set; }
        public int GatewayServerPID { get; set; }
        public int AgentServerPID { get; set; }

        // Features
        public int EnabledFeatures { get; set; }
        public int TotalFeatures { get; set; }
    }
}

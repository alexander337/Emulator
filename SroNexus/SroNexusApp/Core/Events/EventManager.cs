using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Configuration;
using Serilog;
using Timer = System.Timers.Timer;

namespace SroNexus.Core.Events
{
    /// <summary>
    /// Advanced Event Management System
    /// Handles all server events including scheduled, random, and special events
    /// </summary>
    public class EventManager : IDisposable
    {
        private readonly ILogger _logger = Log.ForContext<EventManager>();
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, ServerEvent> _activeEvents = new();
        private readonly Dictionary<string, Timer> _eventTimers = new();
        private readonly Random _random = new();
        private readonly SemaphoreSlim _eventLock = new(1, 1);
        
        // Event configurations
        private readonly List<EventConfiguration> _scheduledEvents = new();
        private readonly List<EventConfiguration> _randomEvents = new();
        private readonly List<EventConfiguration> _specialEvents = new();
        
        public EventManager(IConfiguration configuration)
        {
            _configuration = configuration;
            LoadEventConfigurations();
            InitializeEventScheduler();
        }

        /// <summary>
        /// Loads all event configurations from database and config files
        /// </summary>
        private void LoadEventConfigurations()
        {
            // Scheduled Events
            _scheduledEvents.Add(new EventConfiguration
            {
                EventCode = "FORTRESS_WAR",
                EventName = "Fortress War",
                EventType = EventType.Scheduled,
                Schedule = new EventSchedule
                {
                    DaysOfWeek = new[] { DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
                    StartTime = new TimeSpan(20, 0, 0), // 8:00 PM
                    Duration = TimeSpan.FromHours(2)
                },
                Rewards = new EventRewards
                {
                    GoldReward = 10000000,
                    ExpMultiplier = 3.0,
                    SpecialItems = new[] { 10001, 10002, 10003 }, // Special fortress items
                    TitleReward = "Fortress Conqueror"
                },
                Requirements = new EventRequirements
                {
                    MinLevel = 80,
                    MinGuildLevel = 3,
                    RequiresGuild = true
                }
            });

            _scheduledEvents.Add(new EventConfiguration
            {
                EventCode = "CTF",
                EventName = "Capture The Flag",
                EventType = EventType.Scheduled,
                Schedule = new EventSchedule
                {
                    DaysOfWeek = Enum.GetValues<DayOfWeek>().ToArray(),
                    StartTime = new TimeSpan(14, 0, 0), // 2:00 PM
                    Duration = TimeSpan.FromMinutes(30),
                    Interval = TimeSpan.FromHours(4) // Every 4 hours
                },
                Rewards = new EventRewards
                {
                    GoldReward = 1000000,
                    ExpMultiplier = 2.0,
                    HonorPoints = 500,
                    EventTokens = 10
                },
                Requirements = new EventRequirements
                {
                    MinLevel = 40,
                    MaxLevel = 140,
                    MinParticipants = 20,
                    MaxParticipants = 100
                }
            });

            _scheduledEvents.Add(new EventConfiguration
            {
                EventCode = "BATTLE_ARENA",
                EventName = "Battle Arena Tournament",
                EventType = EventType.Scheduled,
                Schedule = new EventSchedule
                {
                    DaysOfWeek = new[] { DayOfWeek.Wednesday, DayOfWeek.Saturday },
                    StartTime = new TimeSpan(19, 0, 0), // 7:00 PM
                    Duration = TimeSpan.FromHours(1)
                },
                Rewards = new EventRewards
                {
                    GoldReward = 5000000,
                    ExpMultiplier = 2.5,
                    HonorPoints = 1000,
                    SpecialItems = new[] { 20001, 20002 }, // Arena champion items
                    TitleReward = "Arena Champion"
                },
                Requirements = new EventRequirements
                {
                    MinLevel = 60,
                    RegistrationRequired = true,
                    RegistrationDeadline = TimeSpan.FromMinutes(30)
                }
            });

            // Random Events
            _randomEvents.Add(new EventConfiguration
            {
                EventCode = "MONSTER_INVASION",
                EventName = "Monster Invasion",
                EventType = EventType.Random,
                RandomChance = 0.05, // 5% chance per hour
                Rewards = new EventRewards
                {
                    GoldReward = 500000,
                    ExpMultiplier = 1.5,
                    DropRateMultiplier = 2.0
                },
                EventData = new Dictionary<string, object>
                {
                    ["MonsterCount"] = 1000,
                    ["BossMonsters"] = new[] { 5001, 5002, 5003 },
                    ["InvasionZones"] = new[] { "Jangan", "Hotan", "Alexandria" }
                }
            });

            _randomEvents.Add(new EventConfiguration
            {
                EventCode = "TREASURE_HUNT",
                EventName = "Hidden Treasure Hunt",
                EventType = EventType.Random,
                RandomChance = 0.10, // 10% chance per hour
                Duration = TimeSpan.FromMinutes(30),
                Rewards = new EventRewards
                {
                    SpecialItems = new[] { 30001, 30002, 30003 }, // Treasure items
                    EventTokens = 5
                },
                EventData = new Dictionary<string, object>
                {
                    ["TreasureCount"] = 50,
                    ["ClueNPCs"] = new[] { 1001, 1002, 1003 }
                }
            });

            _randomEvents.Add(new EventConfiguration
            {
                EventCode = "LUCKY_PARTY",
                EventName = "Lucky Party Time",
                EventType = EventType.Random,
                RandomChance = 0.08,
                Duration = TimeSpan.FromHours(1),
                Rewards = new EventRewards
                {
                    ExpMultiplier = 2.0,
                    DropRateMultiplier = 1.5,
                    GoldMultiplier = 1.5
                }
            });

            // Special Events (Seasonal/Holiday)
            _specialEvents.Add(new EventConfiguration
            {
                EventCode = "CHRISTMAS_EVENT",
                EventName = "Christmas Celebration",
                EventType = EventType.Special,
                StartDate = new DateTime(DateTime.Now.Year, 12, 20),
                EndDate = new DateTime(DateTime.Now.Year, 12, 31),
                Rewards = new EventRewards
                {
                    SpecialItems = new[] { 40001, 40002, 40003 }, // Christmas items
                    ExpMultiplier = 3.0,
                    EventTokens = 20,
                    TitleReward = "Santa's Helper"
                },
                EventData = new Dictionary<string, object>
                {
                    ["SantaNPC"] = 9001,
                    ["GiftBoxDropRate"] = 0.1,
                    ["SpecialQuests"] = new[] { 8001, 8002, 8003 }
                }
            });

            _specialEvents.Add(new EventConfiguration
            {
                EventCode = "HALLOWEEN_EVENT",
                EventName = "Halloween Horror Night",
                EventType = EventType.Special,
                StartDate = new DateTime(DateTime.Now.Year, 10, 25),
                EndDate = new DateTime(DateTime.Now.Year, 11, 2),
                Rewards = new EventRewards
                {
                    SpecialItems = new[] { 50001, 50002, 50003 }, // Halloween items
                    ExpMultiplier = 2.5,
                    TitleReward = "Pumpkin King"
                },
                EventData = new Dictionary<string, object>
                {
                    ["HorrorBosses"] = new[] { 6001, 6002, 6003 },
                    ["CandyDropRate"] = 0.15,
                    ["CostumeNPC"] = 9002
                }
            });

            _logger.Information("Loaded {ScheduledCount} scheduled, {RandomCount} random, and {SpecialCount} special events",
                _scheduledEvents.Count, _randomEvents.Count, _specialEvents.Count);
        }

        /// <summary>
        /// Initializes the event scheduler
        /// </summary>
        private void InitializeEventScheduler()
        {
            // Schedule all scheduled events
            foreach (var eventConfig in _scheduledEvents)
            {
                ScheduleEvent(eventConfig);
            }

            // Start random event checker
            var randomEventTimer = new Timer(TimeSpan.FromMinutes(10).TotalMilliseconds);
            randomEventTimer.Elapsed += CheckRandomEvents;
            randomEventTimer.Start();
            _eventTimers["RANDOM_CHECKER"] = randomEventTimer;

            // Start special event checker
            var specialEventTimer = new Timer(TimeSpan.FromHours(1).TotalMilliseconds);
            specialEventTimer.Elapsed += CheckSpecialEvents;
            specialEventTimer.Start();
            _eventTimers["SPECIAL_CHECKER"] = specialEventTimer;

            _logger.Information("Event scheduler initialized");
        }

        /// <summary>
        /// Schedules a specific event
        /// </summary>
        private void ScheduleEvent(EventConfiguration config)
        {
            if (config.EventType != EventType.Scheduled) return;

            var timer = new Timer();
            timer.Elapsed += async (sender, e) => await CheckAndStartScheduledEvent(config);
            
            // Calculate next occurrence
            var nextOccurrence = CalculateNextOccurrence(config.Schedule);
            var delay = nextOccurrence - DateTime.Now;
            
            if (delay.TotalMilliseconds > 0)
            {
                timer.Interval = delay.TotalMilliseconds;
                timer.AutoReset = false;
                timer.Start();
                _eventTimers[config.EventCode] = timer;
                
                _logger.Information("Scheduled event {EventName} for {NextTime}",
                    config.EventName, nextOccurrence);
            }
        }

        /// <summary>
        /// Starts an event
        /// </summary>
        public async Task<bool> StartEvent(string eventCode, bool forced = false)
        {
            await _eventLock.WaitAsync();
            try
            {
                if (_activeEvents.ContainsKey(eventCode))
                {
                    _logger.Warning("Event {EventCode} is already active", eventCode);
                    return false;
                }

                var config = GetEventConfiguration(eventCode);
                if (config == null)
                {
                    _logger.Error("Event configuration not found for {EventCode}", eventCode);
                    return false;
                }

                // Check requirements
                if (!forced && !await CheckEventRequirements(config))
                {
                    _logger.Warning("Event {EventCode} requirements not met", eventCode);
                    return false;
                }

                // Create event instance
                var serverEvent = new ServerEvent
                {
                    EventID = Guid.NewGuid().ToString(),
                    Configuration = config,
                    StartTime = DateTime.UtcNow,
                    Status = EventStatus.Starting,
                    Participants = new List<int>()
                };

                _activeEvents[eventCode] = serverEvent;

                // Initialize event
                await InitializeEvent(serverEvent);

                // Announce event
                await AnnounceEvent(serverEvent, EventAnnouncement.Started);

                // Start event logic
                _ = Task.Run(async () => await RunEventLogic(serverEvent));

                // Schedule event end if duration is set
                if (config.Duration.HasValue)
                {
                    _ = Task.Delay(config.Duration.Value).ContinueWith(async _ => 
                        await EndEvent(eventCode));
                }

                serverEvent.Status = EventStatus.Active;
                _logger.Information("Event {EventName} started successfully", config.EventName);
                
                return true;
            }
            finally
            {
                _eventLock.Release();
            }
        }

        /// <summary>
        /// Ends an event
        /// </summary>
        public async Task<bool> EndEvent(string eventCode)
        {
            await _eventLock.WaitAsync();
            try
            {
                if (!_activeEvents.TryGetValue(eventCode, out var serverEvent))
                {
                    _logger.Warning("Event {EventCode} is not active", eventCode);
                    return false;
                }

                serverEvent.Status = EventStatus.Ending;
                serverEvent.EndTime = DateTime.UtcNow;

                // Calculate and distribute rewards
                await DistributeRewards(serverEvent);

                // Clean up event
                await CleanupEvent(serverEvent);

                // Announce event end
                await AnnounceEvent(serverEvent, EventAnnouncement.Ended);

                // Remove from active events
                _activeEvents.Remove(eventCode);

                // Reschedule if it's a scheduled event
                if (serverEvent.Configuration.EventType == EventType.Scheduled)
                {
                    ScheduleEvent(serverEvent.Configuration);
                }

                _logger.Information("Event {EventName} ended. Duration: {Duration}, Participants: {Count}",
                    serverEvent.Configuration.EventName,
                    serverEvent.EndTime - serverEvent.StartTime,
                    serverEvent.Participants.Count);

                return true;
            }
            finally
            {
                _eventLock.Release();
            }
        }

        /// <summary>
        /// Registers a player for an event
        /// </summary>
        public async Task<EventRegistrationResult> RegisterForEvent(string eventCode, int characterId)
        {
            await _eventLock.WaitAsync();
            try
            {
                if (!_activeEvents.TryGetValue(eventCode, out var serverEvent))
                {
                    return new EventRegistrationResult
                    {
                        Success = false,
                        Message = "Event is not active"
                    };
                }

                if (serverEvent.Participants.Contains(characterId))
                {
                    return new EventRegistrationResult
                    {
                        Success = false,
                        Message = "Already registered for this event"
                    };
                }

                var config = serverEvent.Configuration;
                
                // Check max participants
                if (config.Requirements?.MaxParticipants > 0 &&
                    serverEvent.Participants.Count >= config.Requirements.MaxParticipants)
                {
                    return new EventRegistrationResult
                    {
                        Success = false,
                        Message = "Event is full"
                    };
                }

                // Check player requirements
                if (!await CheckPlayerRequirements(characterId, config.Requirements))
                {
                    return new EventRegistrationResult
                    {
                        Success = false,
                        Message = "You do not meet the requirements for this event"
                    };
                }

                serverEvent.Participants.Add(characterId);
                
                // Track player stats
                if (!serverEvent.PlayerStats.ContainsKey(characterId))
                {
                    serverEvent.PlayerStats[characterId] = new PlayerEventStats
                    {
                        CharacterID = characterId,
                        JoinTime = DateTime.UtcNow
                    };
                }

                _logger.Information("Player {CharacterID} registered for event {EventName}",
                    characterId, config.EventName);

                return new EventRegistrationResult
                {
                    Success = true,
                    Message = $"Successfully registered for {config.EventName}",
                    EventInfo = new EventInfo
                    {
                        EventCode = eventCode,
                        EventName = config.EventName,
                        StartTime = serverEvent.StartTime,
                        ParticipantCount = serverEvent.Participants.Count
                    }
                };
            }
            finally
            {
                _eventLock.Release();
            }
        }

        /// <summary>
        /// Updates player stats in an event
        /// </summary>
        public async Task UpdatePlayerEventStats(string eventCode, int characterId, string statType, object value)
        {
            if (!_activeEvents.TryGetValue(eventCode, out var serverEvent))
                return;

            if (!serverEvent.PlayerStats.TryGetValue(characterId, out var stats))
                return;

            switch (statType)
            {
                case "Kills":
                    stats.Kills += Convert.ToInt32(value);
                    break;
                case "Deaths":
                    stats.Deaths += Convert.ToInt32(value);
                    break;
                case "Points":
                    stats.Points += Convert.ToInt32(value);
                    break;
                case "DamageDealt":
                    stats.DamageDealt += Convert.ToInt64(value);
                    break;
                case "DamageTaken":
                    stats.DamageTaken += Convert.ToInt64(value);
                    break;
                case "ObjectivesCaptured":
                    stats.ObjectivesCaptured += Convert.ToInt32(value);
                    break;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Gets active events
        /// </summary>
        public List<EventInfo> GetActiveEvents()
        {
            return _activeEvents.Values.Select(e => new EventInfo
            {
                EventCode = e.Configuration.EventCode,
                EventName = e.Configuration.EventName,
                EventType = e.Configuration.EventType.ToString(),
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                ParticipantCount = e.Participants.Count,
                Status = e.Status.ToString(),
                Rewards = e.Configuration.Rewards
            }).ToList();
        }

        /// <summary>
        /// Gets event schedule
        /// </summary>
        public List<EventScheduleInfo> GetEventSchedule()
        {
            var schedule = new List<EventScheduleInfo>();
            
            foreach (var config in _scheduledEvents)
            {
                var nextOccurrence = CalculateNextOccurrence(config.Schedule);
                schedule.Add(new EventScheduleInfo
                {
                    EventCode = config.EventCode,
                    EventName = config.EventName,
                    NextOccurrence = nextOccurrence,
                    Duration = config.Duration,
                    Requirements = config.Requirements,
                    Rewards = config.Rewards
                });
            }
            
            return schedule.OrderBy(s => s.NextOccurrence).ToList();
        }

        /// <summary>
        /// Checks and starts scheduled events
        /// </summary>
        private async Task CheckAndStartScheduledEvent(EventConfiguration config)
        {
            var now = DateTime.Now;
            
            if (config.Schedule.DaysOfWeek.Contains(now.DayOfWeek) &&
                Math.Abs((now.TimeOfDay - config.Schedule.StartTime).TotalMinutes) < 1)
            {
                await StartEvent(config.EventCode);
            }
            
            // Reschedule for next occurrence
            ScheduleEvent(config);
        }

        /// <summary>
        /// Checks for random events
        /// </summary>
        private async void CheckRandomEvents(object sender, ElapsedEventArgs e)
        {
            foreach (var config in _randomEvents)
            {
                if (_activeEvents.ContainsKey(config.EventCode))
                    continue;
                
                var roll = _random.NextDouble();
                if (roll < config.RandomChance)
                {
                    _logger.Information("Random event {EventName} triggered (roll: {Roll} < {Chance})",
                        config.EventName, roll, config.RandomChance);
                    
                    await StartEvent(config.EventCode);
                }
            }
        }

        /// <summary>
        /// Checks for special events
        /// </summary>
        private async void CheckSpecialEvents(object sender, ElapsedEventArgs e)
        {
            var now = DateTime.Now;
            
            foreach (var config in _specialEvents)
            {
                if (_activeEvents.ContainsKey(config.EventCode))
                    continue;
                
                if (config.StartDate <= now && now <= config.EndDate)
                {
                    await StartEvent(config.EventCode);
                }
            }
        }

        /// <summary>
        /// Initializes event-specific logic
        /// </summary>
        private async Task InitializeEvent(ServerEvent serverEvent)
        {
            var config = serverEvent.Configuration;
            
            switch (config.EventCode)
            {
                case "FORTRESS_WAR":
                    await InitializeFortressWar(serverEvent);
                    break;
                case "CTF":
                    await InitializeCaptureTheFlag(serverEvent);
                    break;
                case "BATTLE_ARENA":
                    await InitializeBattleArena(serverEvent);
                    break;
                case "MONSTER_INVASION":
                    await InitializeMonsterInvasion(serverEvent);
                    break;
                case "TREASURE_HUNT":
                    await InitializeTreasureHunt(serverEvent);
                    break;
            }
        }

        /// <summary>
        /// Runs event-specific logic
        /// </summary>
        private async Task RunEventLogic(ServerEvent serverEvent)
        {
            var config = serverEvent.Configuration;
            
            try
            {
                switch (config.EventCode)
                {
                    case "FORTRESS_WAR":
                        await RunFortressWarLogic(serverEvent);
                        break;
                    case "CTF":
                        await RunCaptureTheFlagLogic(serverEvent);
                        break;
                    case "BATTLE_ARENA":
                        await RunBattleArenaLogic(serverEvent);
                        break;
                    case "MONSTER_INVASION":
                        await RunMonsterInvasionLogic(serverEvent);
                        break;
                    case "TREASURE_HUNT":
                        await RunTreasureHuntLogic(serverEvent);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error running event logic for {EventCode}", config.EventCode);
            }
        }

        // Event-specific initialization methods
        private async Task InitializeFortressWar(ServerEvent serverEvent)
        {
            // Initialize fortress war
            await Task.CompletedTask;
        }

        private async Task InitializeCaptureTheFlag(ServerEvent serverEvent)
        {
            // Initialize CTF
            await Task.CompletedTask;
        }

        private async Task InitializeBattleArena(ServerEvent serverEvent)
        {
            // Initialize battle arena
            await Task.CompletedTask;
        }

        private async Task InitializeMonsterInvasion(ServerEvent serverEvent)
        {
            // Spawn invasion monsters
            if (serverEvent.Configuration.EventData.TryGetValue("MonsterCount", out var monsterCount))
            {
                // Spawn monsters in invasion zones
                _logger.Information("Spawning {Count} invasion monsters", monsterCount);
            }
            await Task.CompletedTask;
        }

        private async Task InitializeTreasureHunt(ServerEvent serverEvent)
        {
            // Place treasures
            if (serverEvent.Configuration.EventData.TryGetValue("TreasureCount", out var treasureCount))
            {
                // Place treasures randomly
                _logger.Information("Placing {Count} treasures", treasureCount);
            }
            await Task.CompletedTask;
        }

        // Event-specific logic methods
        private async Task RunFortressWarLogic(ServerEvent serverEvent)
        {
            while (serverEvent.Status == EventStatus.Active)
            {
                // Update fortress war state
                await Task.Delay(1000);
            }
        }

        private async Task RunCaptureTheFlagLogic(ServerEvent serverEvent)
        {
            while (serverEvent.Status == EventStatus.Active)
            {
                // Update CTF state
                await Task.Delay(1000);
            }
        }

        private async Task RunBattleArenaLogic(ServerEvent serverEvent)
        {
            while (serverEvent.Status == EventStatus.Active)
            {
                // Update arena state
                await Task.Delay(1000);
            }
        }

        private async Task RunMonsterInvasionLogic(ServerEvent serverEvent)
        {
            while (serverEvent.Status == EventStatus.Active)
            {
                // Monitor invasion progress
                await Task.Delay(5000);
            }
        }

        private async Task RunTreasureHuntLogic(ServerEvent serverEvent)
        {
            while (serverEvent.Status == EventStatus.Active)
            {
                // Monitor treasure collection
                await Task.Delay(5000);
            }
        }

        /// <summary>
        /// Distributes rewards to participants
        /// </summary>
        private async Task DistributeRewards(ServerEvent serverEvent)
        {
            var config = serverEvent.Configuration;
            var rewards = config.Rewards;
            
            if (rewards == null) return;
            
            // Sort players by performance
            var sortedPlayers = serverEvent.PlayerStats.Values
                .OrderByDescending(p => p.Points)
                .ThenByDescending(p => p.Kills)
                .ThenBy(p => p.Deaths)
                .ToList();
            
            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                var player = sortedPlayers[i];
                var rank = i + 1;
                
                // Calculate reward multiplier based on rank
                var multiplier = rank switch
                {
                    1 => 2.0,    // 1st place
                    2 => 1.5,    // 2nd place
                    3 => 1.25,   // 3rd place
                    <= 10 => 1.0, // Top 10
                    _ => 0.5     // Participation
                };
                
                // Give rewards
                if (rewards.GoldReward > 0)
                {
                    var goldAmount = (long)(rewards.GoldReward * multiplier);
                    await GiveGold(player.CharacterID, goldAmount);
                }
                
                if (rewards.HonorPoints > 0)
                {
                    var honorAmount = (int)(rewards.HonorPoints * multiplier);
                    await GiveHonorPoints(player.CharacterID, honorAmount);
                }
                
                if (rewards.EventTokens > 0)
                {
                    await GiveEventTokens(player.CharacterID, rewards.EventTokens);
                }
                
                // Special rewards for top players
                if (rank <= 3 && rewards.SpecialItems != null && rewards.SpecialItems.Length > 0)
                {
                    foreach (var itemId in rewards.SpecialItems)
                    {
                        await GiveItem(player.CharacterID, itemId);
                    }
                }
                
                // Title reward for winner
                if (rank == 1 && !string.IsNullOrEmpty(rewards.TitleReward))
                {
                    await GiveTitle(player.CharacterID, rewards.TitleReward);
                }
                
                _logger.Information("Distributed rewards to player {CharacterID} (Rank: {Rank})",
                    player.CharacterID, rank);
            }
        }

        /// <summary>
        /// Cleans up event resources
        /// </summary>
        private async Task CleanupEvent(ServerEvent serverEvent)
        {
            // Clean up event-specific resources
            switch (serverEvent.Configuration.EventCode)
            {
                case "MONSTER_INVASION":
                    // Remove invasion monsters
                    break;
                case "TREASURE_HUNT":
                    // Remove uncollected treasures
                    break;
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Announces event to all players
        /// </summary>
        private async Task AnnounceEvent(ServerEvent serverEvent, EventAnnouncement type)
        {
            var config = serverEvent.Configuration;
            var message = type switch
            {
                EventAnnouncement.Starting => $"[EVENT] {config.EventName} will start in 5 minutes! Type /join {config.EventCode} to participate!",
                EventAnnouncement.Started => $"[EVENT] {config.EventName} has started!",
                EventAnnouncement.Ending => $"[EVENT] {config.EventName} will end in 5 minutes!",
                EventAnnouncement.Ended => $"[EVENT] {config.EventName} has ended! Thank you for participating!",
                _ => ""
            };
            
            // Send global announcement
            await SendGlobalMessage(message);
            
            _logger.Information("Event announcement sent: {Message}", message);
        }

        // Helper methods
        private EventConfiguration GetEventConfiguration(string eventCode)
        {
            return _scheduledEvents.FirstOrDefault(e => e.EventCode == eventCode) ??
                   _randomEvents.FirstOrDefault(e => e.EventCode == eventCode) ??
                   _specialEvents.FirstOrDefault(e => e.EventCode == eventCode);
        }

        private DateTime CalculateNextOccurrence(EventSchedule schedule)
        {
            var now = DateTime.Now;
            var nextDate = now.Date;
            
            // Find next day that matches schedule
            for (int i = 0; i < 7; i++)
            {
                if (schedule.DaysOfWeek.Contains(nextDate.DayOfWeek))
                {
                    var nextTime = nextDate.Add(schedule.StartTime);
                    if (nextTime > now)
                        return nextTime;
                }
                nextDate = nextDate.AddDays(1);
            }
            
            return nextDate.Add(schedule.StartTime);
        }

        private async Task<bool> CheckEventRequirements(EventConfiguration config)
        {
            if (config.Requirements == null) return true;
            
            // Check minimum participants
            if (config.Requirements.MinParticipants > 0)
            {
                var onlineCount = await GetOnlinePlayerCount();
                if (onlineCount < config.Requirements.MinParticipants)
                    return false;
            }
            
            return true;
        }

        private async Task<bool> CheckPlayerRequirements(int characterId, EventRequirements requirements)
        {
            if (requirements == null) return true;
            
            // Check level requirements
            // Check guild requirements
            // etc.
            
            return await Task.FromResult(true);
        }

        // Database/Game interaction methods (placeholders)
        private async Task<int> GetOnlinePlayerCount() => await Task.FromResult(100);
        private async Task SendGlobalMessage(string message) => await Task.CompletedTask;
        private async Task GiveGold(int characterId, long amount) => await Task.CompletedTask;
        private async Task GiveHonorPoints(int characterId, int amount) => await Task.CompletedTask;
        private async Task GiveEventTokens(int characterId, int amount) => await Task.CompletedTask;
        private async Task GiveItem(int characterId, int itemId) => await Task.CompletedTask;
        private async Task GiveTitle(int characterId, string title) => await Task.CompletedTask;

        public void Dispose()
        {
            foreach (var timer in _eventTimers.Values)
            {
                timer?.Stop();
                timer?.Dispose();
            }
            _eventTimers.Clear();
            _eventLock?.Dispose();
        }
    }

    // Supporting classes
    public class EventConfiguration
    {
        public string EventCode { get; set; }
        public string EventName { get; set; }
        public EventType EventType { get; set; }
        public EventSchedule Schedule { get; set; }
        public TimeSpan? Duration { get; set; }
        public double RandomChance { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public EventRewards Rewards { get; set; }
        public EventRequirements Requirements { get; set; }
        public Dictionary<string, object> EventData { get; set; } = new();
    }

    public class EventSchedule
    {
        public DayOfWeek[] DaysOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan? Interval { get; set; }
    }

    public class EventRewards
    {
        public long GoldReward { get; set; }
        public double ExpMultiplier { get; set; }
        public double DropRateMultiplier { get; set; }
        public double GoldMultiplier { get; set; }
        public int HonorPoints { get; set; }
        public int EventTokens { get; set; }
        public int[] SpecialItems { get; set; }
        public string TitleReward { get; set; }
    }

    public class EventRequirements
    {
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public int MinGuildLevel { get; set; }
        public bool RequiresGuild { get; set; }
        public int MinParticipants { get; set; }
        public int MaxParticipants { get; set; }
        public bool RegistrationRequired { get; set; }
        public TimeSpan RegistrationDeadline { get; set; }
    }

    public class ServerEvent
    {
        public string EventID { get; set; }
        public EventConfiguration Configuration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public EventStatus Status { get; set; }
        public List<int> Participants { get; set; } = new();
        public Dictionary<int, PlayerEventStats> PlayerStats { get; set; } = new();
    }

    public class PlayerEventStats
    {
        public int CharacterID { get; set; }
        public DateTime JoinTime { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Points { get; set; }
        public long DamageDealt { get; set; }
        public long DamageTaken { get; set; }
        public int ObjectivesCaptured { get; set; }
    }

    public class EventInfo
    {
        public string EventCode { get; set; }
        public string EventName { get; set; }
        public string EventType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int ParticipantCount { get; set; }
        public string Status { get; set; }
        public EventRewards Rewards { get; set; }
    }

    public class EventScheduleInfo
    {
        public string EventCode { get; set; }
        public string EventName { get; set; }
        public DateTime NextOccurrence { get; set; }
        public TimeSpan? Duration { get; set; }
        public EventRequirements Requirements { get; set; }
        public EventRewards Rewards { get; set; }
    }

    public class EventRegistrationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public EventInfo EventInfo { get; set; }
    }

    public enum EventType
    {
        Scheduled,
        Random,
        Special,
        Manual
    }

    public enum EventStatus
    {
        Scheduled,
        Starting,
        Active,
        Ending,
        Ended,
        Cancelled
    }

    public enum EventAnnouncement
    {
        Starting,
        Started,
        Ending,
        Ended
    }
}

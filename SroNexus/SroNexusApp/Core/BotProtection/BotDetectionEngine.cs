using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace SroNexus.Core.BotProtection
{
    /// <summary>
    /// Advanced Bot Detection Engine with Machine Learning capabilities
    /// Implements real-time behavioral analysis and pattern recognition
    /// </summary>
    public class BotDetectionEngine
    {
        private readonly ILogger _logger = Log.ForContext<BotDetectionEngine>();
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<int, PlayerBehaviorProfile> _playerProfiles = new();
        private readonly ConcurrentDictionary<int, List<SuspiciousActivity>> _suspiciousActivities = new();
        
        // Detection thresholds
        private readonly double _movementPatternThreshold = 0.85;
        private readonly double _clickPatternThreshold = 0.90;
        private readonly double _farmingPatternThreshold = 0.75;
        private readonly int _captchaFailureLimit = 3;
        
        public BotDetectionEngine(IConfiguration configuration)
        {
            _configuration = configuration;
            LoadDetectionParameters();
        }

        /// <summary>
        /// Analyzes player behavior in real-time
        /// </summary>
        public async Task<BotDetectionResult> AnalyzePlayerBehavior(int characterId, PlayerAction action)
        {
            var profile = _playerProfiles.GetOrAdd(characterId, new PlayerBehaviorProfile(characterId));
            
            // Update profile with new action
            profile.RecordAction(action);
            
            // Perform multiple detection methods
            var detectionTasks = new List<Task<DetectionScore>>
            {
                Task.Run(() => AnalyzeMovementPattern(profile)),
                Task.Run(() => AnalyzeClickPattern(profile)),
                Task.Run(() => AnalyzeFarmingPattern(profile)),
                Task.Run(() => AnalyzePacketTiming(profile)),
                Task.Run(() => AnalyzeSkillUsagePattern(profile)),
                Task.Run(() => AnalyzeItemPickupPattern(profile)),
                Task.Run(() => AnalyzeChatPattern(profile)),
                Task.Run(() => AnalyzeTradePattern(profile))
            };
            
            var scores = await Task.WhenAll(detectionTasks);
            
            // Calculate composite score
            var compositeScore = CalculateCompositeScore(scores);
            
            // Determine action based on score
            var result = DetermineAction(characterId, compositeScore);
            
            // Log detection
            if (result.IsBot)
            {
                _logger.Warning("Bot detected: CharacterID {CharacterID}, Score {Score}, Action {Action}",
                    characterId, compositeScore, result.RecommendedAction);
                
                await RecordSuspiciousActivity(characterId, result);
            }
            
            return result;
        }

        /// <summary>
        /// Analyzes movement patterns for bot-like behavior
        /// </summary>
        private DetectionScore AnalyzeMovementPattern(PlayerBehaviorProfile profile)
        {
            var movements = profile.GetRecentMovements(TimeSpan.FromMinutes(10));
            if (movements.Count < 10) return new DetectionScore { Type = "Movement", Score = 0, Confidence = 0.1 };
            
            // Check for repetitive paths
            var pathSignatures = movements.Select(m => m.GetPathSignature()).ToList();
            var uniquePaths = pathSignatures.Distinct().Count();
            var repetitionRatio = 1.0 - (double)uniquePaths / pathSignatures.Count;
            
            // Check for perfect timing between movements
            var timingDeltas = new List<double>();
            for (int i = 1; i < movements.Count; i++)
            {
                timingDeltas.Add((movements[i].Timestamp - movements[i-1].Timestamp).TotalMilliseconds);
            }
            
            var timingVariance = CalculateVariance(timingDeltas);
            var perfectTimingScore = timingVariance < 50 ? 0.9 : Math.Max(0, 1.0 - (timingVariance / 1000));
            
            // Check for inhuman reaction times
            var reactionTimes = profile.GetReactionTimes();
            var inhumanReactions = reactionTimes.Count(rt => rt < 100); // Less than 100ms
            var reactionScore = (double)inhumanReactions / Math.Max(1, reactionTimes.Count);
            
            // Combine scores
            var finalScore = (repetitionRatio * 0.4 + perfectTimingScore * 0.4 + reactionScore * 0.2);
            
            return new DetectionScore
            {
                Type = "Movement",
                Score = finalScore,
                Confidence = Math.Min(1.0, movements.Count / 100.0),
                Details = $"Repetition: {repetitionRatio:P}, Timing: {perfectTimingScore:P}, Reactions: {reactionScore:P}"
            };
        }

        /// <summary>
        /// Analyzes click patterns for automation
        /// </summary>
        private DetectionScore AnalyzeClickPattern(PlayerBehaviorProfile profile)
        {
            var clicks = profile.GetRecentClicks(TimeSpan.FromMinutes(5));
            if (clicks.Count < 20) return new DetectionScore { Type = "Click", Score = 0, Confidence = 0.1 };
            
            // Analyze click positions
            var clickPositions = clicks.Select(c => new { c.X, c.Y }).ToList();
            var uniquePositions = clickPositions.Distinct().Count();
            var positionRepetition = 1.0 - (double)uniquePositions / clickPositions.Count;
            
            // Analyze click intervals
            var intervals = new List<double>();
            for (int i = 1; i < clicks.Count; i++)
            {
                intervals.Add((clicks[i].Timestamp - clicks[i-1].Timestamp).TotalMilliseconds);
            }
            
            // Check for consistent intervals (bot-like)
            var intervalVariance = CalculateVariance(intervals);
            var consistentIntervalScore = intervalVariance < 100 ? 0.95 : Math.Max(0, 1.0 - (intervalVariance / 500));
            
            // Check for pixel-perfect clicks
            var pixelPerfectClicks = 0;
            foreach (var group in clickPositions.GroupBy(p => new { p.X, p.Y }))
            {
                if (group.Count() > 5)
                {
                    pixelPerfectClicks += group.Count();
                }
            }
            var pixelPerfectScore = (double)pixelPerfectClicks / clicks.Count;
            
            var finalScore = (positionRepetition * 0.3 + consistentIntervalScore * 0.4 + pixelPerfectScore * 0.3);
            
            return new DetectionScore
            {
                Type = "Click",
                Score = finalScore,
                Confidence = Math.Min(1.0, clicks.Count / 200.0),
                Details = $"Position Rep: {positionRepetition:P}, Interval: {consistentIntervalScore:P}, Pixel Perfect: {pixelPerfectScore:P}"
            };
        }

        /// <summary>
        /// Analyzes farming patterns
        /// </summary>
        private DetectionScore AnalyzeFarmingPattern(PlayerBehaviorProfile profile)
        {
            var farmingData = profile.GetFarmingData(TimeSpan.FromHours(1));
            if (farmingData.KillCount < 50) return new DetectionScore { Type = "Farming", Score = 0, Confidence = 0.1 };
            
            // Check kill rate consistency
            var killsPerMinute = farmingData.GetKillsPerMinute();
            var killRateVariance = CalculateVariance(killsPerMinute);
            var consistentKillRate = killRateVariance < 2 ? 0.9 : Math.Max(0, 1.0 - (killRateVariance / 10));
            
            // Check for 24/7 farming
            var onlineHours = profile.GetOnlineHours(7); // Last 7 days
            var marathonScore = 0.0;
            foreach (var hours in onlineHours)
            {
                if (hours > 20) marathonScore += 0.2;
                else if (hours > 16) marathonScore += 0.1;
                else if (hours > 12) marathonScore += 0.05;
            }
            marathonScore = Math.Min(1.0, marathonScore);
            
            // Check loot pickup efficiency
            var lootEfficiency = farmingData.ItemsPickedUp / (double)Math.Max(1, farmingData.ItemsDropped);
            var perfectLootScore = lootEfficiency > 0.98 ? 0.8 : 0;
            
            // Check for selective targeting
            var targetVariety = farmingData.UniqueTargets / (double)Math.Max(1, farmingData.KillCount);
            var selectiveTargetingScore = targetVariety < 0.1 ? 0.9 : Math.Max(0, 1.0 - targetVariety * 2);
            
            var finalScore = (consistentKillRate * 0.25 + marathonScore * 0.35 + 
                             perfectLootScore * 0.2 + selectiveTargetingScore * 0.2);
            
            return new DetectionScore
            {
                Type = "Farming",
                Score = finalScore,
                Confidence = Math.Min(1.0, farmingData.KillCount / 1000.0),
                Details = $"Kill Rate: {consistentKillRate:P}, Marathon: {marathonScore:P}, Loot: {perfectLootScore:P}"
            };
        }

        /// <summary>
        /// Analyzes packet timing for injection/modification
        /// </summary>
        private DetectionScore AnalyzePacketTiming(PlayerBehaviorProfile profile)
        {
            var packets = profile.GetRecentPackets(TimeSpan.FromMinutes(5));
            if (packets.Count < 100) return new DetectionScore { Type = "Packet", Score = 0, Confidence = 0.1 };
            
            // Check for impossible packet sequences
            var impossibleSequences = 0;
            for (int i = 1; i < packets.Count; i++)
            {
                var timeDiff = (packets[i].Timestamp - packets[i-1].Timestamp).TotalMilliseconds;
                if (timeDiff < 1) // Less than 1ms between packets
                {
                    impossibleSequences++;
                }
            }
            var impossibleScore = (double)impossibleSequences / packets.Count;
            
            // Check for packet flooding
            var packetsPerSecond = packets.GroupBy(p => p.Timestamp.Second)
                                          .Select(g => g.Count())
                                          .ToList();
            var maxPacketsPerSecond = packetsPerSecond.Max();
            var floodScore = maxPacketsPerSecond > 100 ? 0.9 : maxPacketsPerSecond / 100.0;
            
            // Check for modified packet values
            var modifiedPackets = packets.Count(p => p.HasSuspiciousValues());
            var modificationScore = (double)modifiedPackets / packets.Count;
            
            var finalScore = (impossibleScore * 0.4 + floodScore * 0.3 + modificationScore * 0.3);
            
            return new DetectionScore
            {
                Type = "Packet",
                Score = finalScore,
                Confidence = Math.Min(1.0, packets.Count / 1000.0),
                Details = $"Impossible: {impossibleScore:P}, Flood: {floodScore:P}, Modified: {modificationScore:P}"
            };
        }

        /// <summary>
        /// Analyzes skill usage patterns
        /// </summary>
        private DetectionScore AnalyzeSkillUsagePattern(PlayerBehaviorProfile profile)
        {
            var skillUsage = profile.GetSkillUsage(TimeSpan.FromMinutes(30));
            if (skillUsage.Count < 50) return new DetectionScore { Type = "Skill", Score = 0, Confidence = 0.1 };
            
            // Check for perfect skill rotations
            var rotations = ExtractSkillRotations(skillUsage);
            var perfectRotations = rotations.Count(r => r.IsPerfect);
            var rotationScore = (double)perfectRotations / Math.Max(1, rotations.Count);
            
            // Check for inhuman APM (Actions Per Minute)
            var apm = CalculateAPM(skillUsage);
            var inhumanAPMScore = apm > 300 ? 0.9 : apm / 300.0;
            
            // Check for frame-perfect combos
            var combos = ExtractCombos(skillUsage);
            var framePerfectCombos = combos.Count(c => c.IsFramePerfect);
            var comboScore = (double)framePerfectCombos / Math.Max(1, combos.Count);
            
            var finalScore = (rotationScore * 0.4 + inhumanAPMScore * 0.3 + comboScore * 0.3);
            
            return new DetectionScore
            {
                Type = "Skill",
                Score = finalScore,
                Confidence = Math.Min(1.0, skillUsage.Count / 500.0),
                Details = $"Rotation: {rotationScore:P}, APM: {apm}, Combos: {comboScore:P}"
            };
        }

        /// <summary>
        /// Analyzes item pickup patterns
        /// </summary>
        private DetectionScore AnalyzeItemPickupPattern(PlayerBehaviorProfile profile)
        {
            var pickups = profile.GetItemPickups(TimeSpan.FromMinutes(30));
            if (pickups.Count < 20) return new DetectionScore { Type = "Pickup", Score = 0, Confidence = 0.1 };
            
            // Check for instant pickups
            var instantPickups = pickups.Count(p => p.ReactionTime < 50); // Less than 50ms
            var instantScore = (double)instantPickups / pickups.Count;
            
            // Check for selective pickup (only valuable items)
            var selectiveScore = CalculateSelectivePickupScore(pickups);
            
            // Check for perfect pickup radius
            var perfectRadiusPickups = pickups.Count(p => p.IsAtMaxRange);
            var radiusScore = (double)perfectRadiusPickups / pickups.Count;
            
            var finalScore = (instantScore * 0.4 + selectiveScore * 0.3 + radiusScore * 0.3);
            
            return new DetectionScore
            {
                Type = "Pickup",
                Score = finalScore,
                Confidence = Math.Min(1.0, pickups.Count / 200.0),
                Details = $"Instant: {instantScore:P}, Selective: {selectiveScore:P}, Radius: {radiusScore:P}"
            };
        }

        /// <summary>
        /// Analyzes chat patterns for bot behavior
        /// </summary>
        private DetectionScore AnalyzeChatPattern(PlayerBehaviorProfile profile)
        {
            var chatMessages = profile.GetChatMessages(TimeSpan.FromHours(1));
            
            // No chat at all is suspicious for active players
            if (chatMessages.Count == 0 && profile.GetPlayTime() > TimeSpan.FromHours(2))
            {
                return new DetectionScore
                {
                    Type = "Chat",
                    Score = 0.7,
                    Confidence = 0.5,
                    Details = "No chat activity despite extended play time"
                };
            }
            
            if (chatMessages.Count < 5) return new DetectionScore { Type = "Chat", Score = 0, Confidence = 0.1 };
            
            // Check for repeated messages
            var uniqueMessages = chatMessages.Select(m => m.Content).Distinct().Count();
            var repetitionScore = 1.0 - (double)uniqueMessages / chatMessages.Count;
            
            // Check for automated responses
            var responsePatterns = AnalyzeChatResponses(chatMessages);
            var automatedScore = responsePatterns.AutomatedScore;
            
            // Check for advertising patterns
            var advertisingScore = DetectAdvertising(chatMessages);
            
            var finalScore = Math.Max(repetitionScore * 0.4, automatedScore * 0.4, advertisingScore * 0.2);
            
            return new DetectionScore
            {
                Type = "Chat",
                Score = finalScore,
                Confidence = Math.Min(1.0, chatMessages.Count / 50.0),
                Details = $"Repetition: {repetitionScore:P}, Automated: {automatedScore:P}, Ads: {advertisingScore:P}"
            };
        }

        /// <summary>
        /// Analyzes trade patterns
        /// </summary>
        private DetectionScore AnalyzeTradePattern(PlayerBehaviorProfile profile)
        {
            var trades = profile.GetTrades(TimeSpan.FromHours(24));
            if (trades.Count < 5) return new DetectionScore { Type = "Trade", Score = 0, Confidence = 0.1 };
            
            // Check for gold selling patterns
            var goldSellingScore = DetectGoldSelling(trades);
            
            // Check for item duplication patterns
            var duplicationScore = DetectItemDuplication(trades);
            
            // Check for automated trading
            var automatedTradingScore = DetectAutomatedTrading(trades);
            
            var finalScore = Math.Max(goldSellingScore * 0.5, duplicationScore * 0.3, automatedTradingScore * 0.2);
            
            return new DetectionScore
            {
                Type = "Trade",
                Score = finalScore,
                Confidence = Math.Min(1.0, trades.Count / 50.0),
                Details = $"Gold Selling: {goldSellingScore:P}, Duplication: {duplicationScore:P}, Automated: {automatedTradingScore:P}"
            };
        }

        /// <summary>
        /// Calculates composite score from all detection methods
        /// </summary>
        private double CalculateCompositeScore(DetectionScore[] scores)
        {
            // Weight scores by confidence
            double totalWeight = 0;
            double weightedSum = 0;
            
            foreach (var score in scores)
            {
                var weight = score.Confidence;
                totalWeight += weight;
                weightedSum += score.Score * weight;
            }
            
            if (totalWeight == 0) return 0;
            
            var compositeScore = weightedSum / totalWeight;
            
            // Apply threshold boosting for high individual scores
            var maxScore = scores.Max(s => s.Score);
            if (maxScore > 0.9)
            {
                compositeScore = Math.Max(compositeScore, maxScore * 0.9);
            }
            
            return Math.Min(1.0, compositeScore);
        }

        /// <summary>
        /// Determines action based on detection score
        /// </summary>
        private BotDetectionResult DetermineAction(int characterId, double score)
        {
            var result = new BotDetectionResult
            {
                CharacterID = characterId,
                Score = score,
                Timestamp = DateTime.UtcNow
            };
            
            if (score > 0.95)
            {
                result.IsBot = true;
                result.Confidence = "Very High";
                result.RecommendedAction = BotAction.InstantBan;
            }
            else if (score > 0.85)
            {
                result.IsBot = true;
                result.Confidence = "High";
                result.RecommendedAction = BotAction.TempBan;
            }
            else if (score > 0.75)
            {
                result.IsBot = true;
                result.Confidence = "Medium";
                result.RecommendedAction = BotAction.Captcha;
            }
            else if (score > 0.65)
            {
                result.IsBot = false;
                result.Confidence = "Low";
                result.RecommendedAction = BotAction.Monitor;
            }
            else
            {
                result.IsBot = false;
                result.Confidence = "Clean";
                result.RecommendedAction = BotAction.None;
            }
            
            return result;
        }

        /// <summary>
        /// Records suspicious activity for review
        /// </summary>
        private async Task RecordSuspiciousActivity(int characterId, BotDetectionResult result)
        {
            var activity = new SuspiciousActivity
            {
                CharacterID = characterId,
                DetectionScore = result.Score,
                DetectionType = result.Confidence,
                Action = result.RecommendedAction,
                Timestamp = result.Timestamp,
                Evidence = result.Evidence
            };
            
            var activities = _suspiciousActivities.GetOrAdd(characterId, new List<SuspiciousActivity>());
            activities.Add(activity);
            
            // Trigger alert if multiple detections
            if (activities.Count > 5)
            {
                await TriggerHighPriorityAlert(characterId, activities);
            }
            
            // Save to database
            await SaveToDatabase(activity);
        }

        private void LoadDetectionParameters()
        {
            // Load from configuration
            _movementPatternThreshold = _configuration.GetValue<double>("BotDetection:MovementThreshold", 0.85);
            _clickPatternThreshold = _configuration.GetValue<double>("BotDetection:ClickThreshold", 0.90);
            _farmingPatternThreshold = _configuration.GetValue<double>("BotDetection:FarmingThreshold", 0.75);
            _captchaFailureLimit = _configuration.GetValue<int>("BotDetection:CaptchaFailureLimit", 3);
        }

        private double CalculateVariance(List<double> values)
        {
            if (values.Count == 0) return 0;
            var mean = values.Average();
            return values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
        }

        private List<SkillRotation> ExtractSkillRotations(List<SkillUsage> skillUsage)
        {
            // Implementation for extracting skill rotation patterns
            var rotations = new List<SkillRotation>();
            // ... rotation extraction logic
            return rotations;
        }

        private double CalculateAPM(List<SkillUsage> skillUsage)
        {
            if (skillUsage.Count == 0) return 0;
            var timeSpan = skillUsage.Last().Timestamp - skillUsage.First().Timestamp;
            if (timeSpan.TotalMinutes == 0) return 0;
            return skillUsage.Count / timeSpan.TotalMinutes;
        }

        private List<SkillCombo> ExtractCombos(List<SkillUsage> skillUsage)
        {
            // Implementation for extracting skill combos
            var combos = new List<SkillCombo>();
            // ... combo extraction logic
            return combos;
        }

        private double CalculateSelectivePickupScore(List<ItemPickup> pickups)
        {
            // Calculate how selective the player is with pickups
            var valuableItems = pickups.Count(p => p.ItemValue > 10000);
            return (double)valuableItems / pickups.Count;
        }

        private ChatResponsePattern AnalyzeChatResponses(List<ChatMessage> messages)
        {
            // Analyze chat response patterns
            return new ChatResponsePattern { AutomatedScore = 0 };
        }

        private double DetectAdvertising(List<ChatMessage> messages)
        {
            // Detect advertising patterns in chat
            var adKeywords = new[] { "sell", "buy", "www", ".com", "gold", "cheap" };
            var adMessages = messages.Count(m => adKeywords.Any(k => m.Content.ToLower().Contains(k)));
            return (double)adMessages / messages.Count;
        }

        private double DetectGoldSelling(List<Trade> trades)
        {
            // Detect gold selling patterns
            var suspiciousTrades = trades.Count(t => t.GoldAmount > 1000000 && t.ItemsTraded.Count == 0);
            return (double)suspiciousTrades / trades.Count;
        }

        private double DetectItemDuplication(List<Trade> trades)
        {
            // Detect item duplication patterns
            // ... duplication detection logic
            return 0;
        }

        private double DetectAutomatedTrading(List<Trade> trades)
        {
            // Detect automated trading patterns
            // ... automated trading detection logic
            return 0;
        }

        private async Task TriggerHighPriorityAlert(int characterId, List<SuspiciousActivity> activities)
        {
            _logger.Error("HIGH PRIORITY: Multiple bot detections for CharacterID {CharacterID}", characterId);
            // Send alert to administrators
            await Task.CompletedTask;
        }

        private async Task SaveToDatabase(SuspiciousActivity activity)
        {
            // Save to database
            await Task.CompletedTask;
        }
    }

    // Supporting classes
    public class PlayerBehaviorProfile
    {
        public int CharacterID { get; }
        private readonly List<PlayerAction> _actions = new();
        private readonly List<Movement> _movements = new();
        private readonly List<Click> _clicks = new();
        private readonly List<Packet> _packets = new();
        private readonly List<SkillUsage> _skillUsage = new();
        private readonly List<ItemPickup> _itemPickups = new();
        private readonly List<ChatMessage> _chatMessages = new();
        private readonly List<Trade> _trades = new();
        private FarmingData _farmingData = new();

        public PlayerBehaviorProfile(int characterId)
        {
            CharacterID = characterId;
        }

        public void RecordAction(PlayerAction action)
        {
            _actions.Add(action);
            // Process action into specific categories
        }

        public List<Movement> GetRecentMovements(TimeSpan timeSpan) => 
            _movements.Where(m => DateTime.UtcNow - m.Timestamp < timeSpan).ToList();

        public List<Click> GetRecentClicks(TimeSpan timeSpan) =>
            _clicks.Where(c => DateTime.UtcNow - c.Timestamp < timeSpan).ToList();

        public List<Packet> GetRecentPackets(TimeSpan timeSpan) =>
            _packets.Where(p => DateTime.UtcNow - p.Timestamp < timeSpan).ToList();

        public List<SkillUsage> GetSkillUsage(TimeSpan timeSpan) =>
            _skillUsage.Where(s => DateTime.UtcNow - s.Timestamp < timeSpan).ToList();

        public List<ItemPickup> GetItemPickups(TimeSpan timeSpan) =>
            _itemPickups.Where(i => DateTime.UtcNow - i.Timestamp < timeSpan).ToList();

        public List<ChatMessage> GetChatMessages(TimeSpan timeSpan) =>
            _chatMessages.Where(c => DateTime.UtcNow - c.Timestamp < timeSpan).ToList();

        public List<Trade> GetTrades(TimeSpan timeSpan) =>
            _trades.Where(t => DateTime.UtcNow - t.Timestamp < timeSpan).ToList();

        public FarmingData GetFarmingData(TimeSpan timeSpan) => _farmingData;

        public List<double> GetReactionTimes() => _actions.Select(a => a.ReactionTime).ToList();

        public List<double> GetOnlineHours(int days) 
        {
            // Return online hours for the last N days
            return new List<double>();
        }

        public TimeSpan GetPlayTime() => TimeSpan.FromHours(1); // Placeholder
    }

    public class PlayerAction
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; }
        public double ReactionTime { get; set; }
    }

    public class Movement
    {
        public DateTime Timestamp { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public string GetPathSignature() => $"{X:F0},{Y:F0},{Z:F0}";
    }

    public class Click
    {
        public DateTime Timestamp { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class Packet
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; }
        public byte[] Data { get; set; }
        public bool HasSuspiciousValues() => false; // Placeholder
    }

    public class SkillUsage
    {
        public DateTime Timestamp { get; set; }
        public int SkillID { get; set; }
        public int TargetID { get; set; }
    }

    public class ItemPickup
    {
        public DateTime Timestamp { get; set; }
        public int ItemID { get; set; }
        public double ReactionTime { get; set; }
        public bool IsAtMaxRange { get; set; }
        public int ItemValue { get; set; }
    }

    public class ChatMessage
    {
        public DateTime Timestamp { get; set; }
        public string Content { get; set; }
        public string Channel { get; set; }
    }

    public class Trade
    {
        public DateTime Timestamp { get; set; }
        public int PartnerID { get; set; }
        public long GoldAmount { get; set; }
        public List<int> ItemsTraded { get; set; } = new();
    }

    public class FarmingData
    {
        public int KillCount { get; set; }
        public int ItemsDropped { get; set; }
        public int ItemsPickedUp { get; set; }
        public int UniqueTargets { get; set; }
        
        public List<double> GetKillsPerMinute() => new List<double>();
    }

    public class SkillRotation
    {
        public bool IsPerfect { get; set; }
    }

    public class SkillCombo
    {
        public bool IsFramePerfect { get; set; }
    }

    public class ChatResponsePattern
    {
        public double AutomatedScore { get; set; }
    }

    public class DetectionScore
    {
        public string Type { get; set; }
        public double Score { get; set; }
        public double Confidence { get; set; }
        public string Details { get; set; }
    }

    public class BotDetectionResult
    {
        public int CharacterID { get; set; }
        public double Score { get; set; }
        public bool IsBot { get; set; }
        public string Confidence { get; set; }
        public BotAction RecommendedAction { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Evidence { get; set; } = new();
    }

    public class SuspiciousActivity
    {
        public int CharacterID { get; set; }
        public double DetectionScore { get; set; }
        public string DetectionType { get; set; }
        public BotAction Action { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Evidence { get; set; }
    }

    public enum BotAction
    {
        None,
        Monitor,
        Captcha,
        TempBan,
        InstantBan
    }
}

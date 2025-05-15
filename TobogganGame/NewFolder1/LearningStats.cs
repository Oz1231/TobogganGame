using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TobogganGame
{
    /// <summary>
    /// Learning statistics to track AI progress
    /// </summary>
    public class LearningStats
    {
        public int TrainingSteps { get; set; } = 0;
        public double TotalReward { get; set; } = 0;
        public int EpisodeCounter { get; set; } = 0;
        public double AverageLoss { get; set; } = 0;
        public double ExplorationRate { get; set; } = 0.99;
        public List<double> RewardHistory { get; set; } = new List<double>();
        public List<double> LossHistory { get; set; } = new List<double>();
        public List<int> ScoreHistory { get; set; } = new List<int>();
        public int MaxScore { get; set; } = 0;
        public int TotalGamesPlayed { get; set; } = 0;
        public double AccumulatedReward { get; set; } = 0;
        public int LastEpisodeScore { get; set; } = 0;

        // New tracking metrics
        public List<int> RecentScores { get; private set; } = new List<int>();
        public int RecentGamesWithScore { get; private set; } = 0;
        public DateTime TrainingStartTime { get; private set; } = DateTime.Now;

        // Maximum Loss allowed for display - prevents extreme values disrupting UI
        private const double MaxDisplayLoss = 50.0;

        // Historical tracking of min/max loss for scaling
        public double MinLoss { get; private set; } = double.MaxValue;
        public double MaxLoss { get; private set; } = double.MinValue;

        /// <summary>
        /// Initializes a new instance of the LearningStats class
        /// </summary>
        public LearningStats()
        {
            TrainingStartTime = DateTime.Now;
        }

        /// <summary>
        /// Saves statistics to a file in binary format
        /// </summary>
        /// <param name="filePath">Path to save the file</param>
        public void SaveToFile(string filePath)
        {
            try
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
                {
                    writer.Write(TrainingSteps);
                    writer.Write(TotalReward);
                    writer.Write(EpisodeCounter);
                    writer.Write(AverageLoss);
                    writer.Write(ExplorationRate);
                    writer.Write(MaxScore);
                    writer.Write(TotalGamesPlayed);
                    writer.Write(AccumulatedReward);
                    writer.Write(MinLoss);
                    writer.Write(MaxLoss);
                    writer.Write(RecentGamesWithScore);

                    // Write DateTime
                    writer.Write(TrainingStartTime.Ticks);

                    // Save recent history (last 100 entries)
                    int rewardCount = Math.Min(RewardHistory.Count, 100);
                    writer.Write(rewardCount);
                    for (int i = RewardHistory.Count - rewardCount; i < RewardHistory.Count; i++)
                    {
                        writer.Write(RewardHistory[i]);
                    }

                    int lossCount = Math.Min(LossHistory.Count, 100);
                    writer.Write(lossCount);
                    for (int i = LossHistory.Count - lossCount; i < LossHistory.Count; i++)
                    {
                        writer.Write(LossHistory[i]);
                    }

                    int scoreCount = Math.Min(ScoreHistory.Count, 100);
                    writer.Write(scoreCount);
                    for (int i = ScoreHistory.Count - scoreCount; i < ScoreHistory.Count; i++)
                    {
                        writer.Write(ScoreHistory[i]);
                    }

                    // Save recent scores
                    int recentScoreCount = RecentScores.Count;
                    writer.Write(recentScoreCount);
                    for (int i = 0; i < recentScoreCount; i++)
                    {
                        writer.Write(RecentScores[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving learning stats: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads statistics from a file
        /// </summary>
        /// <param name="filePath">Path to the file to load</param>
        /// <returns>True if successfully loaded, false otherwise</returns>
        public bool LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
                {
                    TrainingSteps = reader.ReadInt32();
                    TotalReward = reader.ReadDouble();
                    EpisodeCounter = reader.ReadInt32();
                    AverageLoss = reader.ReadDouble();
                    ExplorationRate = reader.ReadDouble();
                    MaxScore = reader.ReadInt32();
                    TotalGamesPlayed = reader.ReadInt32();
                    AccumulatedReward = reader.ReadDouble();

                    // Try to read min/max loss values if they exist
                    try
                    {
                        MinLoss = reader.ReadDouble();
                        MaxLoss = reader.ReadDouble();
                        RecentGamesWithScore = reader.ReadInt32();

                        // Try to read DateTime
                        try
                        {
                            long ticks = reader.ReadInt64();
                            TrainingStartTime = new DateTime(ticks);
                        }
                        catch
                        {
                            TrainingStartTime = DateTime.Now;
                        }
                    }
                    catch
                    {
                        // Use defaults if not in the file
                        MinLoss = 0;
                        MaxLoss = 1;
                        RecentGamesWithScore = 0;
                        TrainingStartTime = DateTime.Now;
                    }

                    RewardHistory.Clear();
                    int rewardCount = reader.ReadInt32();
                    for (int i = 0; i < rewardCount; i++)
                    {
                        RewardHistory.Add(reader.ReadDouble());
                    }

                    LossHistory.Clear();
                    int lossCount = reader.ReadInt32();
                    for (int i = 0; i < lossCount; i++)
                    {
                        double lossValue = reader.ReadDouble();
                        // Validate loss value
                        if (!double.IsNaN(lossValue) && !double.IsInfinity(lossValue))
                        {
                            LossHistory.Add(lossValue);

                            // Update min/max with higher cap
                            if (lossValue < MinLoss) MinLoss = lossValue;
                            if (lossValue > MaxLoss) MaxLoss = Math.Min(lossValue, 200.0);
                        }
                    }

                    ScoreHistory.Clear();
                    int scoreCount = reader.ReadInt32();
                    for (int i = 0; i < scoreCount; i++)
                    {
                        ScoreHistory.Add(reader.ReadInt32());
                    }

                    // Try to read recent scores
                    try
                    {
                        RecentScores.Clear();
                        int recentScoreCount = reader.ReadInt32();
                        for (int i = 0; i < recentScoreCount; i++)
                        {
                            RecentScores.Add(reader.ReadInt32());
                        }
                    }
                    catch
                    {
                        // If not present, leave empty
                        RecentScores.Clear();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading learning stats: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Records the result of a completed game
        /// </summary>
        /// <param name="score">Final score of the game</param>
        /// <param name="episodeReward">Total reward earned in the episode</param>
        public void RecordGameResult(int score, double episodeReward)
        {
            TotalGamesPlayed++;
            MaxScore = Math.Max(MaxScore, score);
            AccumulatedReward += episodeReward;
            LastEpisodeScore = score;

            ScoreHistory.Add(score);
            RewardHistory.Add(episodeReward);

            // Track recent scores for adaptive learning
            RecentScores.Add(score);
            if (RecentScores.Count > 50) // Keep last 50 scores
                RecentScores.RemoveAt(0);

            // Count games with non-zero score
            if (score > 0)
                RecentGamesWithScore++;

            // Keep history at a reasonable size
            if (ScoreHistory.Count > 1000)
                ScoreHistory.RemoveAt(0);
            if (RewardHistory.Count > 1000)
                RewardHistory.RemoveAt(0);
        }

        /// <summary>
        /// Adds a new loss value to the history with adaptive smoothing
        /// </summary>
        /// <param name="loss">The loss value to record</param>
        public void AddLossValue(double loss)
        {
            // Only add valid loss values
            if (!double.IsNaN(loss) && !double.IsInfinity(loss))
            {
                // Apply adaptive smoothing with previous value if available
                if (LossHistory.Count > 0)
                {
                    double previous = LossHistory.Last();

                    // Dynamic smoothing factor based on magnitude of change
                    double smoothingFactor;

                    if (loss > previous * 5)
                    {
                        // Very large jump, minimal impact (2%)
                        smoothingFactor = 0.02;
                    }
                    else if (loss > previous * 3)
                    {
                        // Large jump, small impact (5%)
                        smoothingFactor = 0.05;
                    }
                    else if (loss > previous * 2)
                    {
                        // Medium jump (10%)
                        smoothingFactor = 0.1;
                    }
                    else
                    {
                        // Reasonable change (20%)
                        smoothingFactor = 0.2;
                    }

                    // Apply smoothing
                    loss = (smoothingFactor * loss) + ((1 - smoothingFactor) * previous);
                }

                // Limit loss value before adding to history
                loss = Math.Min(loss, 200.0);

                LossHistory.Add(loss);

                // Update min/max for scaling
                if (loss < MinLoss) MinLoss = loss;
                if (loss > MaxLoss) MaxLoss = Math.Min(loss, 200.0);

                // Trim history if needed
                if (LossHistory.Count > 1000)
                    LossHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Gets the average score from recent games
        /// </summary>
        /// <param name="count">Number of recent games to average</param>
        /// <returns>Average score</returns>
        public double GetRecentAverageScore(int count = 20)
        {
            if (ScoreHistory.Count == 0)
                return 0;

            count = Math.Min(count, ScoreHistory.Count);
            return ScoreHistory.Skip(ScoreHistory.Count - count).Average();
        }

        /// <summary>
        /// Gets the average loss from recent training steps with outlier rejection
        /// </summary>
        /// <param name="count">Number of recent steps to average</param>
        /// <returns>Average loss value</returns>
        public double GetRecentAverageLoss(int count = 50)
        {
            if (LossHistory.Count == 0)
                return 0;

            count = Math.Min(count, LossHistory.Count);

            // Get recent loss values
            var recentLosses = LossHistory.Skip(LossHistory.Count - count).ToList();

            // Filter out any extreme values
            var validLosses = recentLosses
                .Where(l => !double.IsNaN(l) && !double.IsInfinity(l) && l < 200.0)
                .ToList();

            if (validLosses.Count == 0)
                return 0;

            // Compute average with outlier rejection
            double mean = validLosses.Average();
            double stdDev = Math.Sqrt(validLosses.Select(l => Math.Pow(l - mean, 2)).Average());

            // Keep values within 4 standard deviations of the mean
            var normalizedLosses = validLosses
                .Where(l => Math.Abs(l - mean) <= 4 * stdDev)
                .ToList();

            if (normalizedLosses.Count == 0)
                return Math.Min(mean, MaxDisplayLoss);

            double result = normalizedLosses.Average();

            return Math.Min(result, MaxDisplayLoss);
        }

        /// <summary>
        /// Gets a normalized loss value for visualization (0.0 - 1.0 range) with logarithmic scaling
        /// </summary>
        /// <returns>Normalized loss value between 0 and 1</returns>
        public double GetNormalizedLoss()
        {
            if (LossHistory.Count == 0)
                return 0;

            double currentLoss = GetRecentAverageLoss(10);

            // Handle the case where min and max are the same or invalid
            if (MinLoss >= MaxLoss || double.IsNaN(MinLoss) || double.IsInfinity(MinLoss) ||
                double.IsNaN(MaxLoss) || double.IsInfinity(MaxLoss))
            {
                return 0.5; // Default to middle value
            }

            // Normalize between 0 and 1 with logarithmic scaling for better visualization
            double logMin = Math.Log10(Math.Max(0.001, MinLoss));
            double logMax = Math.Log10(Math.Max(0.01, MaxLoss));
            double logCurrent = Math.Log10(Math.Max(0.001, currentLoss));

            // Normalized calculation on logarithmic scale
            double normalized = (logCurrent - logMin) / (logMax - logMin);

            // Clamp to valid range
            return Math.Max(0, Math.Min(1, normalized));
        }

        /// <summary>
        /// Gets success rate (percentage of games with score > 0) in recent games
        /// </summary>
        /// <param name="count">Number of recent games to check</param>
        /// <returns>Success rate as a value between 0 and 1</returns>
        public double GetRecentSuccessRate(int count = 50)
        {
            if (ScoreHistory.Count == 0)
                return 0;

            count = Math.Min(count, ScoreHistory.Count);
            var recentScores = ScoreHistory.Skip(ScoreHistory.Count - count).ToList();

            int successCount = recentScores.Count(s => s > 0);
            return (double)successCount / recentScores.Count;
        }

        /// <summary>
        /// Gets total training time in hours
        /// </summary>
        /// <returns>Training time in hours</returns>
        public double GetTrainingTimeHours()
        {
            TimeSpan elapsed = DateTime.Now - TrainingStartTime;
            return elapsed.TotalHours;
        }
    }
}
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

        // Maximum Loss allowed for display and storage - more restricted
        private const double MaxDisplayLoss = 40.0;
        private const double MaxStoredLoss = 40.0;

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
                        MinLoss = 0.01;
                        MaxLoss = 1.0;
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
                            // Apply stricter bounds on loaded values
                            lossValue = Math.Min(Math.Max(0.01, lossValue), MaxStoredLoss);
                            LossHistory.Add(lossValue);

                            // Update min/max with higher cap
                            if (lossValue < MinLoss) MinLoss = lossValue;
                            if (lossValue > MaxLoss) MaxLoss = Math.Min(lossValue, MaxStoredLoss);
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
        /// Adds a new loss value to the history with very aggressive smoothing for stability
        /// </summary>
        /// <param name="loss">The loss value to record</param>
        public void AddLossValue(double loss)
        {
            // Only add valid loss values
            if (double.IsNaN(loss) || double.IsInfinity(loss))
            {
                // Skip invalid loss values completely
                return;
            }

            // More aggressive bounds - prevent any extreme values
            loss = Math.Min(Math.Max(0.01, loss), MaxStoredLoss);

            // If history exists, apply improved smoothing
            if (LossHistory.Count > 0)
            {
                double previous = LossHistory[LossHistory.Count - 1];

                // Very aggressive smoothing to prevent jumps
                // Use weighted average with 90% previous value, 10% new value
                double smoothingFactor = 0.1; // Only 10% of new value

                // Even more aggressive for very large changes
                if (loss > previous * 2 || loss < previous / 2)
                {
                    smoothingFactor = 0.05; // Only 5% of new value for big jumps
                }

                // Apply the smoothing
                loss = (smoothingFactor * loss) + ((1 - smoothingFactor) * previous);
            }

            // Add to history with final bounds check
            loss = Math.Min(Math.Max(0.01, loss), MaxStoredLoss);
            LossHistory.Add(loss);

            // Update min/max for scaling
            if (loss < MinLoss) MinLoss = loss;
            if (loss > MaxLoss) MaxLoss = Math.Min(loss, MaxStoredLoss);

            // Trim history if needed
            if (LossHistory.Count > 1000)
                LossHistory.RemoveAt(0);

            // Check for patterns every time we add a value
            if (LossHistory.Count % 5 == 0)
            {
                CheckLossPatternAndReset();
            }
        }

        /// <summary>
        /// Gets the average score from recent games
        /// </summary>
        /// <param name="count">Number of recent games to average</param>
        public double GetRecentAverageScore(int count = 20)
        {
            if (ScoreHistory.Count == 0)
                return 0;

            count = Math.Min(count, ScoreHistory.Count);
            return ScoreHistory.Skip(ScoreHistory.Count - count).Average();
        }

        /// <summary>
        /// Gets the average loss from recent training steps with maximum stability
        /// </summary>
        /// <param name="count">Number of recent steps to average</param>
        public double GetRecentAverageLoss(int count = 50)
        {
            if (LossHistory.Count == 0)
                return 0;

            // Limit count to available history
            count = Math.Min(count, LossHistory.Count);

            // Use median instead of mean - much more robust to outliers
            List<double> recentLosses = new List<double>();
            int startIndex = Math.Max(0, LossHistory.Count - count);
            for (int i = startIndex; i < LossHistory.Count; i++)
            {
                recentLosses.Add(LossHistory[i]);
            }

            if (recentLosses.Count == 0)
                return 0;

            // Sort and take median value
            recentLosses.Sort();
            double median;

            if (recentLosses.Count % 2 == 0)
            {
                // Even count - average middle two values
                int midIndex = recentLosses.Count / 2;
                median = (recentLosses[midIndex - 1] + recentLosses[midIndex]) / 2.0;
            }
            else
            {
                // Odd count - take middle value
                median = recentLosses[recentLosses.Count / 2];
            }

            // Hard cap at display limit
            return Math.Min(median, MaxDisplayLoss);
        }

        /// <summary>
        /// Check for problematic patterns in loss history and fix them
        /// </summary>
        public void CheckLossPatternAndReset()
        {
            if (LossHistory.Count < 10) return;

            // Get last 10 values
            List<double> recent = new List<double>();
            int startIndex = Math.Max(0, LossHistory.Count - 10);
            for (int i = startIndex; i < LossHistory.Count; i++)
            {
                recent.Add(LossHistory[i]);
            }

            // Check for extreme values or large oscillations
            bool hasExtremeValues = false;
            bool hasOscillations = false;

            // Check for extreme values
            for (int i = 0; i < recent.Count; i++)
            {
                if (recent[i] > 20.0 || recent[i] < 0.001)
                {
                    hasExtremeValues = true;
                    break;
                }
            }

            // Check for oscillations
            if (recent.Count >= 4)
            {
                for (int i = 0; i < recent.Count - 3; i++)
                {
                    if ((recent[i] > recent[i + 1] * 2 && recent[i + 1] < recent[i + 2] / 2) ||
                        (recent[i] < recent[i + 1] / 2 && recent[i + 1] > recent[i + 2] * 2))
                    {
                        hasOscillations = true;
                        break;
                    }
                }
            }

            // If problems detected, fix them
            if (hasExtremeValues || hasOscillations)
            {
                StabilizeLoss();
            }
        }

        /// <summary>
        /// Stabilizes loss history by replacing extreme values with more reasonable ones
        /// </summary>
        public void StabilizeLoss()
        {
            if (LossHistory.Count < 10) return;

            // Calculate stable average from earlier history
            double stableValue = 1.0; // Default value
            int stableCount = 0;

            // Try to find a stable section of history
            if (LossHistory.Count > 30)
            {
                List<double> candidates = new List<double>();
                for (int i = 0; i < LossHistory.Count - 10; i++)
                {
                    if (LossHistory[i] >= 0.01 && LossHistory[i] <= 10.0)
                    {
                        candidates.Add(LossHistory[i]);
                    }
                }

                if (candidates.Count > 5)
                {
                    // Sort and take median
                    candidates.Sort();
                    stableValue = candidates[candidates.Count / 2];
                }
                else
                {
                    // If no good candidates, use conservative default
                    stableValue = 1.0;
                }
            }

            // Replace problematic values in the last 10 entries
            for (int i = Math.Max(0, LossHistory.Count - 10); i < LossHistory.Count; i++)
            {
                double current = LossHistory[i];

                // If the value is extreme
                if (current > 10.0 || current < 0.01 ||
                    (i > 0 && (current > LossHistory[i - 1] * 3 || current < LossHistory[i - 1] / 3)))
                {
                    // Replace with a value closer to the stable value
                    LossHistory[i] = stableValue;
                }
            }

            // Fix min/max after stabilization
            UpdateMinMaxLoss();
        }

        /// <summary>
        /// Updates min/max loss values after stabilization
        /// </summary>
        private void UpdateMinMaxLoss()
        {
            if (LossHistory.Count == 0) return;

            MinLoss = double.MaxValue;
            MaxLoss = 0.0;

            foreach (double loss in LossHistory)
            {
                if (loss < MinLoss) MinLoss = loss;
                if (loss > MaxLoss && loss <= MaxStoredLoss) MaxLoss = loss;
            }

            // Ensure reasonable min/max values
            MinLoss = Math.Max(0.01, MinLoss);
            MaxLoss = Math.Min(MaxStoredLoss, Math.Max(1.0, MaxLoss));

            // Ensure min is less than max
            if (MinLoss >= MaxLoss)
            {
                MinLoss = Math.Min(MinLoss, 0.1);
                MaxLoss = Math.Max(MaxLoss, 1.0);
            }
        }
    }
}
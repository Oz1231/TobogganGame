using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;

namespace TobogganGame
{
    /// <summary>
    /// AI Controller with learning capabilities for better flag collection
    /// </summary>
    public class AIController
    {
        #region Fields and Properties

        // Game state
        private GameEngine gameEngine;
        private int gridWidth;
        private int gridHeight;

        // Neural networks
        private NeuralNetwork qNetwork;
        private NeuralNetwork targetNetwork;

        // Neural network structure
        private const int InputSize = 32;
        private const int HiddenSize = 128;
        private const int OutputSize = 8;

        // Learning parameters
        private double currentLearningRate;
        private const double InitialLearningRate = 0.0005;  
        private const double LearningRateDecay = 0.9998;    
        private const double MinLearningRate = 0.0001;

        private const double DiscountFactor = 0.95;
        private const double InitialExplorationRate = 0.99; 
        private const double ExplorationRateDecay = 0.9998;
        private const double MinExplorationRate = 0.05;      

        // Target network updates
        private const double TargetNetworkTau = 0.05;
        private const int HardUpdateFrequency = 250;
        private int updateCounter = 0;

        // Experience replay buffer
        private List<Experience> replayBuffer;
        private const int ReplayBufferCapacity = 20000;
        private const int MinBufferSizeForTraining = 2000;
        private const int BatchSize = 192;

        // Learning frequency control
        private int frameCounter = 0;
        private int learningFrequency = 1;

        // Position tracking
        private Queue<Point> positionHistory;
        private const int PositionHistorySize = 15;
        private double distanceToFlagPrevious = 0;

        // Flag collection tracking
        private int framesSinceLastFlag = 0;
        private const int DirectMoveThreshold = 10;
        private double minDistanceToFlag = double.MaxValue;

        // State representation
        private double[] currentState;
        private int lastAction;

        // Ray casting
        private Random random;
        private RayInfo[] currentRays;
        private readonly Point[] directions = new Point[]
        {
            new Point(0, -1),   // Up
            new Point(1, -1),   // Up-right
            new Point(1, 0),    // Right
            new Point(1, 1),    // Down-right
            new Point(0, 1),    // Down
            new Point(-1, 1),   // Down-left
            new Point(-1, 0),   // Left
            new Point(-1, -1)   // Up-left
        };

        // Save/load file paths
        private const string NetworkWeightsFile = "improved_nn_weights.dat";
        private const string StatsFile = "improved_nn_stats.dat";
        private const string BufferFile = "replay_buffer.dat";

        // Buffer saving settings
        private const int AutoSaveInterval = 50; 
        private int gamesSinceLastBufferSave = 0;

        // Training statistics
        public LearningStats Stats { get; private set; } = new LearningStats();
        private double currentEpisodeReward = 0;

        // Training mode flag
        public bool TrainingMode { get; private set; } = false;

        // Performance tracking
        private List<double> recentQValues = new List<double>();
        private bool adaptiveLearningEnabled = true;

        #endregion

        #region Initialization and Reset

        /// <summary>
        /// Creates a new AI controller
        /// </summary>
        public AIController(GameEngine gameEngine)
        {
            this.gameEngine = gameEngine;
            this.gridWidth = gameEngine.GetGridWidth();
            this.gridHeight = gameEngine.GetGridHeight();

            // Set initial learning rate
            currentLearningRate = InitialLearningRate;

            // Initialize neural networks
            qNetwork = new NeuralNetwork(InputSize, HiddenSize, OutputSize);
            qNetwork.SetLearningRate(currentLearningRate);
            targetNetwork = qNetwork.Clone();

            // Initialize random number generator
            random = new Random();

            // Initialize experience replay buffer
            replayBuffer = new List<Experience>();

            // Initialize position history
            positionHistory = new Queue<Point>();

            // Try to load saved weights and stats
            bool weightsLoaded = qNetwork.LoadWeights(NetworkWeightsFile);
            bool statsLoaded = Stats.LoadFromFile(StatsFile);
            bool bufferLoaded = LoadReplayBuffer();

            // If only one type was loaded, reset both for consistency
            if (weightsLoaded != statsLoaded)
            {
                Stats = weightsLoaded ? new LearningStats() : Stats;
                qNetwork = statsLoaded ? new NeuralNetwork(InputSize, HiddenSize, OutputSize) : qNetwork;
            }

            // If starting fresh, set initial exploration rate
            if (!statsLoaded)
            {
                Stats.ExplorationRate = InitialExplorationRate;
            }

            // Initialize state arrays
            currentState = new double[InputSize];
            currentRays = new RayInfo[directions.Length];

            // Initialize distances
            Point head = gameEngine.Toboggan.Head;
            Point flag = gameEngine.Flag.Position;
            distanceToFlagPrevious = CalculateDistance(head, flag);
            minDistanceToFlag = distanceToFlagPrevious;

            // Calculate initial state
            UpdateState();
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => SaveAll();
        }

        /// <summary>
        /// Resets the neural network to initial state
        /// </summary>
        public void ResetNetwork()
        {
            // Reset learning rate
            currentLearningRate = InitialLearningRate;

            // Create new networks
            qNetwork = new NeuralNetwork(InputSize, HiddenSize, OutputSize);
            qNetwork.SetLearningRate(currentLearningRate);
            targetNetwork = qNetwork.Clone();

            // Reset experience buffer
            replayBuffer.Clear();
            positionHistory.Clear();

            // Reset stats
            Stats = new LearningStats();
            Stats.ExplorationRate = InitialExplorationRate;

            // Reset counters
            framesSinceLastFlag = 0;
            minDistanceToFlag = double.MaxValue;
            currentEpisodeReward = 0;
            recentQValues.Clear();
            gamesSinceLastBufferSave = 0;

            // Delete saved files
            TryDeleteFiles(new[] { NetworkWeightsFile, StatsFile, BufferFile });
        }

        /// <summary>
        /// Attempts to delete specified files
        /// </summary>
        private void TryDeleteFiles(string[] filePaths)
        {
            foreach (string path in filePaths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file {path}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Sets the training mode
        /// </summary>
        public void SetTrainingMode(bool isTraining)
        {
            TrainingMode = isTraining;

            // Save current state at start of training
            if (TrainingMode)
            {
                SaveNetworkWeights();
                SaveLearningStats();
                SaveReplayBuffer();
            }

            // Adjust learning frequency
            AdjustLearningFrequency();
        }

        /// <summary>
        /// Updates the game engine reference for a new game
        /// </summary>
        public void SetGameEngine(GameEngine newGameEngine)
        {
            this.gameEngine = newGameEngine;
            currentRays = new RayInfo[directions.Length];
            currentEpisodeReward = 0;
            framesSinceLastFlag = 0;
            minDistanceToFlag = double.MaxValue;
            UpdateState();
        }

        /// <summary>
        /// Adjusts the learning frequency based on buffer size and training progress
        /// </summary>
        private void AdjustLearningFrequency()
        {
            int bufferSize = replayBuffer.Count;
            double recentAvgScore = Stats.GetRecentAverageScore(20);

            if (!TrainingMode)
            {
                learningFrequency = 10; // Non-training mode - minimal learning
                return;
            }

            // Base frequency on buffer size and training progress
            if (bufferSize < MinBufferSizeForTraining)
            {
                learningFrequency = 5; // Collect experiences without much learning
            }
            else if (Stats.TotalGamesPlayed < 500)
            {
                learningFrequency = 1; // Learn frequently in early training
            }
            else if (Stats.MaxScore < 3)
            {
                learningFrequency = 2; // Still learning basic skills
            }
            else
            {
                learningFrequency = 3; // Fine-tuning phase
            }

            // Adjust based on recent performance
            if (recentAvgScore > 3.0)
            {
                // Doing well, reduce learning frequency
                learningFrequency = Math.Min(5, learningFrequency + 1);
            }
            else if (recentAvgScore < 0.5 && Stats.TotalGamesPlayed > 200)
            {
                // Struggling, increase learning rate
                learningFrequency = Math.Max(1, learningFrequency - 1);
            }
        }

        #endregion

        #region Core AI Methods

        /// <summary>
        /// Determines the next move using policy selection
        /// </summary>
        /// <returns>The direction to move</returns>
        public Direction GetNextMove()
        {
            int action;

            
            double currentExplorationRate = Stats.ExplorationRate;
            bool shouldExplore = random.NextDouble() < currentExplorationRate;

            if (shouldExplore)
            {
                // Exploration strategy
                action = SelectExplorationAction();
            }
            else
            {
                // Exploitation: Choose action based on Q-values with softmax
                action = SelectExploitationAction();
            }

            // Convert action to direction
            Direction nextDirection = DirectionFromAction(action);
            lastAction = action;
            return nextDirection;
        }

        /// <summary>
        /// Selects an action for exploration
        /// </summary>
        private int SelectExplorationAction()
        {
            bool needsDirectedMovement = framesSinceLastFlag > DirectMoveThreshold;

            if (needsDirectedMovement && random.NextDouble() < 0.85)
            {
                // Move toward flag with small randomization
                return GetDirectedFlagAction();
            }

            // Pure random exploration
            return random.Next(OutputSize);
        }

        /// <summary>
        /// Selects an action for exploitation using softmax over Q-values
        /// </summary>
        private int SelectExploitationAction()
        {
            double[] qValues = qNetwork.FeedForward(currentState);

            // Track Q-values for monitoring
            TrackQValues(qValues);

            // Apply softmax for stochastic policy
            double[] probabilities = Softmax(qValues, 2.0);

            return SampleFromDistribution(probabilities);
        }

        /// <summary>
        /// Samples an action index from a probability distribution
        /// </summary>
        private int SampleFromDistribution(double[] probabilities)
        {
            double selectionValue = random.NextDouble();
            double cumulativeProbability = 0;

            for (int i = 0; i < probabilities.Length; i++)
            {
                cumulativeProbability += probabilities[i];
                if (selectionValue < cumulativeProbability)
                {
                    return i;
                }
            }

            // Default to last action if sampling fails
            return probabilities.Length - 1;
        }

        /// <summary>
        /// Tracks Q-values for performance monitoring
        /// </summary>
        private void TrackQValues(double[] qValues)
        {
            if (recentQValues.Count > 100)
            {
                recentQValues.RemoveAt(0);
            }
            recentQValues.Add(qValues.Max());
        }

        /// <summary>
        /// Gets an action that moves toward the flag with some randomization
        /// </summary>
        /// <returns>The action index</returns>
        private int GetDirectedFlagAction()
        {
            Point head = gameEngine.Toboggan.Head;
            Point flag = gameEngine.Flag.Position;
            int deltaX = flag.X - head.X;
            int deltaY = flag.Y - head.Y;

            // Calculate optimal direction
            Direction optimalDirection = CalculateOptimalDirection(deltaX, deltaY);
            int optimalAction = (int)optimalDirection;

            // Check if optimal direction is safe
            if (IsSafeDirection(optimalDirection))
            {
                // If the optimal direction is safe, use it with small randomization
                if (random.NextDouble() < 0.05)  // 5% chance of randomization
                {
                    int variance = random.Next(-1, 2);
                    return (optimalAction + variance + 8) % 8;
                }
                return optimalAction;
            }

            // If the optimal direction is not safe, find a safe alternative
            List<int> safeDirections = FindSafeDirections();

            if (safeDirections.Count > 0)
            {
                // Choose the safe direction that is closest to the optimal direction
                int bestDirection = safeDirections[0];
                int minDeviation = Math.Min(
                    Math.Abs(bestDirection - optimalAction),
                    Math.Min(Math.Abs(bestDirection - optimalAction + 8),
                             Math.Abs(bestDirection - optimalAction - 8))
                );

                foreach (int dir in safeDirections)
                {
                    int deviation = Math.Min(
                        Math.Abs(dir - optimalAction),
                        Math.Min(Math.Abs(dir - optimalAction + 8),
                                 Math.Abs(dir - optimalAction - 8))
                    );

                    if (deviation < minDeviation)
                    {
                        minDeviation = deviation;
                        bestDirection = dir;
                    }
                }

                return bestDirection;
            }

            // If there are no safe directions, return the least dangerous
            return FindLeastDangerousDirection();
        }

        // Helper function to check if a direction is safe
        private bool IsSafeDirection(Direction direction)
        {
            Point head = gameEngine.Toboggan.Head;
            Point nextPos = CalculateNextPosition(head, direction);

            // Check for wall collision
            if (nextPos.X < 0 || nextPos.X >= gridWidth ||
                nextPos.Y < 0 || nextPos.Y >= gridHeight)
            {
                return false;
            }

            // Check for obstacle collision
            foreach (Obstacle obstacle in gameEngine.Obstacles)
            {
                if (obstacle.Position.X == nextPos.X && obstacle.Position.Y == nextPos.Y)
                {
                    return false;
                }
            }

            // Check for self collision
            foreach (Point segment in gameEngine.Toboggan.Segments)
            {
                // Skip the head
                if (segment.X == head.X && segment.Y == head.Y)
                    continue;

                if (segment.X == nextPos.X && segment.Y == nextPos.Y)
                {
                    return false;
                }
            }

            return true;
        }

        // Helper function to find all safe directions
        private List<int> FindSafeDirections()
        {
            List<int> safeDirections = new List<int>();

            for (int i = 0; i < 8; i++)
            {
                if (IsSafeDirection((Direction)i))
                {
                    safeDirections.Add(i);
                }
            }

            return safeDirections;
        }

        // Helper function to calculate next position given a direction
        private Point CalculateNextPosition(Point current, Direction direction)
        {
            Point next = new Point(current.X, current.Y);

            switch (direction)
            {
                case Direction.Up:
                    next.Y--;
                    break;
                case Direction.UpRight:
                    next.X++;
                    next.Y--;
                    break;
                case Direction.Right:
                    next.X++;
                    break;
                case Direction.DownRight:
                    next.X++;
                    next.Y++;
                    break;
                case Direction.Down:
                    next.Y++;
                    break;
                case Direction.DownLeft:
                    next.X--;
                    next.Y++;
                    break;
                case Direction.Left:
                    next.X--;
                    break;
                case Direction.UpLeft:
                    next.X--;
                    next.Y--;
                    break;
            }

            return next;
        }

        // Find the direction that is least likely to cause immediate problems
        private int FindLeastDangerousDirection()
        {
            // Calculate danger score for each direction
            double[] dangerScores = new double[8];

            for (int i = 0; i < 8; i++)
            {
                Direction dir = (Direction)i;
                Point head = gameEngine.Toboggan.Head;
                Point nextPos = CalculateNextPosition(head, dir);

                // High danger for out of bounds
                if (nextPos.X < 0 || nextPos.X >= gridWidth ||
                    nextPos.Y < 0 || nextPos.Y >= gridHeight)
                {
                    dangerScores[i] = 1000;
                    continue;
                }

                // Calculate danger score - start with distance to flag (higher is worse)
                double distanceToFlag = CalculateDistance(nextPos, gameEngine.Flag.Position);
                dangerScores[i] = distanceToFlag;

                // Add danger for obstacles and self
                foreach (Obstacle obstacle in gameEngine.Obstacles)
                {
                    double obstacleDistance = CalculateDistance(nextPos, obstacle.Position);
                    if (obstacleDistance < 2)
                    {
                        dangerScores[i] += (2 - obstacleDistance) * 20;
                    }
                }

                foreach (Point segment in gameEngine.Toboggan.Segments)
                {
                    if (segment.X == head.X && segment.Y == head.Y)
                        continue;

                    double segmentDistance = CalculateDistance(nextPos, segment);
                    if (segmentDistance < 2)
                    {
                        dangerScores[i] += (2 - segmentDistance) * 20;
                    }
                }
            }

            // Find direction with lowest danger
            int safestDirection = 0;
            double lowestDanger = dangerScores[0];

            for (int i = 1; i < 8; i++)
            {
                if (dangerScores[i] < lowestDanger)
                {
                    lowestDanger = dangerScores[i];
                    safestDirection = i;
                }
            }

            return safestDirection;
        }


        /// <summary>
        /// Applies softmax function to convert values to probabilities
        /// </summary>
        /// <param name="values">Input values</param>
        /// <param name="temperature">Temperature parameter for distribution</param>
        /// <returns>Probability distribution</returns>
        private double[] Softmax(double[] values, double temperature)
        {
            double[] expValues = new double[values.Length];
            double[] probabilities = new double[values.Length];
            double sumExp = 0;

            // Apply exponential with temperature
            for (int i = 0; i < values.Length; i++)
            {
                expValues[i] = Math.Exp(values[i] / temperature);
                sumExp += expValues[i];
            }

            // Normalize to probabilities
            for (int i = 0; i < values.Length; i++)
            {
                probabilities[i] = sumExp > 0 ? expValues[i] / sumExp : 1.0 / values.Length;
            }

            return probabilities;
        }

        /// <summary>
        /// Converts action number to direction
        /// </summary>
        private Direction DirectionFromAction(int action)
        {
            Direction[] directionMap = {
                Direction.Up,
                Direction.UpRight,
                Direction.Right,
                Direction.DownRight,
                Direction.Down,
                Direction.DownLeft,
                Direction.Left,
                Direction.UpLeft
            };

            return action >= 0 && action < directionMap.Length
                ? directionMap[action]
                : Direction.Right;
        }

        /// <summary>
        /// Updates AI after move with adaptive learning
        /// </summary>
        public void UpdateAfterMove(bool collectedFlag, bool hitObstacle)
        {
            // Update position history
            UpdatePositionHistory();

            // Save previous state
            double[] previousState = (double[])currentState.Clone();

            // Calculate reward
            double reward = CalculateReward(collectedFlag, hitObstacle);
            currentEpisodeReward += reward;

            // Update flag collection tracking
            UpdateFlagTracking(collectedFlag, hitObstacle);

            // Update current state
            UpdateState();

            // Save experience and manage buffer
            ManageExperienceBuffer(previousState, reward, hitObstacle || collectedFlag);

            // Handle learning and network updates
            ProcessLearningAndUpdates(collectedFlag);

            // Handle game over
            if (gameEngine.IsGameOver)
            {
                HandleGameOver();
            }
        }

        /// <summary>
        /// Updates the position history queue
        /// </summary>
        private void UpdatePositionHistory()
        {
            Point head = gameEngine.Toboggan.Head;
            positionHistory.Enqueue(head);

            if (positionHistory.Count > PositionHistorySize)
            {
                positionHistory.Dequeue();
            }
        }

        /// <summary>
        /// Updates tracking related to flag collection
        /// </summary>
        private void UpdateFlagTracking(bool collectedFlag, bool hitObstacle)
        {
            if (collectedFlag)
            {
                framesSinceLastFlag = 0;
                minDistanceToFlag = double.MaxValue;
                
            }
            else
            {
                framesSinceLastFlag++;

                // Update minimum distance to flag
                Point head = gameEngine.Toboggan.Head;
                double distanceToFlag = CalculateDistance(head, gameEngine.Flag.Position);
                minDistanceToFlag = Math.Min(minDistanceToFlag, distanceToFlag);
            }

          
        }

        /// <summary>
        /// Manages the experience replay buffer
        /// </summary>
        private void ManageExperienceBuffer(double[] previousState, double reward, bool importantEvent)
        {
            // Create new experience
            Experience experience = new Experience(
                previousState, lastAction, reward, currentState, gameEngine.IsGameOver
            );

            // Remove an experience if buffer is full
            if (replayBuffer.Count >= ReplayBufferCapacity)
            {
                RemoveExperienceFromBuffer(importantEvent);
            }

            // Add new experience
            replayBuffer.Add(experience);
        }

        /// <summary>
        /// Removes an experience from the buffer based on importance
        /// </summary>
        private void RemoveExperienceFromBuffer(bool isImportantEvent)
        {
            if (isImportantEvent)
            {
                // For important experiences, remove a random one
                int indexToRemove = random.Next(replayBuffer.Count);
                replayBuffer.RemoveAt(indexToRemove);
            }
            else
            {
                // Otherwise remove the oldest
                replayBuffer.RemoveAt(0);
            }
        }

        /// <summary>
        /// Processes learning and network updates
        /// </summary>
        private void ProcessLearningAndUpdates(bool collectedFlag)
        {
            // Increment frame counter
            frameCounter++;

            // Learn at controlled frequency
            bool shouldLearn = replayBuffer.Count >= MinBufferSizeForTraining && frameCounter >= learningFrequency;

            if (shouldLearn)
            {
                Learn();
                frameCounter = 0;

                // Periodically adjust learning parameters based on progress
                if (adaptiveLearningEnabled && Stats.TrainingSteps % 50 == 0)
                {
                    AdjustLearningParameters();
                }
            }

            // Manage exploration rate
            UpdateExplorationRate(collectedFlag);

            // Update learning rate with adaptive approach
            UpdateLearningRate();

            // Update target network
            UpdateTargetNetwork();
        }

        /// <summary>
        /// Updates the learning rate using adaptive approach
        /// </summary>
        private void UpdateLearningRate()
        {
            if (currentLearningRate <= MinLearningRate)
            {
                return;
            }

            // Determine decay rate based on performance
            double decayRate = LearningRateDecay;
            double recentScore = Stats.GetRecentAverageScore(20);
            double recentLoss = Stats.GetRecentAverageLoss(20);

            if (recentScore > 1.5)
            {
                // Slow down decay when we're improving
                decayRate = Math.Pow(LearningRateDecay, 0.6);
            }
            else if (recentLoss > 5.0)
            {
                // Speed up decay when loss is unstable
                decayRate = Math.Pow(LearningRateDecay, 1.05);
            }

            // Apply decay
            currentLearningRate *= decayRate;
            currentLearningRate = Math.Max(currentLearningRate, MinLearningRate);
            qNetwork.SetLearningRate(currentLearningRate);
        }

        /// <summary>
        /// Updates the target network
        /// </summary>
        private void UpdateTargetNetwork()
        {
            updateCounter++;

            // Soft update every few steps
            if (updateCounter % 3 == 0)
            {
                // Dynamic tau based on progress
                double dynamicTau = TargetNetworkTau;

                if (Stats.GetRecentAverageScore(20) > 1.5)
                {
                    dynamicTau *= 0.5; // Reduce update speed when performing well
                }

                qNetwork.SoftUpdateTargetNetwork(targetNetwork, dynamicTau);
            }

            // Hard update periodically
            if (updateCounter >= HardUpdateFrequency)
            {
                targetNetwork = qNetwork.Clone();
                updateCounter = 0;

                // Periodic saving
                SaveNetworkWeights();
                SaveLearningStats();
            }
        }

        /// <summary>
        /// Handles game over state
        /// </summary>
        private void HandleGameOver()
        {
            // Record game result for statistics
            Stats.RecordGameResult(gameEngine.Score, currentEpisodeReward);

            // Increment counter and check if we should save buffer
            gamesSinceLastBufferSave++;
            if (TrainingMode && gamesSinceLastBufferSave >= AutoSaveInterval)
            {
                SaveReplayBuffer();
                gamesSinceLastBufferSave = 0;
            }

            // Adjust learning frequency based on recent performance
            AdjustLearningFrequency();

            // Reset for next game
            currentEpisodeReward = 0;
            positionHistory.Clear();
            frameCounter = 0;
            framesSinceLastFlag = 0;
            minDistanceToFlag = double.MaxValue;

            // Save state periodically
            if (TrainingMode && Stats.TotalGamesPlayed % 10 == 0)
            {
                SaveNetworkWeights();
                SaveLearningStats();
            }
        }

        /// <summary>
        /// Dynamically adjusts exploration rate based on performance and game state
        /// </summary>
        private void UpdateExplorationRate(bool collectedFlag)
        {
            // Calculate appropriate minimum exploration rate
            double currentMinExploration = CalculateMinimumExplorationRate();

            // Handle special case: struggling after many games
            if (ShouldBoostExploration(currentMinExploration))
            {
                // Temporary boost to escape local minimum
                Stats.ExplorationRate = Math.Min(Stats.ExplorationRate * 1.5, 0.2);
                return;
            }

            // Normal exploration rate update
            if (Stats.ExplorationRate > currentMinExploration)
            {
                // Basic decay
                Stats.ExplorationRate *= ExplorationRateDecay;

                // Extra decay for successful flag collection
                if (collectedFlag)
                {
                    Stats.ExplorationRate *= 0.999;
                }

                // Ensure we don't go below the current minimum
                Stats.ExplorationRate = Math.Max(Stats.ExplorationRate, currentMinExploration);
            }
            else
            {
                // If below minimum, increase to minimum
                Stats.ExplorationRate = currentMinExploration;
            }
        }
        /// <summary>
        /// Calculates the appropriate minimum exploration rate
        /// </summary>
        private double CalculateMinimumExplorationRate()
        {
            // Base minimum exploration rate
            double currentMinExploration = MinExplorationRate;
            double recentAvgScore = Stats.GetRecentAverageScore(20);

            // Adjust based on training progress
            if (Stats.TotalGamesPlayed > 1000)
            {
                // Gradually reduce minimum exploration rate with training
                int cappedGames = Math.Min(Stats.TotalGamesPlayed, 5000);
                double progressFactor = (cappedGames - 1000) / 4000.0;
                currentMinExploration = MinExplorationRate * (1.0 - (progressFactor * 0.4));
            }

            // If doing well, reduce exploration
            if (Stats.MaxScore > 5 && recentAvgScore > 2.0)
            {
                currentMinExploration *= 0.8;
            }

            return currentMinExploration;
        }

        /// <summary>
        /// Determines if exploration rate should be temporarily boosted
        /// </summary>
        private bool ShouldBoostExploration(double currentMinExploration)
        {
            double recentAvgScore = Stats.GetRecentAverageScore(20);

            if (Stats.TotalGamesPlayed > 100 &&
                recentAvgScore < Stats.GetRecentAverageScore(50) * 0.7 &&
                Stats.ExplorationRate < 0.4)
            {
                return true;
            }

            if (Stats.TotalGamesPlayed > 300 &&
                recentAvgScore < 1.0 &&
                Stats.ExplorationRate < 0.3)
            {
                return true;
            }

            return Stats.TotalGamesPlayed > 500 && 
                   recentAvgScore < 3 &&
                   Stats.ExplorationRate < 0.25; 
        }

        /// <summary>
        /// Adjusts learning parameters based on performance
        /// </summary>
        private void AdjustLearningParameters()
        {
            double recentAvgScore = Stats.GetRecentAverageScore(20);
            double recentLoss = Stats.GetRecentAverageLoss(20);

            // Calculate performance indicators
            bool isStuck = IsPerformanceStuck(recentAvgScore);
            bool unstableLearning = recentLoss > 10.0;
            bool qValueInflation = IsQValueInflated();

            // Apply adjustments based on indicators
            if (isStuck)
            {
                AdjustForStuckPerformance();
            }

            if (unstableLearning)
            {
                AdjustForUnstableLearning();
            }

            if (qValueInflation)
            {
                AdjustForQValueInflation();
            }
        }

        /// <summary>
        /// Checks if performance is stuck in a poor local optimum
        /// </summary>
        private bool IsPerformanceStuck(double recentAvgScore)
        {
            return Stats.TotalGamesPlayed > 500 &&
                   recentAvgScore < 0.5 &&
                   Stats.MaxScore > 2;
        }

        /// <summary>
        /// Checks if Q-values are inflated
        /// </summary>
        private bool IsQValueInflated()
        {
            if (recentQValues.Count == 0)
            {
                return false;
            }

            double avgQValue = recentQValues.Average();
            return avgQValue > 50.0;
        }

        /// <summary>
        /// Adjusts parameters when performance is stuck
        /// </summary>
        private void AdjustForStuckPerformance()
        {
            // Temporarily boost exploration and learning rate
            Stats.ExplorationRate = Math.Max(Stats.ExplorationRate, 0.3);
            currentLearningRate = Math.Max(currentLearningRate, InitialLearningRate * 0.5);
            qNetwork.SetLearningRate(currentLearningRate);
        }

        /// <summary>
        /// Adjusts parameters when learning is unstable
        /// </summary>
        private void AdjustForUnstableLearning()
        {
            // Reduce learning rate to stabilize
            currentLearningRate *= 0.8;
            qNetwork.SetLearningRate(currentLearningRate);
        }

        /// <summary>
        /// Adjusts parameters when Q-values are inflated
        /// </summary>
        private void AdjustForQValueInflation()
        {
            // Soft reset target network to prevent overestimation
            targetNetwork = qNetwork.Clone();
            updateCounter = 0;
        }

        /// <summary>
        /// Learns from experiences using batch selection
        /// </summary>
        private void Learn()
        {
            // Create experience batch with prioritization
            List<Experience> batch = CreatePrioritizedBatch();

            // Process each experience
            double totalLoss = 0;
            int validExamples = 0;

            foreach (Experience experience in batch)
            {
                double targetQ = CalculateTargetQ(experience);
                ProcessExperience(experience, targetQ, ref totalLoss, ref validExamples);
            }

            // Update statistics
            if (validExamples > 0)
            {
                UpdateTrainingStats(totalLoss, validExamples);
            }
        }

        /// <summary>
        /// Creates a batch of experiences with prioritization
        /// </summary>
        private List<Experience> CreatePrioritizedBatch()
        {
            List<Experience> batch = new List<Experience>();
            HashSet<int> selectedIndices = new HashSet<int>();

            // Add recent experiences
            AddRecentExperiences(batch, selectedIndices);

            // Add important flag collection experiences
            AddFlagCollectionExperiences(batch, selectedIndices);

            // Add crash experiences
            AddCrashExperiences(batch, selectedIndices);

            // Fill remaining slots with mixed prioritization
            FillBatchWithPrioritizedSampling(batch, selectedIndices, BatchSize);

            return batch;
        }

        /// <summary>
        /// Adds recent experiences to the batch
        /// </summary>
        private void AddRecentExperiences(List<Experience> batch, HashSet<int> selectedIndices)
        {
            int recentCount = Math.Min(BatchSize / 4, replayBuffer.Count / 10);

            for (int i = 0; i < recentCount; i++)
            {
                int index = Math.Max(0, replayBuffer.Count - 1 - i);
                if (!selectedIndices.Contains(index))
                {
                    batch.Add(replayBuffer[index]);
                    selectedIndices.Add(index);
                }
            }
        }

        /// <summary>
        /// Adds flag collection experiences to the batch
        /// </summary>
        private void AddFlagCollectionExperiences(List<Experience> batch, HashSet<int> selectedIndices)
        {
            // Find flag collection experiences
            List<int> flagExperienceIndices = FindExperienceIndices(exp => exp.Reward > 50.0);

            // Add them to batch (up to a limit)
            AddPrioritizedExperiences(flagExperienceIndices, batch, selectedIndices, BatchSize / 3);
        }

        /// <summary>
        /// Adds crash experiences to the batch
        /// </summary>
        private void AddCrashExperiences(List<Experience> batch, HashSet<int> selectedIndices)
        {
            // Find crash experiences
            List<int> crashExperienceIndices = FindExperienceIndices(exp => exp.Reward < -15.0);

            // Add them to batch (up to a limit)
            AddPrioritizedExperiences(crashExperienceIndices, batch, selectedIndices, BatchSize / 6);
        }

        /// <summary>
        /// Finds experience indices matching a criteria
        /// </summary>
        private List<int> FindExperienceIndices(Func<Experience, bool> predicate)
        {
            List<int> indices = new List<int>();

            for (int i = 0; i < replayBuffer.Count; i++)
            {
                if (predicate(replayBuffer[i]))
                {
                    indices.Add(i);
                }
            }

            return indices;
        }

        /// <summary>
        /// Adds prioritized experiences to the batch
        /// </summary>
        private void AddPrioritizedExperiences(
            List<int> experienceIndices,
            List<Experience> batch,
            HashSet<int> selectedIndices,
            int maxCount)
        {
            if (experienceIndices.Count == 0)
            {
                return;
            }

            // Shuffle indices
            experienceIndices = ShuffleList(experienceIndices);

            // Add up to max count
            int added = 0;
            foreach (int index in experienceIndices)
            {
                if (!selectedIndices.Contains(index) && batch.Count < BatchSize && added < maxCount)
                {
                    batch.Add(replayBuffer[index]);
                    selectedIndices.Add(index);
                    added++;
                }
            }
        }

        /// <summary>
        /// Calculates target Q-value for an experience
        /// </summary>
        private double CalculateTargetQ(Experience experience)
        {
            double targetQ;

            if (experience.IsDone)
            {
                // Terminal state
                targetQ = experience.Reward;
            }
            else
            {
                // Double DQN approach for more stable learning
                double[] nextQValues = qNetwork.FeedForward(experience.NextState);
                int bestAction = Array.IndexOf(nextQValues, nextQValues.Max());

                double[] targetQValues = targetNetwork.FeedForward(experience.NextState);
                targetQ = experience.Reward + DiscountFactor * targetQValues[bestAction];
            }

            // Apply reward clipping for stability
            return Math.Max(-100, Math.Min(100, targetQ));
        }

        /// <summary>
        /// Processes a single experience for learning
        /// </summary>
        private void ProcessExperience(
            Experience experience,
            double targetQ,
            ref double totalLoss,
            ref int validExamples)
        {
           
            int optimalAction = GetDirectedFlagAction();  

            double multiplier = 1.0;

          
            if (experience.Action == optimalAction)
            {
                multiplier = 1.25; 
            }
            else if (experience.Action == (optimalAction + 1) % 8 ||
                     experience.Action == (optimalAction - 1 + 8) % 8)
            {
                multiplier = 1.1; 
            }
            else if (experience.Action == (optimalAction + 2) % 8 ||
                     experience.Action == (optimalAction - 2 + 8) % 8)
            {
                multiplier = 1.1;  
            }

            
            int oppositeAction = (optimalAction + 4) % 8;
            if (experience.Action == oppositeAction)
            {
                multiplier = 0.7;  
            }
            else if (experience.Action == (oppositeAction + 1) % 8 ||
                     experience.Action == (oppositeAction - 1 + 8) % 8)
            {
                multiplier = 0.8;  
            }

            
            if (experience.Reward > 0)
            {
                targetQ *= multiplier; 
            }

            else if (experience.Reward < -40.0)  
            {
                targetQ *= 1.2;  
            }

            else if (experience.Reward < 0 && multiplier < 1.0)
            {
                targetQ *= (multiplier * 0.8); 
            }

            if (experience.Reward > 20.0 && experience.Action == optimalAction)
            {
                targetQ *= 1.15; 
            }

            if (experience.Reward >= 50.0)
            {
                targetQ *= 1.2;
            }

            // Train network
            qNetwork.TrainQ(experience.State, experience.Action, targetQ);

            // Calculate loss for statistics
            double[] currentQs = qNetwork.FeedForward(experience.State);
            double error = targetQ - currentQs[experience.Action];
            double squaredError = Math.Pow(error, 2);

            totalLoss += squaredError;
            validExamples++;
        }

        /// <summary>
        /// Updates training statistics
        /// </summary>
        private void UpdateTrainingStats(double totalLoss, int validExamples)
        {
            Stats.TrainingSteps++;
            double avgLoss = totalLoss / validExamples;
            Stats.AddLossValue(avgLoss);
            Stats.AverageLoss = avgLoss;
        }

        /// <summary>
        /// Fills batch with experiences using prioritized sampling based on reward magnitude
        /// </summary>
    private void FillBatchWithPrioritizedSampling(List<Experience> batch, HashSet<int> selectedIndices, int targetBatchSize)
        {
            // Skip if batch is already full or no more experiences available
            if (batch.Count >= targetBatchSize || replayBuffer.Count <= selectedIndices.Count)
            {
                return;
            }

            // Get remaining indices with their reward magnitudes
            var remainingWithMagnitudes = GetRemainingExperiencesMagnitudes(selectedIndices);

            // Take experiences with mixed strategy of prioritized and random
            int remainingToFill = targetBatchSize - batch.Count;
            int prioritizedCount = remainingToFill / 2;

            // Add high-magnitude experiences first
            AddHighMagnitudeExperiences(remainingWithMagnitudes, batch, selectedIndices, prioritizedCount);

            // Fill remaining with random selection
            AddRandomExperiences(batch, selectedIndices, targetBatchSize);
        }

        /// <summary>
        /// Gets remaining experiences with their reward magnitudes
        /// </summary>
        private List<KeyValuePair<int, double>> GetRemainingExperiencesMagnitudes(HashSet<int> selectedIndices)
        {
            Dictionary<int, double> rewardMagnitudes = new Dictionary<int, double>();

            for (int i = 0; i < replayBuffer.Count; i++)
            {
                if (!selectedIndices.Contains(i))
                {
                    // Prioritize based on absolute reward value
                    rewardMagnitudes[i] = Math.Abs(replayBuffer[i].Reward);
                }
            }

            // Convert to sorted list of (index, magnitude) pairs
            List<KeyValuePair<int, double>> sortedMagnitudes = rewardMagnitudes.ToList();
            sortedMagnitudes.Sort((a, b) => b.Value.CompareTo(a.Value));

            return sortedMagnitudes;
        }

        /// <summary>
        /// Adds high-magnitude experiences to the batch
        /// </summary>
        private void AddHighMagnitudeExperiences(
            List<KeyValuePair<int, double>> sortedMagnitudes,
            List<Experience> batch,
            HashSet<int> selectedIndices,
            int count)
        {
            // Add experiences with highest magnitudes first
            for (int i = 0; i < Math.Min(count, sortedMagnitudes.Count); i++)
            {
                int index = sortedMagnitudes[i].Key;
                batch.Add(replayBuffer[index]);
                selectedIndices.Add(index);
            }
        }

        /// <summary>
        /// Adds random experiences to the batch
        /// </summary>
        private void AddRandomExperiences(List<Experience> batch, HashSet<int> selectedIndices, int targetBatchSize)
        {
            // Get remaining indices that haven't been selected
            List<int> remainingIndices = Enumerable.Range(0, replayBuffer.Count)
                .Where(i => !selectedIndices.Contains(i))
                .ToList();

            // Shuffle the indices
            remainingIndices = ShuffleList(remainingIndices);

            // Take only as many indices as needed to fill the batch
            int remainingCapacity = targetBatchSize - batch.Count;
            IEnumerable<int> indicesToAdd = remainingIndices.Take(remainingCapacity);

            // Add the experiences to the batch
            foreach (int index in indicesToAdd)
            {
                batch.Add(replayBuffer[index]);
                selectedIndices.Add(index);
            }
        }

        /// <summary>
        /// Shuffles a list using Fisher-Yates algorithm
        /// </summary>
        private List<T> ShuffleList<T>(List<T> list)
        {
            List<T> shuffled = new List<T>(list);
            int n = shuffled.Count;

            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = shuffled[k];
                shuffled[k] = shuffled[n];
                shuffled[n] = value;
            }

            return shuffled;
        }

        #endregion

        #region Reward Calculation

        /// <summary>
        /// Calculates reward based on game actions
        /// </summary>
        private double CalculateReward(bool collectedFlag, bool hitObstacle)
        {
            double reward = 0;
            Point head = gameEngine.Toboggan.Head;
            Point flag = gameEngine.Flag.Position;

            // Check for wall collision
            bool hitWall = CheckWallCollision();

            // Strong negative reward for hitting obstacle
            if (hitObstacle)
            {
                reward -= 50.0;
            }

            // Strong negative reward for hitting wall
            if (hitWall)
            {
                reward -= 50.0;
            }

            // Strong positive reward for collecting flag
            if (collectedFlag)
            {
                reward += 75.0;
            }
            else
            {
                // Distance-based reward
                double currentDistance = CalculateDistance(head, flag);
                double previousDistance = distanceToFlagPrevious;

                // Clear signal for movement toward/away from flag
                if (currentDistance < previousDistance)
                {
                    // Reward for getting closer to flag
                    double improvement = previousDistance - currentDistance;
                    // Scale reward by how much closer we got
                    reward += 5.0 * improvement;
                }
                else if (currentDistance > previousDistance)
                {
                    // Smaller penalty for moving away from flag
                    double regression = currentDistance - previousDistance;
                    reward -= 3.0 * regression;
                }

                // Give small time penalty to encourage flag collection
                if (framesSinceLastFlag > 15)
                {
                    reward -= 0.3;
                }
            }

            // Update distance for next calculation
            distanceToFlagPrevious = CalculateDistance(head, flag);

            return reward;
        }

        /// <summary>
        /// Checks if the toboggan head hit a wall
        /// </summary>
        private bool CheckWallCollision()
        {
            Point head = gameEngine.Toboggan.Head;
            return head.X <= 0 || head.X >= gridWidth - 1 || head.Y <= 0 || head.Y >= gridHeight - 1;
        }

        /// <summary>
        /// Calculates penalty for being stuck
        /// </summary>
        private double CalculateStuckPenalty()
        {
            return IsStuck() ? -5.0 : 0.0;
        }

        /// <summary>
        /// Calculates reward based on game outcomes
        /// </summary>
        private double CalculateOutcomeReward(bool collectedFlag, bool hitObstacle)
        {
            double reward = 0;

            // Penalty for hitting obstacle
            if (hitObstacle)
            {
                reward -= 15.0;
            }

            // Reward for collecting flag
            if (collectedFlag)
            {
                reward += 50.0;

                // Add bonus based on training progress
                if (Stats.TotalGamesPlayed < 500)
                {
                    // Early training - big rewards for success
                    reward += 25.0;
                }
                else if (gameEngine.Score > Stats.MaxScore - 1)
                {
                    // Near best performance - extra encouragement
                    reward += 20.0;
                }
            }

            return reward;
        }

        /// <summary>
        /// Calculates reward based on distance changes
        /// </summary>
        private double CalculateDistanceReward(Point head, Point flag)
        {
            double reward = 0;
            double currentDistance = CalculateDistance(head, flag);
            double previousDistance = distanceToFlagPrevious;

            // Reward for getting closer to flag
            if (currentDistance < previousDistance)
            {
                // Calculate improvement
                double improvement = previousDistance - currentDistance;

                // Scale based on grid size
                double relativeFactor = 50.0 / Math.Max(gridWidth, gridHeight);

                // Larger reward as we get closer to the flag
                double proximityBoost = 1.0 + (1.0 / (currentDistance + 1.0));

                reward += 2.0 * improvement * relativeFactor * proximityBoost;
            }
            // Penalty for moving away from flag
            else if (currentDistance > previousDistance)
            {
                double regression = currentDistance - previousDistance;
                reward -= 1.0 * regression;
            }

            // Bonus for achieving new closest distance to flag
            if (currentDistance < minDistanceToFlag)
            {
                double closenessRatio = 1.0 - (currentDistance / Math.Sqrt(gridWidth * gridWidth + gridHeight * gridHeight));
                double newMinBonus = 3.0 + (7.0 * closenessRatio * closenessRatio);

                reward += newMinBonus;
                minDistanceToFlag = currentDistance;
            }

            return reward;
        }

        /// <summary>
        /// Calculates bonus based on proximity to flag
        /// </summary>
        private double CalculateProximityBonus(double distance)
        {
            if (distance < 2.0)
            {
                return 10.0; // Major bonus for being very close
            }

            if (distance < 4.0)
            {
                return 5.0;  // Medium bonus
            }

            if (distance < 7.0)
            {
                return 2.0;  // Small bonus
            }

            return 0.0;
        }

        /// <summary>
        /// Calculates time-based penalty
        /// </summary>
        private double CalculateTimePenalty()
        {
            if (framesSinceLastFlag <= 80)
            {
                return 0.0;
            }

            // Smoother progression and capped maximum
            double timePenalty = Math.Min(1.0 + (framesSinceLastFlag - 80) / 50.0, 5.0);
            return -timePenalty;
        }

        /// <summary>
        /// Detects if the toboggan is stuck in a pattern
        /// </summary>
        private bool IsStuck()
        {
            // Need enough history to detect patterns
            if (positionHistory.Count < PositionHistorySize)
            {
                return false;
            }

            // Check for small loops (few unique positions)
            if (CountUniquePositions() <= 3)
            {
                return true;
            }

            // Check for oscillation patterns
            if (DetectOscillationPatterns())
            {
                return true;
            }

            // Check for lack of progress toward flag
            return DetectLackOfProgress();
        }

        /// <summary>
        /// Counts unique positions in history
        /// </summary>
        private int CountUniquePositions()
        {
            var uniquePositions = new HashSet<Point>(positionHistory, new PointComparer());
            return uniquePositions.Count;
        }

        /// <summary>
        /// Detects oscillation patterns in position history
        /// </summary>
        private bool DetectOscillationPatterns()
        {
            // Need enough history for pattern detection
            if (positionHistory.Count < 8)
            {
                return false;
            }

            // Convert to array for easier access
            Point[] positions = positionHistory.ToArray();
            int count = positions.Length;

            // Check for pattern of length 2
            bool pattern2 = HasRepeatingPattern(positions, 2, 4);

            // Check for pattern of length 3
            bool pattern3 = count >= 9 && HasRepeatingPattern(positions, 3, 3);

            // Check for pattern of length 4
            bool pattern4 = count >= 12 && HasRepeatingPattern(positions, 4, 3);

            return pattern2 || pattern3 || pattern4;
        }

        /// <summary>
        /// Checks for a repeating pattern of specified length
        /// </summary>
        private bool HasRepeatingPattern(Point[] positions, int patternLength, int repetitions)
        {
            int count = positions.Length;

            // Not enough positions to check
            if (count < patternLength * repetitions)
            {
                return false;
            }

            // Check if pattern repeats
            for (int rep = 0; rep < repetitions; rep++)
            {
                for (int i = 0; i < patternLength; i++)
                {
                    int index1 = count - 1 - i;
                    int index2 = count - 1 - i - patternLength;

                    // Pattern doesn't match
                    if (!SamePosition(positions[index1], positions[index2]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Detects lack of progress toward the flag
        /// </summary>
        private bool DetectLackOfProgress()
        {
            double currentDistance = CalculateDistance(gameEngine.Toboggan.Head, gameEngine.Flag.Position);

            // Check for significant regression from best distance
            return framesSinceLastFlag > 60 && currentDistance > minDistanceToFlag * 1.3;
        }

        /// <summary>
        /// Checks if two points are at the same position
        /// </summary>
        private bool SamePosition(Point a, Point b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Calculates distance between two points
        /// </summary>
        private double CalculateDistance(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }

        /// <summary>
        /// Calculates optimal direction towards a target
        /// </summary>
        /// <param name="deltaX">X difference to target</param>
        /// <param name="deltaY">Y difference to target</param>
        /// <returns>Optimal direction</returns>
        private Direction CalculateOptimalDirection(int deltaX, int deltaY)
        {
            // Cardinal directions
            if (deltaX == 0)
            {
                return deltaY < 0 ? Direction.Up : Direction.Down;
            }

            if (deltaY == 0)
            {
                return deltaX > 0 ? Direction.Right : Direction.Left;
            }

            // Diagonal directions
            if (deltaX > 0)
            {
                return deltaY < 0 ? Direction.UpRight : Direction.DownRight;
            }

            return deltaY < 0 ? Direction.UpLeft : Direction.DownLeft;
        }

        /// <summary>
        /// Updates the current state representation
        /// </summary>
        private void UpdateState()
        {
            // Cast rays and fill the first 32 inputs (8 directions x 4 values per ray)
            CastRays();
        }


        /// <summary>
        /// Casts rays in all directions for environment sensing
        /// </summary>
        private void CastRays()
        {
            Point head = gameEngine.Toboggan.Head;
            LinkedList<Point> segments = gameEngine.Toboggan.Segments;
            Point flag = gameEngine.Flag.Position;
            List<Obstacle> obstacles = gameEngine.Obstacles;

            // Cast rays in all 8 directions
            for (int dir = 0; dir < directions.Length; dir++)
            {
                RayResult result = CastSingleRay(
                    head,
                    segments,
                    flag,
                    obstacles,
                    directions[dir],
                    Math.Max(gridWidth, gridHeight)
                );

                // Store ray info for visualization
                currentRays[dir] = result.RayInfo;

                // Store results in state array
                currentState[dir * 4] = 1.0 - result.DistanceToWall;
                currentState[dir * 4 + 1] = 1.0 - result.DistanceToBody;
                currentState[dir * 4 + 2] = CalculateFlagVisibility(
                    result.DistanceToFlag,
                    result.DistanceToWall,
                    result.DistanceToBody,
                    result.DistanceToObstacle,
                    result.FlagFound
                );
                currentState[dir * 4 + 3] = 1.0 - result.DistanceToObstacle;
            }
        }

        /// <summary>
        /// Calculates flag visibility for a ray
        /// </summary>
        private double CalculateFlagVisibility(
            double distanceToFlag,
            double distanceToWall,
            double distanceToBody,
            double distanceToObstacle,
            bool flagFound)
        {
            // Flag is hidden by obstacles or walls
            bool isHidden = (distanceToBody <= distanceToFlag && distanceToBody < 1.0) ||
                           (distanceToObstacle <= distanceToFlag && distanceToObstacle < 1.0) ||
                           (distanceToWall <= distanceToFlag && distanceToWall < 1.0);

            if (isHidden || !flagFound)
            {
                return 0.0;
            }

            return 1.0 - distanceToFlag;
        }

        /// <summary>
        /// Casts a single ray and returns results
        /// </summary>
        private RayResult CastSingleRay(
    Point head,
    LinkedList<Point> segments,
    Point flag,
    List<Obstacle> obstacles,
    Point direction,
    int maxRange)
        {
            // Initialize result
            var result = new RayResult
            {
                DistanceToWall = 1.0,
                DistanceToBody = 1.0,
                DistanceToFlag = 1.0,
                DistanceToObstacle = 1.0,
                FlagFound = false,
                HitType = RayHitType.None,
                RayEnd = head
            };

            // Cast ray using Bresenham's algorithm until hit or max range
            CastRayUntilIntersection(head, direction, maxRange, segments, flag, obstacles, ref result);

            // Create ray visualization info
            result.RayInfo = new RayInfo(
                head,
                result.RayEnd,
                result.HitType,
                GetMinimumDistance(result)
            );

            return result;
        }

        private void CastRayUntilIntersection(
            Point head,
            Point direction,
            int maxRange,
            LinkedList<Point> segments,
            Point flag,
            List<Obstacle> obstacles,
            ref RayResult result)
        {
            int x = head.X;
            int y = head.Y;
            int dx = Math.Abs(direction.X);
            int dy = Math.Abs(direction.Y);
            int sx = direction.X > 0 ? 1 : -1;
            int sy = direction.Y > 0 ? 1 : -1;
            int err = dx - dy;

            // Process points along the ray until we hit something or reach max range
            for (int step = 1; step <= maxRange && !ShouldStopRayCast(result); step++)
            {
                // Calculate next point using Bresenham's algorithm
                MoveToNextPoint(ref x, ref y, dx, dy, sx, sy, ref err);

                Point currentPoint = new Point(x, y);

                // Process this point along the ray
                ProcessRayPoint(
                    currentPoint,
                    step,
                    maxRange,
                    segments,
                    flag,
                    obstacles,
                    ref result
                );
            }
        }

        private void MoveToNextPoint(
            ref int x,
            ref int y,
            int dx,
            int dy,
            int sx,
            int sy,
            ref int err)
        {
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }

        private bool ShouldStopRayCast(RayResult result)
        {
            // Stop ray casting if we hit a wall, body, or obstacle (but not flag)
            return result.WallFound ||
                   result.BodyFound ||
                   (result.ObstacleFound && !result.FlagFound);
        }

        private double GetMinimumDistance(RayResult result)
        {
            return Math.Min(result.DistanceToWall,
                   Math.Min(result.DistanceToBody,
                       Math.Min(result.DistanceToFlag, result.DistanceToObstacle)));
        }

        /// <summary>
        /// Processes a point along a ray cast
        /// </summary>
        /// <returns>True if ray casting should stop, false to continue</returns>
        private bool ProcessRayPoint(
            Point point,
            int step,
            int maxRange,
            LinkedList<Point> segments,
            Point flag,
            List<Obstacle> obstacles,
            ref RayResult result)
        {
            // Check for wall collision
            if (IsWallCollision(point))
            {
                UpdateWallCollision(point, step, maxRange, ref result);
                return true;
            }

            // Check for body collision (if not already found)
            if (!result.BodyFound && IsBodyCollision(point, segments))
            {
                UpdateBodyCollision(point, step, maxRange, ref result);
                return true;
            }

            // Check for obstacle collision (if not already found)
            if (!result.ObstacleFound && IsObstacleCollision(point, obstacles))
            {
                UpdateObstacleCollision(point, step, maxRange, ref result);

                // Stop if this isn't also the flag
                if (!result.FlagFound)
                {
                    return true;
                }
            }

            // Check for flag (if not already found)
            if (!result.FlagFound && IsFlagCollision(point, flag))
            {
                UpdateFlagCollision(point, step, maxRange, ref result);
            }

            return false;
        }

        /// <summary>
        /// Checks if a point collides with a wall
        /// </summary>
        private bool IsWallCollision(Point point)
        {
            return point.X < 0 || point.X >= gridWidth || point.Y < 0 || point.Y >= gridHeight;
        }

        /// <summary>
        /// Updates ray result for wall collision
        /// </summary>
        private void UpdateWallCollision(Point point, int step, int maxRange, ref RayResult result)
        {
            result.DistanceToWall = (double)step / maxRange;
            result.WallFound = true;
            result.RayEnd = new Point(
                Math.Max(0, Math.Min(point.X, gridWidth - 1)),
                Math.Max(0, Math.Min(point.Y, gridHeight - 1))
            );
            result.HitType = RayHitType.Wall;
        }

        /// <summary>
        /// Checks if a point collides with the toboggan body
        /// </summary>
        private bool IsBodyCollision(Point point, LinkedList<Point> segments)
        {
            if (segments.First == null)
            {
                return false;
            }

            LinkedListNode<Point> current = segments.First.Next; // Skip head

            while (current != null)
            {
                if (current.Value.X == point.X && current.Value.Y == point.Y)
                {
                    return true;
                }
                current = current.Next;
            }

            return false;
        }

        /// <summary>
        /// Updates ray result for body collision
        /// </summary>
        private void UpdateBodyCollision(Point point, int step, int maxRange, ref RayResult result)
        {
            result.DistanceToBody = (double)step / maxRange;
            result.BodyFound = true;
            result.RayEnd = point;
            result.HitType = RayHitType.Body;
        }

        /// <summary>
        /// Checks if a point collides with an obstacle
        /// </summary>
        private bool IsObstacleCollision(Point point, List<Obstacle> obstacles)
        {
            if (obstacles == null)
            {
                return false;
            }

            foreach (Obstacle obstacle in obstacles)
            {
                if (obstacle.Position.X == point.X && obstacle.Position.Y == point.Y)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates ray result for obstacle collision
        /// </summary>
        private void UpdateObstacleCollision(Point point, int step, int maxRange, ref RayResult result)
        {
            result.DistanceToObstacle = (double)step / maxRange;
            result.ObstacleFound = true;

            // Update ray end if no closer collision exists
            if (!result.BodyFound)
            {
                result.RayEnd = point;
                result.HitType = RayHitType.Obstacle;
            }
        }

        /// <summary>
        /// Checks if a point collides with the flag
        /// </summary>
        private bool IsFlagCollision(Point point, Point flag)
        {
            return point.X == flag.X && point.Y == flag.Y;
        }

        /// <summary>
        /// Updates ray result for flag collision
        /// </summary>
        private void UpdateFlagCollision(Point point, int step, int maxRange, ref RayResult result)
        {
            result.DistanceToFlag = (double)step / maxRange;
            result.FlagFound = true;

            // Only set hit type if no other collision
            if (result.HitType == RayHitType.None)
            {
                result.RayEnd = point;
                result.HitType = RayHitType.Flag;
            }
        }

        /// <summary>
        /// Forces an update of ray information without performing a move
        /// </summary>
        public void UpdateRaysForCurrentPosition()
        {
            // Update the state representation which includes ray casting
            UpdateState();
        }

        /// <summary>
        /// Result structure for ray casting
        /// </summary>
        private struct RayResult
        {
            public double DistanceToWall;
            public double DistanceToBody;
            public double DistanceToFlag;
            public double DistanceToObstacle;

            public bool WallFound;
            public bool BodyFound;
            public bool FlagFound;
            public bool ObstacleFound;

            public RayHitType HitType;
            public Point RayEnd;
            public RayInfo RayInfo;
        }

        #endregion

        #region Save/Load Methods

        /// <summary>
        /// Saves network weights to file
        /// </summary>
        public void SaveNetworkWeights()
        {
            qNetwork.SaveWeights(NetworkWeightsFile);
        }

        /// <summary>
        /// Saves learning statistics to file
        /// </summary>
        public void SaveLearningStats()
        {
            Stats.SaveToFile(StatsFile);
        }

        /// <summary>
        /// Saves all AI data
        /// </summary>
        public void SaveAll()
        {
            SaveNetworkWeights();
            SaveLearningStats();
            SaveReplayBuffer();
        }

        /// <summary>
        /// Returns current replay buffer size
        /// </summary>
        /// <returns>Number of experiences in buffer</returns>
        public int GetReplayBufferSize()
        {
            return replayBuffer.Count;
        }

        /// <summary>
        /// Saves replay buffer to file
        /// </summary>
        public void SaveReplayBuffer()
        {
            try
            {
                using (FileStream fs = new FileStream(BufferFile, FileMode.Create))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(fs, replayBuffer);
                }
                Console.WriteLine($"Replay buffer saved ({replayBuffer.Count} experiences)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving replay buffer: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads replay buffer from file
        /// </summary>
        /// <returns>True if buffer was loaded successfully, false otherwise</returns>
        public bool LoadReplayBuffer()
        {
            try
            {
                if (File.Exists(BufferFile))
                {
                    using (FileStream fs = new FileStream(BufferFile, FileMode.Open))
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        replayBuffer = (List<Experience>)formatter.Deserialize(fs);
                    }
                    Console.WriteLine($"Replay buffer loaded ({replayBuffer.Count} experiences)");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading replay buffer: {ex.Message}");
                replayBuffer = new List<Experience>();
            }
            return false;
        }

        /// <summary>
        /// Returns current ray info for visualization
        /// </summary>
        /// <returns>Array of ray information</returns>
        public RayInfo[] GetRayInfo()
        {
            return currentRays;
        }

        #endregion
    }

}
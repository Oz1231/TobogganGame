using System;
using System.Collections.Generic;
using System.Drawing;

namespace TobogganGame
{
    /// <summary>
    /// Manages the game logic and state
    /// </summary>
    public class GameEngine
    {
        // Game grid dimensions
        private readonly int gridWidth;
        private readonly int gridHeight;

        // Game objects
        private Toboggan toboggan;
        private Flag flag;
        private List<Obstacle> obstacles;

        // Game state
        private bool isGameOver;
        private int score;
        private int stuckCounter = 0;
        private Point lastPosition;
        private const int StuckThreshold = 50; // Game will end if toboggan is stuck for too long

        // Random number generator for placing flag
        private Random random;

        // Obstacle settings
        private const int ObstaclesPerType = 7; // 7 of each type

        // Predefined obstacle positions for consistent gameplay
        private static readonly Point[] rockPositions = new Point[]
        {
            new Point(5, 5),
            new Point(22, 5),
            new Point(14, 14),
            new Point(8, 18),
            new Point(20, 20),
            new Point(3, 22),
            new Point(25, 15)
        };

        private static readonly Point[] treePositions = new Point[]
        {
            new Point(10, 3),
            new Point(18, 6),
            new Point(4, 10),
            new Point(24, 12),
            new Point(12, 18),
            new Point(16, 22),
            new Point(8, 24)
        };

        private static readonly Point[] iceHillPositions = new Point[]
        {
            new Point(7, 7),
            new Point(22, 2),
            new Point(2, 15),
            new Point(16, 10),
            new Point(26, 18),
            new Point(20, 24),
            new Point(11, 22)
        };

        // Property accessors
        public bool IsGameOver => isGameOver;
        public int Score => score;
        public Toboggan Toboggan => toboggan;
        public Flag Flag => flag;
        public List<Obstacle> Obstacles => obstacles;

        /// <summary>
        /// Initializes a new game engine
        /// </summary>
        /// <param name="gridWidth">Width of the game grid</param>
        /// <param name="gridHeight">Height of the game grid</param>
        public GameEngine(int gridWidth, int gridHeight)
        {
            this.gridWidth = gridWidth;
            this.gridHeight = gridHeight;
            random = new Random();
            obstacles = new List<Obstacle>();
            Reset();
        }

        /// <summary>
        /// Resets the game to initial state
        /// </summary>
        public void Reset()
        {
            // Reset game state
            isGameOver = false;
            score = 0;
            stuckCounter = 0;

            // Create the toboggan at the center of the grid
            toboggan = new Toboggan(gridWidth / 2, gridHeight / 2);
            lastPosition = toboggan.Head;

            // Create obstacles with fixed positions
            obstacles.Clear();
            GenerateFixedObstacles();

            // Create a flag
            flag = new Flag();
            flag.Relocate(gridWidth, gridHeight, toboggan, obstacles);
        }

        /// <summary>
        /// Generates obstacles at fixed positions for consistent gameplay
        /// </summary>
        private void GenerateFixedObstacles()
        {
            // Generate rocks
            for (int i = 0; i < ObstaclesPerType && i < rockPositions.Length; i++)
            {
                obstacles.Add(new Obstacle(rockPositions[i], ObstacleType.Rock));
            }

            // Generate trees
            for (int i = 0; i < ObstaclesPerType && i < treePositions.Length; i++)
            {
                obstacles.Add(new Obstacle(treePositions[i], ObstacleType.Tree));
            }

            // Generate ice hills
            for (int i = 0; i < ObstaclesPerType && i < iceHillPositions.Length; i++)
            {
                obstacles.Add(new Obstacle(iceHillPositions[i], ObstacleType.IceHill));
            }
        }

        /// <summary>
        /// Sets the direction of the toboggan
        /// </summary>
        /// <param name="direction">New direction for the toboggan</param>
        public void SetDirection(Direction direction)
        {
            toboggan.ChangeDirection(direction);
        }

        /// <summary>
        /// Calculates a new head position based on current head and direction
        /// </summary>
        /// <param name="head">Current head position</param>
        /// <param name="direction">Direction to move</param>
        /// <returns>New head position</returns>
        private Point CalculateNewHeadPosition(Point head, Direction direction)
        {
            Point newHead = new Point(head.X, head.Y);

            switch (direction)
            {
                case Direction.Up:
                    newHead.Y--;
                    break;
                case Direction.Down:
                    newHead.Y++;
                    break;
                case Direction.Left:
                    newHead.X--;
                    break;
                case Direction.Right:
                    newHead.X++;
                    break;
                case Direction.UpLeft:
                    newHead.X--;
                    newHead.Y--;
                    break;
                case Direction.UpRight:
                    newHead.X++;
                    newHead.Y--;
                    break;
                case Direction.DownLeft:
                    newHead.X--;
                    newHead.Y++;
                    break;
                case Direction.DownRight:
                    newHead.X++;
                    newHead.Y++;
                    break;
            }

            return newHead;
        }

        /// <summary>
        /// Checks if the position collides with any obstacle
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <returns>True if collision detected, false otherwise</returns>
        private bool CollidesWithObstacle(Point position)
        {
            foreach (Obstacle obstacle in obstacles)
            {
                // All obstacles now cause collisions
                if (obstacle.Position.X == position.X && obstacle.Position.Y == position.Y)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Updates the game state for one tick
        /// </summary>
        /// <returns>True if flag was collected, false otherwise</returns>
        
        public bool Update()
        {
            // Early exit if game is over
            if (isGameOver)
            {
                return false;
            }

            // Calculate new head position
            Point currentHead = toboggan.Head;
            Point newHead = CalculateNewHeadPosition(currentHead, toboggan.Direction);

            // Check for wall collision
            bool wallCollision = newHead.X < 0 || newHead.X >= gridWidth ||
                                newHead.Y < 0 || newHead.Y >= gridHeight;

            // Check obstacle collision
            bool obstacleCollision = CollidesWithObstacle(newHead);

            // Detect self-collision by iterating through body segments and checking for coordinate matches
            bool selfCollision = false;
            LinkedListNode<Point> current = toboggan.Segments.First.Next; // Skip head
            while (current != null && !selfCollision)
            {
                selfCollision = (current.Value.X == newHead.X && current.Value.Y == newHead.Y);
                current = current.Next;
            }

            // Combine all collision checks
            bool anyCollision = wallCollision || obstacleCollision || selfCollision;

            // Handle collision case
            if (anyCollision)
            {
                isGameOver = true;
                return false;
            }

            // Move the toboggan since we've verified it's safe
            toboggan.Move();

            // Handle stuck detection
            bool inSamePosition = newHead.Equals(lastPosition);
            if (inSamePosition)
            {
                stuckCounter++;
                if (stuckCounter > StuckThreshold)
                {
                    isGameOver = true;
                    return false;
                }
            }
            else
            {
                stuckCounter = 0;
                lastPosition = newHead;
            }

            // Check for flag collection
            bool flagCollected = (newHead.X == flag.Position.X && newHead.Y == flag.Position.Y);
            if (flagCollected)
            {
                // Increase score
                score++;

                // Extend the toboggan
                toboggan.Extend();

                // Place a new flag (obstacles stay the same)
                flag.Relocate(gridWidth, gridHeight, toboggan, obstacles);

                // Reset stuck counter when flag is collected
                stuckCounter = 0;
            }

            return flagCollected;
        }

        /// <summary>
        /// Gets the width of the game grid
        /// </summary>
        /// <returns>Grid width</returns>
        public int GetGridWidth()
        {
            return gridWidth;
        }

        /// <summary>
        /// Gets the height of the game grid
        /// </summary>
        /// <returns>Grid height</returns>
        public int GetGridHeight()
        {
            return gridHeight;
        }
    }
}
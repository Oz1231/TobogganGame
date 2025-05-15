using System;
using System.Collections.Generic;
using System.Drawing;

namespace TobogganGame
{
    /// <summary>
    /// Represents a flag (collectible item) in the game
    /// </summary>
    public class Flag
    {
        // Position of the flag
        private Point position;

        // Random number generator
        private Random random;

        // Flag properties
        private Color flagColor;
        private int rotationAngle;

        /// <summary>
        /// Gets the position of the flag
        /// </summary>
        public Point Position => position;

        /// <summary>
        /// Gets the color of the flag
        /// </summary>
        public Color FlagColor => flagColor;

        /// <summary>
        /// Gets the rotation angle of the flag
        /// </summary>
        public int RotationAngle => rotationAngle;

        /// <summary>
        /// Creates a new flag with a random position
        /// </summary>
        public Flag()
        {
            random = new Random();
            position = new Point(0, 0); // Will be set properly with Relocate

            // Pick a random warm color for the flag (reds, oranges, yellows)
            Color[] warmColors = new Color[]
            {
                Color.FromArgb(255, 0, 0),       // Bright Red
                Color.FromArgb(255, 80, 0),      // Red-Orange
                Color.FromArgb(255, 128, 0),     // Orange
                Color.FromArgb(255, 165, 0),     // Dark Orange
                Color.FromArgb(255, 192, 0),     // Amber
                Color.FromArgb(255, 215, 0),     // Gold
                Color.FromArgb(255, 255, 0),     // Bright Yellow
                Color.FromArgb(255, 69, 0)       // Red-Orange (Vermilion)
            };

            flagColor = warmColors[random.Next(warmColors.Length)];

            // Random rotation
            rotationAngle = random.Next(-10, 10);
        }

        /// <summary>
        /// Places the flag at a new random position, avoiding toboggan and obstacles
        /// </summary>
        /// <param name="gridWidth">Width of the game grid</param>
        /// <param name="gridHeight">Height of the game grid</param>
        /// <param name="toboggan">The toboggan to avoid overlapping with</param>
        /// <param name="obstacles">The obstacles to avoid overlapping with</param>
        public void Relocate(int gridWidth, int gridHeight, Toboggan toboggan, List<Obstacle> obstacles = null)
        {
            bool validPosition = false;
            int attempts = 0;
            const int maxAttempts = 100;

            while (!validPosition && attempts <= maxAttempts)
            {
                // Generate a random position
                int x = random.Next(1, gridWidth - 1);
                int y = random.Next(1, gridHeight - 1);
                position = new Point(x, y);

                // Assume valid until proven otherwise
                validPosition = true;

                // Check for overlap with toboggan segments
                foreach (Point segment in toboggan.Segments)
                {
                    bool overlapsSegment = (segment.X == position.X && segment.Y == position.Y);
                    validPosition = validPosition && !overlapsSegment;
                }

                // Check for overlap with obstacles
                if (validPosition && obstacles != null)
                {
                    foreach (Obstacle obstacle in obstacles)
                    {
                        bool overlapsObstacle = (obstacle.Position.X == position.X && obstacle.Position.Y == position.Y);
                        validPosition = validPosition && !overlapsObstacle;
                    }
                }

                attempts++;
            }

            // If exceeded max attempts, keep the last position tried

            // Pick a new random warm color when relocating
            Color[] warmColors = new Color[]
            {
        Color.FromArgb(255, 0, 0),       // Bright Red
        Color.FromArgb(255, 80, 0),      // Red-Orange
        Color.FromArgb(255, 128, 0),     // Orange
        Color.FromArgb(255, 165, 0),     // Dark Orange
        Color.FromArgb(255, 192, 0),     // Amber
        Color.FromArgb(255, 215, 0),     // Gold
        Color.FromArgb(255, 255, 0),     // Bright Yellow
        Color.FromArgb(255, 69, 0)       // Red-Orange (Vermilion)
            };

            flagColor = warmColors[random.Next(warmColors.Length)];

            // New random rotation
            rotationAngle = random.Next(-10, 10);
        }

        /// <summary>
        /// Draws the flag on the graphics surface
        /// </summary>
        /// <param name="g">Graphics object to draw on</param>
        /// <param name="cellSize">Size of each grid cell</param>
        public void Draw(Graphics g, int cellSize)
        {
            // Save current transform
            System.Drawing.Drawing2D.Matrix originalTransform = g.Transform.Clone();

            try
            {
                // Get center position for the flag
                float centerX = position.X * cellSize + cellSize / 2f;
                float centerY = position.Y * cellSize + cellSize / 2f;

                // Apply transform for rotation
                g.TranslateTransform(centerX, centerY);
                g.RotateTransform(rotationAngle);

                // Flag pole
                using (Pen polePen = new Pen(Color.FromArgb(100, 100, 100), 2))
                {
                    g.DrawLine(polePen, 0, -cellSize / 2 + 2, 0, cellSize / 2 - 2);
                }

                // Flag
                Point[] flagPoints = new Point[]
                {
                    new Point(0, -cellSize/2 + 4),           // Top of the pole
                    new Point(cellSize/2 - 2, -cellSize/4),  // Top corner of flag
                    new Point(cellSize/2 - 2, cellSize/4),   // Bottom corner of flag
                    new Point(0, 0)                          // Middle of pole
                };

                using (SolidBrush flagBrush = new SolidBrush(flagColor))
                {
                    g.FillPolygon(flagBrush, flagPoints);
                }

                using (Pen flagPen = new Pen(Color.Black, 1))
                {
                    g.DrawPolygon(flagPen, flagPoints);
                }
            }
            finally
            {
                // Restore original transform
                g.Transform = originalTransform;
            }
        }
    }
}
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace TobogganGame
{
    /// <summary>
    /// Type of obstacle
    /// </summary>
    public enum ObstacleType
    {
        Rock,
        Tree,
        IceHill
    }

    /// <summary>
    /// Represents an obstacle in the game
    /// </summary>
    public class Obstacle
    {
        // Position of the obstacle
        private Point position;

        // Type of obstacle
        private ObstacleType type;

        // Size multiplier for all obstacles
        private const float SizeMultiplier = 0.7f; // 70% of original size

        /// <summary>
        /// Gets the position of the obstacle
        /// </summary>
        public Point Position => position;

        /// <summary>
        /// Gets the type of obstacle
        /// </summary>
        public ObstacleType Type => type;

        /// <summary>
        /// Creates a new obstacle at the specified position with the specified type
        /// </summary>
        /// <param name="position">Position of the obstacle on the game grid</param>
        /// <param name="type">Type of obstacle (Rock, Tree, or IceHill)</param>
        public Obstacle(Point position, ObstacleType type)
        {
            this.position = position;
            this.type = type;
        }

        /// <summary>
        /// Draws the obstacle on the graphics surface
        /// </summary>
        /// <param name="g">Graphics object to draw on</param>
        /// <param name="cellSize">Size of each grid cell</param>
        public void Draw(Graphics g, int cellSize)
        {
            int x = position.X * cellSize;
            int y = position.Y * cellSize;

            // Calculate reduced size
            int adjustedSize = (int)(cellSize * SizeMultiplier);

            // Calculate offset to center in cell
            int offset = (cellSize - adjustedSize) / 2;

            // Adjusted rectangle that's smaller and centered
            Rectangle rect = new Rectangle(x + offset, y + offset, adjustedSize - 2, adjustedSize - 2);

            switch (type)
            {
                case ObstacleType.Rock:
                    // Draw a  gray rock
                    using (SolidBrush brush = new SolidBrush(Color.DarkGray))
                    {
                        g.FillEllipse(brush, rect);
                    }
                    using (Pen pen = new Pen(Color.Black, 1))
                    {
                        g.DrawEllipse(pen, rect);

                        // Add some rock details - scaled down
                        int detailSize = adjustedSize / 4;
                        g.DrawLine(pen,
                            x + offset + detailSize,
                            y + offset + detailSize,
                            x + offset + 2 * detailSize,
                            y + offset + detailSize);

                        g.DrawLine(pen,
                            x + offset + adjustedSize - detailSize - 1,
                            y + offset + adjustedSize - detailSize - 1,
                            x + offset + adjustedSize - 2 * detailSize - 1,
                            y + offset + adjustedSize - detailSize - 1);
                    }
                    break;

                case ObstacleType.Tree:
                    // Draw a tree with trunk and green top
                    int trunkWidth = adjustedSize / 3;
                    int trunkHeight = adjustedSize / 2;

                    // Draw trunk
                    using (SolidBrush brush = new SolidBrush(Color.SaddleBrown))
                    {
                        g.FillRectangle(brush,
                            x + offset + (adjustedSize - trunkWidth) / 2,
                            y + offset + adjustedSize - trunkHeight,
                            trunkWidth, trunkHeight);
                    }

                    // Draw tree top (triangle)
                    Point[] treeTop = new Point[]
                    {
                        new Point(x + offset + adjustedSize / 2, y + offset + 1),
                        new Point(x + offset + 1, y + offset + adjustedSize - trunkHeight),
                        new Point(x + offset + adjustedSize - 1, y + offset + adjustedSize - trunkHeight)
                    };

                    using (SolidBrush brush = new SolidBrush(Color.ForestGreen))
                    {
                        g.FillPolygon(brush, treeTop);
                    }

                    using (Pen pen = new Pen(Color.DarkGreen, 1))
                    {
                        g.DrawPolygon(pen, treeTop);
                    }
                    break;

                case ObstacleType.IceHill:
                    // Save graphics state
                    GraphicsState state = g.Save();

                    try
                    {
                        // Calculate a smaller area within the adjusted cell
                        int padding = adjustedSize / 6;
                        int smallerSize = adjustedSize - (padding * 2);

                        // Adjust x and y for the padding
                        int sx = x + offset + padding;
                        int sy = y + offset + padding;

                        // Create multiple ice spikes for a more threatening appearance
                        // Main central spike (taller)
                        Point[] mainSpike = new Point[]
                        {
                            new Point(sx + smallerSize / 2, sy),                // Top point (sharper)
                            new Point(sx + smallerSize, sy + smallerSize),      // Bottom right
                            new Point(sx, sy + smallerSize)                     // Bottom left
                        };

                        // Left smaller spike
                        Point[] leftSpike = new Point[]
                        {
                            new Point(sx + smallerSize / 4, sy + smallerSize / 3),  // Top point
                            new Point(sx + smallerSize / 2, sy + smallerSize),      // Bottom right
                            new Point(sx, sy + smallerSize)                         // Bottom left
                        };

                        // Right smaller spike
                        Point[] rightSpike = new Point[]
                        {
                            new Point(sx + 3 * smallerSize / 4, sy + smallerSize / 3),  // Top point
                            new Point(sx + smallerSize, sy + smallerSize),              // Bottom right
                            new Point(sx + smallerSize / 2, sy + smallerSize)           // Bottom left
                        };

                        // Define the smaller rectangle for the gradient
                        Rectangle smallerRect = new Rectangle(sx, sy, smallerSize, smallerSize);

                        // Fill with brighter, more saturated ice blue gradient
                        using (LinearGradientBrush mainBrush = new LinearGradientBrush(
                            smallerRect,
                            Color.FromArgb(220, 240, 255),       // Light ice blue
                            Color.FromArgb(30, 130, 240),        // More vivid/saturated blue
                            LinearGradientMode.Vertical))
                        {
                            g.FillPolygon(mainBrush, mainSpike);
                        }

                        using (LinearGradientBrush sideBrush = new LinearGradientBrush(
                            smallerRect,
                            Color.FromArgb(200, 230, 255),       // Light ice blue
                            Color.FromArgb(40, 150, 250),        // More vivid/saturated blue
                            LinearGradientMode.Vertical))
                        {
                            g.FillPolygon(sideBrush, leftSpike);
                            g.FillPolygon(sideBrush, rightSpike);
                        }

                        // First add a thicker darker blue outline
                        using (Pen darkOutlinePen = new Pen(Color.FromArgb(10, 80, 150), 1.5f)) // Reduced line width
                        {
                            g.DrawPolygon(darkOutlinePen, mainSpike);
                            g.DrawPolygon(darkOutlinePen, leftSpike);
                            g.DrawPolygon(darkOutlinePen, rightSpike);
                        }

                        // Then add a thinner cyan/blue outline for contrast
                        using (Pen blueOutlinePen = new Pen(Color.FromArgb(80, 180, 255), 0.8f)) // Reduced line width
                        {
                            g.DrawPolygon(blueOutlinePen, mainSpike);
                            g.DrawPolygon(blueOutlinePen, leftSpike);
                            g.DrawPolygon(blueOutlinePen, rightSpike);
                        }

                        // Add more prominent white highlights
                        using (Pen highlightPen = new Pen(Color.White, 1.0f)) // Reduced line width
                        {
                            // Highlight on main spike
                            g.DrawLine(highlightPen,
                                sx + smallerSize / 3, sy + smallerSize / 3,
                                sx + smallerSize / 2, sy + smallerSize / 6);

                            // Highlights on side spikes
                            g.DrawLine(highlightPen,
                                sx + smallerSize / 5, sy + smallerSize / 2,
                                sx + smallerSize / 4, sy + smallerSize / 3);

                            g.DrawLine(highlightPen,
                                sx + 3 * smallerSize / 4, sy + smallerSize / 3,
                                sx + 4 * smallerSize / 5, sy + smallerSize / 2);
                        }
                    }
                    finally
                    {
                        // Restore graphics state
                        g.Restore(state);
                    }
                    break;
            }
        }
    }
}
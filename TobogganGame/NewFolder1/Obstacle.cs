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
            Rectangle rect = new Rectangle(x + 1, y + 1, cellSize - 2, cellSize - 2);

            switch (type)
            {
                case ObstacleType.Rock:
                    // Draw a gray rock
                    using (SolidBrush brush = new SolidBrush(Color.DarkGray))
                    {
                        g.FillEllipse(brush, rect);
                    }
                    using (Pen pen = new Pen(Color.Black, 1))
                    {
                        g.DrawEllipse(pen, rect);

                        // Add some rock details
                        int detailSize = cellSize / 4;
                        g.DrawLine(pen, x + detailSize, y + detailSize,
                            x + 2 * detailSize, y + detailSize);
                        g.DrawLine(pen, x + cellSize - detailSize - 1, y + cellSize - detailSize - 1,
                            x + cellSize - 2 * detailSize - 1, y + cellSize - detailSize - 1);
                    }
                    break;

                case ObstacleType.Tree:
                    // Draw a tree with trunk and green top
                    int trunkWidth = cellSize / 3;
                    int trunkHeight = cellSize / 2;

                    // Draw trunk
                    using (SolidBrush brush = new SolidBrush(Color.SaddleBrown))
                    {
                        g.FillRectangle(brush,
                            x + (cellSize - trunkWidth) / 2,
                            y + cellSize - trunkHeight,
                            trunkWidth, trunkHeight);
                    }

                    // Draw tree top (triangle)
                    Point[] treeTop = new Point[]
                    {
                        new Point(x + cellSize / 2, y + 1),
                        new Point(x + 1, y + cellSize - trunkHeight),
                        new Point(x + cellSize - 1, y + cellSize - trunkHeight)
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
                        // Calculate a smaller area within the cell
                        int padding = cellSize / 6;  // Increased padding to make hills smaller
                        int smallerSize = cellSize - (padding * 2);

                        // Adjust x and y for the padding
                        int sx = x + padding;
                        int sy = y + padding;

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
                        using (Pen darkOutlinePen = new Pen(Color.FromArgb(10, 80, 150), 2f))
                        {
                            g.DrawPolygon(darkOutlinePen, mainSpike);
                            g.DrawPolygon(darkOutlinePen, leftSpike);
                            g.DrawPolygon(darkOutlinePen, rightSpike);
                        }

                        // Then add a thinner cyan/blue outline for contrast
                        using (Pen blueOutlinePen = new Pen(Color.FromArgb(80, 180, 255), 1f))
                        {
                            g.DrawPolygon(blueOutlinePen, mainSpike);
                            g.DrawPolygon(blueOutlinePen, leftSpike);
                            g.DrawPolygon(blueOutlinePen, rightSpike);
                        }

                        // Add more prominent white highlights
                        using (Pen highlightPen = new Pen(Color.White, 1.5f))
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
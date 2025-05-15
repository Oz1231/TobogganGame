using System;
using System.Collections.Generic;
using System.Drawing;

namespace TobogganGame
{
    /// <summary>
    /// Represents the toboggan sled in the game
    /// </summary>
    public class Toboggan
    {
        // The toboggan represented as a linked list of positions
        private LinkedList<Point> segments;

        // Current movement direction
        private Direction direction;

        // Colors for toboggan segments
        private readonly Color headColor = Color.FromArgb(255, 165, 0); // Orange for head
        private readonly Color bodyColor = Color.FromArgb(160, 82, 45); // Brown for body

        /// <summary>
        /// Gets the current direction of the toboggan
        /// </summary>
        public Direction Direction => direction;

        /// <summary>
        /// Gets the toboggan's segments
        /// </summary>
        public LinkedList<Point> Segments => segments;

        /// <summary>
        /// Gets the front of the toboggan (head)
        /// </summary>
        public Point Head => segments.First.Value;

        /// <summary>
        /// Gets the color for the head
        /// </summary>
        public Color HeadColor => headColor;

        /// <summary>
        /// Gets the color for the body
        /// </summary>
        public Color BodyColor => bodyColor;

        /// <summary>
        /// Initializes a new toboggan at the specified starting position
        /// </summary>
        /// <param name="startX">Starting X coordinate</param>
        /// <param name="startY">Starting Y coordinate</param>
        public Toboggan(int startX, int startY)
        {
            Initialize(startX, startY);
        }

        /// <summary>
        /// Initializes or resets the toboggan to the starting position
        /// </summary>
        /// <param name="startX">Starting X coordinate</param>
        /// <param name="startY">Starting Y coordinate</param>
        public void Initialize(int startX, int startY)
        {
            // Create a new linked list
            segments = new LinkedList<Point>();

            // Create initial toboggan with 3 segments
            segments.AddFirst(new Point(startX, startY));         // Front
            segments.AddLast(new Point(startX - 1, startY));      // Middle
            segments.AddLast(new Point(startX - 2, startY));      // Rear

            // Set initial direction to right
            direction = Direction.Right;
        }

        /// <summary>
        /// Changes the toboggan's direction
        /// </summary>
        /// <param name="newDirection">New direction to set</param>
        public void ChangeDirection(Direction newDirection)
        {
            // Check opposite directions to prevent 180-degree turns
            if ((direction == Direction.Up && newDirection == Direction.Down) ||
                (direction == Direction.Down && newDirection == Direction.Up) ||
                (direction == Direction.Left && newDirection == Direction.Right) ||
                (direction == Direction.Right && newDirection == Direction.Left) ||
                (direction == Direction.UpLeft && newDirection == Direction.DownRight) ||
                (direction == Direction.UpRight && newDirection == Direction.DownLeft) ||
                (direction == Direction.DownLeft && newDirection == Direction.UpRight) ||
                (direction == Direction.DownRight && newDirection == Direction.UpLeft))
            {
                return;
            }

            direction = newDirection;
        }

        /// <summary>
        /// Moves the toboggan in its current direction
        /// </summary>
        /// <returns>The new position of the toboggan's head</returns>
        public Point Move()
        {
            // Calculate new head position
            Point head = segments.First.Value;
            Point newHead = CalculateNewHeadPosition(head);

            // Add the new head to the front
            segments.AddFirst(newHead);

            // Remove the tail (we'll add it back if we collect a flag)
            segments.RemoveLast();

            return newHead;
        }

        /// <summary>
        /// Extends the toboggan by one segment (after collecting a flag)
        /// </summary>
        public void Extend()
        {
            // Calculate a new position for the tail
            Point tail = segments.Last.Value;
            Point beforeTail = segments.Last.Previous.Value;

            // Add a new segment at the end - position it to extend the current tail
            int deltaX = tail.X - beforeTail.X;
            int deltaY = tail.Y - beforeTail.Y;

            Point newTail = new Point(tail.X + deltaX, tail.Y + deltaY);
            segments.AddLast(newTail);
        }

        /// <summary>
        /// Calculates a new head position based on current head and direction
        /// </summary>
        /// <param name="head">Current head position</param>
        /// <returns>New head position</returns>
        private Point CalculateNewHeadPosition(Point head)
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
        /// Checks if the toboggan has collided with itself
        /// </summary>
        /// <returns>True if collision detected, false otherwise</returns>
        public bool HasCollidedWithSelf()
        {
            Point head = segments.First.Value;
            LinkedListNode<Point> current = segments.First.Next; // Start from second segment

            // Check each segment (except the head)
            while (current != null)
            {
                if (current.Value.X == head.X && current.Value.Y == head.Y)
                {
                    return true;
                }
                current = current.Next;
            }

            return false;
        }

        /// <summary>
        /// Draws the toboggan on the graphics surface
        /// </summary>
        /// <param name="g">Graphics object to draw on</param>
        /// <param name="cellSize">Size of each grid cell</param>
        public void Draw(Graphics g, int cellSize)
        {
            bool isHead = true;
            int segmentCount = 0;

            foreach (Point segment in segments)
            {
                Rectangle rect = new Rectangle(
                    segment.X * cellSize + 1,
                    segment.Y * cellSize + 1,
                    cellSize - 2,
                    cellSize - 2
                );

                // Different style for head vs body
                if (isHead)
                {
                    // Draw the front/head of the toboggan
                    using (SolidBrush brush = new SolidBrush(headColor))
                    {
                        g.FillRectangle(brush, rect);
                    }

                    using (Pen pen = new Pen(Color.Black, 1))
                    {
                        g.DrawRectangle(pen, rect);
                    }

                    // Add a directional indicator
                    int arrowSize = cellSize / 3;
                    Point center = new Point(
                        segment.X * cellSize + cellSize / 2,
                        segment.Y * cellSize + cellSize / 2
                    );

                    using (SolidBrush arrowBrush = new SolidBrush(Color.White))
                    {
                        // Draw a different shape based on direction
                        switch (direction)
                        {
                            case Direction.Up:
                                // Draw triangle pointing up
                                Point[] upPoints = {
                                    new Point(center.X, center.Y - arrowSize),
                                    new Point(center.X - arrowSize, center.Y + arrowSize/2),
                                    new Point(center.X + arrowSize, center.Y + arrowSize/2)
                                };
                                g.FillPolygon(arrowBrush, upPoints);
                                break;

                            case Direction.Down:
                                // Draw triangle pointing down
                                Point[] downPoints = {
                                    new Point(center.X, center.Y + arrowSize),
                                    new Point(center.X - arrowSize, center.Y - arrowSize/2),
                                    new Point(center.X + arrowSize, center.Y - arrowSize/2)
                                };
                                g.FillPolygon(arrowBrush, downPoints);
                                break;

                            case Direction.Left:
                                // Draw triangle pointing left
                                Point[] leftPoints = {
                                    new Point(center.X - arrowSize, center.Y),
                                    new Point(center.X + arrowSize/2, center.Y - arrowSize),
                                    new Point(center.X + arrowSize/2, center.Y + arrowSize)
                                };
                                g.FillPolygon(arrowBrush, leftPoints);
                                break;

                            case Direction.Right:
                                // Draw triangle pointing right
                                Point[] rightPoints = {
                                    new Point(center.X + arrowSize, center.Y),
                                    new Point(center.X - arrowSize/2, center.Y - arrowSize),
                                    new Point(center.X - arrowSize/2, center.Y + arrowSize)
                                };
                                g.FillPolygon(arrowBrush, rightPoints);
                                break;

                            case Direction.UpLeft:
                                // Draw triangle pointing up-left
                                Point[] upLeftPoints = {
                                    new Point(center.X - arrowSize, center.Y - arrowSize),
                                    new Point(center.X + arrowSize/2, center.Y - arrowSize/2),
                                    new Point(center.X - arrowSize/2, center.Y + arrowSize/2)
                                };
                                g.FillPolygon(arrowBrush, upLeftPoints);
                                break;

                            case Direction.UpRight:
                                // Draw triangle pointing up-right
                                Point[] upRightPoints = {
                                    new Point(center.X + arrowSize, center.Y - arrowSize),
                                    new Point(center.X - arrowSize/2, center.Y - arrowSize/2),
                                    new Point(center.X + arrowSize/2, center.Y + arrowSize/2)
                                };
                                g.FillPolygon(arrowBrush, upRightPoints);
                                break;

                            case Direction.DownLeft:
                                // Draw triangle pointing down-left
                                Point[] downLeftPoints = {
                                    new Point(center.X - arrowSize, center.Y + arrowSize),
                                    new Point(center.X - arrowSize/2, center.Y - arrowSize/2),
                                    new Point(center.X + arrowSize/2, center.Y + arrowSize/2)
                                };
                                g.FillPolygon(arrowBrush, downLeftPoints);
                                break;

                            case Direction.DownRight:
                                // Draw triangle pointing down-right
                                Point[] downRightPoints = {
                                    new Point(center.X + arrowSize, center.Y + arrowSize),
                                    new Point(center.X - arrowSize/2, center.Y + arrowSize/2),
                                    new Point(center.X + arrowSize/2, center.Y - arrowSize/2)
                                };
                                g.FillPolygon(arrowBrush, downRightPoints);
                                break;
                        }
                    }

                    isHead = false;
                }
                else
                {
                    // Alternate between dark and light brown for body segments
                    Color segmentColor = (segmentCount % 2 == 0)
                        ? bodyColor
                        : Color.FromArgb(bodyColor.R + 20, bodyColor.G + 20, bodyColor.B + 20);

                    using (SolidBrush brush = new SolidBrush(segmentColor))
                    {
                        g.FillRectangle(brush, rect);
                    }

                    using (Pen pen = new Pen(Color.Black, 1))
                    {
                        g.DrawRectangle(pen, rect);
                    }

                    // Add wood grain lines
                    if (segmentCount % 2 == 0)
                    {
                        using (Pen linePen = new Pen(Color.FromArgb(80, 40, 0), 1))
                        {
                            // Draw horizontal lines for "wooden slats" effect
                            g.DrawLine(linePen,
                                rect.Left + 3, rect.Top + rect.Height / 3,
                                rect.Right - 3, rect.Top + rect.Height / 3);

                            g.DrawLine(linePen,
                                rect.Left + 3, rect.Top + 2 * rect.Height / 3,
                                rect.Right - 3, rect.Top + 2 * rect.Height / 3);
                        }
                    }
                }

                segmentCount++;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace TobogganGame
{
    /// <summary>
    /// Represents the toboggan sled in the game
    /// </summary>
    public class Toboggan
    {
        // The toboggan represented as a linked list of positions
        private LinkedList<Point> segments;

        // Track previous positions for snow trails
        private Queue<Point> previousPositions;
        private const int MAX_TRAIL_POSITIONS = 15;  // Length of the snow trail

        // Current movement direction
        private Direction direction;

        // Colors for toboggan segments
        private readonly Color sledColor = Color.FromArgb(170, 100, 40);       // Dark wood for sled
        private readonly Color runnersColor = Color.FromArgb(120, 140, 160);   // Metal runners
        private readonly Color personColor = Color.FromArgb(40, 60, 110);      // Person (blue jacket)

        // Random for trail effects
        private Random random = new Random();

        // Animation properties
        private float animationOffset = 0;
        private const float ANIMATION_SPEED = 0.2f;
        private float animationDirection = 1;

        // Breath effect
        private int breathCounter = 0;
        private const int BREATH_INTERVAL = 20;
        private float breathScale = 0;

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

            // Initialize trail
            previousPositions = new Queue<Point>();

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

            // Record position for snow trail before removing tail
            if (segments.Count > 0)
            {
                Point trailPosition = segments.Last.Value;
                previousPositions.Enqueue(trailPosition);

                // Keep trail length limited
                if (previousPositions.Count > MAX_TRAIL_POSITIONS)
                {
                    previousPositions.Dequeue();
                }
            }

            // Add the new head to the front
            segments.AddFirst(newHead);

            // Remove the tail (we'll add it back if we collect a flag)
            segments.RemoveLast();

            // Update animation
            UpdateAnimation();

            return newHead;
        }

        /// <summary>
        /// Updates the animation state for toboggan movement
        /// </summary>
        private void UpdateAnimation()
        {
            // Sliding animation offset
            animationOffset += ANIMATION_SPEED * animationDirection;
            if (animationOffset > 1.5f || animationOffset < -1.5f)
            {
                animationDirection *= -1;
            }

            // Breath effect
            breathCounter++;
            if (breathCounter >= BREATH_INTERVAL)
            {
                breathCounter = 0;
                breathScale = 1.0f;
            }
            else if (breathScale > 0)
            {
                breathScale -= 0.1f;
                if (breathScale < 0) breathScale = 0;
            }
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
            // Draw snow trails first (so they appear behind the sled)
            DrawSnowTrails(g, cellSize);

            // First draw all cargo segments (segments beyond the main sled)
            DrawExtraSegments(g, cellSize);

            // Then draw the main sled if we have enough segments
            if (segments.Count >= 3)
            {
                DrawMainSled(g, cellSize);
            }
            else if (segments.Count > 0)
            {
                // If we don't have enough segments for the full sled, draw simple segments
                foreach (Point point in segments)
                {
                    DrawSimpleSegment(g, point, cellSize);
                }
            }

            // Draw breath effect from the head
            DrawBreathEffect(g, cellSize);
        }

        /// <summary>
        /// Draws the main part of the sled (the first three segments)
        /// </summary>
        private void DrawMainSled(Graphics g, int cellSize)
        {
            if (segments.Count < 3) return;

            // Get positions for the sled parts
            Point[] sledPoints = segments.Take(3).ToArray();

            // Calculate center point of the sled
            float centerX = 0, centerY = 0;
            foreach (Point pt in sledPoints)
            {
                centerX += pt.X;
                centerY += pt.Y;
            }
            centerX = (centerX / sledPoints.Length) * cellSize + cellSize / 2;
            centerY = (centerY / sledPoints.Length) * cellSize + cellSize / 2;

            // Calculate rotation angle based on direction
            float angle = 0;
            switch (direction)
            {
                case Direction.Right: angle = 0; break;
                case Direction.DownRight: angle = 45; break;
                case Direction.Down: angle = 90; break;
                case Direction.DownLeft: angle = 135; break;
                case Direction.Left: angle = 180; break;
                case Direction.UpLeft: angle = 225; break;
                case Direction.Up: angle = 270; break;
                case Direction.UpRight: angle = 315; break;
            }

            // Save graphics state before rotation
            GraphicsState state = g.Save();

            // Apply transform: move to center, rotate, then add sliding animation
            g.TranslateTransform(centerX, centerY);
            g.RotateTransform(angle);
            g.TranslateTransform(animationOffset, 0); // Sliding animation

            // Calculate sled dimensions based on cell size
            float sledLength = cellSize * 2.6f;
            float sledWidth = cellSize * 0.6f;

            // Draw the wooden sled base with rounded front
            using (SolidBrush sledBrush = new SolidBrush(sledColor))
            {
                // Sled base (elongated rounded rectangle)
                g.FillPath(sledBrush, CreateRoundedRectanglePath(
                    -sledLength / 2, -sledWidth / 2,
                    sledLength, sledWidth,
                    sledWidth / 2));

                // Add wood grain lines for decoration
                using (Pen grainPen = new Pen(Color.FromArgb(100, 60, 30), 1))
                {
                    for (int i = 1; i < 5; i++)
                    {
                        float yOffset = (-sledWidth / 2) + (sledWidth * i / 5);
                        g.DrawLine(grainPen,
                            -sledLength / 2 + sledWidth / 2, yOffset,
                            sledLength / 2 - sledWidth / 4, yOffset);
                    }
                }
            }

            // Draw the runners (metal skis under the sled)
            using (Pen runnerPen = new Pen(runnersColor, 3))
            {
                float runnerOffset = sledWidth * 0.4f;

                // Left runner
                g.DrawLine(runnerPen,
                    -sledLength / 2 + sledWidth / 2, -runnerOffset,
                    sledLength / 2 - sledWidth / 4, -runnerOffset);

                // Right runner
                g.DrawLine(runnerPen,
                    -sledLength / 2 + sledWidth / 2, runnerOffset,
                    sledLength / 2 - sledWidth / 4, runnerOffset);

                // Runner curves at front
                g.DrawArc(runnerPen,
                    -sledLength / 2 + sledWidth / 4, -runnerOffset - sledWidth / 4,
                    sledWidth / 2, sledWidth / 2,
                    180, 90);
                g.DrawArc(runnerPen,
                    -sledLength / 2 + sledWidth / 4, runnerOffset - sledWidth / 4,
                    sledWidth / 2, sledWidth / 2,
                    90, 90);
            }

            // Draw the person riding the sled
            DrawSledRider(g, sledLength, sledWidth);

            // Add sled outline
            using (Pen outlinePen = new Pen(Color.FromArgb(sledColor.R - 40, sledColor.G - 40, sledColor.B - 20), 1))
            {
                g.DrawPath(outlinePen, CreateRoundedRectanglePath(
                    -sledLength / 2, -sledWidth / 2,
                    sledLength, sledWidth,
                    sledWidth / 2));
            }

            // Restore graphics state
            g.Restore(state);
        }

        /// <summary>
        /// Draws the person riding the toboggan
        /// </summary>
        private void DrawSledRider(Graphics g, float sledLength, float sledWidth)
        {
            // Person sits in the back half of the sled
            float riderX = 0;
            float riderY = 0;

            // Body size
            float bodyWidth = sledWidth * 0.6f;
            float bodyHeight = sledWidth * 0.8f;

            // Draw the body (blue jacket)
            using (SolidBrush bodyBrush = new SolidBrush(personColor))
            {
                g.FillEllipse(bodyBrush,
                    riderX - bodyWidth / 2,
                    riderY - bodyHeight / 2,
                    bodyWidth, bodyHeight);
            }

            // Draw the head
            using (SolidBrush headBrush = new SolidBrush(Color.FromArgb(255, 211, 170)))
            {
                float headSize = bodyWidth * 0.6f;
                g.FillEllipse(headBrush,
                    riderX - headSize / 2,
                    riderY - bodyHeight / 2 - headSize * 0.8f,
                    headSize, headSize);
            }

            // Draw a simple hat
            using (SolidBrush hatBrush = new SolidBrush(Color.FromArgb(200, 30, 30)))
            {
                float headSize = bodyWidth * 0.6f;

                // Hat base
                g.FillEllipse(hatBrush,
                    riderX - headSize / 2 - 2,
                    riderY - bodyHeight / 2 - headSize * 0.9f,
                    headSize + 4, headSize / 2);

                // Hat top
                g.FillEllipse(hatBrush,
                    riderX - headSize / 3,
                    riderY - bodyHeight / 2 - headSize * 1.2f,
                    headSize / 2, headSize / 2);
            }

            // Draw arms holding the sled
            using (Pen armPen = new Pen(personColor, 4))
            {
                float armLength = bodyWidth * 0.7f;

                // Arms extend forward to hold the front of the sled
                g.DrawLine(armPen,
                    riderX - bodyWidth / 4, riderY - bodyHeight / 4,
                    riderX - bodyWidth - armLength / 2, riderY);
                g.DrawLine(armPen,
                    riderX + bodyWidth / 4, riderY - bodyHeight / 4,
                    riderX + bodyWidth + armLength / 2, riderY);
            }
        }

        /// <summary>
        /// Draws all extra segments beyond the main sled
        /// </summary>
        private void DrawExtraSegments(Graphics g, int cellSize)
        {
            if (segments.Count <= 3) return; // No extra segments

            // Get all segments beyond the first three
            var extraSegments = segments.Skip(3).ToList();

            // Draw from back to front
            for (int i = extraSegments.Count - 1; i >= 0; i--)
            {
                Point segmentPos = extraSegments[i];
                int segmentIndex = i + 3; // Offset for main sled segments

                // Draw cargo (alternating between styles)
                DrawCargoItem(g, segmentPos, cellSize, segmentIndex % 5);
            }
        }

        /// <summary>
        /// Draws a simple segment for when we don't have enough for the full sled
        /// </summary>
        private void DrawSimpleSegment(Graphics g, Point position, int cellSize)
        {
            // Calculate rectangle for the segment
            Rectangle rect = new Rectangle(
                position.X * cellSize + 2,
                position.Y * cellSize + 2,
                cellSize - 4,
                cellSize - 4
            );

            // Determine if this is the head
            bool isHead = position.Equals(segments.First.Value);

            // Choose color based on segment type
            Color segmentColor = isHead ?
                sledColor : // Head uses sled color
                Color.FromArgb(sledColor.R - 20, sledColor.G - 20, sledColor.B - 10); // Body slightly darker

            // Draw rounded rectangle for segment
            using (SolidBrush brush = new SolidBrush(segmentColor))
            {
                g.FillRoundedRectangle(rect.X, rect.Y, rect.Width, rect.Height,
                    isHead ? rect.Height / 2 : rect.Height / 4, brush);
            }

            // Draw outline
            using (Pen outlinePen = new Pen(Color.FromArgb(segmentColor.R - 40, segmentColor.G - 40, segmentColor.B - 20), 1))
            {
                g.DrawRoundedRectangle(rect.X, rect.Y, rect.Width, rect.Height,
                    isHead ? rect.Height / 2 : rect.Height / 4, outlinePen);
            }

            // If head, draw direction indicator
            if (isHead)
            {
                DrawDirectionIndicator(g, new Point(
                    position.X * cellSize + cellSize / 2,
                    position.Y * cellSize + cellSize / 2
                ), cellSize);
            }
        }

        /// <summary>
        /// Draws a direction indicator on the head
        /// </summary>
        private void DrawDirectionIndicator(Graphics g, Point center, int cellSize)
        {
            int arrowSize = cellSize / 3;

            using (SolidBrush arrowBrush = new SolidBrush(Color.White))
            {
                // Draw a different arrow based on direction
                switch (direction)
                {
                    case Direction.Up:
                        // Arrow pointing up
                        Point[] upPoints = {
                            new Point(center.X, center.Y - arrowSize/2),
                            new Point(center.X - arrowSize/3, center.Y + arrowSize/3),
                            new Point(center.X + arrowSize/3, center.Y + arrowSize/3)
                        };
                        g.FillPolygon(arrowBrush, upPoints);
                        break;

                    case Direction.Down:
                        // Arrow pointing down
                        Point[] downPoints = {
                            new Point(center.X, center.Y + arrowSize/2),
                            new Point(center.X - arrowSize/3, center.Y - arrowSize/3),
                            new Point(center.X + arrowSize/3, center.Y - arrowSize/3)
                        };
                        g.FillPolygon(arrowBrush, downPoints);
                        break;

                    case Direction.Left:
                        // Arrow pointing left
                        Point[] leftPoints = {
                            new Point(center.X - arrowSize/2, center.Y),
                            new Point(center.X + arrowSize/3, center.Y - arrowSize/3),
                            new Point(center.X + arrowSize/3, center.Y + arrowSize/3)
                        };
                        g.FillPolygon(arrowBrush, leftPoints);
                        break;

                    case Direction.Right:
                        // Arrow pointing right
                        Point[] rightPoints = {
                            new Point(center.X + arrowSize/2, center.Y),
                            new Point(center.X - arrowSize/3, center.Y - arrowSize/3),
                            new Point(center.X - arrowSize/3, center.Y + arrowSize/3)
                        };
                        g.FillPolygon(arrowBrush, rightPoints);
                        break;

                    // Handle diagonal directions
                    case Direction.UpLeft:
                        Point[] upLeftPoints = {
                            new Point(center.X - arrowSize/3, center.Y - arrowSize/3),
                            new Point(center.X + arrowSize/4, center.Y - arrowSize/6),
                            new Point(center.X - arrowSize/6, center.Y + arrowSize/4)
                        };
                        g.FillPolygon(arrowBrush, upLeftPoints);
                        break;

                    case Direction.UpRight:
                        Point[] upRightPoints = {
                            new Point(center.X + arrowSize/3, center.Y - arrowSize/3),
                            new Point(center.X - arrowSize/4, center.Y - arrowSize/6),
                            new Point(center.X + arrowSize/6, center.Y + arrowSize/4)
                        };
                        g.FillPolygon(arrowBrush, upRightPoints);
                        break;

                    case Direction.DownLeft:
                        Point[] downLeftPoints = {
                            new Point(center.X - arrowSize/3, center.Y + arrowSize/3),
                            new Point(center.X + arrowSize/4, center.Y + arrowSize/6),
                            new Point(center.X - arrowSize/6, center.Y - arrowSize/4)
                        };
                        g.FillPolygon(arrowBrush, downLeftPoints);
                        break;

                    case Direction.DownRight:
                        Point[] downRightPoints = {
                            new Point(center.X + arrowSize/3, center.Y + arrowSize/3),
                            new Point(center.X - arrowSize/4, center.Y + arrowSize/6),
                            new Point(center.X + arrowSize/6, center.Y - arrowSize/4)
                        };
                        g.FillPolygon(arrowBrush, downRightPoints);
                        break;
                }
            }
        }

        /// <summary>
        /// Draws a cargo item for an extra segment
        /// </summary>
        private void DrawCargoItem(Graphics g, Point position, int cellSize, int cargoType)
        {
            // Calculate center of cell
            float centerX = position.X * cellSize + cellSize / 2f;
            float centerY = position.Y * cellSize + cellSize / 2f;

            // Choose cargo color based on type
            Color cargoColor;
            switch (cargoType)
            {
                case 0: // Red luggage
                    cargoColor = Color.FromArgb(180, 60, 60);
                    break;
                case 1: // Green backpack
                    cargoColor = Color.FromArgb(60, 140, 60);
                    break;
                case 2: // Blue cooler
                    cargoColor = Color.FromArgb(60, 100, 180);
                    break;
                case 3: // Brown wooden crate
                    cargoColor = Color.FromArgb(140, 100, 60);
                    break;
                default: // Purple gift
                    cargoColor = Color.FromArgb(150, 70, 180);
                    break;
            }

            // Get the segment direction (between this and the next)
            Direction segmentDir = GetSegmentDirection(position);

            // Calculate rotation angle based on direction
            float angle = 0;
            switch (segmentDir)
            {
                case Direction.Right: angle = 0; break;
                case Direction.DownRight: angle = 45; break;
                case Direction.Down: angle = 90; break;
                case Direction.DownLeft: angle = 135; break;
                case Direction.Left: angle = 180; break;
                case Direction.UpLeft: angle = 225; break;
                case Direction.Up: angle = 270; break;
                case Direction.UpRight: angle = 315; break;
            }

            // Save graphics state
            GraphicsState state = g.Save();

            try
            {
                // Apply transform
                g.TranslateTransform(centerX, centerY);
                g.RotateTransform(angle);

                // Add a little bounce animation based on the animation offset
                g.TranslateTransform(0, animationOffset * 0.5f);

                // Draw based on cargo type
                switch (cargoType)
                {
                    case 0: // Red luggage
                        DrawLuggage(g, cellSize, cargoColor);
                        break;
                    case 1: // Green backpack
                        DrawBackpack(g, cellSize, cargoColor);
                        break;
                    case 2: // Blue cooler
                        DrawCooler(g, cellSize, cargoColor);
                        break;
                    case 3: // Brown wooden crate
                        DrawWoodenCrate(g, cellSize, cargoColor);
                        break;
                    default: // Purple gift
                        DrawGift(g, cellSize, cargoColor);
                        break;
                }
            }
            finally
            {
                // Restore graphics state
                g.Restore(state);
            }
        }

        /// <summary>
        /// Gets the direction between this segment and the next one
        /// </summary>
        private Direction GetSegmentDirection(Point position)
        {
            // Find the segment in the list
            int index = -1;
            int i = 0;
            foreach (Point p in segments)
            {
                if (p.Equals(position))
                {
                    index = i;
                    break;
                }
                i++;
            }

            if (index < 0 || index >= segments.Count - 1)
                return direction; // Default to main direction

            // Get next segment position
            Point next = GetSegmentAt(index + 1);

            // Calculate direction
            int dx = position.X - next.X;
            int dy = position.Y - next.Y;

            if (dx == 1 && dy == 0) return Direction.Left;
            if (dx == -1 && dy == 0) return Direction.Right;
            if (dx == 0 && dy == 1) return Direction.Up;
            if (dx == 0 && dy == -1) return Direction.Down;
            if (dx == 1 && dy == 1) return Direction.UpLeft;
            if (dx == -1 && dy == 1) return Direction.UpRight;
            if (dx == 1 && dy == -1) return Direction.DownLeft;
            if (dx == -1 && dy == -1) return Direction.DownRight;

            return direction; // Default
        }

        /// <summary>
        /// Draws a luggage item
        /// </summary>
        private void DrawLuggage(Graphics g, int cellSize, Color color)
        {
            float width = cellSize * 0.7f;
            float height = cellSize * 0.55f;
            float depth = height * 0.2f;

            // Get darker shade for details
            Color darkColor = Color.FromArgb(
                Math.Max(0, color.R - 50),
                Math.Max(0, color.G - 50),
                Math.Max(0, color.B - 50));

            // Draw main luggage box
            RectangleF luggageRect = new RectangleF(-width / 2, -height / 2, width, height);

            using (SolidBrush luggageBrush = new SolidBrush(color))
            {
                g.FillRoundedRectangle(luggageRect.X, luggageRect.Y, luggageRect.Width, luggageRect.Height, 5, luggageBrush);
            }

            // Draw luggage top border
            RectangleF topRect = new RectangleF(-width / 2, -height / 2 - depth, width, depth);

            using (SolidBrush topBrush = new SolidBrush(darkColor))
            {
                g.FillRoundedRectangle(topRect.X, topRect.Y, topRect.Width, topRect.Height, 3, topBrush);
            }

            // Draw horizontal line dividing top and bottom parts
            using (Pen detailPen = new Pen(darkColor, 2))
            {
                g.DrawLine(detailPen, -width / 2 + 5, 0, width / 2 - 5, 0);
            }

            // Draw handle
            using (Pen handlePen = new Pen(darkColor, 3))
            {
                float handleWidth = width * 0.3f;
                float handleHeight = height * 0.2f;

                g.DrawArc(handlePen,
                    -handleWidth / 2,
                    -height / 2 - depth - handleHeight,
                    handleWidth,
                    handleHeight * 2,
                    0, 180);
            }

            // Draw two luggage clasps
            float claspSize = width * 0.1f;
            using (SolidBrush claspBrush = new SolidBrush(Color.FromArgb(200, 200, 200)))
            {
                // Left clasp
                g.FillRectangle(claspBrush, -width * 0.3f - claspSize / 2, -height * 0.1f - claspSize / 2, claspSize, claspSize);

                // Right clasp
                g.FillRectangle(claspBrush, width * 0.3f - claspSize / 2, -height * 0.1f - claspSize / 2, claspSize, claspSize);
            }

            // Draw outline
            using (Pen outlinePen = new Pen(darkColor, 1))
            {
                g.DrawRoundedRectangle(luggageRect.X, luggageRect.Y, luggageRect.Width, luggageRect.Height, 5, outlinePen);
                g.DrawRoundedRectangle(topRect.X, topRect.Y, topRect.Width, topRect.Height, 3, outlinePen);
            }
        }

        /// <summary>
        /// Draws a backpack item
        /// </summary>
        private void DrawBackpack(Graphics g, int cellSize, Color color)
        {
            float width = cellSize * 0.65f;
            float height = cellSize * 0.7f;

            // Get darker color for details
            Color darkColor = Color.FromArgb(
                Math.Max(0, color.R - 50),
                Math.Max(0, color.G - 50),
                Math.Max(0, color.B - 50));

            // Get lighter color for highlights
            Color lightColor = Color.FromArgb(
                Math.Min(255, color.R + 30),
                Math.Min(255, color.G + 30),
                Math.Min(255, color.B + 30));

            // Main backpack body
            RectangleF bodyRect = new RectangleF(-width / 2, -height / 2, width, height);

            using (SolidBrush bodyBrush = new SolidBrush(color))
            {
                g.FillRoundedRectangle(bodyRect.X, bodyRect.Y, bodyRect.Width, bodyRect.Height, 5, bodyBrush);
            }

            // Draw front pocket
            RectangleF pocketRect = new RectangleF(-width * 0.4f, -height * 0.1f, width * 0.8f, height * 0.3f);

            using (SolidBrush pocketBrush = new SolidBrush(darkColor))
            {
                g.FillRoundedRectangle(pocketRect.X, pocketRect.Y, pocketRect.Width, pocketRect.Height, 3, pocketBrush);
            }

            // Draw zipper on pocket
            using (Pen zipperPen = new Pen(Color.FromArgb(220, 220, 220), 1))
            {
                zipperPen.DashStyle = DashStyle.Dot;
                g.DrawLine(zipperPen, -width * 0.3f, -height * 0.1f + 5, width * 0.3f, -height * 0.1f + 5);
            }

            // Draw backpack straps
            using (Pen strapPen = new Pen(darkColor, 4))
            {
                // Left strap
                g.DrawArc(strapPen, -width * 0.45f, -height * 0.5f, width * 0.3f, height * 0.5f, 180, 90);

                // Right strap
                g.DrawArc(strapPen, width * 0.15f, -height * 0.5f, width * 0.3f, height * 0.5f, 270, 90);
            }

            // Draw small decorative details
            using (SolidBrush detailBrush = new SolidBrush(lightColor))
            {
                // Logo or patch
                g.FillEllipse(detailBrush, -width * 0.1f, -height * 0.35f, width * 0.2f, width * 0.2f);
            }

            // Draw outline
            using (Pen outlinePen = new Pen(darkColor, 1))
            {
                g.DrawRoundedRectangle(bodyRect.X, bodyRect.Y, bodyRect.Width, bodyRect.Height, 5, outlinePen);
                g.DrawRoundedRectangle(pocketRect.X, pocketRect.Y, pocketRect.Width, pocketRect.Height, 3, outlinePen);
            }
        }

        /// <summary>
        /// Draws a cooler item
        /// </summary>
        private void DrawCooler(Graphics g, int cellSize, Color color)
        {
            float width = cellSize * 0.65f;
            float height = cellSize * 0.6f;
            float lidHeight = height * 0.2f;

            // Get darker color for details
            Color darkColor = Color.FromArgb(
                Math.Max(0, color.R - 50),
                Math.Max(0, color.G - 50),
                Math.Max(0, color.B - 50));

            // Get lighter color for highlights
            Color lightColor = Color.FromArgb(
                Math.Min(255, color.R + 30),
                Math.Min(255, color.G + 30),
                Math.Min(255, color.B + 30));

            // Main cooler body
            RectangleF bodyRect = new RectangleF(-width / 2, -height / 2 + lidHeight, width, height - lidHeight);

            using (SolidBrush bodyBrush = new SolidBrush(color))
            {
                g.FillRectangle(bodyBrush, bodyRect);
            }

            // Cooler lid
            RectangleF lidRect = new RectangleF(-width / 2, -height / 2, width, lidHeight);

            using (SolidBrush lidBrush = new SolidBrush(darkColor))
            {
                g.FillRectangle(lidBrush, lidRect);
            }

            // Draw handle on top
            using (Pen handlePen = new Pen(darkColor, 3))
            {
                float handleWidth = width * 0.4f;
                float handleHeight = lidHeight * 0.8f;

                // Handle arc
                g.DrawArc(handlePen,
                    -handleWidth / 2,
                    -height / 2 - handleHeight,
                    handleWidth,
                    handleHeight * 2,
                    0, 180);
            }

            // Draw cooler latch
            using (SolidBrush latchBrush = new SolidBrush(lightColor))
            {
                g.FillRectangle(latchBrush, -width * 0.1f, -height / 2 + lidHeight * 0.5f, width * 0.2f, lidHeight * 0.5f);
            }

            // Draw some decorative ice/snow pattern on the side
            using (Pen detailPen = new Pen(lightColor, 1))
            {
                // Snowflake-like pattern
                float patternSize = width * 0.15f;
                float patternX = -width * 0.25f;
                float patternY = 0;

                // Draw simple snowflake
                g.DrawLine(detailPen, patternX - patternSize / 2, patternY, patternX + patternSize / 2, patternY);
                g.DrawLine(detailPen, patternX, patternY - patternSize / 2, patternX, patternY + patternSize / 2);
                g.DrawLine(detailPen, patternX - patternSize / 3, patternY - patternSize / 3, patternX + patternSize / 3, patternY + patternSize / 3);
                g.DrawLine(detailPen, patternX - patternSize / 3, patternY + patternSize / 3, patternX + patternSize / 3, patternY - patternSize / 3);
            }

            // Draw outline
            using (Pen outlinePen = new Pen(darkColor, 1))
            {
                g.DrawRectangle(outlinePen, bodyRect.X, bodyRect.Y, bodyRect.Width, bodyRect.Height);
                g.DrawRectangle(outlinePen, lidRect.X, lidRect.Y, lidRect.Width, lidRect.Height);
            }
        }

        /// <summary>
        /// Draws a wooden crate item
        /// </summary>
        private void DrawWoodenCrate(Graphics g, int cellSize, Color color)
        {
            float size = cellSize * 0.7f;

            // Get darker color for details
            Color darkColor = Color.FromArgb(
                Math.Max(0, color.R - 40),
                Math.Max(0, color.G - 40),
                Math.Max(0, color.B - 40));

            // Main crate square
            RectangleF crateRect = new RectangleF(-size / 2, -size / 2, size, size);

            using (SolidBrush crateBrush = new SolidBrush(color))
            {
                g.FillRectangle(crateBrush, crateRect);
            }

            // Draw wood plank details
            using (Pen plankPen = new Pen(darkColor, 1))
            {
                // Horizontal planks
                int plankCount = 3;
                for (int i = 1; i < plankCount; i++)
                {
                    float y = -size / 2 + (size * i / plankCount);
                    g.DrawLine(plankPen, -size / 2, y, size / 2, y);
                }

                // Vertical planks
                for (int i = 1; i < plankCount; i++)
                {
                    float x = -size / 2 + (size * i / plankCount);
                    g.DrawLine(plankPen, x, -size / 2, x, size / 2);
                }
            }

            // Draw nail details at corners and intersections
            using (SolidBrush nailBrush = new SolidBrush(Color.FromArgb(150, 150, 150)))
            {
                // Corner nails
                float nailSize = size * 0.05f;
                float nailOffset = size * 0.08f;

                // Draw nails at corners
                g.FillEllipse(nailBrush, -size / 2 + nailOffset - nailSize / 2, -size / 2 + nailOffset - nailSize / 2, nailSize, nailSize);
                g.FillEllipse(nailBrush, size / 2 - nailOffset - nailSize / 2, -size / 2 + nailOffset - nailSize / 2, nailSize, nailSize);
                g.FillEllipse(nailBrush, -size / 2 + nailOffset - nailSize / 2, size / 2 - nailOffset - nailSize / 2, nailSize, nailSize);
                g.FillEllipse(nailBrush, size / 2 - nailOffset - nailSize / 2, size / 2 - nailOffset - nailSize / 2, nailSize, nailSize);
            }

            // Draw simple label or marking
            using (SolidBrush labelBrush = new SolidBrush(darkColor))
            {
                // Create a simple stamp-like rectangle with rounded corners
                float labelSize = size * 0.3f;
                g.FillRoundedRectangle(
                    -labelSize / 2,
                    -labelSize / 2,
                    labelSize,
                    labelSize,
                    3,
                    labelBrush);

                // Add a simple mark inside the label
                using (Pen markPen = new Pen(Color.FromArgb(220, 220, 200), 1))
                {
                    // Draw an X
                    g.DrawLine(markPen, -labelSize / 4, -labelSize / 4, labelSize / 4, labelSize / 4);
                    g.DrawLine(markPen, -labelSize / 4, labelSize / 4, labelSize / 4, -labelSize / 4);
                }
            }

            // Draw outline
            using (Pen outlinePen = new Pen(darkColor, 2))
            {
                g.DrawRectangle(outlinePen, crateRect.X, crateRect.Y, crateRect.Width, crateRect.Height);
            }
        }

        /// <summary>
        /// Draws a gift/present item
        /// </summary>
        private void DrawGift(Graphics g, int cellSize, Color color)
        {
            float size = cellSize * 0.6f;

            // Get darker color for details
            Color darkColor = Color.FromArgb(
                Math.Max(0, color.R - 40),
                Math.Max(0, color.G - 40),
                Math.Max(0, color.B - 40));

            // Get contrasting ribbon color (yellow/gold for purple)
            Color ribbonColor = Color.FromArgb(220, 180, 40);

            // Main gift box
            RectangleF giftRect = new RectangleF(-size / 2, -size / 2, size, size);

            using (SolidBrush giftBrush = new SolidBrush(color))
            {
                g.FillRectangle(giftBrush, giftRect);
            }

            // Draw ribbon
            using (SolidBrush ribbonBrush = new SolidBrush(ribbonColor))
            {
                // Horizontal ribbon
                g.FillRectangle(ribbonBrush, -size / 2, -size / 6, size, size / 3);

                // Vertical ribbon
                g.FillRectangle(ribbonBrush, -size / 6, -size / 2, size / 3, size);
            }

            // Draw bow on top
            float bowSize = size * 0.4f;

            using (SolidBrush bowBrush = new SolidBrush(ribbonColor))
            {
                // Draw bow loops
                // Left loop
                g.FillEllipse(bowBrush, -bowSize / 2, -size / 2 - bowSize / 2, bowSize / 2, bowSize / 2);

                // Right loop
                g.FillEllipse(bowBrush, 0, -size / 2 - bowSize / 2, bowSize / 2, bowSize / 2);

                // Center knot
                g.FillEllipse(bowBrush, -bowSize / 4, -size / 2 - bowSize / 4, bowSize / 2, bowSize / 2);
            }

            // Draw ribbon ends
            using (Pen ribbonPen = new Pen(ribbonColor, 3))
            {
                // Left ribbon end
                g.DrawLine(ribbonPen,
                    -bowSize / 4,
                    -size / 2,
                    -bowSize / 2,
                    -size / 2 + bowSize / 2);

                // Right ribbon end
                g.DrawLine(ribbonPen,
                    bowSize / 4,
                    -size / 2,
                    bowSize / 2,
                    -size / 2 + bowSize / 2);
            }

            // Draw outline
            using (Pen outlinePen = new Pen(darkColor, 1))
            {
                g.DrawRectangle(outlinePen, giftRect.X, giftRect.Y, giftRect.Width, giftRect.Height);
            }
        }

        /// <summary>
        /// Draws the snow trails behind the toboggan
        /// </summary>
        private void DrawSnowTrails(Graphics g, int cellSize)
        {
            if (previousPositions.Count < 2) return;

            // Create arrays for trail points
            Point[] leftTrailPoints = new Point[previousPositions.Count];
            Point[] rightTrailPoints = new Point[previousPositions.Count];

            // Convert queue to array for easier processing
            Point[] posArray = previousPositions.ToArray();

            // Create parallel rail tracks in the snow
            for (int i = 0; i < posArray.Length; i++)
            {
                // Calculate center of cell
                float x = posArray[i].X * cellSize + cellSize / 2f;
                float y = posArray[i].Y * cellSize + cellSize / 2f;

                // Calculate trail offset and fading
                float trailWidth = cellSize / 6f;
                float fadeScale = 1.0f - ((float)i / posArray.Length);

                // Find direction from next point to this point, or use current direction for end of trail
                Direction trailDir = direction;
                if (i < posArray.Length - 1)
                {
                    Point next = posArray[i + 1];
                    int dx = posArray[i].X - next.X;
                    int dy = posArray[i].Y - next.Y;

                    if (dx == 1 && dy == 0) trailDir = Direction.Left;
                    else if (dx == -1 && dy == 0) trailDir = Direction.Right;
                    else if (dx == 0 && dy == 1) trailDir = Direction.Up;
                    else if (dx == 0 && dy == -1) trailDir = Direction.Down;
                    else if (dx == 1 && dy == 1) trailDir = Direction.UpLeft;
                    else if (dx == -1 && dy == 1) trailDir = Direction.UpRight;
                    else if (dx == 1 && dy == -1) trailDir = Direction.DownLeft;
                    else if (dx == -1 && dy == -1) trailDir = Direction.DownRight;
                }

                // Determine parallel track offset based on direction
                float xOffset = 0, yOffset = 0;

                switch (trailDir)
                {
                    case Direction.Up:
                    case Direction.Down:
                        xOffset = trailWidth;
                        break;
                    case Direction.Left:
                    case Direction.Right:
                        yOffset = trailWidth;
                        break;
                    case Direction.UpLeft:
                    case Direction.DownRight:
                        xOffset = trailWidth * 0.7f;
                        yOffset = -trailWidth * 0.7f;
                        break;
                    case Direction.UpRight:
                    case Direction.DownLeft:
                        xOffset = trailWidth * 0.7f;
                        yOffset = trailWidth * 0.7f;
                        break;
                }

                // Create left and right trail points
                leftTrailPoints[i] = new Point(
                    (int)(x - xOffset),
                    (int)(y - yOffset)
                );

                rightTrailPoints[i] = new Point(
                    (int)(x + xOffset),
                    (int)(y + yOffset)
                );

                // Draw connecting dots/disturbed snow between tracks
                using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(
                    (int)(fadeScale * 90), 255, 255, 255)))
                {
                    if (i % 2 == 0) // Only draw every other point for performance
                    {
                        // Draw a few random disturbed snow particles between the tracks
                        for (int j = 0; j < 2; j++)
                        {
                            float xRand = random.Next(-8, 8) * fadeScale;
                            float yRand = random.Next(-8, 8) * fadeScale;
                            float dotSize = 1 + 2 * fadeScale;

                            g.FillEllipse(dotBrush,
                                x + xRand - dotSize / 2,
                                y + yRand - dotSize / 2,
                                dotSize, dotSize);
                        }
                    }
                }
            }

            // Draw the two parallel tracks
            for (int i = 0; i < posArray.Length - 1; i++)
            {
                // Calculate fading for trail
                int alpha = (int)(255 * (1.0f - ((float)i / posArray.Length)));
                using (Pen trailPen = new Pen(Color.FromArgb(alpha, 220, 220, 220), 2))
                {
                    // Draw left track segment
                    g.DrawLine(trailPen,
                        leftTrailPoints[i],
                        leftTrailPoints[i + 1]);

                    // Draw right track segment
                    g.DrawLine(trailPen,
                        rightTrailPoints[i],
                        rightTrailPoints[i + 1]);
                }
            }
        }

        /// <summary>
        /// Draws breath effect in cold air
        /// </summary>
        private void DrawBreathEffect(Graphics g, int cellSize)
        {
            if (breathScale <= 0) return;

            // Get head position for breath origin
            Point head = segments.First.Value;

            // Calculate center of the cell
            float x = head.X * cellSize + cellSize / 2f;
            float y = head.Y * cellSize + cellSize / 2f;

            // Calculate offset based on direction (where the face would be)
            float offsetX = 0, offsetY = 0;
            float dirMult = cellSize / 3f; // Multiplier for direction offset

            switch (direction)
            {
                case Direction.Right: offsetX = dirMult; break;
                case Direction.DownRight: offsetX = dirMult * 0.7f; offsetY = dirMult * 0.7f; break;
                case Direction.Down: offsetY = dirMult; break;
                case Direction.DownLeft: offsetX = -dirMult * 0.7f; offsetY = dirMult * 0.7f; break;
                case Direction.Left: offsetX = -dirMult; break;
                case Direction.UpLeft: offsetX = -dirMult * 0.7f; offsetY = -dirMult * 0.7f; break;
                case Direction.Up: offsetY = -dirMult; break;
                case Direction.UpRight: offsetX = dirMult * 0.7f; offsetY = -dirMult * 0.7f; break;
            }

            // Calculate breath size based on breath scale
            float breathSize = cellSize / 3f * breathScale;

            // Draw multiple breath particles for cloud effect
            using (SolidBrush breathBrush = new SolidBrush(Color.FromArgb(
                (int)(150 * breathScale), 255, 255, 255)))
            {
                for (int i = 0; i < 3; i++)
                {
                    float randX = random.Next(-5, 5);
                    float randY = random.Next(-5, 5);
                    float randSize = random.Next(80, 120) / 100f * breathSize;

                    g.FillEllipse(breathBrush,
                        x + offsetX + randX - randSize / 2,
                        y + offsetY + randY - randSize / 2,
                        randSize, randSize);
                }
            }
        }

        /// <summary>
        /// Helper to get a segment at a specific index
        /// </summary>
        private Point GetSegmentAt(int index)
        {
            int i = 0;
            foreach (Point p in segments)
            {
                if (i == index)
                    return p;
                i++;
            }
            return segments.Last.Value; // Default if index is out of range
        }

        /// <summary>
        /// Creates a rounded rectangle path
        /// </summary>
        private GraphicsPath CreateRoundedRectanglePath(float x, float y, float width, float height, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2;

            // Top left corner
            path.AddArc(x, y, diameter, diameter, 180, 90);

            // Top edge
            path.AddLine(x + radius, y, x + width - radius, y);

            // Top right corner
            path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);

            // Right edge
            path.AddLine(x + width, y + radius, x + width, y + height - radius);

            // Bottom right corner
            path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);

            // Bottom edge
            path.AddLine(x + width - radius, y + height, x + radius, y + height);

            // Bottom left corner
            path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);

            // Left edge
            path.AddLine(x, y + height - radius, x, y + radius);

            path.CloseFigure();
            return path;
        }
    }

    /// <summary>
    /// Extension methods for Graphics
    /// </summary>
    public static class GraphicsExtensions
    {
        /// <summary>
        /// Fills a rounded rectangle
        /// </summary>
        public static void FillRoundedRectangle(this Graphics g, float x, float y, float width, float height, float radius, Brush brush)
        {
            using (GraphicsPath path = CreateRoundedRectanglePath(x, y, width, height, radius))
            {
                g.FillPath(brush, path);
            }
        }

        /// <summary>
        /// Draws a rounded rectangle outline
        /// </summary>
        public static void DrawRoundedRectangle(this Graphics g, float x, float y, float width, float height, float radius, Pen pen)
        {
            using (GraphicsPath path = CreateRoundedRectanglePath(x, y, width, height, radius))
            {
                g.DrawPath(pen, path);
            }
        }

        /// <summary>
        /// Creates a rounded rectangle path
        /// </summary>
        private static GraphicsPath CreateRoundedRectanglePath(float x, float y, float width, float height, float radius)
        {
            GraphicsPath path = new GraphicsPath();

            // Top left corner
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);

            // Top edge and top right corner
            path.AddLine(x + radius, y, x + width - radius, y);
            path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);

            // Right edge and bottom right corner
            path.AddLine(x + width, y + radius, x + width, y + height - radius);
            path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);

            // Bottom edge and bottom left corner
            path.AddLine(x + width - radius, y + height, x + radius, y + height);
            path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);

            // Left edge
            path.AddLine(x, y + height - radius, x, y + radius);

            path.CloseFigure();
            return path;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace TobogganGame
{
    /// <summary>
    /// Main game form that handles drawing and user input
    /// </summary>
    public partial class GameForm : Form
    {
        #region Fields and Constants

        // Game objects
        private GameEngine gameEngine;
        private AIController aiController;

        // Game settings
        private const int CellSize = 25;
        private const int GridWidth = 30;
        private const int GridHeight = 26;
        private const int InitialGameSpeed = 100;

        // Game speed settings
        private const int MaxSpeed = 100;
        private const int MinGameInterval = 10;
        private const int MaxGameInterval = 250;

        // UI elements
        private System.Windows.Forms.Timer gameTimer;
        private Panel controlPanel;
        private CheckBox aiControlCheckbox;
        private CheckBox showRaysCheckbox;
        private CheckBox trainingModeCheckbox;
        private Label scoreLabel;
        private Label trainingStatsLabel;
        private Button resetButton;
        private Button resetAIButton;
        private TrackBar speedTrackBar;
        private Label speedLabel;
        private Panel learningGraphPanel;

        // Control panel width
        private const int ControlPanelWidth = 250;

        // Game state
        private bool aiControlled = true;
        private bool showRays = true;
        private bool trainingMode = false;

        // Visual settings
        private Color backgroundColor = Color.FromArgb(240, 248, 255);
        private bool showPerformanceGraph = true;

        // Timer for graph updates
        private System.Windows.Forms.Timer graphUpdateTimer;
        private const int GraphUpdateInterval = 1000;

        // Track currently pressed keys for diagonal movement
        private bool upKeyPressed = false;
        private bool downKeyPressed = false;
        private bool leftKeyPressed = false;
        private bool rightKeyPressed = false;

        #endregion

        #region Initialization and Setup

        /// <summary>
        /// Initializes a new instance of the GameForm class
        /// </summary>
        public GameForm()
        {
            InitializeComponent();
            SetupForm();
            SetupControls();
            SetupGraphUpdateTimer();
            InitializeGame();
        }

        /// <summary>
        /// Sets up the graph update timer
        /// </summary>
        private void SetupGraphUpdateTimer()
        {
            graphUpdateTimer = new System.Windows.Forms.Timer
            {
                Interval = GraphUpdateInterval
            };

            graphUpdateTimer.Tick += (s, e) =>
            {
                if (learningGraphPanel != null && learningGraphPanel.Visible)
                {
                    learningGraphPanel.Invalidate();
                }
            };

            graphUpdateTimer.Start();
        }

        /// <summary>
        /// Configures the main form properties and event handlers
        /// </summary>
        private void SetupForm()
        {
            this.Text = "Toboggan Game with Neural Network";
            this.ClientSize = new Size(GridWidth * CellSize + ControlPanelWidth, GridHeight * CellSize);
            this.DoubleBuffered = true;
            this.BackColor = backgroundColor;
            this.KeyPreview = true;

            this.Paint += GameForm_Paint;
            this.KeyDown += GameForm_KeyDown;
            this.KeyUp += GameForm_KeyUp;
            this.FormClosing += GameForm_FormClosing;
        }

        /// <summary>
        /// Creates and configures all UI controls
        /// </summary>
        private void SetupControls()
        {
            // Create control panel on the right side
            controlPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = ControlPanelWidth,
                BackColor = Color.FromArgb(230, 230, 230)
            };

            CreateCheckboxes();
            CreateLabels();
            CreateButtons();
            CreateSpeedControls();
            CreateLearningGraphPanel();

            // Add all controls to the panel
            AddControlsToPanel();

            // Add control panel to form
            this.Controls.Add(controlPanel);
        }

        private void CreateCheckboxes()
        {
            // Create AI control checkbox
            aiControlCheckbox = new CheckBox
            {
                Text = "AI Control",
                AutoSize = true,
                Location = new Point(15, 20),
                Checked = aiControlled,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            aiControlCheckbox.CheckedChanged += (s, e) =>
            {
                aiControlled = aiControlCheckbox.Checked;
            };

            // Create ray visualization checkbox
            showRaysCheckbox = new CheckBox
            {
                Text = "Show Sensors",
                AutoSize = true,
                Location = new Point(15, 50),
                Checked = showRays,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            showRaysCheckbox.CheckedChanged += (s, e) =>
            {
                showRays = showRaysCheckbox.Checked;
                this.Invalidate();
            };

            // Create training mode checkbox
            trainingModeCheckbox = new CheckBox
            {
                Text = "Training Mode",
                AutoSize = true,
                Location = new Point(15, 80),
                Checked = trainingMode,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            trainingModeCheckbox.CheckedChanged += (s, e) =>
            {
                trainingMode = trainingModeCheckbox.Checked;
                UpdateTrainingMode();
            };
        }

        private void UpdateTrainingMode()
        {
            if (aiController != null)
            {
                aiController.SetTrainingMode(trainingMode);
            }

            // Auto-enable AI control in training mode
            if (trainingMode && !aiControlled)
            {
                aiControlled = true;
                aiControlCheckbox.Checked = true;
            }

            // Update speed for new mode
            UpdateGameSpeed();
        }

        private void CreateLabels()
        {
            // Create score label
            scoreLabel = new Label
            {
                AutoSize = true,
                Location = new Point(15, 120),
                Text = "Score: 0",
                Font = new Font("Arial", 14, FontStyle.Bold)
            };

            // Create training stats label
            trainingStatsLabel = new Label
            {
                AutoSize = true,
                Location = new Point(15, 150),
                Text = "Training: N/A",
                Font = new Font("Arial", 9)
            };

            // Create additional stats label
            Label additionalStatsLabel = new Label
            {
                AutoSize = true,
                Location = new Point(15, 170),
                Text = "Network: N/A",
                Font = new Font("Arial", 9),
                Name = "additionalStatsLabel"
            };

            // Create training progress label
            Label trainingProgressLabel = new Label
            {
                AutoSize = true,
                Location = new Point(15, 190),
                Text = "Games: 0 | Max Score: 0",
                Font = new Font("Arial", 9),
                Name = "trainingProgressLabel"
            };

            // Add labels to the control panel
            controlPanel.Controls.Add(scoreLabel);
            controlPanel.Controls.Add(trainingStatsLabel);
            controlPanel.Controls.Add(additionalStatsLabel);
            controlPanel.Controls.Add(trainingProgressLabel);
        }

        private void CreateButtons()
        {
            // Create reset button
            resetButton = new Button
            {
                Text = "Reset Game",
                Location = new Point(15, 220),
                Size = new Size(180, 30),
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            resetButton.Click += (s, e) => InitializeGame();

            // Create reset AI button
            resetAIButton = new Button
            {
                Text = "Reset AI",
                Location = new Point(15, 260),
                Size = new Size(180, 30),
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            resetAIButton.Click += (s, e) => HandleResetAI();
        }

        private void HandleResetAI()
        {
            DialogResult result = MessageBox.Show(
                "This will erase all learned knowledge and start with a fresh neural network. Continue?",
                "Reset AI Learning",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            bool shouldResetAI = result == DialogResult.Yes && aiController != null;

            if (shouldResetAI)
            {
                aiController.ResetNetwork();
                UpdateTrainingStats();
                this.Invalidate();
                MessageBox.Show("AI neural network has been reset to random weights.", "Reset Complete");
            }
        }

        private void CreateSpeedControls()
        {
            // Create speed control
            speedTrackBar = new TrackBar
            {
                Minimum = 1,
                Maximum = MaxSpeed,
                Value = 10,
                Location = new Point(15, 300),
                Size = new Size(180, 40),
                TickFrequency = 10
            };

            speedTrackBar.ValueChanged += (s, e) => UpdateGameSpeed();

            speedLabel = new Label
            {
                AutoSize = true,
                Location = new Point(15, 345),
                Text = $"Speed: {speedTrackBar.Value}",
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
        }

        private void CreateLearningGraphPanel()
        {
            // Create learning graph panel
            learningGraphPanel = new Panel
            {
                Location = new Point(15, 370),
                Size = new Size(190, 200),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            learningGraphPanel.Paint += LearningGraphPanel_Paint;
        }

        private void AddControlsToPanel()
        {
            controlPanel.Controls.Add(aiControlCheckbox);
            controlPanel.Controls.Add(showRaysCheckbox);
            controlPanel.Controls.Add(trainingModeCheckbox);
            controlPanel.Controls.Add(resetButton);
            controlPanel.Controls.Add(resetAIButton);
            controlPanel.Controls.Add(speedTrackBar);
            controlPanel.Controls.Add(speedLabel);
            controlPanel.Controls.Add(learningGraphPanel);
        }

        #endregion

        #region Game Logic

        /// <summary>
        /// Updates the game speed based on the track bar value
        /// </summary>
        private void UpdateGameSpeed()
        {
            if (gameTimer == null) return;

            // Calculate interval (higher speed value = lower interval)
            double speedRatio = speedTrackBar.Value / (double)MaxSpeed;
            int interval;

            // Determine interval based on mode
            interval = trainingMode
                ? CalculateTrainingModeInterval(speedRatio)
                : CalculateNormalModeInterval(speedRatio);

            // Ensure we stay within bounds
            interval = Math.Max(MinGameInterval, Math.Min(MaxGameInterval, interval));

            // Set the timer interval
            gameTimer.Interval = interval;

            // Update label
            speedLabel.Text = $"Speed: {speedTrackBar.Value}";
        }

        private int CalculateTrainingModeInterval(double speedRatio)
        {
            // In training mode, allow faster speeds (using squared ratio for more dramatic effect)
            return (int)(MaxGameInterval - (speedRatio * speedRatio * (MaxGameInterval - MinGameInterval)));
        }

        private int CalculateNormalModeInterval(double speedRatio)
        {
            // Normal mode - linear speed scaling
            return (int)(MaxGameInterval - (speedRatio * (MaxGameInterval - MinGameInterval)));
        }

        /// <summary>
        /// Initializes or resets the game state
        /// </summary>
        private void InitializeGame()
        {
            // Create the game engine
            gameEngine = new GameEngine(GridWidth, GridHeight);

            // Initialize or update AI controller
            InitializeAIController();

            // Set up game timer if needed
            SetupGameTimer();

            // Update score display
            scoreLabel.Text = $"Score: 0";

            // Update training stats
            UpdateTrainingStats();

            // Refresh the display
            this.Invalidate();
        }

        private void InitializeAIController()
        {
            bool isNewController = aiController == null;

            if (isNewController)
            {
                aiController = new AIController(gameEngine);
                aiController.SetTrainingMode(trainingMode);
            }
            else
            {
                // Reset the game engine reference in the existing AI controller
                aiController.SetGameEngine(gameEngine);
            }
        }

        private void SetupGameTimer()
        {
            bool isNewTimer = gameTimer == null;

            if (isNewTimer)
            {
                gameTimer = new System.Windows.Forms.Timer();
                gameTimer.Tick += GameTimer_Tick;
            }

            // Apply speed settings
            UpdateGameSpeed();

            // Start the timer
            gameTimer.Start();
        }

        /// <summary>
        /// Handles the game timer tick event for game updates
        /// </summary>
        private void GameTimer_Tick(object sender, EventArgs e)
        {
            bool isGameOver = gameEngine.IsGameOver;

            if (isGameOver)
            {
                // If game is over but AI is controlling, update the rays one last time
                if (aiControlled && aiController != null)
                {
                    // Force AI to update rays for final position without actually moving
                    aiController.UpdateRaysForCurrentPosition();
                }

                // Force immediate redraw to show final state before handling game over
                this.Invalidate();
                this.Update();

                // Small delay to ensure the screen updates before showing message
                Thread.Sleep(200);

                HandleGameOver();
                return;
            }

            UpdateGameState();

            // Redraw to update the screen
            this.Invalidate();
        }

        private void HandleGameOver()
        {
            // Stop the timer
            gameTimer.Stop();

            if (trainingMode)
            {
                // In training mode, automatically restart the game
                InitializeGame();
                return;
            }

            // Save neural network weights when game ends (in non-training mode)
            if (aiController != null)
            {
                aiController.SaveAll();
            }

            MessageBox.Show($"Game Over! Score: {gameEngine.Score}", "Game Over");
        }

        private void UpdateGameState()
        {
            // If AI is controlling, get its next move
            if (aiControlled)
            {
                UpdateAIControl();
            }

            // Update game state
            bool collectedFlag = gameEngine.Update();

            // If AI is controlling, update it with the result
            if (aiControlled)
            {
                aiController.UpdateAfterMove(collectedFlag, gameEngine.IsGameOver);

                // Update training stats display
                UpdateTrainingStats();
            }

            // Update score display
            scoreLabel.Text = $"Score: {gameEngine.Score}";

            // Redraw the game
            this.Invalidate();
        }

        private void UpdateAIControl()
        {
            Direction aiMove = aiController.GetNextMove();
            gameEngine.SetDirection(aiMove);
        }

        /// <summary>
        /// Updates the training statistics display
        /// </summary>
        private void UpdateTrainingStats()
        {
            if (aiController == null) return;

            var stats = aiController.Stats;

            // Format exploration rate as percentage
            double explorationRate = stats.ExplorationRate * 100;

            // Update main label
            trainingStatsLabel.Text = $"Explore: {explorationRate:F1}% | Buffer: {GetReplayBufferSize()}";

            UpdateLossDisplay(stats);
            UpdateTrainingProgressDisplay(stats);
        }

        private void UpdateLossDisplay(dynamic stats)
        {
            bool hasAdditionalLabel = controlPanel.Controls.ContainsKey("additionalStatsLabel");
            if (!hasAdditionalLabel) return;

            Label additionalLabel = (Label)controlPanel.Controls["additionalStatsLabel"];
            double avgLoss = stats.GetRecentAverageLoss();

            // Get color and format based on loss value
            (Color textColor, string lossText) = GetLossDisplayFormat(avgLoss);

            additionalLabel.ForeColor = textColor;
            additionalLabel.Text = $"{lossText} | Avg: {stats.GetRecentAverageScore():F1}";
        }

        private (Color, string) GetLossDisplayFormat(double avgLoss)
        {
            if (avgLoss < 0.01)
                return (Color.Green, $"Loss: {avgLoss:F5}");
            if (avgLoss < 0.1)
                return (Color.DarkGreen, $"Loss: {avgLoss:F4}");
            if (avgLoss < 1.0)
                return (Color.Black, $"Loss: {avgLoss:F3}");
            if (avgLoss < 5.0)
                return (Color.DarkOrange, $"Loss: {avgLoss:F2}");

            return (Color.Red, $"Loss: {avgLoss:F1}");
        }

        private void UpdateTrainingProgressDisplay(dynamic stats)
        {
            bool hasProgressLabel = controlPanel.Controls.ContainsKey("trainingProgressLabel");
            if (!hasProgressLabel) return;

            Label progressLabel = (Label)controlPanel.Controls["trainingProgressLabel"];
            progressLabel.Text = $"Games: {stats.TotalGamesPlayed} | Max: {stats.MaxScore}";
        }

        /// <summary>
        /// Gets the current size of the replay buffer
        /// </summary>
        /// <returns>The number of experiences in the replay buffer</returns>
        private int GetReplayBufferSize()
        {
            return aiController != null ? aiController.GetReplayBufferSize() : 0;
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Paints the learning progress graph
        /// </summary>
        private void LearningGraphPanel_Paint(object sender, PaintEventArgs e)
        {
            bool hasNoData = aiController == null || aiController.Stats == null;
            if (hasNoData) return;

            var stats = aiController.Stats;
            Graphics g = e.Graphics;

            // Clear the panel
            g.Clear(Color.White);

            // Draw border
            g.DrawRectangle(Pens.Gray, 0, 0, learningGraphPanel.Width - 1, learningGraphPanel.Height - 1);

            // Draw title with larger font
            g.DrawString("Learning Progress", new Font("Arial", 10, FontStyle.Bold),
                Brushes.Black, 10, 10);

            DrawGraphLegend(g);

            // Check if we have data to draw
            bool notEnoughData = stats.ScoreHistory.Count < 2;
            if (notEnoughData)
            {
                g.DrawString("Not enough data yet", new Font("Arial", 9),
                    Brushes.Gray, 50, learningGraphPanel.Height / 2);
                return;
            }

            // Get recent scores for the graph (limited to last 50)
            List<int> scores = stats.ScoreHistory.Skip(Math.Max(0, stats.ScoreHistory.Count - 50)).ToList();

            // Find max score for scaling
            int maxScore = scores.Count > 0 ? Math.Max(scores.Max(), 1) : 1;

            // Compute graph area
            Rectangle graphArea = new Rectangle(
                50, 50,
                learningGraphPanel.Width - 70,
                learningGraphPanel.Height - 80);

            DrawGraphAxes(g, graphArea);
            DrawGraphGridAndLabels(g, graphArea, maxScore);
            DrawMovingAverage(g, graphArea, scores, maxScore);
            DrawScorePoints(g, graphArea, scores, maxScore);
        }

        private void DrawGraphLegend(Graphics g)
        {
            int legendY = 30;

            // Blue dot for score
            g.FillEllipse(Brushes.Blue, 20, legendY, 8, 8);
            g.DrawString("Score", new Font("Arial", 8), Brushes.Black, 35, legendY);

            // Red line for average
            g.DrawLine(new Pen(Color.Red, 2), 90, legendY + 4, 110, legendY + 4);
            g.DrawString("Average", new Font("Arial", 8), Brushes.Black, 120, legendY);
        }

        private void DrawGraphAxes(Graphics g, Rectangle graphArea)
        {
            // Draw axes
            g.DrawLine(Pens.Gray, graphArea.Left, graphArea.Bottom, graphArea.Right, graphArea.Bottom); // X axis
            g.DrawLine(Pens.Gray, graphArea.Left, graphArea.Top, graphArea.Left, graphArea.Bottom);     // Y axis

            // Draw labels - X axis (Games)
            g.DrawString("Games", new Font("Arial", 9, FontStyle.Bold),
                Brushes.Black, graphArea.Left + graphArea.Width / 2 - 15, graphArea.Bottom + 10);

            // Draw labels - Y axis (Score)
            GraphicsState state = g.Save();
            g.TranslateTransform(15, graphArea.Top + graphArea.Height / 2);
            g.RotateTransform(-90);
            g.DrawString("Score", new Font("Arial", 9, FontStyle.Bold), Brushes.Black, -20, 0);
            g.Restore(state);
        }

        private void DrawGraphGridAndLabels(Graphics g, Rectangle graphArea, int maxScore)
        {
            using (Pen gridPen = new Pen(Color.LightGray, 1) { DashStyle = DashStyle.Dot })
            {
                // Horizontal grid lines
                for (int y = 1; y <= 4; y++)
                {
                    int yPos = graphArea.Bottom - (y * graphArea.Height / 5);
                    g.DrawLine(gridPen, graphArea.Left, yPos, graphArea.Right, yPos);

                    // Draw y-axis score labels
                    int value = y * maxScore / 5;
                    g.DrawString(value.ToString(), new Font("Arial", 8),
                        Brushes.Black, graphArea.Left - 25, yPos - 7);
                }
            }
        }

        private void DrawMovingAverage(Graphics g, Rectangle graphArea, List<int> scores, int maxScore)
        {
            if (scores.Count < 5) return;

            // Calculate moving average
            List<double> movingAvg = CalculateMovingAverage(scores);

            // Draw moving average line
            if (movingAvg.Count > 1)
            {
                DrawAverageLine(g, graphArea, movingAvg, maxScore);
            }
        }

        private void DrawAverageLine(Graphics g, Rectangle graphArea, List<double> movingAvg, int maxScore)
        {
            Point[] points = new Point[movingAvg.Count];

            for (int i = 0; i < movingAvg.Count; i++)
            {
                int x = graphArea.Left + (i * graphArea.Width / (movingAvg.Count - 1));
                int y = graphArea.Bottom - (int)(movingAvg[i] * graphArea.Height / maxScore);
                points[i] = new Point(x, y);
            }

            using (Pen linePen = new Pen(Color.Red, 2))
            {
                g.DrawLines(linePen, points);
            }
        }

        private List<double> CalculateMovingAverage(List<int> scores)
        {
            List<double> movingAvg = new List<double>();

            for (int i = 0; i < scores.Count; i++)
            {
                int start = Math.Max(0, i - 4);
                int count = i - start + 1;
                double avg = 0;

                for (int j = start; j <= i; j++)
                {
                    avg += scores[j];
                }

                avg /= count;
                movingAvg.Add(avg);
            }

            return movingAvg;
        }

        private void DrawScorePoints(Graphics g, Rectangle graphArea, List<int> scores, int maxScore)
        {
            for (int i = 0; i < scores.Count; i++)
            {
                int x = graphArea.Left + (i * graphArea.Width / (scores.Count - 1));
                int y = graphArea.Bottom - (int)(scores[i] * graphArea.Height / maxScore);

                g.FillEllipse(Brushes.Blue, x - 3, y - 3, 6, 6);
            }
        }

        /// <summary>
        /// Main paint event handler for the game form
        /// </summary>
        private void GameForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // Draw snowy background
            g.Clear(Color.FromArgb(230, 240, 255));

            // Define game area rectangle
            Rectangle gameArea = new Rectangle(0, 0, GridWidth * CellSize, GridHeight * CellSize);

            DrawGameBackground(g, gameArea);
            DrawGameElements(g);

            // Only draw game over message in normal mode when game is over
            bool shouldDrawGameOver = gameEngine.IsGameOver && !trainingMode;
            if (shouldDrawGameOver)
            {
                DrawGameOverMessage(g, gameArea);
            }

            // Show training mode label when appropriate
            bool shouldShowTrainingLabel = aiControlled && trainingMode;
            if (shouldShowTrainingLabel)
            {
                DrawTrainingModeLabel(g);
            }
        }

        private void DrawGameBackground(Graphics g, Rectangle gameArea)
        {
            // Draw game area border
            g.DrawRectangle(new Pen(Color.Gray, 2), gameArea);

            // Add snow effects
            DrawSnowEffects(g, gameArea);

            // Draw grid lines (subtle)
        }

        private void DrawSnowEffects(Graphics g, Rectangle gameArea)
        {
            using (Brush snowBrush = new SolidBrush(Color.White))
            {
                Random r = new Random(42); // Fixed seed for consistent snow pattern

                for (int i = 0; i < 300; i++)
                {
                    int x = r.Next(gameArea.Width);
                    int y = r.Next(gameArea.Height);
                    int size = r.Next(2, 5);
                    g.FillEllipse(snowBrush, x, y, size, size);
                }
            }
        }

        private void DrawGameElements(Graphics g)
        {
            DrawObstacles(g);

            // Draw ray visualization if enabled
            bool shouldDrawRays = showRays && aiControlled && aiController != null;
            if (shouldDrawRays)
            {
                DrawRays(g);
            }

            DrawFlag(g);
            DrawToboggan(g);
        }

        private void DrawObstacles(Graphics g)
        {
            bool hasObstacles = gameEngine != null && gameEngine.Obstacles != null;
            if (!hasObstacles) return;

            foreach (Obstacle obstacle in gameEngine.Obstacles)
            {
                obstacle.Draw(g, CellSize);
            }
        }

        private void DrawFlag(Graphics g)
        {
            bool hasFlag = gameEngine != null && gameEngine.Flag != null;
            if (hasFlag)
            {
                gameEngine.Flag.Draw(g, CellSize);
            }
        }

        private void DrawToboggan(Graphics g)
        {
            bool hasToboggan = gameEngine != null && gameEngine.Toboggan != null;
            if (hasToboggan)
            {
                gameEngine.Toboggan.Draw(g, CellSize);
            }
        }

        private void DrawGameOverMessage(Graphics g, Rectangle gameArea)
        {
            string message = "Game Over! Press R to restart.";
            using (Font font = new Font("Arial", 20, FontStyle.Bold))
            {
                SizeF textSize = g.MeasureString(message, font);

                // Draw semi-transparent background
                using (SolidBrush background = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                {
                    g.FillRectangle(
                        background,
                        (gameArea.Width - textSize.Width) / 2 - 10,
                        (gameArea.Height - textSize.Height) / 2 - 10,
                        textSize.Width + 20,
                        textSize.Height + 10
                    );
                }

                // Draw text
                using (Brush brush = new SolidBrush(Color.FromArgb(255, 255, 0, 0)))
                {
                    g.DrawString(
                        message,
                        font,
                        brush,
                        (gameArea.Width - textSize.Width) / 2,
                        (gameArea.Height - textSize.Height) / 2 - 10
                    );
                }
            }
        }

        private void DrawTrainingModeLabel(Graphics g)
        {
            string modeText = "TRAINING MODE";

            using (Font font = new Font("Arial", 16, FontStyle.Bold))
            {
                g.DrawString(
                    modeText,
                    font,
                    Brushes.Red,
                    15,
                    15
                );
            }
        }

        /// <summary>
        /// Draws ray visualizations for AI sensors
        /// </summary>
        private void DrawRays(Graphics g)
        {
            if (aiController == null) return;

            RayInfo[] rays = aiController.GetRayInfo();
            if (rays == null) return;

            Point flagPos = gameEngine.Flag.Position;

            foreach (RayInfo ray in rays)
            {
                RayRenderInfo renderInfo = CalculateRayRenderInfo(ray, flagPos);
                DrawRay(g, renderInfo);
            }
        }

        private struct RayRenderInfo
        {
            public Point Start;
            public Point End;
            public Color Color;
            public float Thickness;
            public bool DrawDot;
        }

        private RayRenderInfo CalculateRayRenderInfo(RayInfo ray, Point flagPos)
        {
            // Initialize with original ray info
            RayRenderInfo renderInfo = new RayRenderInfo
            {
                Start = ray.Start,
                End = ray.End,
                DrawDot = ray.HitType != RayHitType.None,
                Thickness = ray.HitType == RayHitType.None ? 1.0f : 2.5f
            };

            // Check if ray intersects with flag
            bool rayPassesThroughFlag = CheckRayFlagIntersection(ray, flagPos, ref renderInfo);

            // Set color based on what the ray hits
            renderInfo.Color = GetRayColor(ray.HitType, rayPassesThroughFlag);

            return renderInfo;
        }

        private bool CheckRayFlagIntersection(RayInfo ray, Point flagPos, ref RayRenderInfo renderInfo)
        {
            // Direct flag hit
            if (ray.HitType == RayHitType.Flag)
            {
                return true;
            }

            // Skip rays with no direction
            Point direction = new Point(ray.End.X - ray.Start.X, ray.End.Y - ray.Start.Y);
            bool hasNoDirection = direction.X == 0 && direction.Y == 0;

            if (hasNoDirection)
            {
                return false;
            }

            // Calculate unit vector for ray direction
            double length = Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
            double dirX = direction.X / length;
            double dirY = direction.Y / length;

            return ScanRayForFlag(ray, flagPos, dirX, dirY, ref renderInfo);
        }

        private bool ScanRayForFlag(RayInfo ray, Point flagPos, double dirX, double dirY, ref RayRenderInfo renderInfo)
        {
            // Calculate maximum scan distance
            double maxDistance = Math.Max(CellSize * GridWidth, CellSize * GridHeight);
            double stepSize = 0.5; // Precision of scanning

            // Scan along the ray for flag intersection
            for (double step = 0; step < maxDistance; step += stepSize)
            {
                // Calculate current scan position
                int checkX = (int)Math.Round(ray.Start.X + dirX * step);
                int checkY = (int)Math.Round(ray.Start.Y + dirY * step);

                // If we reached the end point, no flag intersection was found
                bool reachedEndPoint = checkX == ray.End.X && checkY == ray.End.Y;
                if (reachedEndPoint)
                {
                    return false;
                }

                // Check if this point is on the flag
                bool hitFlag = checkX == flagPos.X && checkY == flagPos.Y;
                if (hitFlag)
                {
                    // Update render info to stop at flag
                    renderInfo.End = new Point(flagPos.X, flagPos.Y);
                    renderInfo.DrawDot = true;
                    return true;
                }
            }

            return false;
        }

        private Color GetRayColor(RayHitType hitType, bool rayPassesThroughFlag)
        {
            // Flag hits get priority coloring
            if (rayPassesThroughFlag)
            {
                return Color.Green;
            }

            // Otherwise use hit type for color
            switch (hitType)
            {
                case RayHitType.Wall:
                    return Color.Red;
                case RayHitType.Body:
                    return Color.Blue;
                case RayHitType.Flag:
                    return Color.Green;
                case RayHitType.Obstacle:
                    return Color.Purple;
                default:
                    return Color.Gray;
            }
        }

        private void DrawRay(Graphics g, RayRenderInfo renderInfo)
        {
            // Convert grid positions to screen coordinates
            Point startPx = GridToScreen(renderInfo.Start);
            Point endPx = GridToScreen(renderInfo.End);

            // Draw the ray line with semi-transparency
            using (Pen rayPen = new Pen(Color.FromArgb(130, renderInfo.Color), renderInfo.Thickness))
            {
                g.DrawLine(rayPen, startPx.X, startPx.Y, endPx.X, endPx.Y);
            }

            // Draw hit point if needed
            if (renderInfo.DrawDot)
            {
                DrawHitPoint(g, endPx, renderInfo.Color);
            }
        }

        private Point GridToScreen(Point gridPoint)
        {
            return new Point(
                gridPoint.X * CellSize + CellSize / 2,
                gridPoint.Y * CellSize + CellSize / 2
            );
        }

        private void DrawHitPoint(Graphics g, Point position, Color color)
        {
            int dotSize = 6;
            Rectangle dotRect = new Rectangle(
                position.X - dotSize / 2,
                position.Y - dotSize / 2,
                dotSize,
                dotSize
            );

            using (Brush dotBrush = new SolidBrush(color))
            {
                g.FillEllipse(dotBrush, dotRect);
            }
        }

        #endregion

        #region Input Handling

        /// <summary>
        /// Handles key down events for game control
        /// </summary>
        private void GameForm_KeyDown(object sender, KeyEventArgs e)
        {
            bool isGameOverInNormalMode = gameEngine.IsGameOver && !trainingMode;

            if (isGameOverInNormalMode)
            {
                HandleGameOverKeyDown(e);
                return;
            }

            // Handle special control keys that work in all modes
            bool specialKeyHandled = HandleSpecialKeys(e);
            if (specialKeyHandled)
            {
                return;
            }

            // Either handle AI control keys or player movement
            if (aiControlled)
            {
                HandleAIControlKeys(e);
            }
            else
            {
                HandlePlayerMovementKeyDown(e);
            }
        }

        private void HandleGameOverKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.R:
                case Keys.Enter:
                    InitializeGame();
                    break;
            }
        }

        private bool HandleSpecialKeys(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F2:
                    ToggleTrainingMode();
                    return true;

                case Keys.F5:
                    InitializeGame();
                    return true;

                default:
                    return false;
            }
        }

        private void ToggleTrainingMode()
        {
            trainingMode = !trainingMode;
            trainingModeCheckbox.Checked = trainingMode;

            if (aiController != null)
            {
                aiController.SetTrainingMode(trainingMode);
            }

            // Update game speed
            UpdateGameSpeed();
        }

        private void HandleAIControlKeys(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.A:
                    ToggleAIControl();
                    break;

                case Keys.V:
                    ToggleRayVisualization();
                    break;

                case Keys.Space:
                    ToggleGameTimer();
                    break;

                case Keys.Add:
                case Keys.Oemplus:
                case Keys.Up:
                    IncreaseSpeed();
                    break;

                case Keys.Subtract:
                case Keys.OemMinus:
                case Keys.Down:
                    DecreaseSpeed();
                    break;
            }
        }

        private void ToggleAIControl()
        {
            aiControlled = !aiControlled;
            aiControlCheckbox.Checked = aiControlled;
        }

        private void ToggleRayVisualization()
        {
            showRays = !showRays;
            showRaysCheckbox.Checked = showRays;
            Invalidate();
        }

        private void ToggleGameTimer()
        {
            gameTimer.Enabled = !gameTimer.Enabled;
        }

        private void IncreaseSpeed()
        {
            if (speedTrackBar.Value < speedTrackBar.Maximum)
            {
                speedTrackBar.Value++;
            }
        }

        private void DecreaseSpeed()
        {
            if (speedTrackBar.Value > speedTrackBar.Minimum)
            {
                speedTrackBar.Value--;
            }
        }

        private void HandlePlayerMovementKeyDown(KeyEventArgs e)
        {
            // Update key state based on pressed key
            switch (e.KeyCode)
            {
                case Keys.Up:
                case Keys.W:
                    upKeyPressed = true;
                    break;

                case Keys.Down:
                case Keys.S:
                    downKeyPressed = true;
                    break;

                case Keys.Left:
                case Keys.A:
                    leftKeyPressed = true;
                    break;

                case Keys.Right:
                case Keys.D:
                    rightKeyPressed = true;
                    break;

                case Keys.Space:
                    ToggleGameTimer();
                    break;
            }

            // Determine direction based on key combinations
            UpdateDirection();
        }

        /// <summary>
        /// Handles key up events to track released keys
        /// </summary>
        private void GameForm_KeyUp(object sender, KeyEventArgs e)
        {
            // Update key state based on released key
            switch (e.KeyCode)
            {
                case Keys.Up:
                case Keys.W:
                    upKeyPressed = false;
                    break;

                case Keys.Down:
                case Keys.S:
                    downKeyPressed = false;
                    break;

                case Keys.Left:
                case Keys.A:
                    leftKeyPressed = false;
                    break;

                case Keys.Right:
                case Keys.D:
                    rightKeyPressed = false;
                    break;
            }

            // Determine direction based on key combinations
            UpdateDirection();
        }

        /// <summary>
        /// Determines movement direction based on currently pressed keys
        /// </summary>
        private void UpdateDirection()
        {
            if (aiControlled)
            {
                return;
            }

            Direction newDirection = DetermineDirectionFromKeyState();
            gameEngine.SetDirection(newDirection);
        }

        private Direction DetermineDirectionFromKeyState()
        {
            int keyState = (upKeyPressed ? 1 : 0) |
                           (rightKeyPressed ? 2 : 0) |
                           (downKeyPressed ? 4 : 0) |
                           (leftKeyPressed ? 8 : 0);

            switch (keyState)
            {
                case 1:     // Up only
                    return Direction.Up;
                case 2:     // Right only
                    return Direction.Right;
                case 4:     // Down only
                    return Direction.Down;
                case 8:     // Left only
                    return Direction.Left;
                case 3:     // Up + Right
                    return Direction.UpRight;
                case 5:     // Up + Down (invalid, but handle anyway)
                    return upKeyPressed ? Direction.Up : Direction.Down;
                case 9:     // Up + Left
                    return Direction.UpLeft;
                case 6:     // Right + Down
                    return Direction.DownRight;
                case 10:    // Right + Left (invalid, but handle anyway)
                    return rightKeyPressed ? Direction.Right : Direction.Left;
                case 12:    // Down + Left
                    return Direction.DownLeft;
                default:    // No direction or other combinations
                            // במקום Direction.None, שמור על הכיוון הנוכחי
                    return gameEngine.Toboggan.Direction;
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Handles form closing event to clean up resources
        /// </summary>
        private void GameForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopTimers();
            SaveAIState();
        }

        private void StopTimers()
        {
            if (gameTimer != null && gameTimer.Enabled)
            {
                gameTimer.Stop();
            }

            if (graphUpdateTimer != null && graphUpdateTimer.Enabled)
            {
                graphUpdateTimer.Stop();
            }
        }

        private void SaveAIState()
        {
            // Save neural network weights and stats when closing the application
            if (aiController != null)
            {
                aiController.SaveAll();
            }
        }

        #endregion
    }
}
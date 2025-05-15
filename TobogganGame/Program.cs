using System;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace TobogganGame
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // Create application folder if it doesn't exist
                string appDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TobogganGame");

                if (!Directory.Exists(appDataFolder))
                {
                    Directory.CreateDirectory(appDataFolder);
                }

                // Start the game
                Application.Run(new GameForm());
            }
            catch (Exception ex)
            {
                // Log the error
                string errorLog = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TobogganGame",
                    "error.log");

                try
                {
                    // Write the error to a log file
                    using (StreamWriter writer = new StreamWriter(errorLog, true))
                    {
                        writer.WriteLine($"[{DateTime.Now}] Error: {ex.Message}");
                        writer.WriteLine($"Stack Trace: {ex.StackTrace}");
                        writer.WriteLine(new string('-', 50));
                    }
                }
                catch
                {
                    // If we can't write to the log file, show the error directly
                }

                // Show error message to the user
                MessageBox.Show(
                    $"An unexpected error occurred:\n\n{ex.Message}\n\nThe application will now close.",
                    "Toboggan Game Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
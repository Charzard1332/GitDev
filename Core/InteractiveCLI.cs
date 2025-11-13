using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace GitDev.Core
{
    /// <summary>
    /// Provides an interactive command-line interface with improved user experience.
    /// </summary>
    public class InteractiveCLI
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly List<string> commandHistory;
        private int maxHistorySize = 50;

        public InteractiveCLI()
        {
            commandHistory = new List<string>();
        }

        /// <summary>
        /// Display a formatted welcome message.
        /// </summary>
        public void DisplayWelcome(string username)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    GitDev - Advanced                       ║");
            Console.WriteLine("║              Git & GitHub Management Tool                  ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Welcome, @{username}!");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Type 'help' for available commands or 'exit' to quit.");
            Console.WriteLine("Type 'history' to view recent commands.");
            Console.WriteLine();
        }

        /// <summary>
        /// Display the main help menu.
        /// </summary>
        public void DisplayHelp()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("═══════════════════ AVAILABLE COMMANDS ═══════════════════");
            Console.ResetColor();
            Console.WriteLine();
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Repository Operations:");
            Console.ResetColor();
            Console.WriteLine("  init                  - Initialize a new Git repository");
            Console.WriteLine("  clone <url> <path>    - Clone a repository");
            Console.WriteLine("  create-repo <name>    - Create a new GitHub repository");
            Console.WriteLine("  delete-repo <name>    - Delete a GitHub repository");
            Console.WriteLine("  list-repos            - List all your GitHub repositories");
            Console.WriteLine();
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Collaborator Operations:");
            Console.ResetColor();
            Console.WriteLine("  add-collaborator <repo> <user>    - Add a collaborator to a repository");
            Console.WriteLine("  remove-collaborator <repo> <user> - Remove a collaborator from a repository");
            Console.WriteLine("  list-collaborators <repo>         - List all collaborators of a repository");
            Console.WriteLine();
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Branch Operations:");
            Console.ResetColor();
            Console.WriteLine("  branch <name>         - Create a new branch");
            Console.WriteLine("  list-branches         - List all branches");
            Console.WriteLine("  merge <branch>        - Merge a branch");
            Console.WriteLine();
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Git Operations:");
            Console.ResetColor();
            Console.WriteLine("  status <path>         - Show repository status");
            Console.WriteLine("  push <path> <msg>     - Commit and push changes");
            Console.WriteLine("  pull <path>           - Pull latest changes");
            Console.WriteLine("  stash <path>          - Stash uncommitted changes");
            Console.WriteLine("  rebase <path> <branch>- Rebase onto a branch");
            Console.WriteLine();
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Batch Operations:");
            Console.ResetColor();
            Console.WriteLine("  batch-pull <paths...> - Pull changes for multiple repos");
            Console.WriteLine("  batch-push <paths...> - Push changes for multiple repos");
            Console.WriteLine();
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Plugin Commands:");
            Console.ResetColor();
            Console.WriteLine("  plugin-list           - List all loaded plugins");
            Console.WriteLine("  plugin-info <id>      - Get information about a plugin");
            Console.WriteLine("  plugin-run <id>       - Execute a plugin");
            Console.WriteLine("  plugin-load <path>    - Load a plugin from a file");
            Console.WriteLine("  plugin-unload <id>    - Unload a plugin");
            Console.WriteLine();
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Other Commands:");
            Console.ResetColor();
            Console.WriteLine("  help                  - Show this help message");
            Console.WriteLine("  history               - Show command history");
            Console.WriteLine("  clear                 - Clear the screen");
            Console.WriteLine("  exit                  - Exit GitDev");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.ResetColor();
            Console.WriteLine();
        }

        /// <summary>
        /// Read user input with prompt.
        /// </summary>
        public string ReadCommand(string username, string currentPath = "root")
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"@{username}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(":");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write($"~/{currentPath}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("$ ");
            Console.ResetColor();
            
            var command = Console.ReadLine()?.Trim();
            
            if (!string.IsNullOrEmpty(command))
            {
                AddToHistory(command);
            }
            
            return command;
        }

        /// <summary>
        /// Add command to history.
        /// </summary>
        private void AddToHistory(string command)
        {
            if (commandHistory.Count >= maxHistorySize)
            {
                commandHistory.RemoveAt(0);
            }
            
            commandHistory.Add(command);
            logger.Debug($"Command added to history: {command}");
        }

        /// <summary>
        /// Display command history.
        /// </summary>
        public void DisplayHistory()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("═══════════════════ COMMAND HISTORY ═══════════════════");
            Console.ResetColor();
            Console.WriteLine();
            
            if (commandHistory.Count == 0)
            {
                Console.WriteLine("No commands in history.");
            }
            else
            {
                for (int i = 0; i < commandHistory.Count; i++)
                {
                    Console.WriteLine($"  {i + 1}. {commandHistory[i]}");
                }
            }
            
            Console.WriteLine();
        }

        /// <summary>
        /// Display a success message.
        /// </summary>
        public void DisplaySuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ {message}");
            Console.ResetColor();
        }

        /// <summary>
        /// Display an error message.
        /// </summary>
        public void DisplayError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ {message}");
            Console.ResetColor();
        }

        /// <summary>
        /// Display a warning message.
        /// </summary>
        public void DisplayWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠ {message}");
            Console.ResetColor();
        }

        /// <summary>
        /// Display an info message.
        /// </summary>
        public void DisplayInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"ℹ {message}");
            Console.ResetColor();
        }

        /// <summary>
        /// Display a progress indicator for long-running operations.
        /// </summary>
        public void DisplayProgress(string operation, int current, int total)
        {
            var percent = (double)current / total * 100;
            var progressBar = new string('█', (int)(percent / 5)) + new string('░', 20 - (int)(percent / 5));
            
            Console.Write($"\r{operation}: [{progressBar}] {percent:F1}%");
            
            if (current == total)
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Prompt user for confirmation.
        /// </summary>
        public bool PromptConfirmation(string message)
        {
            Console.Write($"{message} (y/n): ");
            var response = Console.ReadLine()?.Trim().ToLower();
            return response == "y" || response == "yes";
        }

        /// <summary>
        /// Display a separator line.
        /// </summary>
        public void DisplaySeparator()
        {
            Console.WriteLine(new string('─', 60));
        }

        /// <summary>
        /// Parse command and arguments.
        /// </summary>
        public (string command, List<string> args) ParseCommand(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return (string.Empty, new List<string>());
            }

            var parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLower();
            var args = parts.Skip(1).ToList();

            return (command, args);
        }
    }
}

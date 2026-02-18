using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Xunit;
using GitDev.Plugins;
using GitDev.Core;

/// <summary>
/// Main application class for GitDev - Advanced Git and GitHub Management Tool.
/// Refactored with modular architecture, multi-threading support, and enhanced features.
/// </summary>
class GitDev
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    // Core managers
    private static AuthenticationManager authManager;
    private static GitOperationsManager gitOpsManager;
    private static GitHubRepositoryManager githubRepoManager;
    private static InteractiveCLI cli;
    private static WebSocketServerManager wsManager;
    private static ConfigurationManager configManager;
    private static PluginManager pluginManager;

    // Analytics managers
    private static AnalyticsManager analyticsManager;
    private static PerformanceMonitor performanceMonitor;
    private static ErrorLogManager errorLogManager;
    private static DashboardRenderer dashboardRenderer;

    // Application state
    private static string currentRepoPath;
    private static bool isRunning = true;

	// Test
	public static string Test = "Hello Test";
	public static string Test2 = "Hello Test";
	public static string Test3 = "Hello Test";
	public static string Test4 = "Hello Flame";
    public static string Test5 = "Hello Peoples";
    public static string Test6 = "Hello World";
    public static string Test7 = "Hello World";
    public static string Test8 = "This is test8 of AxiomIDE";
    
    // Configuration constants
    private const string CLIENT_ID = "Iv23liIAUGDEbAGLQaRr";
    private const string CLIENT_SECRET = "YOUR_CLIENT_SECRET";
    private const string REDIRECT_URI = "http://localhost:5000/callback";

    static async Task Main()
    {
        try
        {
            logger.Info("GitDev application starting");
            
            // Initialize configuration manager
            configManager = new ConfigurationManager();
            
            // Initialize CLI
            cli = new InteractiveCLI();
            
            // Authenticate user
            if (!await AuthenticateUserAsync())
            {
                cli.DisplayError("Authentication failed. Exiting application.");
                return;
            }

            // Initialize managers with authenticated user context
            int maxConcurrentOps = configManager.GetValue("MaxConcurrentOperations", 3);
            gitOpsManager = new GitOperationsManager(authManager.Username, null, maxConcurrentOps);
            githubRepoManager = new GitHubRepositoryManager(authManager.Client, authManager.Username);

            // Initialize analytics system
            InitializeAnalyticsSystem();

            // Record user signup/login
            analyticsManager.RecordSignup(authManager.Username);
            analyticsManager.RecordUserActive(authManager.Username);

            // Initialize plugin system
            await InitializePluginSystemAsync();

            // Display welcome message
            Console.Title = $"GitDev - {authManager.Username}";
            cli.DisplayWelcome(authManager.Username);

            // Start main application loop
            await RunApplicationLoopAsync();

            // Cleanup
            await ShutdownAsync();
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Fatal error in application");
            Console.WriteLine($"Fatal error: {ex.Message}");
        }
    }

    /// <summary>
    /// Authenticate user with GitHub OAuth.
    /// </summary>
    private static async Task<bool> AuthenticateUserAsync()
    {
        try
        {
            logger.Info("Starting user authentication");
            
            authManager = new AuthenticationManager(CLIENT_ID, CLIENT_SECRET, REDIRECT_URI);
            bool success = await authManager.AuthenticateAsync();
            
            if (success)
            {
                logger.Info($"User authenticated successfully: {authManager.Username}");
            }
            
            return success;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Authentication error");
            return false;
        }
    }

    /// <summary>
    /// Initialize the plugin system.
    /// </summary>
    private static async Task InitializePluginSystemAsync()
    {
        try
        {
            logger.Info("Initializing plugin system");
            
            string pluginDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
            pluginManager = PluginManager.GetInstance(pluginDirectory, "1.0.0");
            
            await Task.Run(() => pluginManager.Initialize());
            
            var plugins = pluginManager.ListPlugins();
            logger.Info($"Plugin system initialized with {plugins.Count} plugins");
            
            if (plugins.Count > 0)
            {
                cli.DisplayInfo($"Loaded {plugins.Count} plugin(s)");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to initialize plugin system");
            cli.DisplayWarning("Plugin system initialization failed");
        }
    }

    /// <summary>
    /// Initialize the analytics system.
    /// </summary>
    private static void InitializeAnalyticsSystem()
    {
        try
        {
            logger.Info("Initializing analytics system");
            
            // Create data directory in user's home or app directory
            string dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GitDev",
                "data"
            );
            
            analyticsManager = new AnalyticsManager(dataDirectory);
            performanceMonitor = new PerformanceMonitor();
            errorLogManager = new ErrorLogManager();
            dashboardRenderer = new DashboardRenderer(
                analyticsManager,
                performanceMonitor,
                errorLogManager,
                cli
            );
            
            logger.Info("Analytics system initialized successfully");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to initialize analytics system");
            cli.DisplayWarning("Analytics system initialization failed");
        }
    }

    /// <summary>
    /// Main application loop for processing user commands.
    /// </summary>
    private static async Task RunApplicationLoopAsync()
    {
        while (isRunning)
        {
            try
            {
                string command = cli.ReadCommand(authManager.Username, currentRepoPath ?? "root");
                
                if (string.IsNullOrEmpty(command))
                {
                    continue;
                }

                var (cmd, args) = cli.ParseCommand(command);
                await ProcessCommandAsync(cmd, args);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error processing command");
                cli.DisplayError($"Command error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Process user commands with improved command handling.
    /// </summary>
    private static async Task ProcessCommandAsync(string command, List<string> args)
    {
        logger.Debug($"Processing command: {command}");

        // Track command execution with performance monitoring
        using (var tracker = performanceMonitor?.StartOperation(command))
        {
            try
            {
                // Record activity
                analyticsManager?.RecordActivity(authManager.Username, command, string.Join(" ", args));
                analyticsManager?.RecordUserActive(authManager.Username);

                await ExecuteCommandAsync(command, args);
            }
            catch (Exception ex)
            {
                tracker?.MarkAsFailed();
                errorLogManager?.RecordError(command, ex.Message, ex.ToString());
                throw;
            }
        }
    }

    /// <summary>
    /// Execute the actual command logic.
    /// </summary>
    private static async Task ExecuteCommandAsync(string command, List<string> args)
    {
        switch (command)
        {
            case "help":
                cli.DisplayHelp();
                break;

            case "history":
                cli.DisplayHistory();
                break;

            case "clear":
                Console.Clear();
                cli.DisplayWelcome(authManager.Username);
                break;

            case "config":
                configManager.DisplayConfiguration();
                break;

            case "dashboard":
                HandleDashboardCommand();
                break;

            case "exit":
            case "quit":
                isRunning = false;
                break;

            // Repository operations
            case "init":
                await HandleInitCommandAsync(args);
                break;

            case "clone":
                await HandleCloneCommandAsync(args);
                break;

            case "create-repo":
                await HandleCreateRepoCommandAsync(args);
                break;

            case "delete-repo":
                await HandleDeleteRepoCommandAsync(args);
                break;

            case "list-repos":
                await HandleListReposCommandAsync();
                break;

            // Collaborator operations
            case "add-collaborator":
                await HandleAddCollaboratorCommandAsync(args);
                break;

            case "remove-collaborator":
                await HandleRemoveCollaboratorCommandAsync(args);
                break;

            case "list-collaborators":
                await HandleListCollaboratorsCommandAsync(args);
                break;

            // Branch operations
            case "branch":
                await HandleBranchCommandAsync(args);
                break;

            case "list-branches":
                await HandleListBranchesCommandAsync(args);
                break;

            // Git operations
            case "status":
                await HandleStatusCommandAsync(args);
                break;

            case "push":
                await HandlePushCommandAsync(args);
                break;

            case "pull":
                await HandlePullCommandAsync(args);
                break;

            case "stash":
                await HandleStashCommandAsync(args);
                break;

            case "rebase":
                await HandleRebaseCommandAsync(args);
                break;

            // Batch operations
            case "batch-pull":
                await HandleBatchPullCommandAsync(args);
                break;

            case "batch-push":
                await HandleBatchPushCommandAsync(args);
                break;

            // WebSocket server
            case "start-ws":
                await HandleStartWebSocketCommandAsync(args);
                break;

            case "stop-ws":
                HandleStopWebSocketCommand();
                break;

            // Plugin operations
            case "plugin-list":
                HandlePluginListCommand();
                break;

            case "plugin-info":
                HandlePluginInfoCommand(args);
                break;

            case "plugin-run":
                await HandlePluginRunCommandAsync(args);
                break;

            case "plugin-load":
                HandlePluginLoadCommand(args);
                break;

            case "plugin-unload":
                HandlePluginUnloadCommand(args);
                break;

            default:
                cli.DisplayWarning($"Unknown command: {command}");
                cli.DisplayInfo("Type 'help' for available commands");
                break;
        }
    }

    // ==================== Command Handlers ====================

    private static async Task HandleInitCommandAsync(List<string> args)
    {
        string repoPath = args.Count > 0 ? args[0] : PromptForInput("Enter local repository path");
        
        if (gitOpsManager.InitRepository(repoPath))
        {
            cli.DisplaySuccess($"Repository initialized at {repoPath}");
            currentRepoPath = repoPath;
        }
        else
        {
            cli.DisplayError("Failed to initialize repository");
        }
        
        await Task.CompletedTask;
    }

    private static async Task HandleCloneCommandAsync(List<string> args)
    {
        string repoUrl = args.Count > 0 ? args[0] : PromptForInput("Enter repository URL");
        string localPath = args.Count > 1 ? args[1] : PromptForInput("Enter local path");
        
        cli.DisplayInfo($"Cloning {repoUrl}...");
        
        if (await gitOpsManager.CloneRepositoryAsync(repoUrl, localPath))
        {
            cli.DisplaySuccess($"Repository cloned to {localPath}");
            currentRepoPath = localPath;
        }
        else
        {
            cli.DisplayError("Failed to clone repository");
        }
    }

    private static async Task HandleCreateRepoCommandAsync(List<string> args)
    {
        string repoName = args.Count > 0 ? args[0] : PromptForInput("Enter repository name");
        bool isPrivate = args.Count > 1 && args[1].ToLower() == "private";
        
        if (await githubRepoManager.CreateRepositoryAsync(repoName, isPrivate))
        {
            cli.DisplaySuccess($"Repository '{repoName}' created successfully");
        }
        else
        {
            cli.DisplayError("Failed to create repository");
        }
    }

    private static async Task HandleDeleteRepoCommandAsync(List<string> args)
    {
        string repoName = args.Count > 0 ? args[0] : PromptForInput("Enter repository name to delete");
        
        if (cli.PromptConfirmation($"Are you sure you want to delete '{repoName}'?"))
        {
            if (await githubRepoManager.DeleteRepositoryAsync(repoName))
            {
                cli.DisplaySuccess($"Repository '{repoName}' deleted successfully");
            }
            else
            {
                cli.DisplayError("Failed to delete repository");
            }
        }
        else
        {
            cli.DisplayInfo("Delete operation cancelled");
        }
    }

    private static async Task HandleListReposCommandAsync()
    {
        cli.DisplayInfo("Fetching repositories...");
        var repos = await githubRepoManager.ListRepositoriesAsync();
        
        if (repos.Count == 0)
        {
            cli.DisplayWarning("No repositories found");
        }
        else
        {
            cli.DisplaySuccess($"Found {repos.Count} repositories");
        }
    }
    
    private static async Task HandleAddCollaboratorCommandAsync(List<string> args)
    {
        string repoName = args.Count > 0 ? args[0] : PromptForInput("Enter repository name");
        string collaboratorUsername = args.Count > 1 ? args[1] : PromptForInput("Enter collaborator username");
        string permission = args.Count > 2 ? args[2] : "push";
        
        if (await githubRepoManager.AddCollaboratorAsync(repoName, collaboratorUsername, permission))
        {
            cli.DisplaySuccess($"Collaborator '{collaboratorUsername}' added to '{repoName}' successfully");
        }
        else
        {
            cli.DisplayError("Failed to add collaborator");
        }
    }
    
    private static async Task HandleRemoveCollaboratorCommandAsync(List<string> args)
    {
        string repoName = args.Count > 0 ? args[0] : PromptForInput("Enter repository name");
        string collaboratorUsername = args.Count > 1 ? args[1] : PromptForInput("Enter collaborator username");
        
        if (cli.PromptConfirmation($"Are you sure you want to remove '{collaboratorUsername}' from '{repoName}'?"))
        {
            if (await githubRepoManager.RemoveCollaboratorAsync(repoName, collaboratorUsername))
            {
                cli.DisplaySuccess($"Collaborator '{collaboratorUsername}' removed from '{repoName}' successfully");
            }
            else
            {
                cli.DisplayError("Failed to remove collaborator");
            }
        }
        else
        {
            cli.DisplayInfo("Remove operation cancelled");
        }
    }
    
    private static async Task HandleListCollaboratorsCommandAsync(List<string> args)
    {
        string repoName = args.Count > 0 ? args[0] : PromptForInput("Enter repository name");
        
        cli.DisplayInfo($"Fetching collaborators for '{repoName}'...");
        var collaborators = await githubRepoManager.ListCollaboratorsAsync(repoName);
        
        if (collaborators.Count == 0)
        {
            cli.DisplayWarning("No collaborators found");
        }
        else
        {
            cli.DisplaySuccess($"Found {collaborators.Count} collaborators");
        }
    }
        
        if (repos.Count > 0)
        {
            cli.DisplayInfo($"Found {repos.Count} repositories");
        }
        else
        {
            cli.DisplayWarning("No repositories found");
        }
    }

    private static async Task HandleBranchCommandAsync(List<string> args)
    {
        string repoPath = currentRepoPath ?? (args.Count > 0 ? args[0] : PromptForInput("Enter repository path"));
        string branchName = args.Count > 1 ? args[1] : (args.Count > 0 ? args[0] : PromptForInput("Enter branch name"));
        
        if (gitOpsManager.CreateBranch(repoPath, branchName))
        {
            cli.DisplaySuccess($"Branch '{branchName}' created");
        }
        else
        {
            cli.DisplayError("Failed to create branch");
        }
        
        await Task.CompletedTask;
    }

    private static async Task HandleListBranchesCommandAsync(List<string> args)
    {
        string repoPath = currentRepoPath ?? (args.Count > 0 ? args[0] : PromptForInput("Enter repository path"));
        
        cli.DisplayInfo("Branches:");
        gitOpsManager.ListBranches(repoPath);
        
        await Task.CompletedTask;
    }

    private static async Task HandleStatusCommandAsync(List<string> args)
    {
        string repoPath = currentRepoPath ?? (args.Count > 0 ? args[0] : PromptForInput("Enter repository path"));
        
        string status = gitOpsManager.GetRepositoryStatus(repoPath);
        cli.DisplayInfo($"Status: {status}");
        
        await Task.CompletedTask;
    }

    private static async Task HandlePushCommandAsync(List<string> args)
    {
        string repoPath = currentRepoPath ?? (args.Count > 0 ? args[0] : PromptForInput("Enter repository path"));
        string message = args.Count > 1 ? string.Join(" ", args.Skip(1)) : PromptForInput("Enter commit message");
        
        cli.DisplayInfo("Pushing changes...");
        
        if (await gitOpsManager.PushChangesAsync(repoPath, message))
        {
            cli.DisplaySuccess("Changes pushed successfully");
        }
        else
        {
            cli.DisplayError("Failed to push changes");
        }
    }

    private static async Task HandlePullCommandAsync(List<string> args)
    {
        string repoPath = currentRepoPath ?? (args.Count > 0 ? args[0] : PromptForInput("Enter repository path"));
        
        cli.DisplayInfo("Pulling changes...");
        
        if (await gitOpsManager.PullChangesAsync(repoPath))
        {
            cli.DisplaySuccess("Changes pulled successfully");
        }
        else
        {
            cli.DisplayError("Failed to pull changes");
        }
    }

    private static async Task HandleStashCommandAsync(List<string> args)
    {
        string repoPath = currentRepoPath ?? (args.Count > 0 ? args[0] : PromptForInput("Enter repository path"));
        string message = args.Count > 1 ? string.Join(" ", args.Skip(1)) : "Stashed changes";
        
        if (gitOpsManager.StashChanges(repoPath, message))
        {
            cli.DisplaySuccess("Changes stashed successfully");
        }
        else
        {
            cli.DisplayError("Failed to stash changes");
        }
        
        await Task.CompletedTask;
    }

    private static async Task HandleRebaseCommandAsync(List<string> args)
    {
        string repoPath = currentRepoPath ?? (args.Count > 0 ? args[0] : PromptForInput("Enter repository path"));
        string branchName = args.Count > 1 ? args[1] : (args.Count > 0 ? args[0] : PromptForInput("Enter branch to rebase onto"));
        
        cli.DisplayInfo($"Rebasing onto {branchName}...");
        
        if (gitOpsManager.RebaseOnto(repoPath, branchName))
        {
            cli.DisplaySuccess("Rebase completed successfully");
        }
        else
        {
            cli.DisplayError("Rebase failed");
        }
        
        await Task.CompletedTask;
    }

    private static async Task HandleBatchPullCommandAsync(List<string> args)
    {
        if (args.Count == 0)
        {
            cli.DisplayError("Please provide at least one repository path");
            return;
        }

        cli.DisplayInfo($"Starting batch pull for {args.Count} repositories...");
        
        var operations = args.Select(path => ("pull", path, new Dictionary<string, string>())).ToList();
        var results = await gitOpsManager.ExecuteBatchOperationsAsync(operations);
        
        int successCount = results.Count(r => r.Value);
        cli.DisplayInfo($"Batch pull completed: {successCount}/{results.Count} successful");
    }

    private static async Task HandleBatchPushCommandAsync(List<string> args)
    {
        if (args.Count == 0)
        {
            cli.DisplayError("Please provide at least one repository path");
            return;
        }

        string message = PromptForInput("Enter commit message for all repositories");
        
        cli.DisplayInfo($"Starting batch push for {args.Count} repositories...");
        
        var operations = args.Select(path => 
            ("push", path, new Dictionary<string, string> { { "message", message } })
        ).ToList();
        
        var results = await gitOpsManager.ExecuteBatchOperationsAsync(operations);
        
        int successCount = results.Count(r => r.Value);
        cli.DisplayInfo($"Batch push completed: {successCount}/{results.Count} successful");
    }

    private static async Task HandleStartWebSocketCommandAsync(List<string> args)
    {
        string repoPath = currentRepoPath ?? (args.Count > 0 ? args[0] : PromptForInput("Enter repository path"));
        
        if (wsManager == null)
        {
            int port = configManager.GetValue("WebSocketPort", 5001);
            wsManager = new WebSocketServerManager(port);
        }
        
        wsManager.Start(repoPath);
        cli.DisplaySuccess("WebSocket server started");
        
        await Task.CompletedTask;
    }

    private static void HandleStopWebSocketCommand()
    {
        if (wsManager != null && wsManager.IsRunning)
        {
            wsManager.Stop();
            cli.DisplaySuccess("WebSocket server stopped");
        }
        else
        {
            cli.DisplayWarning("WebSocket server is not running");
        }
    }

    private static void HandlePluginListCommand()
    {
        try
        {
            var plugins = pluginManager.ListPlugins();
            
            if (plugins.Count == 0)
            {
                cli.DisplayInfo("No plugins loaded");
                return;
            }

            Console.WriteLine();
            cli.DisplayInfo($"Loaded Plugins ({plugins.Count}):");
            cli.DisplaySeparator();
            
            foreach (var plugin in plugins)
            {
                Console.WriteLine($"  ID:          {plugin.Id}");
                Console.WriteLine($"  Name:        {plugin.Name}");
                Console.WriteLine($"  Version:     {plugin.Version}");
                Console.WriteLine($"  Author:      {plugin.Author}");
                Console.WriteLine($"  Description: {plugin.Description}");
                cli.DisplaySeparator();
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error listing plugins");
            cli.DisplayError($"Failed to list plugins: {ex.Message}");
        }
    }

    private static void HandlePluginInfoCommand(List<string> args)
    {
        if (args.Count == 0)
        {
            cli.DisplayError("Usage: plugin-info <plugin-id>");
            return;
        }

        try
        {
            string pluginId = args[0];
            var plugin = pluginManager.GetPlugin(pluginId);
            
            if (plugin == null)
            {
                cli.DisplayError($"Plugin not found: {pluginId}");
                return;
            }

            var metadata = plugin.Metadata;
            Console.WriteLine();
            cli.DisplayInfo("Plugin Information:");
            Console.WriteLine($"  ID:               {metadata.Id}");
            Console.WriteLine($"  Name:             {metadata.Name}");
            Console.WriteLine($"  Version:          {metadata.Version}");
            Console.WriteLine($"  Author:           {metadata.Author}");
            Console.WriteLine($"  Description:      {metadata.Description}");
            Console.WriteLine($"  Min Host Version: {metadata.MinimumHostVersion}");
            
            if (metadata.Dependencies != null && metadata.Dependencies.Length > 0)
            {
                Console.WriteLine($"  Dependencies:     {string.Join(", ", metadata.Dependencies)}");
            }
            
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error showing plugin info");
            cli.DisplayError($"Failed to show plugin info: {ex.Message}");
        }
    }

    private static async Task HandlePluginRunCommandAsync(List<string> args)
    {
        if (args.Count == 0)
        {
            cli.DisplayError("Usage: plugin-run <plugin-id> [args]");
            return;
        }

        try
        {
            string pluginId = args[0];
            cli.DisplayInfo($"Executing plugin: {pluginId}...");
            
            var pluginArgs = new Dictionary<string, object>
            {
                { "name", authManager.Username }
            };

            var result = await Task.Run(() => pluginManager.ExecutePlugin(pluginId, pluginArgs));
            
            if (result.Success)
            {
                cli.DisplaySuccess("Plugin executed successfully!");
                
                if (!string.IsNullOrEmpty(result.Message))
                {
                    Console.WriteLine($"  Message: {result.Message}");
                }
                
                if (result.Data != null)
                {
                    Console.WriteLine($"  Data: {result.Data}");
                }
            }
            else
            {
                cli.DisplayError("Plugin execution failed!");
                Console.WriteLine($"  Error: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error running plugin");
            cli.DisplayError($"Failed to run plugin: {ex.Message}");
        }
    }

    private static void HandlePluginLoadCommand(List<string> args)
    {
        if (args.Count == 0)
        {
            cli.DisplayError("Usage: plugin-load <plugin-path>");
            return;
        }

        try
        {
            string pluginPath = args[0];
            cli.DisplayInfo($"Loading plugin from: {pluginPath}...");
            
            if (pluginManager.LoadPlugin(pluginPath))
            {
                cli.DisplaySuccess("Plugin loaded successfully!");
            }
            else
            {
                cli.DisplayError("Failed to load plugin. Check logs for details.");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error loading plugin");
            cli.DisplayError($"Failed to load plugin: {ex.Message}");
        }
    }

    private static void HandlePluginUnloadCommand(List<string> args)
    {
        if (args.Count == 0)
        {
            cli.DisplayError("Usage: plugin-unload <plugin-id>");
            return;
        }

        try
        {
            string pluginId = args[0];
            cli.DisplayInfo($"Unloading plugin: {pluginId}...");
            
            if (pluginManager.UnloadPlugin(pluginId))
            {
                cli.DisplaySuccess("Plugin unloaded successfully!");
            }
            else
            {
                cli.DisplayError("Failed to unload plugin. Check logs for details.");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error unloading plugin");
            cli.DisplayError($"Failed to unload plugin: {ex.Message}");
        }
    }

    private static void HandleDashboardCommand()
    {
        try
        {
            logger.Info("Displaying analytics dashboard");
            dashboardRenderer.RenderDashboard();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error displaying dashboard");
            cli.DisplayError($"Failed to display dashboard: {ex.Message}");
        }
    }

    // ==================== Helper Methods ====================

    private static string PromptForInput(string prompt)
    {
        Console.Write($"{prompt}: ");
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    private static async Task ShutdownAsync()
    {
        logger.Info("Shutting down application");
        cli.DisplayInfo("Shutting down...");
        
        // Stop WebSocket server
        if (wsManager != null && wsManager.IsRunning)
        {
            wsManager.Stop();
        }

        // Shutdown plugin system
        if (pluginManager != null)
        {
            await Task.Run(() => pluginManager.Shutdown());
        }

        // Save configuration
        if (configManager != null)
        {
            configManager.SaveConfiguration();
        }

        logger.Info("Application shutdown complete");
        cli.DisplaySuccess("Goodbye!");
    }

    // ==================== Tests ====================

    public class GitDevTests
    {
        [Fact]
        public void ConfigurationManager_ShouldLoadDefaultValues()
        {
            var config = new ConfigurationManager();
            int maxOps = config.GetValue("MaxConcurrentOperations", 0);
            Assert.True(maxOps > 0);
        }

        [Fact]
        public void InteractiveCLI_ParseCommand_ShouldSplitCorrectly()
        {
            var cli = new InteractiveCLI();
            var (command, args) = cli.ParseCommand("clone http://example.com /path/to/repo");
            
            Assert.Equal("clone", command);
            Assert.Equal(2, args.Count);
            Assert.Equal("http://example.com", args[0]);
            Assert.Equal("/path/to/repo", args[1]);
        }
    }
}

using System;
using System.IO;
using System.Net;
using System.Text;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Octokit;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using NLog;
using Xunit;
using WebSocketSharp.Server;
using WebSocketSharp;
using GitDev.Plugins;
using System.Collections.Generic;
using System.Linq;

class GitDev
{
    private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

    static GitHubClient client;
    static string username;
    static PluginManager pluginManager;

    static string clientId = "Iv23liIAUGDEbAGLQaRr";
    static string clientSecret = "YOUR_CLIENT_SECRET";
    static string redirectUri = "http://localhost:5000/callback";

    static async Task Main()
    {
        await AuthenticateUser();

        // Initialize plugin system
        string pluginDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
        pluginManager = PluginManager.GetInstance(pluginDirectory, "1.0.0");
        pluginManager.Initialize();

        Console.Title = "GitDev Login";
        Console.WriteLine($"Welcome, @{username}/root");
        Console.Clear();

        while (true)
        {
            Console.Clear();
            Console.Title = $"GitDev Logged In - {username}";
            Console.Write($"@{username}/root: ");
            Console.Write("Enter repo URL (.git): ");
            string repoUrl = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(repoUrl) || !repoUrl.EndsWith(".git"))
            {
                Console.WriteLine("Invalid repo URL. Please provide a valid .git URL");
                return;
            }
            Console.Write("Enter local repo path to clone into: ");
            string repoPath = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(repoPath))
            {
                Console.WriteLine("Invalid repo path");
                return;
            }
            try
            {
                Console.WriteLine("Cloning repo...");
                LibGit2Sharp.Repository.Clone(repoUrl, repoPath);
                Console.WriteLine("repo cloned successfully");
            }
            catch (Exception EX)
            {
                Console.WriteLine($"Failed to clone repo {EX.Message}");
                return;
            }
            StartWebSocketServer(repoPath);
            string command = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(command)) continue;

            string[] args = command.Split(new char[] { ' ' }, 2);
            string cmd = args[0].ToLower();
            string param = args.Length > 1 ? args[1] : "";

            switch (cmd)
            {
                case "dev":
                    if (string.IsNullOrEmpty(param))
                    {
                        DisplayHelp();
                        break;
                    }
                    await ProcessGitCommand(param);
                    break;
                case "exit":
                    Console.WriteLine("Exiting GitDev.");
                    if (pluginManager != null)
                    {
                        pluginManager.Shutdown();
                    }
                    return;
                default:
                    Console.WriteLine("Unknown command. Type 'dev' for available commands.");
                    break;
            }
        }
    }

    static void DisplayHelp()
    {
        Console.WriteLine("Available commands:");
        Console.WriteLine("  dev init - Initialize a new repository");
        Console.WriteLine("  dev clone - Clone a repository");
        Console.WriteLine("  dev create-repo - Create a new GitHub repository");
        Console.WriteLine("  dev delete-repo - Delete a GitHub repository");
        Console.WriteLine("  dev branch - Create a branch");
        Console.WriteLine("  dev merge - Merge a branch");
        Console.WriteLine("  dev push - Push changes");
        Console.WriteLine("  dev stash - Stash changes");
        Console.WriteLine("  dev rebase - Rebase branch");
        Console.WriteLine("  dev pull - Pull changes");
        Console.WriteLine("  dev list - List branches");
        Console.WriteLine("  dev status - Show repository status");
        Console.WriteLine("");
        Console.WriteLine("Plugin commands:");
        Console.WriteLine("  dev plugin-list - List all loaded plugins");
        Console.WriteLine("  dev plugin-info <id> - Get information about a plugin");
        Console.WriteLine("  dev plugin-run <id> [args] - Execute a plugin");
        Console.WriteLine("  dev plugin-load <path> - Load a plugin from a file");
        Console.WriteLine("  dev plugin-unload <id> - Unload a plugin");
    }

    static void StartWebSocketServer(string repoPath)
    {
        WebSocketServer wss = new WebSocketServer("ws://localhost:5001");
        wss.AddWebSocketService("/git-status", () => new GitStatusService(repoPath));
        wss.Start();
        Console.WriteLine("WebSocket server started at ws://localhost:5001/git-status");

        // Start watching the repository for file changes
        StartFileWatcher(repoPath);
    }


    static void StartFileWatcher(string repoPath)
    {
        FileSystemWatcher watcher = new FileSystemWatcher
        {
            Path = repoPath,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            Filter = "*.",
            EnableRaisingEvents = true
        };

        watcher.Changed += (sender, e) =>
        {
            Console.WriteLine($"File changed: {e.FullPath}");
            BroadcastChange(e.FullPath);
        };

        watcher.Created += (sender, e) =>
        {
            Console.WriteLine($"File created: {e.FullPath}");
            BroadcastChange(e.FullPath);
        };

        watcher.Deleted += (sender, e) =>
        {
            Console.WriteLine($"File deleted: {e.FullPath}");
            BroadcastChange(e.FullPath);
        };
    }

    static void BroadcastChange(string filePath)
    {
        Console.WriteLine($"Broadcasting change: {filePath}");
    }

    static async Task AuthenticateUser()
    {
        try
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5000/");
            listener.Start();

            string authUrl = $"https://github.com/login/oauth/authorize?client_id={clientId}&redirect_uri={redirectUri}&scope=repo";
            Console.WriteLine("Opening GitHub authentication page...");
            Process.Start(new ProcessStartInfo
            {
                FileName = authUrl,
                UseShellExecute = true
            });

            Console.WriteLine("Waiting for authentication callback...");
            var context = await listener.GetContextAsync();
            var request = context.Request;
            var response = context.Response;

            string code = request.QueryString["code"];
            if (string.IsNullOrEmpty(code))
            {
                logger.Error("Authentication failed. No authorization code received.");
                Console.WriteLine("Authentication failed. No authorization code received.");
                response.StatusCode = 400;
                byte[] errorBuffer = Encoding.UTF8.GetBytes("<html><body><h2>Authentication Failed</h2><p>No authorization code received.</p></body></html>");
                response.OutputStream.Write(errorBuffer, 0, errorBuffer.Length);
                response.OutputStream.Close();
                return;
            }

            response.StatusCode = 200;
            string successHtml = @"
            <html>
            <head>
                <title>GitDev Authentication</title>
                <style>
                    body { font-family: Arial, sans-serif; text-align: center; padding: 50px; background-color: #f4f4f4; }
                    .container { background: white; padding: 20px; border-radius: 10px; box-shadow: 0px 0px 10px 0px #aaa; display: inline-block; }
                    h2 { color: #333; }
                    p { font-size: 18px; }
                    .success { color: green; font-weight: bold; }
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>Authentication Successful!</h2>
                    <p class='success'>You can now close this window and return to GitDev.</p>
                </div>
            </body>
            </html>";

            byte[] buffer = Encoding.UTF8.GetBytes(successHtml);
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            listener.Stop();

            string token = await ExchangeCodeForToken(code);

            client = new GitHubClient(new ProductHeaderValue("GitDev"))
            {
                Credentials = new Octokit.Credentials(token)
            };

            var user = await client.User.Current();
            username = user.Login;
            Console.WriteLine($"Authenticated as: {username}");
        }
        catch (Exception ex)
        {
            logger.Error($"Error during authentication: {ex.Message}");
            Console.WriteLine($"Error during authentication: {ex.Message}");
        }
    }


    static async Task<string> ExchangeCodeForToken(string code)
    {
        using (var webClient = new WebClient())
        {
            try
            {
                var values = new System.Collections.Specialized.NameValueCollection
                {
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["code"] = code,
                    ["redirect_uri"] = redirectUri
                };

                var response = await webClient.UploadValuesTaskAsync("https://github.com/login/oauth/access_token", values);
                string responseString = Encoding.UTF8.GetString(response);
                if (!responseString.Contains("access_token"))
                {
                    logger.Error("Error: Unable to retrieve access token."); // logging
                    Console.WriteLine("Error: Unable to retrieve access token.");
                    return null;
                }

                string token = responseString.Split('&')[0].Split('=')[1];
                return token;
            }
            catch (Exception ex)
            {
                logger.Error($"Error exchanging code for token: {ex.Message}"); // logging
                Console.WriteLine($"Error exchanging code for token: {ex.Message}");
                return null;
            }
        }
    }

    public class GitDevTests
    {
        [Fact]
        public async Task AuthenticateUser_ShouldHandleInvalidCode()
        {
            await Assert.ThrowsAsync<Exception>(async () => await GitDev.ExchangeCodeForToken("invalid_code"));
        }

        [Fact]
        public async Task ExchangeCodeForToken_ShouldReturnNullForInvalidCode()
        {
            string token = await GitDev.ExchangeCodeForToken("invalid_code");
            Assert.Null(token);
        }
    }

    class GitStatusService : WebSocketBehavior
    {
        private string repoPath;

        public GitStatusService(string repoPath)
        {
            this.repoPath = repoPath;
        }

        public GitStatusService() { } // Parameterless constructor for WebSocketServer

        protected override void OnMessage(MessageEventArgs e)
        {
            if (string.IsNullOrEmpty(repoPath))
            {
                Send("Error: Repository path is not set.");
                return;
            }

            using (var repo = new LibGit2Sharp.Repository(repoPath))
            {
                var status = repo.RetrieveStatus();
                Send($"Repo Status: {status}");
            }
        }

        public void SetRepoPath(string path)
        {
            this.repoPath = path;
        }
    }

    static async Task ProcessGitCommand(string param)
    {
        string[] args = param.Split(new char[] { ' ' }, 2);
        string command = args[0].ToLower();

        switch (command)
        {
            case "init":
                Console.Write("Enter local repository path: ");
                string repoPath = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(repoPath) || !Directory.Exists(repoPath))
                {
                    logger.Error("Invalid directory path"); // logging
                    Console.WriteLine("Invalid directory path.");
                    return;
                }
                LibGit2Sharp.Repository.Init(repoPath);
                Console.WriteLine("Initialized empty Git repository.");
                break;
            case "rebase":
                Console.Write("Enter local repository path: ");
                repoPath = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(repoPath) || !Directory.Exists(repoPath))
                {
                    Console.WriteLine("Invalid repository path.");
                    return;
                }
                Console.Write("Enter branch to rebase onto: ");
                string rebaseBranch = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(rebaseBranch))
                {
                    Console.WriteLine("Branch name cannot be empty.");
                    return;
                }
                logger.Info($"Rebasing onto {rebaseBranch}"); // logging
                Console.WriteLine($"Rebasing onto {rebaseBranch}...");
                try
                {
                    using (var repo = new LibGit2Sharp.Repository(repoPath))
                    {
                        var branch = repo.Branches[rebaseBranch];
                        if (branch == null)
                        {
                            Console.WriteLine("Branch not found.");
                            return;
                        }
                        var signature = new LibGit2Sharp.Signature(username, "email@example.com", DateTime.Now);
                        var rebaseOptions = new RebaseOptions
                        {
                            FileConflictStrategy = CheckoutFileConflictStrategy.Merge
                        };
                        repo.Rebase.Start(repo.Head, branch, repo.Head, new Identity(username, "email@example.com"), rebaseOptions);
                        logger.Info($"Successfully rebased onto {rebaseBranch}"); // logging
                        Console.WriteLine($"Successfully rebased onto {rebaseBranch}.");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"Rebase failed: {ex.Message}"); // logging
                    Console.WriteLine($"Rebase failed: {ex.Message}");
                }
                break;
            case "stash":
                Console.Write("Enter local repository path: ");
                repoPath = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(repoPath) || !Directory.Exists(repoPath))
                {
                    Console.WriteLine("Invalid repository path.");
                    return;
                }
                logger.Info("Stashing changes..."); // logging
                Console.WriteLine("Stashing changes...");
                try
                {
                    using (var repo = new LibGit2Sharp.Repository(repoPath))
                    {
                        var stashIndex = repo.Stashes.Add(new LibGit2Sharp.Signature(username, "email@example.com", DateTime.Now), "Stashed changes");
                        repo.Stashes.Add(new LibGit2Sharp.Signature(username, "email@example.com", DateTime.Now), "Stashed changes");
                        logger.Info($"Changes stashed successfully with index {stashIndex}"); // logging
                        Console.WriteLine($"Changes stashed successfully with index {stashIndex}");
                    }
                }
                catch (Exception EX)
                {
                    logger.Error($"Stash failed: {EX.Message}"); // logging
                    Console.WriteLine($"Stash failed: {EX.Message}");
                }
                break;
            case "create-repo":
                Console.Write("Enter new repository name: ");
                string repoName = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(repoName))
                {
                    logger.Warn("Repository cannot be empty."); // logging
                    Console.WriteLine("Repository name cannot be empty.");
                    return;
                }
                await CreateGitHubRepo(repoName);
                break;
            case "delete-repo":
                Console.Write("Enter repository name to delete: ");
                string repoToDelete = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(repoToDelete))
                {
                    logger.Warn("Repository name cannot be empty."); // logging
                    Console.WriteLine("Repository name cannot be empty.");
                    return;
                }
                await DeleteGitHubRepo(repoToDelete);
                break;
            case "plugin-list":
                ListPlugins();
                break;
            case "plugin-info":
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: dev plugin-info <plugin-id>");
                    return;
                }
                ShowPluginInfo(args[1]);
                break;
            case "plugin-run":
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: dev plugin-run <plugin-id> [args]");
                    return;
                }
                await RunPlugin(args[1]);
                break;
            case "plugin-load":
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: dev plugin-load <plugin-path>");
                    return;
                }
                LoadPlugin(args[1]);
                break;
            case "plugin-unload":
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: dev plugin-unload <plugin-id>");
                    return;
                }
                UnloadPlugin(args[1]);
                break;
            default:
                Console.WriteLine("Invalid dev command. Type 'dev' for a list of available commands.");
                break;
        }
    }

    static async Task CreateGitHubRepo(string repoName)
    {
        try
        {
            if (client == null || client.Credentials == null)
            {
                logger.Error("Error: GitHub client is not authenticated. Ensure you have a valid OAuth token."); // logging
                Console.WriteLine("Error: GitHub client is not authenticated. Ensure you have a valid OAuth token.");
                return;
            }

            var newRepo = new NewRepository(repoName) { Private = false };
            var repo = await client.Repository.Create(newRepo);
            Console.WriteLine($"Repo created: {repo.HtmlUrl}");
            Thread.Sleep(5000); // 5sec sleep to show msg
        }
        catch (ForbiddenException ex)
        {
            logger.Error("Error: Forbidden - Check if your token has 'repo' or 'public_repo' permissions."); // logging
            Console.WriteLine("Error: Forbidden - Check if your token has 'repo' or 'public_repo' permissions.");
            Console.WriteLine($"Exception: {ex.Message}");
            Thread.Sleep(5000);
        }
        catch (AuthorizationException ex)
        {
            logger.Error("Error: Unauthorized - Your credentials might be incorrect or expired."); // logging
            Console.WriteLine("Error: Unauthorized - Your credentials might be incorrect or expired.");
            Console.WriteLine($"Exception: {ex.Message}");
            Thread.Sleep(5000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }


    static async Task DeleteGitHubRepo(string repoName)
    {
        await client.Repository.Delete(username, repoName);
        Console.WriteLine("Repository deleted successfully.");
        Thread.Sleep(5000);
    }

    static void CloneRepository(string repoUrl, string localPath, string username, string token)
    {
        var options = new CloneOptions();
        options.FetchOptions.CredentialsProvider = (_url, _user, _cred) =>
            new UsernamePasswordCredentials { Username = username, Password = token };

        LibGit2Sharp.Repository.Clone(repoUrl, localPath, options);
        Console.WriteLine("Repository cloned successfully.");
    }

    static void CreateBranch(string repoPath, string branchName)
    {
        using (var repo = new LibGit2Sharp.Repository(repoPath))
        {
            repo.CreateBranch(branchName);
            Console.WriteLine($"Branch '{branchName}' created successfully.");
        }
    }

    static void PushChanges(string repoPath, string commitMessage, string username, string token)
    {
        using (var repo = new LibGit2Sharp.Repository(repoPath))
        {
            Commands.Stage(repo, "*");
            var author = new LibGit2Sharp.Signature(username, "email@example.com", DateTime.Now);
            var committer = author;
            repo.Commit(commitMessage, author, committer);

            var remote = repo.Network.Remotes["origin"];
            var options = new PushOptions();
            options.CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = username, Password = token };
            repo.Network.Push(remote, repo.Head.CanonicalName, options);

            Console.WriteLine("Changes pushed successfully.");
        }
    }

    static void PullChanges(string repoPath, string username, string token)
    {
        using (var repo = new LibGit2Sharp.Repository(repoPath))
        {
            var remote = repo.Network.Remotes["origin"];
            var credentials = new UsernamePasswordCredentials { Username = username, Password = token };
            var options = new PullOptions();
            options.FetchOptions.CredentialsProvider = (_url, _user, _cred) => credentials;

            var signature = new LibGit2Sharp.Signature(username, "email@example.com", DateTime.Now);
            Commands.Pull(repo, signature, options);

            Console.WriteLine("Repository updated successfully.");
        }
    }

    static void ListBranches(string repoPath)
    {
        using (var repo = new LibGit2Sharp.Repository(repoPath))
        {
            foreach (var branch in repo.Branches)
            {
                Console.WriteLine(branch.FriendlyName);
            }
        }
    }

    static void ListPlugins()
    {
        try
        {
            var plugins = pluginManager.ListPlugins();
            if (plugins.Count == 0)
            {
                Console.WriteLine("No plugins loaded.");
                return;
            }

            Console.WriteLine($"\nLoaded Plugins ({plugins.Count}):");
            Console.WriteLine("".PadRight(60, '-'));
            foreach (var plugin in plugins)
            {
                Console.WriteLine($"ID:          {plugin.Id}");
                Console.WriteLine($"Name:        {plugin.Name}");
                Console.WriteLine($"Version:     {plugin.Version}");
                Console.WriteLine($"Author:      {plugin.Author}");
                Console.WriteLine($"Description: {plugin.Description}");
                Console.WriteLine("".PadRight(60, '-'));
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error listing plugins: {ex.Message}");
            Console.WriteLine($"Error listing plugins: {ex.Message}");
        }
    }

    static void ShowPluginInfo(string pluginId)
    {
        try
        {
            var plugin = pluginManager.GetPlugin(pluginId);
            if (plugin == null)
            {
                Console.WriteLine($"Plugin not found: {pluginId}");
                return;
            }

            var metadata = plugin.Metadata;
            Console.WriteLine("\nPlugin Information:");
            Console.WriteLine("".PadRight(60, '='));
            Console.WriteLine($"ID:               {metadata.Id}");
            Console.WriteLine($"Name:             {metadata.Name}");
            Console.WriteLine($"Version:          {metadata.Version}");
            Console.WriteLine($"Author:           {metadata.Author}");
            Console.WriteLine($"Description:      {metadata.Description}");
            Console.WriteLine($"Min Host Version: {metadata.MinimumHostVersion}");
            if (metadata.Dependencies != null && metadata.Dependencies.Length > 0)
            {
                Console.WriteLine($"Dependencies:     {string.Join(", ", metadata.Dependencies)}");
            }
            Console.WriteLine("".PadRight(60, '='));
        }
        catch (Exception ex)
        {
            logger.Error($"Error showing plugin info: {ex.Message}");
            Console.WriteLine($"Error showing plugin info: {ex.Message}");
        }
    }

    static async Task RunPlugin(string pluginId)
    {
        try
        {
            Console.WriteLine($"\nExecuting plugin: {pluginId}...");
            
            // For this example, we'll pass some default arguments
            var args = new Dictionary<string, object>
            {
                { "name", username ?? "User" }
            };

            var result = pluginManager.ExecutePlugin(pluginId, args);
            
            if (result.Success)
            {
                Console.WriteLine($"✓ Plugin executed successfully!");
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
                Console.WriteLine($"✗ Plugin execution failed!");
                Console.WriteLine($"  Error: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error running plugin: {ex.Message}");
            Console.WriteLine($"Error running plugin: {ex.Message}");
        }
        
        await Task.CompletedTask;
    }

    static void LoadPlugin(string pluginPath)
    {
        try
        {
            Console.WriteLine($"\nLoading plugin from: {pluginPath}...");
            
            if (pluginManager.LoadPlugin(pluginPath))
            {
                Console.WriteLine("✓ Plugin loaded successfully!");
            }
            else
            {
                Console.WriteLine("✗ Failed to load plugin. Check logs for details.");
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading plugin: {ex.Message}");
            Console.WriteLine($"Error loading plugin: {ex.Message}");
        }
    }

    static void UnloadPlugin(string pluginId)
    {
        try
        {
            Console.WriteLine($"\nUnloading plugin: {pluginId}...");
            
            if (pluginManager.UnloadPlugin(pluginId))
            {
                Console.WriteLine("✓ Plugin unloaded successfully!");
            }
            else
            {
                Console.WriteLine("✗ Failed to unload plugin. Check logs for details.");
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error unloading plugin: {ex.Message}");
            Console.WriteLine($"Error unloading plugin: {ex.Message}");
        }
    }
}

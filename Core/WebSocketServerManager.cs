using System;
using System.IO;
using System.Threading.Tasks;
using LibGit2Sharp;
using NLog;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace GitDev.Core
{
    /// <summary>
    /// Manages WebSocket server for real-time Git repository monitoring.
    /// </summary>
    public class WebSocketServerManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private WebSocketServer server;
        private FileSystemWatcher watcher;
        private readonly int port;

        public bool IsRunning { get; private set; }

        public WebSocketServerManager(int port = 5001)
        {
            this.port = port;
        }

        /// <summary>
        /// Start the WebSocket server and file watcher.
        /// </summary>
        public void Start(string repoPath)
        {
            try
            {
                if (IsRunning)
                {
                    logger.Warn("WebSocket server is already running");
                    return;
                }

                logger.Info($"Starting WebSocket server on port {port}");
                
                server = new WebSocketServer($"ws://localhost:{port}");
                server.AddWebSocketService("/git-status", () => new GitStatusService(repoPath));
                server.Start();
                
                IsRunning = true;
                logger.Info($"WebSocket server started at ws://localhost:{port}/git-status");
                Console.WriteLine($"WebSocket server started at ws://localhost:{port}/git-status");

                StartFileWatcher(repoPath);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to start WebSocket server");
                Console.WriteLine($"Failed to start WebSocket server: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop the WebSocket server and file watcher.
        /// </summary>
        public void Stop()
        {
            try
            {
                if (!IsRunning)
                {
                    return;
                }

                logger.Info("Stopping WebSocket server");
                
                if (watcher != null)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                    watcher = null;
                }

                if (server != null)
                {
                    server.Stop();
                    server = null;
                }

                IsRunning = false;
                logger.Info("WebSocket server stopped");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error stopping WebSocket server");
            }
        }

        /// <summary>
        /// Start monitoring file changes in the repository.
        /// </summary>
        private void StartFileWatcher(string repoPath)
        {
            try
            {
                if (!Directory.Exists(repoPath))
                {
                    logger.Error($"Repository path does not exist: {repoPath}");
                    return;
                }

                logger.Info($"Starting file watcher for {repoPath}");

                watcher = new FileSystemWatcher
                {
                    Path = repoPath,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    Filter = "*.*",
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true
                };

                watcher.Changed += OnFileChanged;
                watcher.Created += OnFileCreated;
                watcher.Deleted += OnFileDeleted;
                watcher.Renamed += OnFileRenamed;

                logger.Info("File watcher started successfully");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to start file watcher");
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            logger.Debug($"File changed: {e.FullPath}");
            BroadcastChange($"Changed: {e.Name}");
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            logger.Debug($"File created: {e.FullPath}");
            BroadcastChange($"Created: {e.Name}");
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            logger.Debug($"File deleted: {e.FullPath}");
            BroadcastChange($"Deleted: {e.Name}");
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            logger.Debug($"File renamed: {e.OldFullPath} -> {e.FullPath}");
            BroadcastChange($"Renamed: {e.OldName} -> {e.Name}");
        }

        private void BroadcastChange(string message)
        {
            try
            {
                // Broadcast to all connected WebSocket clients
                logger.Debug($"Broadcasting change: {message}");
                
                if (server != null && server.IsListening)
                {
                    server.WebSocketServices.Broadcast(message);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error broadcasting change");
            }
        }
    }

    /// <summary>
    /// WebSocket service for Git status monitoring.
    /// </summary>
    public class GitStatusService : WebSocketBehavior
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private string repoPath;

        public GitStatusService(string repoPath)
        {
            this.repoPath = repoPath;
        }

        public GitStatusService() { }

        protected override void OnOpen()
        {
            logger.Info("WebSocket connection opened");
            Send("Connected to GitDev WebSocket server");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            try
            {
                logger.Debug($"Received WebSocket message: {e.Data}");

                if (string.IsNullOrEmpty(repoPath))
                {
                    Send("Error: Repository path is not set.");
                    return;
                }

                if (!Directory.Exists(repoPath))
                {
                    Send($"Error: Repository path does not exist: {repoPath}");
                    return;
                }

                using (var repo = new Repository(repoPath))
                {
                    var status = repo.RetrieveStatus();
                    var statusMessage = $"Repository Status:\n" +
                                      $"  Modified: {status.Modified.Count()}\n" +
                                      $"  Added: {status.Added.Count()}\n" +
                                      $"  Removed: {status.Removed.Count()}\n" +
                                      $"  Untracked: {status.Untracked.Count()}";
                    
                    Send(statusMessage);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error processing WebSocket message");
                Send($"Error: {ex.Message}");
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            logger.Info($"WebSocket connection closed: {e.Reason}");
        }

        protected override void OnError(ErrorEventArgs e)
        {
            logger.Error(e.Exception, "WebSocket error occurred");
        }

        public void SetRepoPath(string path)
        {
            this.repoPath = path;
        }
    }
}

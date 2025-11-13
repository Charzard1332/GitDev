using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using NLog;
using Octokit;

namespace GitDev.Core
{
    /// <summary>
    /// Manages Git operations with support for batch processing and multi-threading.
    /// </summary>
    public class GitOperationsManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string username;
        private readonly string token;
        private readonly SemaphoreSlim semaphore;

        public GitOperationsManager(string username, string token = null, int maxConcurrentOperations = 3)
        {
            this.username = username ?? throw new ArgumentNullException(nameof(username));
            this.token = token;
            this.semaphore = new SemaphoreSlim(maxConcurrentOperations, maxConcurrentOperations);
        }

        /// <summary>
        /// Initialize a new Git repository.
        /// </summary>
        public bool InitRepository(string repoPath)
        {
            try
            {
                if (string.IsNullOrEmpty(repoPath) || !Directory.Exists(repoPath))
                {
                    logger.Error("Invalid directory path for init operation");
                    Console.WriteLine("Invalid directory path.");
                    return false;
                }

                logger.Info($"Initializing repository at {repoPath}");
                LibGit2Sharp.Repository.Init(repoPath);
                Console.WriteLine("Initialized empty Git repository.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to initialize repository");
                Console.WriteLine($"Failed to initialize repository: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clone a repository with credentials support.
        /// </summary>
        public async Task<bool> CloneRepositoryAsync(string repoUrl, string localPath)
        {
            await semaphore.WaitAsync();
            
            try
            {
                logger.Info($"Cloning repository from {repoUrl} to {localPath}");
                
                var options = new CloneOptions();
                if (!string.IsNullOrEmpty(token))
                {
                    options.FetchOptions.CredentialsProvider = (url, user, cred) =>
                        new UsernamePasswordCredentials { Username = username, Password = token };
                }

                await Task.Run(() => LibGit2Sharp.Repository.Clone(repoUrl, localPath, options));
                
                logger.Info("Repository cloned successfully");
                Console.WriteLine("Repository cloned successfully.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to clone repository");
                Console.WriteLine($"Failed to clone repository: {ex.Message}");
                return false;
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Create a new branch in the repository.
        /// </summary>
        public bool CreateBranch(string repoPath, string branchName)
        {
            try
            {
                logger.Info($"Creating branch {branchName} in {repoPath}");
                
                using (var repo = new LibGit2Sharp.Repository(repoPath))
                {
                    repo.CreateBranch(branchName);
                    Console.WriteLine($"Branch '{branchName}' created successfully.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to create branch {branchName}");
                Console.WriteLine($"Failed to create branch: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Push changes to remote repository with retry logic.
        /// </summary>
        public async Task<bool> PushChangesAsync(string repoPath, string commitMessage, int maxRetries = 3)
        {
            await semaphore.WaitAsync();
            
            int retryCount = 0;
            while (retryCount < maxRetries)
            {
                try
                {
                    logger.Info($"Pushing changes to remote (attempt {retryCount + 1}/{maxRetries})");
                    
                    await Task.Run(() =>
                    {
                        using (var repo = new LibGit2Sharp.Repository(repoPath))
                        {
                            Commands.Stage(repo, "*");
                            
                            var author = new LibGit2Sharp.Signature(username, "email@example.com", DateTime.Now);
                            repo.Commit(commitMessage, author, author);

                            var remote = repo.Network.Remotes["origin"];
                            var options = new PushOptions();
                            
                            if (!string.IsNullOrEmpty(token))
                            {
                                options.CredentialsProvider = (url, user, cred) =>
                                    new UsernamePasswordCredentials { Username = username, Password = token };
                            }
                            
                            repo.Network.Push(remote, repo.Head.CanonicalName, options);
                        }
                    });

                    logger.Info("Changes pushed successfully");
                    Console.WriteLine("Changes pushed successfully.");
                    return true;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    logger.Warn(ex, $"Push attempt {retryCount} failed");
                    
                    if (retryCount >= maxRetries)
                    {
                        logger.Error("Max retry attempts reached for push operation");
                        Console.WriteLine($"Failed to push changes after {maxRetries} attempts: {ex.Message}");
                        return false;
                    }
                    
                    await Task.Delay(1000 * retryCount); // Exponential backoff
                }
                finally
                {
                    if (retryCount >= maxRetries)
                    {
                        semaphore.Release();
                    }
                }
            }
            
            semaphore.Release();
            return false;
        }

        /// <summary>
        /// Pull changes from remote repository.
        /// </summary>
        public async Task<bool> PullChangesAsync(string repoPath)
        {
            await semaphore.WaitAsync();
            
            try
            {
                logger.Info($"Pulling changes from remote for {repoPath}");
                
                await Task.Run(() =>
                {
                    using (var repo = new LibGit2Sharp.Repository(repoPath))
                    {
                        var options = new PullOptions();
                        
                        if (!string.IsNullOrEmpty(token))
                        {
                            options.FetchOptions.CredentialsProvider = (url, user, cred) =>
                                new UsernamePasswordCredentials { Username = username, Password = token };
                        }

                        var signature = new LibGit2Sharp.Signature(username, "email@example.com", DateTime.Now);
                        Commands.Pull(repo, signature, options);
                    }
                });

                logger.Info("Repository updated successfully");
                Console.WriteLine("Repository updated successfully.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to pull changes");
                Console.WriteLine($"Failed to pull changes: {ex.Message}");
                return false;
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Perform rebase operation.
        /// </summary>
        public bool RebaseOnto(string repoPath, string branchName)
        {
            try
            {
                logger.Info($"Rebasing onto {branchName} in {repoPath}");
                
                using (var repo = new LibGit2Sharp.Repository(repoPath))
                {
                    var branch = repo.Branches[branchName];
                    if (branch == null)
                    {
                        logger.Error($"Branch {branchName} not found");
                        Console.WriteLine("Branch not found.");
                        return false;
                    }

                    var rebaseOptions = new RebaseOptions
                    {
                        FileConflictStrategy = CheckoutFileConflictStrategy.Merge
                    };
                    
                    repo.Rebase.Start(repo.Head, branch, repo.Head, 
                        new Identity(username, "email@example.com"), rebaseOptions);
                    
                    logger.Info($"Successfully rebased onto {branchName}");
                    Console.WriteLine($"Successfully rebased onto {branchName}.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Rebase failed");
                Console.WriteLine($"Rebase failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stash uncommitted changes.
        /// </summary>
        public bool StashChanges(string repoPath, string message = "Stashed changes")
        {
            try
            {
                logger.Info($"Stashing changes in {repoPath}");
                
                using (var repo = new LibGit2Sharp.Repository(repoPath))
                {
                    var signature = new LibGit2Sharp.Signature(username, "email@example.com", DateTime.Now);
                    var stash = repo.Stashes.Add(signature, message);
                    
                    logger.Info($"Changes stashed successfully");
                    Console.WriteLine($"Changes stashed successfully.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Stash failed");
                Console.WriteLine($"Stash failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// List all branches in the repository.
        /// </summary>
        public List<string> ListBranches(string repoPath)
        {
            var branches = new List<string>();
            
            try
            {
                logger.Debug($"Listing branches in {repoPath}");
                
                using (var repo = new LibGit2Sharp.Repository(repoPath))
                {
                    foreach (var branch in repo.Branches)
                    {
                        branches.Add(branch.FriendlyName);
                        Console.WriteLine(branch.FriendlyName);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to list branches");
                Console.WriteLine($"Failed to list branches: {ex.Message}");
            }
            
            return branches;
        }

        /// <summary>
        /// Execute batch Git operations concurrently.
        /// </summary>
        public async Task<Dictionary<string, bool>> ExecuteBatchOperationsAsync(
            List<(string operation, string repoPath, Dictionary<string, string> parameters)> operations)
        {
            logger.Info($"Executing {operations.Count} batch operations");
            
            var results = new Dictionary<string, bool>();
            var tasks = new List<Task>();

            foreach (var (operation, repoPath, parameters) in operations)
            {
                tasks.Add(Task.Run(async () =>
                {
                    bool success = false;
                    string key = $"{operation}:{repoPath}";

                    try
                    {
                        switch (operation.ToLower())
                        {
                            case "pull":
                                success = await PullChangesAsync(repoPath);
                                break;
                            case "push":
                                var message = parameters.ContainsKey("message") ? parameters["message"] : "Batch commit";
                                success = await PushChangesAsync(repoPath, message);
                                break;
                            case "stash":
                                var stashMsg = parameters.ContainsKey("message") ? parameters["message"] : "Stashed changes";
                                success = StashChanges(repoPath, stashMsg);
                                break;
                            default:
                                logger.Warn($"Unknown batch operation: {operation}");
                                break;
                        }

                        lock (results)
                        {
                            results[key] = success;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"Batch operation {operation} failed for {repoPath}");
                        lock (results)
                        {
                            results[key] = false;
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
            
            logger.Info($"Batch operations completed. Success: {results.Count(r => r.Value)}/{results.Count}");
            return results;
        }

        /// <summary>
        /// Get repository status.
        /// </summary>
        public string GetRepositoryStatus(string repoPath)
        {
            try
            {
                using (var repo = new LibGit2Sharp.Repository(repoPath))
                {
                    var status = repo.RetrieveStatus();
                    var statusText = $"Modified: {status.Modified.Count()}, Added: {status.Added.Count()}, " +
                                   $"Removed: {status.Removed.Count()}, Untracked: {status.Untracked.Count()}";
                    
                    logger.Debug($"Repository status: {statusText}");
                    return statusText;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to get repository status");
                return $"Error: {ex.Message}";
            }
        }
    }
}

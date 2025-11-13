using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Octokit;

namespace GitDev.Core
{
    /// <summary>
    /// Manages GitHub repository operations through the Octokit API.
    /// </summary>
    public class GitHubRepositoryManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly GitHubClient client;
        private readonly string username;
        
        // Retry configuration for better error handling
        private const int MaxRetries = 3;
        private const int InitialRetryDelayMs = 1000;

        public GitHubRepositoryManager(GitHubClient client, string username)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.username = username ?? throw new ArgumentNullException(nameof(username));
        }
        
        /// <summary>
        /// Execute an API call with retry logic and exponential backoff.
        /// </summary>
        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> apiCall, string operationName)
        {
            int attempt = 0;
            while (attempt < MaxRetries)
            {
                try
                {
                    return await apiCall();
                }
                catch (RateLimitExceededException ex)
                {
                    logger.Warn(ex, $"Rate limit exceeded during {operationName}");
                    
                    if (attempt < MaxRetries - 1)
                    {
                        int delayMs = InitialRetryDelayMs * (int)Math.Pow(2, attempt);
                        logger.Info($"Waiting {delayMs}ms before retry {attempt + 1}/{MaxRetries}");
                        await Task.Delay(delayMs);
                        attempt++;
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                                               ex.StatusCode == System.Net.HttpStatusCode.GatewayTimeout)
                {
                    logger.Warn(ex, $"Temporary error during {operationName}");
                    
                    if (attempt < MaxRetries - 1)
                    {
                        int delayMs = InitialRetryDelayMs * (int)Math.Pow(2, attempt);
                        logger.Info($"Waiting {delayMs}ms before retry {attempt + 1}/{MaxRetries}");
                        await Task.Delay(delayMs);
                        attempt++;
                    }
                    else
                    {
                        throw;
                    }
                }
                catch
                {
                    throw;
                }
            }
            
            throw new InvalidOperationException($"Failed to execute {operationName} after {MaxRetries} attempts");
        }

        /// <summary>
        /// Create a new GitHub repository.
        /// </summary>
        public async Task<bool> CreateRepositoryAsync(string repoName, bool isPrivate = false)
        {
            try
            {
                if (string.IsNullOrEmpty(repoName))
                {
                    logger.Warn("Repository name cannot be empty");
                    Console.WriteLine("Repository name cannot be empty.");
                    return false;
                }

                logger.Info($"Creating repository: {repoName}");
                
                var newRepo = new NewRepository(repoName) { Private = isPrivate };
                var repo = await ExecuteWithRetryAsync(
                    async () => await client.Repository.Create(newRepo),
                    $"create repository {repoName}"
                );
                
                logger.Info($"Repository created successfully: {repo.HtmlUrl}");
                Console.WriteLine($"Repository created: {repo.HtmlUrl}");
                
                return true;
            }
            catch (ForbiddenException ex)
            {
                logger.Error(ex, "Forbidden - check token permissions");
                Console.WriteLine("Error: Forbidden - Check if your token has 'repo' or 'public_repo' permissions.");
                return false;
            }
            catch (AuthorizationException ex)
            {
                logger.Error(ex, "Unauthorized - credentials might be incorrect or expired");
                Console.WriteLine("Error: Unauthorized - Your credentials might be incorrect or expired.");
                return false;
            }
            catch (RateLimitExceededException ex)
            {
                logger.Error(ex, "Rate limit exceeded");
                Console.WriteLine($"Error: GitHub API rate limit exceeded. Resets at {ex.Reset}.");
                return false;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unexpected error creating repository");
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete a GitHub repository.
        /// </summary>
        public async Task<bool> DeleteRepositoryAsync(string repoName)
        {
            try
            {
                if (string.IsNullOrEmpty(repoName))
                {
                    logger.Warn("Repository name cannot be empty");
                    Console.WriteLine("Repository name cannot be empty.");
                    return false;
                }

                logger.Info($"Deleting repository: {repoName}");
                
                await client.Repository.Delete(username, repoName);
                
                logger.Info("Repository deleted successfully");
                Console.WriteLine("Repository deleted successfully.");
                
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to delete repository");
                Console.WriteLine($"Failed to delete repository: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// List all repositories for the authenticated user.
        /// </summary>
        public async Task<List<string>> ListRepositoriesAsync()
        {
            var repoList = new List<string>();
            
            try
            {
                logger.Debug("Fetching user repositories");
                
                var repos = await client.Repository.GetAllForCurrent();
                
                foreach (var repo in repos)
                {
                    repoList.Add($"{repo.Name} - {repo.HtmlUrl}");
                    Console.WriteLine($"  {repo.Name} - {repo.HtmlUrl}");
                }
                
                logger.Info($"Retrieved {repos.Count} repositories");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to list repositories");
                Console.WriteLine($"Failed to list repositories: {ex.Message}");
            }
            
            return repoList;
        }

        /// <summary>
        /// Get repository information.
        /// </summary>
        public async Task<Octokit.Repository> GetRepositoryInfoAsync(string repoName)
        {
            try
            {
                logger.Debug($"Fetching repository info for {repoName}");
                
                var repo = await client.Repository.Get(username, repoName);
                
                logger.Info($"Retrieved repository info for {repoName}");
                return repo;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to get repository info for {repoName}");
                Console.WriteLine($"Failed to get repository info: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create a pull request.
        /// </summary>
        public async Task<bool> CreatePullRequestAsync(string repoName, string title, string head, string baseBranch, string body = "")
        {
            try
            {
                logger.Info($"Creating pull request in {repoName}: {title}");
                
                var newPr = new NewPullRequest(title, head, baseBranch)
                {
                    Body = body
                };
                
                var pr = await client.PullRequest.Create(username, repoName, newPr);
                
                logger.Info($"Pull request created: {pr.HtmlUrl}");
                Console.WriteLine($"Pull request created: {pr.HtmlUrl}");
                
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to create pull request");
                Console.WriteLine($"Failed to create pull request: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// List issues in a repository.
        /// </summary>
        public async Task<List<string>> ListIssuesAsync(string repoName)
        {
            var issueList = new List<string>();
            
            try
            {
                logger.Debug($"Fetching issues for {repoName}");
                
                var issues = await client.Issue.GetAllForRepository(username, repoName);
                
                foreach (var issue in issues)
                {
                    issueList.Add($"#{issue.Number}: {issue.Title} - {issue.State}");
                    Console.WriteLine($"  #{issue.Number}: {issue.Title} - {issue.State}");
                }
                
                logger.Info($"Retrieved {issues.Count} issues");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to list issues");
                Console.WriteLine($"Failed to list issues: {ex.Message}");
            }
            
            return issueList;
        }
        
        /// <summary>
        /// Add a collaborator to a repository.
        /// </summary>
        public async Task<bool> AddCollaboratorAsync(string repoName, string collaboratorUsername, string permission = "push")
        {
            try
            {
                if (string.IsNullOrEmpty(repoName))
                {
                    logger.Warn("Repository name cannot be empty");
                    Console.WriteLine("Repository name cannot be empty.");
                    return false;
                }
                
                if (string.IsNullOrEmpty(collaboratorUsername))
                {
                    logger.Warn("Collaborator username cannot be empty");
                    Console.WriteLine("Collaborator username cannot be empty.");
                    return false;
                }
                
                logger.Info($"Adding collaborator {collaboratorUsername} to {repoName} with {permission} permission");
                
                var response = await ExecuteWithRetryAsync(
                    async () => await client.Repository.Collaborator.Add(username, repoName, collaboratorUsername),
                    $"add collaborator {collaboratorUsername}"
                );
                
                logger.Info($"Collaborator {collaboratorUsername} added successfully");
                Console.WriteLine($"Collaborator {collaboratorUsername} added to {repoName}");
                
                return true;
            }
            catch (ForbiddenException ex)
            {
                logger.Error(ex, "Forbidden - check repository permissions");
                Console.WriteLine("Error: Forbidden - You need admin access to add collaborators.");
                return false;
            }
            catch (NotFoundException ex)
            {
                logger.Error(ex, "Repository or user not found");
                Console.WriteLine("Error: Repository or user not found.");
                return false;
            }
            catch (RateLimitExceededException ex)
            {
                logger.Error(ex, "Rate limit exceeded");
                Console.WriteLine($"Error: GitHub API rate limit exceeded. Resets at {ex.Reset}.");
                return false;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to add collaborator");
                Console.WriteLine($"Failed to add collaborator: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Remove a collaborator from a repository.
        /// </summary>
        public async Task<bool> RemoveCollaboratorAsync(string repoName, string collaboratorUsername)
        {
            try
            {
                if (string.IsNullOrEmpty(repoName))
                {
                    logger.Warn("Repository name cannot be empty");
                    Console.WriteLine("Repository name cannot be empty.");
                    return false;
                }
                
                if (string.IsNullOrEmpty(collaboratorUsername))
                {
                    logger.Warn("Collaborator username cannot be empty");
                    Console.WriteLine("Collaborator username cannot be empty.");
                    return false;
                }
                
                logger.Info($"Removing collaborator {collaboratorUsername} from {repoName}");
                
                await ExecuteWithRetryAsync(
                    async () => {
                        await client.Repository.Collaborator.Delete(username, repoName, collaboratorUsername);
                        return true;
                    },
                    $"remove collaborator {collaboratorUsername}"
                );
                
                logger.Info($"Collaborator {collaboratorUsername} removed successfully");
                Console.WriteLine($"Collaborator {collaboratorUsername} removed from {repoName}");
                
                return true;
            }
            catch (ForbiddenException ex)
            {
                logger.Error(ex, "Forbidden - check repository permissions");
                Console.WriteLine("Error: Forbidden - You need admin access to remove collaborators.");
                return false;
            }
            catch (NotFoundException ex)
            {
                logger.Error(ex, "Repository or user not found");
                Console.WriteLine("Error: Repository or user not found.");
                return false;
            }
            catch (RateLimitExceededException ex)
            {
                logger.Error(ex, "Rate limit exceeded");
                Console.WriteLine($"Error: GitHub API rate limit exceeded. Resets at {ex.Reset}.");
                return false;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to remove collaborator");
                Console.WriteLine($"Failed to remove collaborator: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// List all collaborators for a repository.
        /// </summary>
        public async Task<List<string>> ListCollaboratorsAsync(string repoName)
        {
            var collaboratorList = new List<string>();
            
            try
            {
                if (string.IsNullOrEmpty(repoName))
                {
                    logger.Warn("Repository name cannot be empty");
                    Console.WriteLine("Repository name cannot be empty.");
                    return collaboratorList;
                }
                
                logger.Debug($"Fetching collaborators for {repoName}");
                
                var collaborators = await ExecuteWithRetryAsync(
                    async () => await client.Repository.Collaborator.GetAll(username, repoName),
                    $"list collaborators for {repoName}"
                );
                
                Console.WriteLine($"\nCollaborators for {repoName}:");
                foreach (var collaborator in collaborators)
                {
                    string permissionInfo = collaborator.Permissions != null 
                        ? $" (Admin: {collaborator.Permissions.Admin}, Push: {collaborator.Permissions.Push}, Pull: {collaborator.Permissions.Pull})"
                        : "";
                    
                    collaboratorList.Add($"{collaborator.Login}{permissionInfo}");
                    Console.WriteLine($"  {collaborator.Login}{permissionInfo}");
                }
                
                logger.Info($"Retrieved {collaborators.Count} collaborators");
            }
            catch (ForbiddenException ex)
            {
                logger.Error(ex, "Forbidden - check repository permissions");
                Console.WriteLine("Error: Forbidden - You need access to view collaborators.");
            }
            catch (NotFoundException ex)
            {
                logger.Error(ex, "Repository not found");
                Console.WriteLine("Error: Repository not found.");
            }
            catch (RateLimitExceededException ex)
            {
                logger.Error(ex, "Rate limit exceeded");
                Console.WriteLine($"Error: GitHub API rate limit exceeded. Resets at {ex.Reset}.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to list collaborators");
                Console.WriteLine($"Failed to list collaborators: {ex.Message}");
            }
            
            return collaboratorList;
        }
    }
}

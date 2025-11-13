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

        public GitHubRepositoryManager(GitHubClient client, string username)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.username = username ?? throw new ArgumentNullException(nameof(username));
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
                var repo = await client.Repository.Create(newRepo);
                
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
    }
}

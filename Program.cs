﻿using System;
using System.IO;
using System.Net;
using System.Text;
using LibGit2Sharp;
using Octokit;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

class GitDev
{
    static GitHubClient client;
    static string username;

    static string clientId = "YOUR_GITHUB_CLIENT_ID";
    static string clientSecret = "YOUR_GITHUB_CLIENT_SECRET";
    static string redirectUri = "http://localhost:5000/callback";

    static async Task Main()
    {
        await AuthenticateUser();

        Console.Title = "GitDev Login";
        Console.WriteLine($"Welcome, @{username}/root");
        Console.Clear();

        while (true)
        {
            Console.Clear();
            Console.Title = $"GitDev Logged In - {username}";
            Console.Write($"@{username}/root: ");
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
        Console.WriteLine("  dev pull - Pull changes");
        Console.WriteLine("  dev list - List branches");
        Console.WriteLine("  dev status - Show repository status");
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
                Console.WriteLine("Authentication failed. No authorization code received.");
                return;
            }

            response.StatusCode = 200;
            byte[] buffer = Encoding.UTF8.GetBytes("Authentication successful! You can now close this window!");
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
                    Console.WriteLine("Error: Unable to retrieve access token.");
                    return null;
                }

                string token = responseString.Split('&')[0].Split('=')[1];
                return token;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exchanging code for token: {ex.Message}");
                return null;
            }
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
                    Console.WriteLine("Invalid directory path.");
                    return;
                }
                LibGit2Sharp.Repository.Init(repoPath);
                Console.WriteLine("Initialized empty Git repository.");
                break;
            case "create-repo":
                Console.Write("Enter new repository name: ");
                string repoName = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(repoName))
                {
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
                    Console.WriteLine("Repository name cannot be empty.");
                    return;
                }
                await DeleteGitHubRepo(repoToDelete);
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
            Console.WriteLine("Error: Forbidden - Check if your token has 'repo' or 'public_repo' permissions.");
            Console.WriteLine($"Exception: {ex.Message}");
            Thread.Sleep(5000);
        }
        catch (AuthorizationException ex)
        {
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
}

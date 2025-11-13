# GitDev Usage Guide

Welcome to the comprehensive GitDev usage guide! This document provides detailed instructions and examples for using all features of GitDev.

## Table of Contents
1. [Getting Started](#getting-started)
2. [Repository Operations](#repository-operations)
3. [Collaborator Management](#collaborator-management)
4. [Branch Operations](#branch-operations)
5. [Git Operations](#git-operations)
6. [Batch Operations](#batch-operations)
7. [Plugin System](#plugin-system)
8. [WebSocket Server](#websocket-server)
9. [Configuration](#configuration)
10. [Error Handling](#error-handling)
11. [Best Practices](#best-practices)

## Getting Started

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/Charzard1332/GitDev.git
   cd GitDev
   ```

2. Configure your GitHub OAuth credentials in `Program.cs`:
   ```csharp
   private const string CLIENT_ID = "YOUR_GITHUB_CLIENT_ID";
   private const string CLIENT_SECRET = "YOUR_GITHUB_CLIENT_SECRET";
   ```

3. Build and run:
   ```bash
   dotnet build
   dotnet run
   ```

### First Time Setup
When you first run GitDev, you'll be prompted to authenticate with GitHub using OAuth. Follow the on-screen instructions to complete the authentication process.

## Repository Operations

### Creating a Repository
Create a new GitHub repository with optional privacy settings.

**Command:**
```
create-repo <repository-name> [private]
```

**Examples:**
```bash
# Create a public repository
create-repo my-awesome-project

# Create a private repository
create-repo my-secret-project private
```

### Deleting a Repository
Delete an existing GitHub repository (requires confirmation).

**Command:**
```
delete-repo <repository-name>
```

**Example:**
```bash
delete-repo old-project
```

### Listing Repositories
View all repositories associated with your GitHub account.

**Command:**
```
list-repos
```

**Example Output:**
```
  my-project - https://github.com/username/my-project
  another-repo - https://github.com/username/another-repo
  test-repo - https://github.com/username/test-repo
✓ Found 3 repositories
```

### Cloning a Repository
Clone a repository to your local machine.

**Command:**
```
clone <repository-url> <local-path>
```

**Example:**
```bash
clone https://github.com/username/my-project C:/Projects/my-project
```

### Initializing a Local Repository
Initialize a new Git repository in a local directory.

**Command:**
```
init [path]
```

**Example:**
```bash
init C:/Projects/new-project
```

## Collaborator Management

GitDev now includes comprehensive collaborator management features with automatic retry logic and better error handling.

### Adding a Collaborator
Invite a user to collaborate on your repository.

**Command:**
```
add-collaborator <repository-name> <username> [permission]
```

**Parameters:**
- `repository-name`: Name of your repository
- `username`: GitHub username of the collaborator
- `permission`: Optional. Access level (push, pull, admin). Default is "push"

**Examples:**
```bash
# Add a collaborator with push access (default)
add-collaborator my-project johndoe

# Add a collaborator with specific permission
add-collaborator my-project janedoe admin
```

**Error Handling:**
- Automatically retries on temporary network failures
- Handles rate limiting with exponential backoff
- Provides clear error messages for permission issues

### Removing a Collaborator
Remove a collaborator from your repository.

**Command:**
```
remove-collaborator <repository-name> <username>
```

**Example:**
```bash
remove-collaborator my-project johndoe
```

**Note:** This command requires confirmation before removing the collaborator.

### Listing Collaborators
View all collaborators for a specific repository with their permission levels.

**Command:**
```
list-collaborators <repository-name>
```

**Example:**
```bash
list-collaborators my-project
```

**Example Output:**
```
Collaborators for my-project:
  johndoe (Admin: False, Push: True, Pull: True)
  janedoe (Admin: True, Push: True, Pull: True)
  contributor (Admin: False, Push: False, Pull: True)
✓ Found 3 collaborators
```

## Branch Operations

### Creating a Branch
Create a new branch in your repository.

**Command:**
```
branch <branch-name>
```

**Example:**
```bash
branch feature/new-feature
```

### Listing Branches
View all branches in a repository.

**Command:**
```
list-branches [repository-path]
```

**Example:**
```bash
list-branches C:/Projects/my-project
```

### Merging Branches
Merge a branch into your current branch.

**Command:**
```
merge <branch-name>
```

**Example:**
```bash
merge feature/new-feature
```

## Git Operations

### Checking Repository Status
View the current status of a Git repository.

**Command:**
```
status <repository-path>
```

**Example:**
```bash
status C:/Projects/my-project
```

### Pushing Changes
Commit and push changes to the remote repository.

**Command:**
```
push <repository-path> <commit-message>
```

**Example:**
```bash
push C:/Projects/my-project "Add new feature"
```

### Pulling Changes
Pull the latest changes from the remote repository.

**Command:**
```
pull <repository-path>
```

**Example:**
```bash
pull C:/Projects/my-project
```

### Stashing Changes
Save uncommitted changes for later use.

**Command:**
```
stash <repository-path> [message]
```

**Example:**
```bash
stash C:/Projects/my-project "Work in progress"
```

### Rebasing
Rebase your current branch onto another branch.

**Command:**
```
rebase <repository-path> <target-branch>
```

**Example:**
```bash
rebase C:/Projects/my-project main
```

## Batch Operations

Perform operations on multiple repositories concurrently for improved efficiency.

### Batch Pull
Pull changes from multiple repositories at once.

**Command:**
```
batch-pull <path1> <path2> <path3> ...
```

**Example:**
```bash
batch-pull C:/Projects/repo1 C:/Projects/repo2 C:/Projects/repo3
```

### Batch Push
Push changes to multiple repositories concurrently.

**Command:**
```
batch-push <path1> <path2> <path3> ...
```

**Example:**
```bash
batch-push C:/Projects/repo1 C:/Projects/repo2 C:/Projects/repo3
```

**Performance Note:** GitDev uses multi-threading with configurable concurrency limits to optimize batch operations.

## Plugin System

GitDev supports a modular plugin architecture for extending functionality.

### Listing Plugins
View all currently loaded plugins.

**Command:**
```
plugin-list
```

**Example Output:**
```
Loaded Plugins:
  ID: greeting-plugin
  Name: Greeting Plugin
  Version: 1.0.0
  Description: A simple greeting plugin
```

### Getting Plugin Information
View detailed information about a specific plugin.

**Command:**
```
plugin-info <plugin-id>
```

**Example:**
```bash
plugin-info greeting-plugin
```

### Running a Plugin
Execute a loaded plugin.

**Command:**
```
plugin-run <plugin-id>
```

**Example:**
```bash
plugin-run greeting-plugin
```

### Loading a Plugin
Load a plugin from a DLL file.

**Command:**
```
plugin-load <plugin-path>
```

**Example:**
```bash
plugin-load C:/Plugins/MyCustomPlugin.dll
```

### Unloading a Plugin
Unload a currently loaded plugin.

**Command:**
```
plugin-unload <plugin-id>
```

**Example:**
```bash
plugin-unload greeting-plugin
```

## WebSocket Server

GitDev includes a WebSocket server for real-time repository monitoring.

### Starting the WebSocket Server
Start monitoring a repository via WebSocket.

**Command:**
```
start-ws [repository-path]
```

**Example:**
```bash
start-ws C:/Projects/my-project
```

**Connection Details:**
- Default WebSocket URL: `ws://localhost:8080/`
- Sends real-time updates about repository changes

### Stopping the WebSocket Server
Stop the WebSocket monitoring server.

**Command:**
```
stop-ws
```

**Example:**
```bash
stop-ws
```

## Configuration

### Viewing Current Configuration
Display the current GitDev configuration.

**Command:**
```
config
```

**Configuration Options:**
- `MaxConcurrentOperations`: Maximum number of concurrent batch operations (default: 3)
- Additional settings can be configured via the ConfigurationManager

### Modifying Configuration
Configuration can be modified programmatically or through configuration files. The settings persist across sessions.

## Error Handling

GitDev implements comprehensive error handling with automatic retry logic:

### Retry Mechanism
- **Automatic Retries:** Failed API requests are automatically retried up to 3 times
- **Exponential Backoff:** Retry delays increase exponentially (1s, 2s, 4s)
- **Rate Limit Handling:** Automatically handles GitHub API rate limits

### Error Types and Solutions

#### Rate Limit Exceeded
**Error:** "GitHub API rate limit exceeded"
**Solution:** Wait until the rate limit resets (time shown in error message)

#### Authentication Errors
**Error:** "Unauthorized - Your credentials might be incorrect or expired"
**Solution:** 
1. Check your OAuth credentials
2. Ensure your access token has the required permissions
3. Re-authenticate if necessary

#### Permission Errors
**Error:** "Forbidden - Check if your token has 'repo' permissions"
**Solution:**
1. Verify your GitHub token has appropriate permissions
2. For collaborator operations, ensure you have admin access to the repository

#### Network Errors
**Error:** Temporary connection failures
**Solution:** GitDev automatically retries with exponential backoff. If the issue persists, check your internet connection.

## Best Practices

### 1. Regular Updates
Keep GitDev updated to benefit from the latest features and security improvements.

### 2. Secure Credentials
- Never commit your OAuth credentials to version control
- Use environment variables or secure configuration files for sensitive data

### 3. Batch Operations
Use batch operations when working with multiple repositories to save time:
```bash
batch-pull C:/Projects/* 
```

### 4. Plugin Security
- Only load plugins from trusted sources
- Review plugin code before loading
- Use the built-in plugin security validation

### 5. Error Monitoring
- Check logs for detailed error information (NLog.config)
- Monitor API rate limits when performing many operations

### 6. Collaborator Management
- Regularly review repository collaborators
- Use appropriate permission levels (push vs admin)
- Remove collaborators who no longer need access

### 7. WebSocket Monitoring
- Use WebSocket monitoring for active development
- Stop the WebSocket server when not needed to conserve resources

## Command History

GitDev maintains a command history for easy reference:

**Command:**
```
history
```

This shows your recent commands, making it easy to repeat or modify previous operations.

## Getting Help

- Type `help` in GitDev to see all available commands
- Refer to the documentation files:
  - `README.md`: Overview and setup
  - `PLUGIN_DEVELOPMENT.md`: Plugin development guide
  - `PLUGIN_SECURITY.md`: Plugin security information
  - `TODO.md`: Upcoming features and improvements

## Troubleshooting

### Issue: Command Not Found
**Solution:** Type `help` to see the list of available commands

### Issue: Repository Not Found
**Solution:** Verify the repository path is correct and you have access permissions

### Issue: Authentication Failed
**Solution:** 
1. Check your CLIENT_ID and CLIENT_SECRET
2. Ensure you completed the OAuth flow
3. Try restarting GitDev

### Issue: Plugin Won't Load
**Solution:**
1. Verify the plugin DLL is compatible
2. Check that the plugin implements the IPlugin interface
3. Review plugin security settings

## Advanced Usage

### Custom Plugin Development
See `PLUGIN_DEVELOPMENT.md` for comprehensive guide on creating custom plugins.

### Multi-threading Configuration
Adjust the maximum concurrent operations in the configuration:
```csharp
configManager.SetValue("MaxConcurrentOperations", 5);
```

### Logging Configuration
Modify `NLog.config` to customize logging levels and output destinations.

## Support and Contribution

- **Issues:** Report bugs or request features on GitHub
- **Contributions:** Pull requests are welcome!
- **Documentation:** Help improve this guide by submitting updates

---

**Note:** This guide is for GitDev version 1.0+. Some features may vary in earlier versions.

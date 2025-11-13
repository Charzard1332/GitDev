# 🚀 GitDev - Advanced

GitDev is an advanced command-line tool that allows users to interact with GitHub repositories using Octokit and LibGit2Sharp. It features a modular architecture, multi-threading support, and an enhanced interactive CLI for improved developer experience.

## ✨ Features

### Core Functionality
- 🔑 **OAuth-based GitHub authentication** with improved error handling
- 📂 **Repository management** - Create, delete, clone, and list repositories
- 👥 **Collaborator management** - Add, remove, and list repository collaborators
- 🌿 **Branch management** - Create, merge, rebase, and list branches
- ⚡ **Git operations** - Push, pull, stash, status checks with retry logic
- 🔌 **Plugin system** - Extend functionality with custom plugins
- 🎨 **Interactive CLI** - Color-coded interface with command history

### Advanced Features
- 🔄 **Multi-threading** - Concurrent Git operations with configurable thread pool
- 📦 **Batch operations** - Execute multiple Git commands simultaneously
- 🌐 **WebSocket server** - Real-time repository monitoring
- ⚙️ **Configuration management** - Persistent settings and preferences
- 📊 **Enhanced logging** - Structured logging with NLog
- 🔒 **Thread-safe operations** - Semaphore-based concurrency control
- 🔁 **Automatic retry logic** - Exponential backoff for API failures and rate limits

## 🏗️ Architecture

GitDev uses a modular architecture with the following components:

### Core Namespace Components
- **AuthenticationManager** - Handles OAuth flow and GitHub authentication
- **GitOperationsManager** - Manages Git operations with multi-threading support
- **GitHubRepositoryManager** - Manages GitHub API operations
- **InteractiveCLI** - Provides enhanced command-line interface
- **WebSocketServerManager** - Real-time repository monitoring
- **ConfigurationManager** - Application configuration persistence

## 📌 Commands

### Repository Operations
- `init` - Initialize a new Git repository
- `clone <url> <path>` - Clone a repository
- `create-repo <name> [private]` - Create a new GitHub repository
- `delete-repo <name>` - Delete a GitHub repository
- `list-repos` - List all your GitHub repositories

### Collaborator Operations
- `add-collaborator <repo> <user> [permission]` - Add a collaborator to a repository
- `remove-collaborator <repo> <user>` - Remove a collaborator from a repository
- `list-collaborators <repo>` - List all collaborators of a repository

### Branch Operations
- `branch <name>` - Create a new branch
- `list-branches [path]` - List all branches

### Git Operations
- `status [path]` - Show repository status
- `push <path> <message>` - Commit and push changes
- `pull <path>` - Pull latest changes
- `stash <path> [message]` - Stash uncommitted changes
- `rebase <path> <branch>` - Rebase onto a branch

### Batch Operations
- `batch-pull <paths...>` - Pull changes for multiple repos concurrently
- `batch-push <paths...>` - Push changes for multiple repos concurrently

### WebSocket Server
- `start-ws [path]` - Start WebSocket server for real-time monitoring
- `stop-ws` - Stop WebSocket server

### Plugin Commands
- `plugin-list` - List all loaded plugins
- `plugin-info <id>` - Get information about a plugin
- `plugin-run <id>` - Execute a plugin
- `plugin-load <path>` - Load a plugin from a file
- `plugin-unload <id>` - Unload a plugin

### Utility Commands
- `help` - Show all available commands
- `history` - Show command history
- `config` - Display current configuration
- `clear` - Clear the screen
- `exit` - Exit GitDev

## 🛠 Prerequisites
- 🏗 .NET 6.0 or later
- 🧑‍💻 A GitHub account
- 🔑 A personal access token with `repo` scope (if not using OAuth authentication)

## 📥 Installation
1. 📥 Clone the repository:
   ```sh
   git clone https://github.com/yourusername/GitHubShell.git
   ```
2. ClientID and Secret
   ```cs
    static string clientId = "YOUR_GITHUB_CLIENT_ID";
    static string clientSecret = "YOUR_GITHUB_CLIENT_SECRET";
    static string redirectUri = "http://localhost:5000/callback";
   ```
## Error Debugging
- ⚠️ If you receive an authentication error, verify that your OAuth token or PAT has repo permissions.
- 🚫 If repository creation fails with a `ForbiddenException`, ensure you have the necessary permissions in your GitHub settings.

## 🔌 Plugin System

GitDev supports a modular plugin architecture that allows you to extend its functionality. 

### Creating Plugins

1. Implement the `IPlugin` interface
2. Compile your plugin as a .NET DLL
3. Place the DLL in the `plugins` directory
4. Reload or restart GitDev

For detailed instructions, see [PLUGIN_DEVELOPMENT.md](PLUGIN_DEVELOPMENT.md).

### Security

The plugin system includes basic security measures:
- File validation (size, extension)
- Optional hash-based whitelisting
- Error isolation

For more information, see [PLUGIN_SECURITY.md](PLUGIN_SECURITY.md).

## 📝 License
This project is open-source and available under the **MIT License**.

---

## 🌟 Contributing
Feel free to submit **issues** or **pull requests** to improve the project!

---

## 📌 Disclaimer
This project is for **educational purposes** and is **not affiliated with GitHub** in any way.

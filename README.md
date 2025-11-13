# 🚀 GitDev

GitDev is a command-line tool that allows users to interact with GitHub repositories using Octokit and LibGit2Sharp. It provides functionalities such as authentication, repository management, and Git operations from the terminal.

## ✨ Features
- 🔑 OAuth-based GitHub authentication
- 📂 Create, delete, and clone repositories
- 🌿 Branch management (create, merge, list branches)
- ⚡ Git operations like push, pull, and status checks
- 🔌 Plugin system for extending functionality

## 📌 Commands
- 🏗 `dev init` - Initialize a new repo
- 📥 `dev clone <repo-url>` - Clone a repo
- 🆕 `dev create-repo <repo-name>` - Create a new git repo
- ❌ `dev delete-repo <repo-name>` - Delete a git repo
- 🌿 `dev branch <branch-name>` - Create a branch
- 🔀 `dev merge <branch-name>` - Merge a branch
- 📤 `dev push <commit-msg>` - Push changes
- 🏗 `dev stash` - Stash changes
- 🏗 `dev rebase` - Rebase branch
- 📥 `dev pull` - Pull latest changes
- 📜 `dev list` - List branches
- 🔍 `dev status` - Shows repo status

### 🔌 Plugin Commands
- 📋 `dev plugin-list` - List all loaded plugins
- ℹ️ `dev plugin-info <id>` - Get plugin information
- ▶️ `dev plugin-run <id>` - Execute a plugin
- 📥 `dev plugin-load <path>` - Load a plugin from file
- ❌ `dev plugin-unload <id>` - Unload a plugin

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

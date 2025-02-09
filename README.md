# 🚀 GitDev

GitDev is a command-line tool that allows users to interact with GitHub repositories using Octokit and LibGit2Sharp. It provides functionalities such as authentication, repository management, and Git operations from the terminal.

## ✨ Features
- 🔑 OAuth-based GitHub authentication
- 📂 Create, delete, and clone repositories
- 🌿 Branch management (create, merge, list branches)
- ⚡ Git operations like push, pull, and status checks

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

## 📝 License
This project is open-source and available under the **MIT License**.

---

## 🌟 Contributing
Feel free to submit **issues** or **pull requests** to improve the project!

---

## 📌 Disclaimer
This project is for **educational purposes** and is **not affiliated with GitHub** in any way.

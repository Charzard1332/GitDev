using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using NLog;
using GitDev.Core;

namespace GitDev.Tests
{
    /// <summary>
    /// Unit tests for the Core namespace components.
    /// </summary>
    public class CoreComponentsTests
    {
        private readonly string _testDirectory;

        public CoreComponentsTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "GitDevCoreTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        // ==================== ConfigurationManager Tests ====================

        [Fact]
        public void ConfigurationManager_ShouldCreateDefaultConfiguration()
        {
            // Arrange & Act
            var config = new ConfigurationManager("test_config.json");

            // Assert
            Assert.True(config.GetValue("MaxConcurrentOperations", 0) > 0);
            Assert.True(config.GetValue("WebSocketPort", 0) > 0);
        }

        [Fact]
        public void ConfigurationManager_SetAndGetValue_ShouldWork()
        {
            // Arrange
            var config = new ConfigurationManager("test_config_2.json");

            // Act
            config.SetValue("TestKey", "TestValue");
            var value = config.GetValue<string>("TestKey");

            // Assert
            Assert.Equal("TestValue", value);
        }

        [Fact]
        public void ConfigurationManager_GetValue_ShouldReturnDefaultForNonExistentKey()
        {
            // Arrange
            var config = new ConfigurationManager("test_config_3.json");

            // Act
            var value = config.GetValue("NonExistentKey", "DefaultValue");

            // Assert
            Assert.Equal("DefaultValue", value);
        }

        [Fact]
        public void ConfigurationManager_HasKey_ShouldReturnCorrectResult()
        {
            // Arrange
            var config = new ConfigurationManager("test_config_4.json");
            config.SetValue("ExistingKey", "Value");

            // Act & Assert
            Assert.True(config.HasKey("ExistingKey"));
            Assert.False(config.HasKey("NonExistentKey"));
        }

        [Fact]
        public void ConfigurationManager_RemoveValue_ShouldWork()
        {
            // Arrange
            var config = new ConfigurationManager("test_config_5.json");
            config.SetValue("KeyToRemove", "Value");

            // Act
            config.RemoveValue("KeyToRemove");

            // Assert
            Assert.False(config.HasKey("KeyToRemove"));
        }

        [Fact]
        public void ConfigurationManager_SaveAndLoad_ShouldPersist()
        {
            // Arrange
            var configFile = "test_config_6.json";
            var config1 = new ConfigurationManager(configFile);
            config1.SetValue("PersistentKey", "PersistentValue");
            config1.SaveConfiguration();

            // Act
            var config2 = new ConfigurationManager(configFile);

            // Assert
            Assert.Equal("PersistentValue", config2.GetValue<string>("PersistentKey", ""));
        }

        // ==================== InteractiveCLI Tests ====================

        [Fact]
        public void InteractiveCLI_ParseCommand_ShouldSplitCorrectly()
        {
            // Arrange
            var cli = new InteractiveCLI();

            // Act
            var (command, args) = cli.ParseCommand("clone http://example.com /path/to/repo");

            // Assert
            Assert.Equal("clone", command);
            Assert.Equal(2, args.Count);
            Assert.Equal("http://example.com", args[0]);
            Assert.Equal("/path/to/repo", args[1]);
        }

        [Fact]
        public void InteractiveCLI_ParseCommand_ShouldHandleEmptyInput()
        {
            // Arrange
            var cli = new InteractiveCLI();

            // Act
            var (command, args) = cli.ParseCommand("");

            // Assert
            Assert.Equal(string.Empty, command);
            Assert.Empty(args);
        }

        [Fact]
        public void InteractiveCLI_ParseCommand_ShouldHandleCommandOnly()
        {
            // Arrange
            var cli = new InteractiveCLI();

            // Act
            var (command, args) = cli.ParseCommand("help");

            // Assert
            Assert.Equal("help", command);
            Assert.Empty(args);
        }

        [Fact]
        public void InteractiveCLI_ParseCommand_ShouldHandleMultipleSpaces()
        {
            // Arrange
            var cli = new InteractiveCLI();

            // Act
            var (command, args) = cli.ParseCommand("clone    http://example.com    /path");

            // Assert
            Assert.Equal("clone", command);
            Assert.Equal(2, args.Count);
        }

        [Fact]
        public void InteractiveCLI_ParseCommand_ShouldBeCaseInsensitive()
        {
            // Arrange
            var cli = new InteractiveCLI();

            // Act
            var (command, args) = cli.ParseCommand("CLONE http://example.com");

            // Assert
            Assert.Equal("clone", command);
        }

        // ==================== GitOperationsManager Tests ====================

        [Fact]
        public void GitOperationsManager_ShouldThrowOnNullUsername()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new GitOperationsManager(null));
        }

        [Fact]
        public void GitOperationsManager_InitRepository_ShouldReturnFalseForInvalidPath()
        {
            // Arrange
            var manager = new GitOperationsManager("testuser");
            var invalidPath = Path.Combine(_testDirectory, "nonexistent");

            // Act
            var result = manager.InitRepository(invalidPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GitOperationsManager_InitRepository_ShouldSucceedForValidPath()
        {
            // Arrange
            var manager = new GitOperationsManager("testuser");
            var validPath = Path.Combine(_testDirectory, "newrepo");
            Directory.CreateDirectory(validPath);

            // Act
            var result = manager.InitRepository(validPath);

            // Assert
            Assert.True(result);
            Assert.True(Directory.Exists(Path.Combine(validPath, ".git")));

            // Cleanup
            Directory.Delete(validPath, true);
        }

        [Fact]
        public void GitOperationsManager_GetRepositoryStatus_ShouldHandleInvalidPath()
        {
            // Arrange
            var manager = new GitOperationsManager("testuser");
            var invalidPath = Path.Combine(_testDirectory, "nonexistent");

            // Act
            var status = manager.GetRepositoryStatus(invalidPath);

            // Assert
            Assert.Contains("Error", status);
        }

        // ==================== WebSocketServerManager Tests ====================

        [Fact]
        public void WebSocketServerManager_ShouldInitializeWithDefaultPort()
        {
            // Arrange & Act
            var manager = new WebSocketServerManager();

            // Assert
            Assert.False(manager.IsRunning);
        }

        [Fact]
        public void WebSocketServerManager_ShouldInitializeWithCustomPort()
        {
            // Arrange & Act
            var manager = new WebSocketServerManager(5002);

            // Assert
            Assert.False(manager.IsRunning);
        }

        [Fact]
        public void WebSocketServerManager_Stop_ShouldHandleNotRunningGracefully()
        {
            // Arrange
            var manager = new WebSocketServerManager();

            // Act & Assert (should not throw)
            manager.Stop();
            Assert.False(manager.IsRunning);
        }

        // ==================== AuthenticationManager Tests ====================

        [Fact]
        public void AuthenticationManager_ShouldThrowOnNullClientId()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AuthenticationManager(null, "secret", "uri"));
        }

        [Fact]
        public void AuthenticationManager_ShouldThrowOnNullClientSecret()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AuthenticationManager("id", null, "uri"));
        }

        [Fact]
        public void AuthenticationManager_ShouldThrowOnNullRedirectUri()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AuthenticationManager("id", "secret", null));
        }

        [Fact]
        public void AuthenticationManager_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var manager = new AuthenticationManager("id", "secret", "uri");

            // Assert
            Assert.Null(manager.Client);
            Assert.Null(manager.Username);
        }

        // ==================== GitHubRepositoryManager Tests ====================

        [Fact]
        public void GitHubRepositoryManager_ShouldThrowOnNullClient()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new GitHubRepositoryManager(null, "username"));
        }

        [Fact]
        public void GitHubRepositoryManager_ShouldThrowOnNullUsername()
        {
            // Arrange
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("test"));

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new GitHubRepositoryManager(client, null));
        }

        [Fact]
        public async Task GitHubRepositoryManager_CreateRepository_ShouldReturnFalseForEmptyName()
        {
            // Arrange
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("test"));
            var manager = new GitHubRepositoryManager(client, "testuser");

            // Act
            var result = await manager.CreateRepositoryAsync("");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GitHubRepositoryManager_DeleteRepository_ShouldReturnFalseForEmptyName()
        {
            // Arrange
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("test"));
            var manager = new GitHubRepositoryManager(client, "testuser");

            // Act
            var result = await manager.DeleteRepositoryAsync("");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GitHubRepositoryManager_AddCollaborator_ShouldReturnFalseForEmptyRepoName()
        {
            // Arrange
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("test"));
            var manager = new GitHubRepositoryManager(client, "testuser");

            // Act
            var result = await manager.AddCollaboratorAsync("", "collaborator");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GitHubRepositoryManager_AddCollaborator_ShouldReturnFalseForEmptyUsername()
        {
            // Arrange
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("test"));
            var manager = new GitHubRepositoryManager(client, "testuser");

            // Act
            var result = await manager.AddCollaboratorAsync("repo", "");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GitHubRepositoryManager_RemoveCollaborator_ShouldReturnFalseForEmptyRepoName()
        {
            // Arrange
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("test"));
            var manager = new GitHubRepositoryManager(client, "testuser");

            // Act
            var result = await manager.RemoveCollaboratorAsync("", "collaborator");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GitHubRepositoryManager_RemoveCollaborator_ShouldReturnFalseForEmptyUsername()
        {
            // Arrange
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("test"));
            var manager = new GitHubRepositoryManager(client, "testuser");

            // Act
            var result = await manager.RemoveCollaboratorAsync("repo", "");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GitHubRepositoryManager_ListCollaborators_ShouldReturnEmptyListForEmptyRepoName()
        {
            // Arrange
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("test"));
            var manager = new GitHubRepositoryManager(client, "testuser");

            // Act
            var result = await manager.ListCollaboratorsAsync("");

            // Assert
            Assert.Empty(result);
        }

        // Cleanup
        ~CoreComponentsTests()
        {
            try
            {
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}

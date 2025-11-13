using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using GitDev.Plugins;
using NLog;

namespace GitDev.Tests
{
    /// <summary>
    /// Unit tests for the plugin system.
    /// </summary>
    public class PluginSystemTests
    {
        private readonly string _testPluginDirectory;

        public PluginSystemTests()
        {
            _testPluginDirectory = Path.Combine(Path.GetTempPath(), "GitDevPluginTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testPluginDirectory);
        }

        [Fact]
        public void PluginMetadata_ToString_ReturnsFormattedString()
        {
            // Arrange
            var metadata = new PluginMetadata
            {
                Name = "Test Plugin",
                Version = "1.0.0",
                Author = "Test Author"
            };

            // Act
            var result = metadata.ToString();

            // Assert
            Assert.Equal("Test Plugin v1.0.0 by Test Author", result);
        }

        [Fact]
        public void PluginResult_Successful_CreatesSuccessfulResult()
        {
            // Arrange & Act
            var result = PluginResult.Successful("Operation completed", new { Value = 42 });

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Operation completed", result.Message);
            Assert.NotNull(result.Data);
            Assert.Null(result.Error);
        }

        [Fact]
        public void PluginResult_Failed_CreatesFailedResult()
        {
            // Arrange
            var exception = new Exception("Test exception");

            // Act
            var result = PluginResult.Failed("Operation failed", exception);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Operation failed", result.Message);
            Assert.Equal(exception, result.Error);
        }

        [Fact]
        public void PluginContext_GetConfigValue_ReturnsNullForNonExistentKey()
        {
            // Arrange
            var logger = LogManager.GetCurrentClassLogger();
            var context = new PluginContext("1.0.0", _testPluginDirectory, logger);

            // Act
            var value = context.GetConfigValue("nonexistent");

            // Assert
            Assert.Null(value);
        }

        [Fact]
        public void PluginContext_SetAndGetConfigValue_WorksCorrectly()
        {
            // Arrange
            var logger = LogManager.GetCurrentClassLogger();
            var context = new PluginContext("1.0.0", _testPluginDirectory, logger);

            // Act
            context.SetConfigValue("testKey", "testValue");
            var value = context.GetConfigValue("testKey");

            // Assert
            Assert.Equal("testValue", value);
        }

        [Fact]
        public void PluginSecurity_ValidatePlugin_ReturnsFalseForNonExistentFile()
        {
            // Arrange
            var security = new PluginSecurity();
            var nonExistentPath = Path.Combine(_testPluginDirectory, "nonexistent.dll");

            // Act
            var result = security.ValidatePlugin(nonExistentPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PluginSecurity_ValidatePlugin_ReturnsFalseForInvalidExtension()
        {
            // Arrange
            var security = new PluginSecurity();
            var testFile = Path.Combine(_testPluginDirectory, "test.txt");
            File.WriteAllText(testFile, "test");

            // Act
            var result = security.ValidatePlugin(testFile);

            // Assert
            Assert.False(result);

            // Cleanup
            File.Delete(testFile);
        }

        [Fact]
        public void PluginLoader_LoadedPlugins_InitiallyEmpty()
        {
            // Arrange
            var logger = LogManager.GetCurrentClassLogger();
            var context = new PluginContext("1.0.0", _testPluginDirectory, logger);
            var loader = new PluginLoader(_testPluginDirectory, context);

            // Act & Assert
            Assert.Empty(loader.LoadedPlugins);
        }

        [Fact]
        public void PluginLoader_UnloadPlugin_ReturnsFalseForNonExistentPlugin()
        {
            // Arrange
            var logger = LogManager.GetCurrentClassLogger();
            var context = new PluginContext("1.0.0", _testPluginDirectory, logger);
            var loader = new PluginLoader(_testPluginDirectory, context);

            // Act
            var result = loader.UnloadPlugin("nonexistent.plugin");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PluginLoader_ListPlugins_ReturnsEmptyListWhenNoPluginsLoaded()
        {
            // Arrange
            var logger = LogManager.GetCurrentClassLogger();
            var context = new PluginContext("1.0.0", _testPluginDirectory, logger);
            var loader = new PluginLoader(_testPluginDirectory, context);

            // Act
            var plugins = loader.ListPlugins();

            // Assert
            Assert.Empty(plugins);
        }

        [Fact]
        public void PluginSecurity_ComputePluginHash_ReturnsNullForNonExistentFile()
        {
            // Arrange
            var security = new PluginSecurity();
            var nonExistentPath = Path.Combine(_testPluginDirectory, "nonexistent.dll");

            // Act
            var hash = security.ComputePluginHash(nonExistentPath);

            // Assert
            Assert.Null(hash);
        }

        [Fact]
        public void PluginSecurity_IsPluginWhitelisted_ReturnsTrueWhenNoWhitelist()
        {
            // Arrange
            var security = new PluginSecurity();

            // Act
            var result = security.IsPluginWhitelisted("somehash");

            // Assert
            Assert.True(result); // No whitelist means allow all (for development)
        }

        [Fact]
        public void PluginManager_GetInstance_ReturnsSingletonInstance()
        {
            // Arrange & Act
            var instance1 = PluginManager.GetInstance(_testPluginDirectory);
            var instance2 = PluginManager.GetInstance(_testPluginDirectory);

            // Assert
            Assert.Same(instance1, instance2);
        }

        // Cleanup
        ~PluginSystemTests()
        {
            try
            {
                if (Directory.Exists(_testPluginDirectory))
                {
                    Directory.Delete(_testPluginDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}

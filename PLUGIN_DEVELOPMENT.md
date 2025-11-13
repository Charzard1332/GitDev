# Plugin Development Guide for GitDev

## Overview

GitDev supports a modular plugin architecture that allows developers to extend its functionality. This guide will help you create, test, and distribute plugins for GitDev.

## Table of Contents

1. [Plugin Architecture](#plugin-architecture)
2. [Getting Started](#getting-started)
3. [Plugin Interface](#plugin-interface)
4. [Creating Your First Plugin](#creating-your-first-plugin)
5. [Plugin Lifecycle](#plugin-lifecycle)
6. [Security Considerations](#security-considerations)
7. [Testing Plugins](#testing-plugins)
8. [Distribution](#distribution)
9. [API Reference](#api-reference)

## Plugin Architecture

GitDev plugins are .NET assemblies (DLL files) that implement the `IPlugin` interface. The plugin system provides:

- **Dynamic Loading**: Plugins are loaded at runtime without recompiling the host application
- **Isolation**: Each plugin runs with its own context and configuration
- **Security**: Basic security validation before loading plugins
- **Lifecycle Management**: Proper initialization and cleanup hooks

### Key Components

1. **IPlugin**: The main interface all plugins must implement
2. **PluginMetadata**: Contains information about the plugin
3. **IPluginContext**: Provides access to host application services
4. **PluginResult**: Standardized return type for plugin execution
5. **PluginManager**: Manages plugin lifecycle and execution

## Getting Started

### Prerequisites

- .NET Framework 4.7.2 or later
- Visual Studio or any C# IDE
- GitDev installed on your system

### Project Setup

1. Create a new Class Library project:
   ```bash
   dotnet new classlib -n MyPlugin
   ```

2. Add reference to GitDev assemblies:
   ```xml
   <ItemGroup>
     <Reference Include="GitDev">
       <HintPath>path\to\GitDev.exe</HintPath>
     </Reference>
   </ItemGroup>
   ```

3. Create your plugin class implementing `IPlugin`

## Plugin Interface

All plugins must implement the `IPlugin` interface:

```csharp
public interface IPlugin
{
    PluginMetadata Metadata { get; }
    void Initialize(IPluginContext context);
    PluginResult Execute(Dictionary<string, object> args);
    void Cleanup();
}
```

### PluginMetadata

Describes your plugin:

```csharp
public class PluginMetadata
{
    public string Id { get; set; }              // Unique identifier
    public string Name { get; set; }            // Display name
    public string Version { get; set; }         // Version (e.g., "1.0.0")
    public string Author { get; set; }          // Your name
    public string Description { get; set; }     // What it does
    public string MinimumHostVersion { get; set; } // Required GitDev version
    public string[] Dependencies { get; set; }  // Other required plugins
}
```

### IPluginContext

Provides access to host services:

```csharp
public interface IPluginContext
{
    string HostVersion { get; }
    string PluginDirectory { get; }
    ILogger Logger { get; }
    string GetConfigValue(string key);
    void SetConfigValue(string key, string value);
}
```

## Creating Your First Plugin

Here's a complete example plugin:

```csharp
using System;
using System.Collections.Generic;
using GitDev.Plugins;

namespace MyCompany.MyPlugin
{
    public class HelloWorldPlugin : IPlugin
    {
        private IPluginContext _context;
        private PluginMetadata _metadata;

        public PluginMetadata Metadata => _metadata;

        public HelloWorldPlugin()
        {
            _metadata = new PluginMetadata
            {
                Id = "com.mycompany.helloworld",
                Name = "Hello World Plugin",
                Version = "1.0.0",
                Author = "Your Name",
                Description = "A simple hello world plugin",
                MinimumHostVersion = "1.0.0",
                Dependencies = Array.Empty<string>()
            };
        }

        public void Initialize(IPluginContext context)
        {
            _context = context;
            _context.Logger.Info($"Initializing {Metadata.Name}");
            
            // Set default configuration
            _context.SetConfigValue("message", "Hello, World!");
        }

        public PluginResult Execute(Dictionary<string, object> args)
        {
            try
            {
                var message = _context.GetConfigValue("message");
                Console.WriteLine(message);
                
                return PluginResult.Successful(
                    "Hello World executed successfully", 
                    new { Message = message }
                );
            }
            catch (Exception ex)
            {
                return PluginResult.Failed(
                    $"Execution failed: {ex.Message}", 
                    ex
                );
            }
        }

        public void Cleanup()
        {
            _context.Logger.Info($"Cleaning up {Metadata.Name}");
            // Release resources here
        }
    }
}
```

## Plugin Lifecycle

### 1. Discovery

The `PluginLoader` scans the plugins directory for DLL files.

### 2. Loading

For each DLL found:
- Security validation is performed
- The assembly is loaded
- Types implementing `IPlugin` are discovered

### 3. Initialization

```csharp
plugin.Initialize(context);
```

This is where you:
- Store the context reference
- Set up initial configuration
- Prepare resources

### 4. Execution

```csharp
var result = plugin.Execute(args);
```

This is your plugin's main logic. Arguments are passed as a dictionary.

### 5. Cleanup

```csharp
plugin.Cleanup();
```

Release resources, close connections, save state, etc.

## Security Considerations

### Plugin Validation

GitDev performs the following security checks:

1. **File Extension**: Only `.dll` files are allowed
2. **File Size**: Maximum 50 MB per plugin
3. **Path Validation**: Files must be in the plugins directory

### Best Practices

1. **Input Validation**: Always validate arguments passed to `Execute()`
2. **Error Handling**: Use try-catch blocks and return proper `PluginResult`
3. **Resource Management**: Clean up resources in `Cleanup()`
4. **Logging**: Use the provided logger instead of `Console.WriteLine()`
5. **Configuration**: Use the context's config methods for persistence

### Security Guidelines

- **Don't** execute arbitrary code from user input
- **Don't** access the file system outside the plugin directory
- **Do** validate all input parameters
- **Do** handle exceptions gracefully
- **Do** use the provided logging mechanism

## Testing Plugins

### Unit Testing

Create test projects for your plugins:

```csharp
using Xunit;
using Moq;

public class HelloWorldPluginTests
{
    [Fact]
    public void Execute_ReturnsSuccess()
    {
        // Arrange
        var mockContext = new Mock<IPluginContext>();
        mockContext.Setup(c => c.GetConfigValue("message"))
                   .Returns("Test Message");
        
        var plugin = new HelloWorldPlugin();
        plugin.Initialize(mockContext.Object);
        
        // Act
        var result = plugin.Execute(new Dictionary<string, object>());
        
        // Assert
        Assert.True(result.Success);
    }
}
```

### Manual Testing

1. Build your plugin DLL
2. Copy it to GitDev's `plugins` directory
3. Run GitDev with plugin commands:
   ```
   dev plugin-list          # List all plugins
   dev plugin-info <id>     # Get plugin information
   dev plugin-run <id>      # Execute a plugin
   ```

## Distribution

### Package Structure

```
MyPlugin/
├── README.md
├── LICENSE.txt
├── MyPlugin.dll
└── (dependencies if any)
```

### Installation Instructions

1. Download the plugin package
2. Extract to GitDev's `plugins` directory
3. Restart GitDev or reload plugins
4. Verify with `dev plugin-list`

## API Reference

### IPlugin Methods

#### Initialize(IPluginContext context)
Called once when the plugin is loaded.

**Parameters:**
- `context`: The plugin context providing access to host services

**Use for:**
- Storing context reference
- Setting up configuration
- Initializing resources

#### Execute(Dictionary<string, object> args)
Executes the plugin's main functionality.

**Parameters:**
- `args`: Dictionary of arguments passed to the plugin

**Returns:**
- `PluginResult`: Result indicating success/failure and optional data

**Use for:**
- Implementing plugin logic
- Processing input arguments
- Returning results

#### Cleanup()
Called when the plugin is unloaded.

**Use for:**
- Releasing resources
- Closing connections
- Saving state

### IPluginContext Properties

- `HostVersion`: GitDev version string
- `PluginDirectory`: Path to plugins directory
- `Logger`: NLog logger instance for the plugin

### IPluginContext Methods

#### GetConfigValue(string key)
Retrieves a configuration value.

#### SetConfigValue(string key, string value)
Stores a configuration value.

### PluginResult Static Methods

#### Successful(string message, object data)
Creates a successful result.

#### Failed(string message, Exception error)
Creates a failed result.

## Advanced Topics

### Plugin Dependencies

If your plugin depends on other plugins:

```csharp
_metadata.Dependencies = new[] { "com.other.plugin" };
```

The plugin system ensures dependencies are loaded first.

### Accessing GitDev Services

Through the context, you can:
- Log messages with different levels
- Store/retrieve configuration
- Access the plugin directory

### Asynchronous Operations

For long-running operations, consider:
- Using async/await patterns in your Execute method
- Providing progress feedback via logging
- Implementing cancellation support

## Examples

Check the `ExamplePlugin` directory for more examples:
- `GreetingPlugin.cs`: Basic plugin structure
- (More examples coming soon)

## Support

For questions or issues:
- GitHub Issues: [repository URL]
- Documentation: [wiki URL]
- Community: [forum/chat URL]

## License

Plugins should specify their own license. GitDev is licensed under MIT.

---

Happy plugin development! 🚀

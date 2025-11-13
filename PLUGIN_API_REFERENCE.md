# Plugin API Reference

## Table of Contents

1. [Core Interfaces](#core-interfaces)
2. [Plugin Metadata](#plugin-metadata)
3. [Plugin Context](#plugin-context)
4. [Plugin Result](#plugin-result)
5. [Plugin Loader](#plugin-loader)
6. [Plugin Manager](#plugin-manager)
7. [Plugin Security](#plugin-security)
8. [Examples](#examples)

---

## Core Interfaces

### IPlugin

The main interface that all plugins must implement.

**Namespace:** `GitDev.Plugins`

**Definition:**
```csharp
public interface IPlugin
{
    PluginMetadata Metadata { get; }
    void Initialize(IPluginContext context);
    PluginResult Execute(Dictionary<string, object> args);
    void Cleanup();
}
```

**Properties:**

#### Metadata
- **Type:** `PluginMetadata`
- **Description:** Gets the metadata information for the plugin
- **Required:** Yes

**Methods:**

#### Initialize(IPluginContext context)
- **Description:** Called when the plugin is first loaded
- **Parameters:**
  - `context` (IPluginContext): The plugin context providing access to host services
- **Returns:** void
- **When Called:** Once, when the plugin is loaded
- **Use For:** Setting up configuration, initializing resources

#### Execute(Dictionary<string, object> args)
- **Description:** Executes the plugin's main functionality
- **Parameters:**
  - `args` (Dictionary<string, object>): Arguments passed to the plugin
- **Returns:** PluginResult
- **When Called:** Each time the plugin is executed
- **Use For:** Implementing the plugin's core logic

#### Cleanup()
- **Description:** Called when the plugin is being unloaded
- **Returns:** void
- **When Called:** Once, when the plugin is unloaded
- **Use For:** Releasing resources, closing connections

---

### IPluginContext

Provides access to host application services.

**Namespace:** `GitDev.Plugins`

**Definition:**
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

**Properties:**

#### HostVersion
- **Type:** `string`
- **Description:** The version of the GitDev host application
- **Example:** "1.0.0"

#### PluginDirectory
- **Type:** `string`
- **Description:** The directory where plugins are stored
- **Use For:** Accessing plugin-specific files

#### Logger
- **Type:** `ILogger` (NLog)
- **Description:** Logger instance for the plugin
- **Use For:** Logging messages, errors, warnings

**Methods:**

#### GetConfigValue(string key)
- **Description:** Retrieves a configuration value
- **Parameters:**
  - `key` (string): The configuration key
- **Returns:** string (null if key doesn't exist)
- **Example:** `var apiKey = context.GetConfigValue("apiKey");`

#### SetConfigValue(string key, string value)
- **Description:** Stores a configuration value
- **Parameters:**
  - `key` (string): The configuration key
  - `value` (string): The configuration value
- **Returns:** void
- **Example:** `context.SetConfigValue("apiKey", "abc123");`

---

## Plugin Metadata

Contains information about a plugin.

**Namespace:** `GitDev.Plugins`

**Definition:**
```csharp
public class PluginMetadata
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public string Description { get; set; }
    public string MinimumHostVersion { get; set; }
    public string[] Dependencies { get; set; }
}
```

**Properties:**

#### Id
- **Type:** `string`
- **Required:** Yes
- **Description:** Unique identifier for the plugin
- **Format:** Reverse domain notation (e.g., "com.company.pluginname")
- **Example:** "com.example.greeting"

#### Name
- **Type:** `string`
- **Required:** Yes
- **Description:** Human-readable name of the plugin
- **Example:** "Greeting Plugin"

#### Version
- **Type:** `string`
- **Required:** Yes
- **Description:** Plugin version in semantic versioning format
- **Format:** "MAJOR.MINOR.PATCH"
- **Example:** "1.0.0"

#### Author
- **Type:** `string`
- **Required:** Yes
- **Description:** Name or organization of the plugin author
- **Example:** "John Doe"

#### Description
- **Type:** `string`
- **Required:** Yes
- **Description:** Brief description of what the plugin does
- **Example:** "Provides greeting functionality"

#### MinimumHostVersion
- **Type:** `string`
- **Required:** No (recommended)
- **Description:** Minimum required GitDev version
- **Format:** "MAJOR.MINOR.PATCH"
- **Example:** "1.0.0"

#### Dependencies
- **Type:** `string[]`
- **Required:** No
- **Description:** Array of plugin IDs that this plugin depends on
- **Default:** Empty array
- **Example:** `new[] { "com.example.utility" }`

**Methods:**

#### ToString()
- **Description:** Returns a formatted string representation
- **Returns:** string
- **Format:** "{Name} v{Version} by {Author}"
- **Example:** "Greeting Plugin v1.0.0 by John Doe"

---

## Plugin Result

Represents the result of a plugin execution.

**Namespace:** `GitDev.Plugins`

**Definition:**
```csharp
public class PluginResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public object Data { get; set; }
    public Exception Error { get; set; }
}
```

**Properties:**

#### Success
- **Type:** `bool`
- **Description:** Whether the execution was successful

#### Message
- **Type:** `string`
- **Description:** Message describing the result

#### Data
- **Type:** `object`
- **Description:** Optional data returned by the plugin

#### Error
- **Type:** `Exception`
- **Description:** Exception if execution failed

**Static Methods:**

#### Successful(string message = "", object data = null)
- **Description:** Creates a successful result
- **Parameters:**
  - `message` (string): Success message (optional)
  - `data` (object): Return data (optional)
- **Returns:** PluginResult
- **Example:** `return PluginResult.Successful("Operation completed", result);`

#### Failed(string message, Exception error = null)
- **Description:** Creates a failed result
- **Parameters:**
  - `message` (string): Error message (required)
  - `error` (Exception): Exception details (optional)
- **Returns:** PluginResult
- **Example:** `return PluginResult.Failed("Operation failed", ex);`

---

## Plugin Loader

Loads and manages plugins dynamically.

**Namespace:** `GitDev.Plugins`

**Definition:**
```csharp
public class PluginLoader
{
    public IReadOnlyDictionary<string, IPlugin> LoadedPlugins { get; }
    public void LoadAllPlugins();
    public bool LoadPlugin(string pluginPath);
    public bool UnloadPlugin(string pluginId);
    public void UnloadAllPlugins();
    public IPlugin GetPlugin(string pluginId);
    public List<PluginMetadata> ListPlugins();
}
```

**Constructor:**
```csharp
public PluginLoader(string pluginDirectory, IPluginContext context)
```

**Properties:**

#### LoadedPlugins
- **Type:** `IReadOnlyDictionary<string, IPlugin>`
- **Description:** Dictionary of currently loaded plugins

**Methods:**

#### LoadAllPlugins()
- **Description:** Discovers and loads all plugins from the plugin directory
- **Returns:** void
- **Side Effects:** Loads all valid plugins found

#### LoadPlugin(string pluginPath)
- **Description:** Loads a specific plugin from a file path
- **Parameters:**
  - `pluginPath` (string): Full path to the plugin DLL
- **Returns:** bool (true if successful)

#### UnloadPlugin(string pluginId)
- **Description:** Unloads a specific plugin
- **Parameters:**
  - `pluginId` (string): The plugin ID
- **Returns:** bool (true if successful)

#### UnloadAllPlugins()
- **Description:** Unloads all loaded plugins
- **Returns:** void

#### GetPlugin(string pluginId)
- **Description:** Gets a loaded plugin by ID
- **Parameters:**
  - `pluginId` (string): The plugin ID
- **Returns:** IPlugin (null if not found)

#### ListPlugins()
- **Description:** Lists metadata for all loaded plugins
- **Returns:** List<PluginMetadata>

---

## Plugin Manager

High-level plugin system management (Singleton).

**Namespace:** `GitDev.Plugins`

**Definition:**
```csharp
public class PluginManager
{
    public static PluginManager GetInstance(string pluginDirectory = null, string hostVersion = "1.0.0");
    public void Initialize();
    public PluginResult ExecutePlugin(string pluginId, Dictionary<string, object> args = null);
    public List<PluginMetadata> ListPlugins();
    public IPlugin GetPlugin(string pluginId);
    public bool LoadPlugin(string pluginPath);
    public bool UnloadPlugin(string pluginId);
    public void Shutdown();
}
```

**Static Methods:**

#### GetInstance(string pluginDirectory = null, string hostVersion = "1.0.0")
- **Description:** Gets the singleton instance
- **Parameters:**
  - `pluginDirectory` (string): Plugin directory path (optional)
  - `hostVersion` (string): Host version (default: "1.0.0")
- **Returns:** PluginManager

**Instance Methods:**

#### Initialize()
- **Description:** Initializes the plugin system and loads all plugins
- **Returns:** void

#### ExecutePlugin(string pluginId, Dictionary<string, object> args = null)
- **Description:** Executes a plugin by ID
- **Parameters:**
  - `pluginId` (string): The plugin ID
  - `args` (Dictionary<string, object>): Arguments (optional)
- **Returns:** PluginResult

#### ListPlugins()
- **Description:** Lists all loaded plugins
- **Returns:** List<PluginMetadata>

#### GetPlugin(string pluginId)
- **Description:** Gets a plugin by ID
- **Parameters:**
  - `pluginId` (string): The plugin ID
- **Returns:** IPlugin (null if not found)

#### LoadPlugin(string pluginPath)
- **Description:** Loads a specific plugin
- **Parameters:**
  - `pluginPath` (string): Full path to the plugin
- **Returns:** bool (true if successful)

#### UnloadPlugin(string pluginId)
- **Description:** Unloads a specific plugin
- **Parameters:**
  - `pluginId` (string): The plugin ID
- **Returns:** bool (true if successful)

#### Shutdown()
- **Description:** Shuts down the plugin system
- **Returns:** void

---

## Plugin Security

Provides security validation for plugins.

**Namespace:** `GitDev.Plugins`

**Definition:**
```csharp
public class PluginSecurity
{
    public bool ValidatePlugin(string pluginPath);
    public string ComputePluginHash(string pluginPath);
    public bool IsPluginWhitelisted(string pluginHash, string whitelistPath = null);
}
```

**Methods:**

#### ValidatePlugin(string pluginPath)
- **Description:** Validates a plugin file before loading
- **Parameters:**
  - `pluginPath` (string): Full path to the plugin
- **Returns:** bool (true if valid)
- **Checks:**
  - File exists
  - Valid extension (.dll)
  - Size under 50 MB

#### ComputePluginHash(string pluginPath)
- **Description:** Computes the SHA256 hash of a plugin
- **Parameters:**
  - `pluginPath` (string): Full path to the plugin
- **Returns:** string (hash in lowercase hex, or null on error)

#### IsPluginWhitelisted(string pluginHash, string whitelistPath = null)
- **Description:** Checks if a plugin is in the whitelist
- **Parameters:**
  - `pluginHash` (string): The plugin hash
  - `whitelistPath` (string): Path to whitelist file (optional)
- **Returns:** bool (true if whitelisted or no whitelist exists)

---

## Examples

### Basic Plugin Implementation

```csharp
using System;
using System.Collections.Generic;
using GitDev.Plugins;

public class MyPlugin : IPlugin
{
    private IPluginContext _context;

    public PluginMetadata Metadata { get; } = new PluginMetadata
    {
        Id = "com.mycompany.myplugin",
        Name = "My Plugin",
        Version = "1.0.0",
        Author = "My Name",
        Description = "Does something useful",
        MinimumHostVersion = "1.0.0"
    };

    public void Initialize(IPluginContext context)
    {
        _context = context;
        _context.Logger.Info("Plugin initialized");
    }

    public PluginResult Execute(Dictionary<string, object> args)
    {
        try
        {
            // Your logic here
            return PluginResult.Successful("Operation completed");
        }
        catch (Exception ex)
        {
            return PluginResult.Failed("Operation failed", ex);
        }
    }

    public void Cleanup()
    {
        _context.Logger.Info("Plugin cleanup");
    }
}
```

### Using Plugin Manager

```csharp
// Get the plugin manager instance
var pluginManager = PluginManager.GetInstance("path/to/plugins");

// Initialize the system
pluginManager.Initialize();

// List all plugins
var plugins = pluginManager.ListPlugins();
foreach (var plugin in plugins)
{
    Console.WriteLine(plugin.ToString());
}

// Execute a plugin
var args = new Dictionary<string, object>
{
    { "param1", "value1" },
    { "param2", 42 }
};
var result = pluginManager.ExecutePlugin("com.example.plugin", args);

if (result.Success)
{
    Console.WriteLine($"Success: {result.Message}");
}
else
{
    Console.WriteLine($"Error: {result.Message}");
}

// Shutdown
pluginManager.Shutdown();
```

### Loading a Plugin Manually

```csharp
var logger = LogManager.GetCurrentClassLogger();
var context = new PluginContext("1.0.0", "path/to/plugins", logger);
var loader = new PluginLoader("path/to/plugins", context);

// Load a specific plugin
bool loaded = loader.LoadPlugin("path/to/MyPlugin.dll");

if (loaded)
{
    var plugin = loader.GetPlugin("com.example.plugin");
    var result = plugin.Execute(new Dictionary<string, object>());
}
```

### Security Validation

```csharp
var security = new PluginSecurity();

// Validate before loading
if (security.ValidatePlugin("path/to/plugin.dll"))
{
    // Compute hash
    string hash = security.ComputePluginHash("path/to/plugin.dll");
    
    // Check whitelist
    if (security.IsPluginWhitelisted(hash, "path/to/whitelist.txt"))
    {
        // Safe to load
    }
}
```

---

## Error Handling

All plugin operations should be wrapped in try-catch blocks:

```csharp
try
{
    var result = pluginManager.ExecutePlugin(pluginId, args);
    // Handle result
}
catch (Exception ex)
{
    logger.Error($"Plugin execution failed: {ex.Message}");
    // Handle error
}
```

## Logging

Use the provided logger:

```csharp
_context.Logger.Info("Informational message");
_context.Logger.Warn("Warning message");
_context.Logger.Error("Error message");
_context.Logger.Debug("Debug message");
```

---

For more information, see:
- [Plugin Development Guide](PLUGIN_DEVELOPMENT.md)
- [Plugin Security Guidelines](PLUGIN_SECURITY.md)

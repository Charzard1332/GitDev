# Plugin Quick Start Guide

This guide will help you create your first GitDev plugin in 5 minutes.

## Prerequisites

- Visual Studio or any C# IDE
- .NET Framework 4.7.2 or later
- GitDev installed

## Step 1: Create a New Project

Create a new Class Library project:

```bash
# Using Visual Studio: File > New > Project > Class Library (.NET Framework)
# Or using command line:
dotnet new classlib -n MyFirstPlugin -f net472
```

## Step 2: Add Reference to GitDev

Add a reference to `GitDev.exe`:

In your `.csproj` file, add:
```xml
<ItemGroup>
  <Reference Include="GitDev">
    <HintPath>C:\Path\To\GitDev.exe</HintPath>
  </Reference>
</ItemGroup>
```

## Step 3: Create Your Plugin

Replace the content of `Class1.cs` with:

```csharp
using System;
using System.Collections.Generic;
using GitDev.Plugins;

namespace MyFirstPlugin
{
    public class HelloPlugin : IPlugin
    {
        private IPluginContext _context;

        // Plugin metadata
        public PluginMetadata Metadata { get; } = new PluginMetadata
        {
            Id = "com.myname.hello",
            Name = "Hello Plugin",
            Version = "1.0.0",
            Author = "Your Name",
            Description = "My first GitDev plugin",
            MinimumHostVersion = "1.0.0"
        };

        // Called when plugin is loaded
        public void Initialize(IPluginContext context)
        {
            _context = context;
            _context.Logger.Info($"{Metadata.Name} initialized!");
        }

        // Main plugin functionality
        public PluginResult Execute(Dictionary<string, object> args)
        {
            try
            {
                // Get the name from arguments, or use "World" as default
                string name = "World";
                if (args.ContainsKey("name") && args["name"] != null)
                {
                    name = args["name"].ToString();
                }

                // Create greeting
                string greeting = $"Hello, {name}!";
                
                // Log it
                _context.Logger.Info($"Generated greeting: {greeting}");
                
                // Return success
                return PluginResult.Successful(greeting);
            }
            catch (Exception ex)
            {
                _context.Logger.Error($"Error: {ex.Message}");
                return PluginResult.Failed("Failed to create greeting", ex);
            }
        }

        // Called when plugin is unloaded
        public void Cleanup()
        {
            _context.Logger.Info($"{Metadata.Name} cleanup complete.");
        }
    }
}
```

## Step 4: Build Your Plugin

Build the project to create the DLL:

```bash
# In Visual Studio: Build > Build Solution
# Or using command line:
msbuild MyFirstPlugin.csproj /p:Configuration=Release
```

This creates `MyFirstPlugin.dll` in the `bin\Release` directory.

## Step 5: Install Your Plugin

1. Locate your GitDev installation directory
2. Create a `plugins` folder if it doesn't exist
3. Copy `MyFirstPlugin.dll` to the `plugins` folder

```
GitDev/
├── GitDev.exe
├── plugins/
│   └── MyFirstPlugin.dll
└── ...
```

## Step 6: Test Your Plugin

1. Start GitDev
2. After authentication, type: `dev plugin-list`
   - You should see your plugin listed

3. Get plugin info: `dev plugin-info com.myname.hello`
   - Shows details about your plugin

4. Run your plugin: `dev plugin-run com.myname.hello`
   - Should output: "Hello, World!"

## Next Steps

### Add Configuration

```csharp
public void Initialize(IPluginContext context)
{
    _context = context;
    
    // Set default configuration
    _context.SetConfigValue("greeting", "Hello");
    _context.SetConfigValue("punctuation", "!");
}

public PluginResult Execute(Dictionary<string, object> args)
{
    string greeting = _context.GetConfigValue("greeting");
    string punctuation = _context.GetConfigValue("punctuation");
    string name = args.ContainsKey("name") ? args["name"].ToString() : "World";
    
    string message = $"{greeting}, {name}{punctuation}";
    return PluginResult.Successful(message);
}
```

### Accept More Parameters

```csharp
public PluginResult Execute(Dictionary<string, object> args)
{
    // Validate required parameters
    if (!args.ContainsKey("requiredParam"))
    {
        return PluginResult.Failed("Missing required parameter: requiredParam");
    }
    
    // Get optional parameters with defaults
    string optional = args.ContainsKey("optionalParam") 
        ? args["optionalParam"].ToString() 
        : "default value";
    
    // Your logic here...
}
```

### Return Data

```csharp
public PluginResult Execute(Dictionary<string, object> args)
{
    var result = new
    {
        Greeting = "Hello, World!",
        Timestamp = DateTime.Now,
        Count = 42
    };
    
    return PluginResult.Successful("Operation completed", result);
}
```

### Handle Errors Gracefully

```csharp
public PluginResult Execute(Dictionary<string, object> args)
{
    try
    {
        // Validate input
        if (args == null || args.Count == 0)
        {
            return PluginResult.Failed("No arguments provided");
        }
        
        // Your logic
        // ...
        
        return PluginResult.Successful("Success");
    }
    catch (ArgumentException ex)
    {
        _context.Logger.Warn($"Invalid argument: {ex.Message}");
        return PluginResult.Failed("Invalid argument", ex);
    }
    catch (Exception ex)
    {
        _context.Logger.Error($"Unexpected error: {ex.Message}");
        return PluginResult.Failed("Unexpected error", ex);
    }
}
```

### Use External Libraries

If your plugin needs external libraries:

1. Add NuGet packages to your project:
```xml
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
</ItemGroup>
```

2. Copy all DLL dependencies to the plugins folder:
```
plugins/
├── MyFirstPlugin.dll
├── Newtonsoft.Json.dll
└── ...
```

## Common Issues

### Plugin Not Loading

**Problem:** Plugin doesn't appear in `dev plugin-list`

**Solutions:**
- Check that the DLL is in the `plugins` folder
- Ensure the plugin implements `IPlugin`
- Check GitDev logs for errors
- Verify the plugin metadata is set correctly

### Missing Dependencies

**Problem:** Plugin loads but fails to execute

**Solutions:**
- Copy all required DLL dependencies to plugins folder
- Check that .NET Framework version matches
- Review error messages in logs

### Plugin Crashes

**Problem:** Plugin crashes GitDev

**Solutions:**
- Add try-catch blocks in all methods
- Validate all inputs
- Test plugin standalone before integration
- Check for null references

## Debugging Tips

### Add Logging

```csharp
_context.Logger.Debug("Entering method X");
_context.Logger.Info($"Processing {count} items");
_context.Logger.Warn("Something unusual happened");
_context.Logger.Error("An error occurred", exception);
```

### Test Standalone

Create a test console application:

```csharp
class Program
{
    static void Main()
    {
        var logger = NLog.LogManager.GetCurrentClassLogger();
        var context = new PluginContext("1.0.0", ".", logger);
        
        var plugin = new HelloPlugin();
        plugin.Initialize(context);
        
        var args = new Dictionary<string, object>
        {
            { "name", "Test" }
        };
        
        var result = plugin.Execute(args);
        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Message: {result.Message}");
    }
}
```

## Resources

- [Full Plugin Development Guide](PLUGIN_DEVELOPMENT.md)
- [API Reference](PLUGIN_API_REFERENCE.md)
- [Security Guidelines](PLUGIN_SECURITY.md)
- [Example Plugins](ExamplePlugin/)

## Get Help

- Check the documentation
- Review example plugins
- Open an issue on GitHub
- Join the community forum

---

Happy coding! 🚀

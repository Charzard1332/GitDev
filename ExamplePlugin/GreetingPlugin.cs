using System;
using System.Collections.Generic;
using GitDev.Plugins;

namespace GitDev.ExamplePlugin
{
    /// <summary>
    /// Example plugin that demonstrates how to create a plugin for GitDev.
    /// This plugin provides a simple greeting functionality.
    /// </summary>
    public class GreetingPlugin : IPlugin
    {
        private IPluginContext _context;
        private PluginMetadata _metadata;

        public PluginMetadata Metadata => _metadata;

        public GreetingPlugin()
        {
            _metadata = new PluginMetadata
            {
                Id = "com.example.greeting",
                Name = "Greeting Plugin",
                Version = "1.0.0",
                Author = "Example Developer",
                Description = "A simple example plugin that provides greeting functionality",
                MinimumHostVersion = "1.0.0",
                Dependencies = Array.Empty<string>()
            };
        }

        public void Initialize(IPluginContext context)
        {
            _context = context;
            _context.Logger.Info($"Initializing {Metadata.Name}...");
            
            // Plugin initialization logic here
            _context.SetConfigValue("greeting", "Hello");
            
            _context.Logger.Info($"{Metadata.Name} initialized successfully.");
        }

        public PluginResult Execute(Dictionary<string, object> args)
        {
            try
            {
                _context.Logger.Info($"Executing {Metadata.Name}...");

                // Get the name parameter, default to "World" if not provided
                var name = args.ContainsKey("name") ? args["name"]?.ToString() : "World";
                
                // Get the greeting from config
                var greeting = _context.GetConfigValue("greeting") ?? "Hello";
                
                // Create the greeting message
                var message = $"{greeting}, {name}!";
                
                _context.Logger.Info($"Generated greeting: {message}");

                return PluginResult.Successful(message, new { Greeting = message, Timestamp = DateTime.Now });
            }
            catch (Exception ex)
            {
                _context.Logger.Error($"Error in {Metadata.Name}: {ex.Message}");
                return PluginResult.Failed($"Failed to generate greeting: {ex.Message}", ex);
            }
        }

        public void Cleanup()
        {
            _context.Logger.Info($"Cleaning up {Metadata.Name}...");
            // Cleanup logic here
            _context.Logger.Info($"{Metadata.Name} cleanup complete.");
        }
    }
}

using System;

namespace GitDev.Plugins
{
    /// <summary>
    /// Represents the result of a plugin execution.
    /// </summary>
    public class PluginResult
    {
        /// <summary>
        /// Gets or sets whether the plugin execution was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the output message from the plugin.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets any data returned by the plugin.
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Gets or sets the error information if execution failed.
        /// </summary>
        public Exception Error { get; set; }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        public static PluginResult Successful(string message = "", object data = null)
        {
            return new PluginResult
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        public static PluginResult Failed(string message, Exception error = null)
        {
            return new PluginResult
            {
                Success = false,
                Message = message,
                Error = error
            };
        }
    }
}

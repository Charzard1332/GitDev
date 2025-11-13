using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Octokit;

namespace GitDev.Core
{
    /// <summary>
    /// Manages GitHub OAuth authentication flow with improved error handling and logging.
    /// </summary>
    public class AuthenticationManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string redirectUri;

        public GitHubClient Client { get; private set; }
        public string Username { get; private set; }

        public AuthenticationManager(string clientId, string clientSecret, string redirectUri)
        {
            this.clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            this.clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
            this.redirectUri = redirectUri ?? throw new ArgumentNullException(nameof(redirectUri));
        }

        /// <summary>
        /// Performs OAuth authentication with GitHub.
        /// </summary>
        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                logger.Info("Starting GitHub OAuth authentication flow");
                
                using (var listener = new HttpListener())
                {
                    listener.Prefixes.Add("http://localhost:5000/");
                    listener.Start();
                    logger.Debug("HTTP listener started on port 5000");

                    string authUrl = $"https://github.com/login/oauth/authorize?client_id={clientId}&redirect_uri={redirectUri}&scope=repo";
                    
                    logger.Info("Opening browser for GitHub authentication");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = authUrl,
                        UseShellExecute = true
                    });

                    Console.WriteLine("Waiting for authentication callback...");
                    var context = await listener.GetContextAsync();
                    var request = context.Request;
                    var response = context.Response;

                    string code = request.QueryString["code"];
                    
                    if (string.IsNullOrEmpty(code))
                    {
                        logger.Error("Authentication failed - no authorization code received");
                        await SendErrorResponse(response, "Authentication Failed", "No authorization code received.");
                        return false;
                    }

                    await SendSuccessResponse(response);
                    listener.Stop();

                    string token = await ExchangeCodeForTokenAsync(code);
                    if (string.IsNullOrEmpty(token))
                    {
                        logger.Error("Failed to exchange code for access token");
                        return false;
                    }

                    Client = new GitHubClient(new ProductHeaderValue("GitDev"))
                    {
                        Credentials = new Credentials(token)
                    };

                    var user = await Client.User.Current();
                    Username = user.Login;
                    
                    logger.Info($"Successfully authenticated as {Username}");
                    Console.WriteLine($"Authenticated as: {Username}");
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error during authentication");
                Console.WriteLine($"Error during authentication: {ex.Message}");
                return false;
            }
        }

        private async Task<string> ExchangeCodeForTokenAsync(string code)
        {
            using (var webClient = new WebClient())
            {
                try
                {
                    logger.Debug("Exchanging authorization code for access token");
                    
                    var values = new System.Collections.Specialized.NameValueCollection
                    {
                        ["client_id"] = clientId,
                        ["client_secret"] = clientSecret,
                        ["code"] = code,
                        ["redirect_uri"] = redirectUri
                    };

                    var response = await webClient.UploadValuesTaskAsync("https://github.com/login/oauth/access_token", values);
                    string responseString = Encoding.UTF8.GetString(response);
                    
                    if (!responseString.Contains("access_token"))
                    {
                        logger.Error("Unable to retrieve access token from response");
                        return null;
                    }

                    string token = responseString.Split('&')[0].Split('=')[1];
                    logger.Debug("Successfully obtained access token");
                    return token;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error exchanging code for token");
                    Console.WriteLine($"Error exchanging code for token: {ex.Message}");
                    return null;
                }
            }
        }

        private async Task SendSuccessResponse(HttpListenerResponse response)
        {
            response.StatusCode = 200;
            string successHtml = @"
            <html>
            <head>
                <title>GitDev Authentication</title>
                <style>
                    body { font-family: Arial, sans-serif; text-align: center; padding: 50px; background-color: #f4f4f4; }
                    .container { background: white; padding: 20px; border-radius: 10px; box-shadow: 0px 0px 10px 0px #aaa; display: inline-block; }
                    h2 { color: #333; }
                    p { font-size: 18px; }
                    .success { color: green; font-weight: bold; }
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>Authentication Successful!</h2>
                    <p class='success'>You can now close this window and return to GitDev.</p>
                </div>
            </body>
            </html>";

            byte[] buffer = Encoding.UTF8.GetBytes(successHtml);
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private async Task SendErrorResponse(HttpListenerResponse response, string title, string message)
        {
            response.StatusCode = 400;
            string errorHtml = $@"
            <html>
            <head>
                <title>GitDev Authentication</title>
                <style>
                    body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; background-color: #f4f4f4; }}
                    .container {{ background: white; padding: 20px; border-radius: 10px; box-shadow: 0px 0px 10px 0px #aaa; display: inline-block; }}
                    h2 {{ color: #d32f2f; }}
                    p {{ font-size: 18px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>{title}</h2>
                    <p>{message}</p>
                </div>
            </body>
            </html>";

            byte[] buffer = Encoding.UTF8.GetBytes(errorHtml);
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }
}

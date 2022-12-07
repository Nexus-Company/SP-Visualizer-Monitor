using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Sicoob.Visualizer.Monitor.Service
{
    internal class GraphHelper
    {
        // Settings object
        private static Settings _settings;
        // Client configured with user authentication
        private static GraphServiceClient _userClient;
        private static string _accessToken;
        public static void InitializeGraphForUserAuthAsync(Settings settings,
            Func<DeviceCodeInfo, CancellationToken, Task> deviceCodePrompt)
        {
            _settings = settings;

            _userClient = new GraphServiceClient(new DelegateAuthenticationProvider(async (requestMessage) =>
            {
                if (string.IsNullOrEmpty(_accessToken))
                    _accessToken = await GetAccessToken(settings);

                requestMessage
                .Headers
                .Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }));
        }

        public static async Task<IGraphServiceDrivesCollectionPage> GetDrivesAsync()
             => await _userClient.Drives.Request().GetAsync();

        public static async Task<Stream> GetReportsAsync()
            => await _userClient.Reports.GetSharePointActivityFileCounts("D7").Request().GetAsync();
        
        private static async Task<string> GetAccessToken(Settings settings)
        {
            var app = PublicClientApplicationBuilder.Create(settings.ClientId)
                     .WithRedirectUri("http://localhost")
                     .Build();

            var options = new SystemWebViewOptions()
            {
                HtmlMessageError = "<p> An error occured: {0}. Details {1}</p>"
            };

            var accounts = await app.GetAccountsAsync();
            AuthenticationResult result;
            try
            {
                result = await app.AcquireTokenSilent(settings.GraphUserScopes, accounts.FirstOrDefault())
                            .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                result = await app.AcquireTokenInteractive(settings.GraphUserScopes)
                                .WithUseEmbeddedWebView(false)
                                .WithSystemWebViewOptions(options)
                                .ExecuteAsync();
            }

            return result.AccessToken;
        }
    }
}

using Azure.Identity;
using Microsoft.Graph;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sicoob.Visualizer.Monitor.Service
{
    internal class GraphHelper
    {
        // Settings object
        private static Settings _settings;
        // User auth token credential
        private static DeviceCodeCredential _deviceCodeCredential;
        // Client configured with user authentication
        private static GraphServiceClient _userClient;

        public static void InitializeGraphForUserAuth(Settings settings,
            Func<DeviceCodeInfo, CancellationToken, Task> deviceCodePrompt)
        {
            _settings = settings;

            _deviceCodeCredential = new DeviceCodeCredential(deviceCodePrompt,
                settings.AuthTenant, settings.ClientId);

            _userClient = new GraphServiceClient(_deviceCodeCredential, settings.GraphUserScopes);
        }
    }
}

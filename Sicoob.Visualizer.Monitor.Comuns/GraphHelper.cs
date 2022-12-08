using Microsoft.Graph;
using Sicoob.Visualizer.Monitor.Comuns.Database.Models;
using System;
using System.Data.Entity;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime;
using System.Threading.Tasks;

namespace Sicoob.Visualizer.Monitor.Comuns
{
    public class GraphHelper
    {
        // Settings object
        private static Settings _settings;
        // Client configured with user authentication
        private static GraphServiceClient _userClient;
        private static GraphAuthentication _accessToken;
        private static Authenticator authenticator;

        public static void InitializeGraphForUserAuthAsync(Settings settings)
        {
            _settings = settings;

            _userClient = new GraphServiceClient(new DelegateAuthenticationProvider(async (requestMessage) =>
            {
                requestMessage
                .Headers
                .Authorization = new AuthenticationHeaderValue(_accessToken.TokenType, _accessToken.AccessToken);

                _ = await Task.FromResult(0);
            }));

            try
            {
                authenticator = new Authenticator(_settings);
            }
            catch (Exception)
            {
            }
        }

        public static async Task GetLoginAsync()
        {
            using (var ctx = _settings.GetContext())
            {
                _accessToken = await ctx.Authentications.FirstAsync();
            }
        }
        public static void RequestLogin()
            => authenticator.RequestLogin();
        public static async Task SaveLoginAsync()
        {
            var result = await GetAuthenticationAsync();

            using (var ctx = _settings.GetContext())
            {
                await ctx.Database.ExecuteSqlCommandAsync("TRUNCATE TABLE [GraphAuthentications]");

                _accessToken = new GraphAuthentication()
                {
                    //ExpiresOn = result.ExpiresOn,
                    AccessToken = result.Access_token,
                    RefreshToken = result.Refresh_token,
                    TokenType = result.Token_type
                };

                ctx.Authentications.Add(_accessToken);

                await ctx.SaveChangesAsync();
            }
        }

        private static async Task<AccessToken> GetAuthenticationAsync()
        {
            RequestLogin(); 
            return await authenticator?.AwaitLoginAsync()
                ?? throw new Exception();
        }

        public static async Task<IGraphServiceDrivesCollectionPage> GetDrivesAsync()
             => await _userClient.Drives.Request().GetAsync();

        public static async Task<Stream> GetReportsAsync()
            => await _userClient.Reports.GetSharePointActivityFileCounts("D7").Request().GetAsync();
    }
}

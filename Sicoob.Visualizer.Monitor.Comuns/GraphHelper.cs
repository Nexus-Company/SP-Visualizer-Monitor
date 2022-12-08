using Microsoft.Graph;
using Microsoft.Identity.Client;
using Sicoob.Visualizer.Monitor.Comuns;
using Sicoob.Visualizer.Monitor.Comuns.Database.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Process = System.Diagnostics.Process;
using Prompt = Microsoft.Identity.Client.Prompt;

namespace Sicoob.Visualizer.Monitor.Comuns
{
    public class GraphHelper
    {
        // Settings object
        private static Settings _settings;
        // Client configured with user authentication
        private static GraphServiceClient _userClient;
        private static string _accessToken;
        public static void InitializeGraphForUserAuthAsync(Settings settings)
        {
            _settings = settings;

            _userClient = new GraphServiceClient(new DelegateAuthenticationProvider(async (requestMessage) =>
            {
                //if (string.IsNullOrEmpty(_accessToken))
                //    _accessToken = await GetAccessToken(settings);

                requestMessage
                .Headers
                .Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }));
        }

        public static async Task SaveLoginAsync()
        {
            var result = await GetAuthenticationAsync();

            using (var ctx = _settings.GetContext())
            {
                await ctx.Database.ExecuteSqlCommandAsync("TRUNCATE TABLE [GraphAuthentications]");

                ctx.Authentications.Add(new GraphAuthentication()
                {
                    //ExpiresOn = result.ExpiresOn,
                    AccessToken = result.Access_token,
                    RefreshToken = result.Refresh_token,
                    TokenType = result.Token_type
                });

                try
                {
                    await ctx.SaveChangesAsync();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {

                    throw;
                }
            }
        }

        private static async Task<AccessToken> GetAuthenticationAsync()
        {
            var auth = new Authenticator(_settings);

            return await auth.GetAccessAsync();
        }

        public static async Task<IGraphServiceDrivesCollectionPage> GetDrivesAsync()
             => await _userClient.Drives.Request().GetAsync();

        public static async Task<Stream> GetReportsAsync()
            => await _userClient.Reports.GetSharePointActivityFileCounts("D7").Request().GetAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Sicoob.Visualizer.Monitor.Comuns.Database;
using Sicoob.Visualizer.Monitor.Comuns.Database.Models;
using System.Net.Http.Headers;
using static Sicoob.Visualizer.Monitor.Comuns.Settings;

namespace Sicoob.Visualizer.Monitor.Comuns
{
    public class GraphHelper : IDisposable
    {
        // Settings object
        private readonly OAuthSettings _settings;
        // Client configured with user authentication
        private readonly GraphServiceClient _userClient;
        private GraphAuthentication? _accessToken;
        private readonly Authenticator? _authenticator;
        private readonly MonitorContext ctx;
        public GraphHelper(OAuthSettings settings, MonitorContext ctx, bool authenticator = false)
        {
            this.ctx = ctx;
            _settings = settings;

            _userClient = new GraphServiceClient(new DelegateAuthenticationProvider(async (requestMessage) =>
            {
                requestMessage
                .Headers
                .Authorization = new AuthenticationHeaderValue(_accessToken.TokenType, _accessToken.AccessToken);

                _ = await Task.FromResult(0);
            }));

            if (authenticator)
                _authenticator = new Authenticator(_settings);
        }

        public async Task GetLoginAsync()
        {
            _accessToken = await ctx.Authentications.FirstAsync();
        }

        public void RequestLogin()
            => _authenticator.RequestLogin();
        public async Task SaveLoginAsync()
        {
            var result = await GetAuthenticationAsync();

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

        private async Task<AccessToken> GetAuthenticationAsync()
        {
            RequestLogin();
            return await _authenticator?.AwaitLoginAsync() ?? throw new Exception();
        }

        public async Task<IGraphServiceDrivesCollectionPage> GetDrivesAsync()
             => await _userClient.Drives.Request().GetAsync();

        public async Task<Stream> GetReportsAsync()
            => await _userClient.Reports.GetSharePointActivityFileCounts("D7").Request().GetAsync();

        public void Dispose()
        {
            ctx.Dispose();
            _authenticator?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

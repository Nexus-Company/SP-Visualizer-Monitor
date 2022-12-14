using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Newtonsoft.Json;
using Sicoob.Visualizer.Monitor.Dal;
using Sicoob.Visualizer.Monitor.Dal.Models;
using Sicoob.Visualizer.Monitor.Dal.Models.Enums;
using System.Collections.Generic;
using System.Net.Http.Headers;
using static Sicoob.Visualizer.Monitor.Comuns.Settings;
using ActivityType = Sicoob.Visualizer.Monitor.Dal.Models.Enums.ActivityType;

namespace Sicoob.Visualizer.Monitor.Comuns
{
    public class GraphHelper : IDisposable
    {
        // Settings object
        private readonly OAuthSettings _settings;
        // Client configured with user authentication
        private readonly GraphServiceClient _userClient;
        private GraphAuthentication? graphAccessToken;
        private GraphAuthentication? sharepointAccessToken;
        private readonly Authenticator? _authenticator;
        private readonly Random _random;
        private readonly MonitorContext ctx;
        private string randomColor => string.Format("#{0:X6}", _random.Next(0x1000000));

        public GraphHelper(OAuthSettings settings, MonitorContext ctx, bool listener = false)
        {
            this.ctx = ctx;
            _settings = settings;
            _random = new Random();

            _userClient = new GraphServiceClient(new DelegateAuthenticationProvider(async (requestMessage) =>
            {
                if (graphAccessToken.RefreshIn <= DateTime.Now)
                {
                    graphAccessToken = await ctx.Authentications.FirstOrDefaultAsync(auth=> auth.Type == AuthenticationType.Graph);

                    if (graphAccessToken.RefreshIn <= DateTime.Now)
                        await CheckRefreshAsync(AuthenticationType.Graph);
                }

                requestMessage
                .Headers
                .Authorization = new AuthenticationHeaderValue(graphAccessToken.TokenType, graphAccessToken.AccessToken);

                _ = await Task.FromResult(0);
            }));

            _authenticator = new Authenticator(_settings, listener);
        }

        #region Authentication
        public async Task GetLoginAsync()
        {
            ctx.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            graphAccessToken = await ctx.Authentications.FirstAsync();
            ctx.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            await CheckRefreshAsync(AuthenticationType.Graph);
        }
        private async Task CheckRefreshAsync(AuthenticationType type)
        {
            if (graphAccessToken.RefreshIn <= DateTime.Now)
            {
                if (string.IsNullOrEmpty(graphAccessToken.RefreshToken))
                    throw new ArgumentNullException("User not authenticated!");

                var result = await _authenticator?.RefreshLoginAsync(graphAccessToken.RefreshToken);

                result.Type = type;
                await SaveLoginAsync(result);
            }
        }
        public void RequestLogin()
            => _authenticator.RequestLogin();
        public void RequestLogin(string domain)
            => _authenticator.RequestLogin(domain);
        public async Task SaveLoginAsync()
        {
            var result = await GetAuthenticationAsync();

            await SaveLoginAsync(result);
        }
        public async Task SaveLoginAsync(string domain)
        {
            var result = await GetAuthenticationAsync(domain);

            await SaveLoginAsync(result);
        }
        private async Task SaveLoginAsync(AccessToken result)
        {
            bool exist = false;
            var auth = await ctx.Authentications.FirstOrDefaultAsync(auth => auth.Type == result.Type);

            if (auth == null)
            {
                auth = new()
                {
                    RefreshIn = DateTime.Now + TimeSpan.FromSeconds(result.Expires_in - 2),
                    AccessToken = result.Access_token,
                    RefreshToken = result.Refresh_token,
                    TokenType = result.Token_type,
                    Type = result.Type
                };
            }
            else
            {
                auth.RefreshIn = DateTime.Now + TimeSpan.FromSeconds(result.Expires_in - 2);
                auth.AccessToken = result.Access_token;
                auth.RefreshToken = result.Refresh_token;
                auth.TokenType = result.Token_type;
                auth.Type = result.Type;
                exist = true;
            }

            if (result.Type == AuthenticationType.Graph)
                graphAccessToken = auth;

            var me = await _userClient.Me.Request().GetAsync();
            var account = await ctx.Accounts.FirstOrDefaultAsync(acc => acc.Email == me.Mail.ToLower());

            if (account == null)
            {
                account = new Account()
                {
                    Id = me.Id,
                    Name = me.DisplayName,
                    Email = me.Mail.ToLower(),
                    Color = randomColor
                };

                await ctx.Accounts.AddAsync(account);
            }

            auth.Account = account.Id;

            if (exist)
            {
                ctx.Entry(auth).State = EntityState.Modified;
            }
            else
            {
                await ctx.Authentications.AddAsync(auth);
            }

            await ctx.SaveChangesAsync();
        }
        private async Task<AccessToken> GetAuthenticationAsync()
        {
            RequestLogin();
            return await _authenticator?.AwaitLoginAsync(AuthenticationType.Graph) ?? throw new Exception();
        }
        private async Task<AccessToken> GetAuthenticationAsync(string domain)
        {
            RequestLogin(domain);
            return await _authenticator?.AwaitLoginAsync(AuthenticationType.Sharepoint) ?? throw new Exception();
        }
        #endregion

        #region User
        public async Task UpdateOrAppendUserAsync(User user)
        {
            Account? account = await ctx.Accounts.FirstOrDefaultAsync(acc => acc.Email == user.Mail);

            if (account == null)
            {
                account = new Account()
                {
                    Id = user.Id,
                    Email = user.Mail,
                    Name = user.DisplayName,
                    Color = randomColor
                };

                await ctx.Accounts.AddAsync(account);
            }
            else
            {
                account.Name = user.DisplayName;

                ctx.Entry(account).State = EntityState.Modified;
            }

            await ctx.SaveChangesAsync();
        }

        public async Task<Account> GetAuthenticatedAccountAsync()
            => await ctx.Accounts.FirstOrDefaultAsync(acc => acc.Id == graphAccessToken.Account) ?? throw new ArgumentException("User not authenticated!");

        public async Task<IGraphServiceUsersCollectionPage> GetUsersAsync()
            => await _userClient.Users.Request().GetAsync();
        #endregion

        public async Task<IGraphServiceDrivesCollectionPage> GetDrivesAsync()
             => await _userClient.Drives.Request().GetAsync();

        public async Task<ISiteListsCollectionPage> GetListsAsync(string drive)
            => await _userClient.Sites[drive].Lists.Request().GetAsync();

        public async Task<IListItemsCollectionPage> GetItemsAsync(string site, string list)
        {
            var request = _userClient.Sites[site].Lists[list].Items;
            request.AppendSegmentToRequestUrl("Name");
            request.AppendSegmentToRequestUrl("DriveItem");

            return await request.Request().GetAsync();
        }

        public async Task<BaseItem> GetItemAsync(string drive, string item)
            => await _userClient.Drives[drive].Items[item].Request().GetAsync();

        public async Task<FileActivity[]> GetActivityAsync(string drive, string item, ActivityType type)
        {
            var url = $"https://graph.microsoft.com/v1.0/drives/{drive}/items/{item.Replace("\"", string.Empty)}/analytics/allTime?%24expand=activities(%24filter%3D{Enum.GetName(type).ToLower()}%20ne%20null)";
            HttpRequestMessage request = new(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue(graphAccessToken.TokenType, graphAccessToken.AccessToken);

            HttpClient httpClient = new();
            var response = await httpClient.SendAsync(request);

            string body = await response.Content.ReadAsStringAsync();

            var obj = JsonConvert.DeserializeObject<ActivitiesResult>(body);

            obj.Activities ??= Array.Empty<FileActivity>();

            foreach (var act in obj.Activities)
            {
                act.Type = type;
                act.ActivityDateTime = act.ActivityDateTime.ToLocalTime();
            }

            return obj.Activities;
        }

        public async Task UpdateActivitiesAsync(BaseItem lItem, FileActivity[] activities)
        {
            Item item = await ctx.Items.FirstOrDefaultAsync(it => it.Id == lItem.Id);

            if (item == null)
            {
                item = new()
                {
                    Id = lItem.Id,
                    WebUrl = lItem.WebUrl,
                    Name = lItem.Name
                };

                await ctx.Items.AddAsync(item);
            }
            else
            {
                item.WebUrl = lItem.WebUrl;
                item.Name = lItem.Name;

                ctx.Entry(item).State = EntityState.Modified;
            }

            await ctx.SaveChangesAsync();

            foreach (var activity in activities)
            {
                Activity? act = await (from actv in ctx.Activities
                                       where actv.Id == activity.Id &&
                                             actv.Target == lItem.Id &&
                                             actv.Type == activity.Type && 
                                             actv.Date == activity.ActivityDateTime
                                       select actv).FirstOrDefaultAsync();
                if (act == null)
                {
                    act = new()
                    {
                        Date = activity.ActivityDateTime,
                        Id = activity.Id,
                        User = activity.Actor.User.Id,
                        Type = activity.Type,
                        Target = lItem.Id
                    };

                    await ctx.Activities.AddAsync(act);
                }
            }

            await ctx.SaveChangesAsync();
        }

        private class ActivitiesResult
        {
            public FileActivity[] Activities { get; set; }
        }
        public class FileActivity
        {
            public int Id { get; set; }
            public DateTime ActivityDateTime { get; set; }
            public ActivityType Type { get; set; }
            public UserActor Actor { get; set; }

            public class UserActor
            {
                public User User { get; set; }
            }
            public class User
            {
                public string DisplayName { get; set; }
                public string Email { get; set; }
                public string Id { get; set; }
            }
        }

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

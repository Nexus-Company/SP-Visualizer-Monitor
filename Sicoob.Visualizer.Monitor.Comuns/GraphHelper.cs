using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Newtonsoft.Json;
using Sicoob.Visualizer.Monitor.Dal;
using Sicoob.Visualizer.Monitor.Dal.Models;
using Sicoob.Visualizer.Monitor.Dal.Models.Enums;
using System.Timers;
using System.Net.Http.Headers;
using static Sicoob.Visualizer.Monitor.Comuns.Settings;
using ActivityType = Sicoob.Visualizer.Monitor.Dal.Models.Enums.ActivityType;
using Timer = System.Timers.Timer;
using System.Net;
using System.Text.Encodings.Web;
using Directory = System.IO.Directory;
using Bytescout.Spreadsheet;
using System.IO;
using Workbook = Bytescout.Spreadsheet.Workbook;

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
        private static Timer _requestCountClearTimer;
        private static int _requestCount;
        private static DateTime _lastRequestAwait;
        private string randomColor => string.Format("#{0:X6}", _random.Next(0x1000000));
        private string directoryResults => Path.GetFullPath("Results");
        public GraphHelper(OAuthSettings settings, MonitorContext ctx, bool listener = false)
        {
            this.ctx = ctx;
            if (_requestCountClearTimer == null)
            {
                _requestCountClearTimer = new Timer(1000 * 60)
                {
                    Enabled = true,
                    AutoReset = true
                };

                _requestCountClearTimer.Elapsed +=
                    (object? sender, ElapsedEventArgs args)
                        =>
                    {
                        _requestCount = 0;
                        _lastRequestAwait = DateTime.Now;
                    };

                _lastRequestAwait = DateTime.Now;

                _requestCountClearTimer.Start();
            }

            _settings = settings;
            _random = new Random();

            _userClient = new GraphServiceClient(new DelegateAuthenticationProvider(async (requestMessage) =>
            {
                if (graphAccessToken.RefreshIn <= DateTime.Now)
                {
                    graphAccessToken = await ctx.Authentications.FirstOrDefaultAsync(auth => auth.Type == AuthenticationType.Graph);

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
            try
            {
                ctx.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                graphAccessToken = await ctx.Authentications.FirstAsync();
                ctx.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

                await CheckRefreshAsync(AuthenticationType.Graph);
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
            catch (Exception)
            {
                throw;
            }
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
        {
            NeedAwait();
            return await _userClient.Drives.Request().GetAsync();
        }

        public async Task<ISiteListsCollectionPage> GetListsAsync(string drive)
        {
            NeedAwait();
            return await _userClient.Sites[drive].Lists.Request().GetAsync();
        }

        public async Task<IListItemsCollectionPage> GetItemsAsync(string site, string list)
        {
            var request = _userClient.Sites[site].Lists[list].Items;
            request.AppendSegmentToRequestUrl("Name");
            request.AppendSegmentToRequestUrl("DriveItem");

            NeedAwait();
            return await request.Request().GetAsync();
        }

        public async Task<DriveItem> GetItemAsync(string drive, string item)
        {
            NeedAwait();
            return await _userClient.Drives[drive].Items[item].Request().GetAsync();
        }

        private static int actCount;
        public async Task<FileActivity[]> GetActivityAsync(string drive, string item, ActivityType type)
        {
            actCount++;
            var url = $"https://graph.microsoft.com/v1.0/drives/{drive}/items/{item.Replace("\"", string.Empty)}/analytics/allTime?%24expand=activities(%24filter%3D{Enum.GetName(type).ToLower()}%20ne%20null)";
            HttpRequestMessage request = new(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue(graphAccessToken.TokenType, graphAccessToken.AccessToken);

            HttpClient httpClient = new();
            var response = await httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                Thread.Sleep((int)response.Headers.RetryAfter.Delta.Value.TotalMilliseconds);
                actCount = 0;
                return await GetActivityAsync(drive, item, type);
            }

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
        public async Task UpdateActivitiesAsync(string dir, DriveItem lItem, FileActivity[] activities)
        {
            Item item = await ctx.Items.FirstOrDefaultAsync(it => it.Id == lItem.Id);
            string directory = Uri.UnescapeDataString(lItem.WebUrl.Split("sharepoint.com/")[1]);
            directory = directory.Substring(dir.Length + 1, (directory.Length - dir.Length - lItem.Name.Length - 1));

            if (item == null)
            {
                item = new()
                {
                    Id = lItem.Id,
                    WebUrl = lItem.WebUrl,
                    Name = lItem.Name,
                    Directory = directory
                };

                await ctx.Items.AddAsync(item);
            }
            else
            {
                item.WebUrl = lItem.WebUrl;
                item.Name = lItem.Name;
                item.Directory = directory;
            }

            await ctx.SaveChangesAsync();

            foreach (var activity in activities)
            {
                Activity? act = await (from actv in ctx.Activities
                                       where actv.Target == lItem.Id &&
                                             actv.Type == activity.Type &&
                                             actv.Date == activity.ActivityDateTime
                                       select actv).FirstOrDefaultAsync();
                if (act == null)
                {
                    act = new()
                    {
                        Date = activity.ActivityDateTime,
                        User = activity.Actor.User.Id,
                        Type = activity.Type,
                        Target = lItem.Id
                    };

                    await ctx.Activities.AddAsync(act);
                }
            }

            await ctx.SaveChangesAsync();
        }

        private static void NeedAwait()
        {
            if (_requestCount >= 100)
                Thread.Sleep((int)((_lastRequestAwait + TimeSpan.FromMilliseconds(1000 * 60)) - DateTime.Now).TotalMilliseconds);

            _requestCount++;
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

        public async Task<string> ExporteExcel(DateTime start, DateTime end, bool ascending)
        {
            if (!Directory.Exists(directoryResults))
                Directory.CreateDirectory(directoryResults);

            string path = Path.Combine(directoryResults, $"{start}-a-{end}.xlsx");

            Spreadsheet document = new();
            Workbook book = document.Workbook;
            Worksheet sheet = book.Worksheets[0];

            var query = (from actv in ctx.Activities
                         where actv.Date > start &&
                               actv.Date < end
                         select actv)
                         .Include(actv => actv.Item)
                         .Include(actv => actv.Account)
                         .OrderByDescending(actv => actv.Date);

            if (ascending)
                query = query.OrderBy(actv => actv.Date);

            var activities = await query.ToArrayAsync();

            for (int i = 0; i < activities.LongLength; i++)
            {
                var row = sheet.Rows[i];
                var actv = activities[i];

                row[0].Value = actv.Account.Email; 
                row[2].Value = actv.Account.Email;
            }

            document.SaveAsXLSX(path);

            return path;
        }

        public void Dispose()
        {
            ctx.Dispose();
            _authenticator?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

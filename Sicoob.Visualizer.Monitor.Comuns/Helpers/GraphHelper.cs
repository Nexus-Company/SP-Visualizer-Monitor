using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Newtonsoft.Json;
using OfficeOpenXml;
using Sicoob.Visualizer.Monitor.Dal;
using Sicoob.Visualizer.Monitor.Dal.Models;
using Sicoob.Visualizer.Monitor.Dal.Models.Enums;
using System.Data;
using System.Net;
using System.Net.Http.Headers;
using static Sicoob.Visualizer.Monitor.Comuns.Settings;
using ActivityType = Sicoob.Visualizer.Monitor.Dal.Models.Enums.ActivityType;
using Item = Sicoob.Visualizer.Monitor.Dal.Models.Item;
using Timer = System.Timers.Timer;

namespace Sicoob.Visualizer.Monitor.Comuns.Helpers
{
    public class GraphHelper : IDisposable
    {
        // Settings object
        private readonly OAuthSettings _settings;
        // Client configured with user authentication
        private readonly GraphServiceClient _userClient;
        private GraphAuthentication? graphAccessToken;
        private readonly Authenticator? _authenticator;
        private readonly Random _random;
        private readonly MonitorContext ctx;
        private static Timer _requestCountClearTimer;
        private static int _requestCount;
        private static DateTime _lastRequestAwait;
        private readonly string connString;
        private string RandomColor => string.Format("#{0:X6}", _random.Next(0x1000000));

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
                    (sender, args)
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
            connString = ctx.Database.GetConnectionString();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
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
                    Color = RandomColor
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
            Account? account = await ctx.Accounts.FirstOrDefaultAsync(acc => acc.Id == user.Id);

            if (account == null)
            {
                account = new Account()
                {
                    Id = user.Id,
                    Email = user.Mail,
                    Name = user.DisplayName,
                    Color = RandomColor
                };

                await ctx.Accounts.AddAsync(account);
            }
            else
            {
                account.Name = user.DisplayName;
                account.Email = user.Mail;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="drive"></param>
        /// <param name="item"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<FileActivity[]> GetActivityAsync(string drive, string item, ActivityType type)
        {
            var url = $"https://graph.microsoft.com/v1.0/drives/{drive}/items/{item.Replace("\"", string.Empty)}/analytics/allTime?%24expand=activities(%24filter%3D{Enum.GetName(type).ToLower()}%20ne%20null)";
            HttpRequestMessage request = new(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue(graphAccessToken.TokenType, graphAccessToken.AccessToken);

            HttpClient httpClient = new();
            var response = await httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                Thread.Sleep((int)(response.Headers.RetryAfter?.Delta?.TotalMilliseconds ?? 5000));
                return await GetActivityAsync(drive, item, type);
            }

            string body = await response.Content.ReadAsStringAsync();

            ActivitiesResult? obj = JsonConvert.DeserializeObject<ActivitiesResult>(body);

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
            directory = directory.Substring(dir.Length + 1, directory.Length - dir.Length - lItem.Name.Length - 1);

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
                Thread.Sleep((int)(_lastRequestAwait + TimeSpan.FromMilliseconds(1000 * 60) - DateTime.Now).TotalMilliseconds);

            _requestCount++;
        }

        /// <summary>
        /// Obtém todas as atividades 
        /// </summary>
        /// <param name="page">Página atual</param>
        /// <param name="ascending">Ordem crescente.</param>
        /// <param name="pages">Retorna a quantidade de páginas.</param>
        /// <param name="perPage">Quantidade de itens das páginas.</param>
        /// <param name="fileName">Nome do arquivo selecionado na busca.</param>
        /// <param name="userName">Nome do usuário que está sendo buscado.</param>
        /// <param name="userEmail">Email do usuário buscado.</param>
        /// <param name="start">Data de inicio da busca.</param>
        /// <param name="end">Data final da busca.</param>
        /// <returns>Lista de atividades filtradas e ordenadas.</returns>
        public Activity[] GetActivities(ref int page, bool ascending, out int pages, int perPage = 0, string? fileName = null, string? userName = null, string? userEmail = null, DateTime? start = null, DateTime? end = null)
        {
            var dbCtx = new MonitorContext(connString);

            bool useMail = string.IsNullOrEmpty(userEmail),
                useName = string.IsNullOrEmpty(userName),
                flName = string.IsNullOrEmpty(fileName);

#pragma warning disable CS8604 // Possível argumento de referência nula.
            IQueryable<Activity> activitiesQuery = from act in dbCtx.Activities
                                                   where (useMail || act.Account.Email.Contains(userEmail)) &&
                                                         (useName || act.Account.Name.Contains(userName)) &&
                                                         (flName || act.Item.Name.Contains(fileName))
                                                   select act;
#pragma warning restore CS8604 // Possível argumento de referência nula.

            if (perPage > 0)
            {
                double pagesDouble = activitiesQuery.Count() / (double)perPage;
                pages = pagesDouble > (int)pagesDouble ? (int)pagesDouble + 1 : (int)pagesDouble;

                if (page < 1)
                    page = 1;

                if (page > pages)
                    page = pages;

                if (pages > 1)
                    activitiesQuery = activitiesQuery.Skip((int)((page - 1) * (double)perPage))
                                                       .Take(perPage);
            }
            else
                pages = 1;

            if (ascending)
                activitiesQuery = activitiesQuery.OrderBy(act => act.Date);
            else
                activitiesQuery = activitiesQuery.OrderByDescending(act => act.Date);

            try
            {
                var activities = activitiesQuery
                                    .Include(act => act.Account)
                                    .Include(act => act.Item)
                                    .ToArray();

                return activities;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void ClearLogin()
        {
            _ = ctx.Database.ExecuteSqlRaw("TRUNCATE TABLE [Authentications]");
        }
        public void Dispose()
        {
            ctx.Dispose();
            _authenticator?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

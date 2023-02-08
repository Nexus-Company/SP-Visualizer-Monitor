using Newtonsoft.Json;
using SP.Visualizer.Monitor.Dal.Models.Enums;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Web;
using static SP.Visualizer.Monitor.Comuns.Settings;
using Process = System.Diagnostics.Process;

namespace SP.Visualizer.Monitor.Comuns
{
    internal class Authenticator : IDisposable
    {
        private OAuthSettings _settings;
        private HttpListener _server;
        private AccessToken? _access;
        private string baseOAuth
            => $"https://login.microsoftonline.com/organizations/oauth2/v2.0/";
        public Authenticator(OAuthSettings settings, bool listener)
        {
            _settings = settings;
            _server = new HttpListener();

            if (listener)
            {
                _server.Prefixes.Add(settings.RedirectUrl);
                _server.Start();
            }
        }
        public void RequestLogin()
        {
            string url = $"{baseOAuth}authorize?grant_type=client_credentials" +
                $"&client_id={HttpUtility.UrlEncode(_settings.ClientId)}" +
                $"&response_type=code" +
                $"&response_mode=query" +
                $"&scope={GetScopes(_settings.GraphUserScopes)}" +
                $"&state=12345";

            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true,
                Verb = "open"
            });

            _access = null;
        }
        public void RequestLogin(string sharepointDomain)
        {
            string url = $"{baseOAuth}authorize?grant_type=client_credentials" +
                $"&client_id={HttpUtility.UrlEncode(_settings.ClientId)}" +
                $"&response_type=code" +
                $"&response_mode=query" +
                $"&scope={GetSharepointScopes(sharepointDomain, _settings.SharepointUserScopes)}" +
                $"&state=12345";

            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true,
                Verb = "open"
            });

            _access = null;
        }
        public async Task<AccessToken?> AwaitLoginAsync(AuthenticationType type)
        {
            while (_access == null)
            {
                HttpListenerContext ctx = _server.GetContext();
                HttpListenerRequest resquet = ctx.Request;

                using HttpListenerResponse resp = ctx.Response;
                var query = HttpUtility.ParseQueryString(resquet.Url?.Query ?? "");
                string code = query["code"] ?? string.Empty;

                if (string.IsNullOrEmpty(code))
                {
                    resp.StatusCode = (int)HttpStatusCode.BadRequest;
                    resp.StatusDescription = "Request is bad";
                    continue;
                }

                resp.StatusCode = (int)HttpStatusCode.OK;
                resp.StatusDescription = "Status OK";

                string content = "grant_type=authorization_code" +
                    $"&client_id={HttpUtility.UrlEncode(_settings.ClientId)}" +
                    $"&scope={GetScopes(_settings.GraphUserScopes)}" +
                    $"&code={HttpUtility.UrlEncode(code)}";

                HttpClient httpClient = new();

                var request = new HttpRequestMessage(HttpMethod.Post, $"{baseOAuth}token")
                {
                    Content = new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                var response = await httpClient.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                _access = JsonConvert.DeserializeObject<AccessToken>(body);
                _access.Type = type;
                return _access;
            }

            return null;
        }

        public async Task<AccessToken> RefreshLoginAsync(string refreshToken)
        {
            string content = "grant_type=refresh_token" +
                            $"&client_id={HttpUtility.UrlEncode(_settings.ClientId)}" +
                            $"&scope={GetScopes(_settings.GraphUserScopes)}" +
                            $"&refresh_token={HttpUtility.UrlEncode(refreshToken)}";

            HttpClient httpClient = new();

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseOAuth}token")
            {
                Content = new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            var response = await httpClient.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            _access = JsonConvert.DeserializeObject<AccessToken>(body);
            return _access;
        }
        private static string GetScopes(string[] graphScopes)
        {
            string scopes = string.Empty;

            foreach (var item in graphScopes)
                scopes += item + "%20";

            scopes = scopes.Remove(scopes.Length - 3, 3);

            return scopes;
        }
        private static string GetSharepointScopes(string domain, string[] sharepointScopes)
        {
            string scopes = string.Empty;

            foreach (var item in sharepointScopes)
                scopes += $"https%3A%2F%2F{domain}%2F{item}%20";

            scopes = scopes.Remove(scopes.Length - 3, 3);

            return scopes;
        }
        public void Dispose()
        {
            _server.Stop();
        }
    }

    public class AccessToken
    {
        public string Token_type { get; set; }
        public string Access_token { get; set; }
        public string Refresh_token { get; set; }
        public string email { get; set; }
        public int Expires_in { get; set; }
        public AuthenticationType Type { get; set; }
    }
}

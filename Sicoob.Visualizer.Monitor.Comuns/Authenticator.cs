using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Process = System.Diagnostics.Process;

namespace Sicoob.Visualizer.Monitor.Comuns
{
    internal class Authenticator : IDisposable
    {
        private Settings _settings;
        private HttpListener _server;
        private AccessToken _access;
        private string baseOAuth
            => $"https://login.microsoftonline.com/organizations/oauth2/v2.0/";
        public Authenticator(Settings settings)
        {
            _settings = settings;
            _server = new HttpListener();
            _server.Prefixes.Add(settings.RedirectUrl);
            _server.Start();
        }
        public void RequestLogin()
        {
            string url = $"{baseOAuth}authorize?grant_type=client_credentials&" +
                $"client_id={HttpUtility.UrlEncode(_settings.ClientId)}&" +
                $"response_type=code&" +
                $"redirect_uri={HttpUtility.UrlEncode(_settings.RedirectUrl)}&" +
                $"response_mode=query&" +
                $"scope={GetScopes(_settings.GraphUserScopes)}" +
                $"&state=12345";

            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true,
                Verb = "open"
            });
        }
        public async Task<AccessToken> AwaitLoginAsync()
        {
            while (_access == null)
            {
                HttpListenerContext ctx = _server.GetContext();
                HttpListenerRequest resquet = ctx.Request;

                using (HttpListenerResponse resp = ctx.Response)
                {
                    var query = HttpUtility.ParseQueryString(resquet.Url?.Query ?? "");
                    string code = query["code"] ?? string.Empty;

                    if (string.IsNullOrEmpty(code))
                    {
                        resp.StatusCode = (int)HttpStatusCode.BadRequest;
                        resp.StatusDescription = "Request is bad";
                    }

                    resp.StatusCode = (int)HttpStatusCode.OK;
                    resp.StatusDescription = "Status OK";

                    string content = "grant_type=client_credentials&" +
                        $"client_id={HttpUtility.UrlEncode(_settings.ClientId)}&" +
                        $"scope={HttpUtility.UrlEncode("https://graph.microsoft.com/.default")} offline_access&" +
                        $"code={HttpUtility.UrlEncode(code)}&" +
                        $"client_secret={HttpUtility.UrlEncode(_settings.ClientSecret)}";
                    HttpClient httpClient = new HttpClient();

                    var request = new HttpRequestMessage(HttpMethod.Post, $"{baseOAuth}token")
                    {
                        Content = new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded")
                    };

                    var response = await httpClient.SendAsync(request);
                    string body = await response.Content.ReadAsStringAsync();

                    _access = JsonConvert.DeserializeObject<AccessToken>(body);
                    return _access;
                }
            }

            return null;
        }
        private static string GetScopes(string[] graphScopes)
        {
            string scopes = string.Empty;

            foreach (var item in graphScopes)
                scopes += item + "%20";

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
        public int Expires_in { get; set; }
    }
}

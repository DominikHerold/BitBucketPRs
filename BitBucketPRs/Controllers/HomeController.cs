using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using BitBucketPRs.Configuration;

using Microsoft.AspNetCore.Mvc;
using BitBucketPRs.Models;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

namespace BitBucketPRs.Controllers
{
    public class HomeController : Controller
    {
        private readonly PrConfiguration _configuration;
        private static bool Focused = true;

        private static readonly HashSet<string> Prs = new HashSet<string>();

        private static bool NewPrs;

        private static string Cookie = "foo=bar";

        public HomeController(IOptions<PrConfiguration> options)
        {
            _configuration = options.Value;
        }

        public async Task<IActionResult> Index()
        {
            var reposResult = await GetContent($"https://{_configuration.Host}/rest/api/latest/projects/{_configuration.ProjectKey}/repos?limit=1000");
            var repos = JsonConvert.DeserializeObject<RootObject>(reposResult);

            var prOverView = new PrOverviews { Prs = new List<PrOverview>(), LastUpdated = DateTime.Now };
            foreach (var value in repos.Values)
            {
                var slug = value.Slug;
                var repoResult = await GetContent($"https://{_configuration.Host}/rest/api/latest/projects/{_configuration.ProjectKey}/repos/{slug}/pull-requests?limit=1000");
                var repo = JsonConvert.DeserializeObject<RootObject>(repoResult);

                foreach (var valueSlug in repo.Values)
                {
                    var reviewers = valueSlug.Reviewers;
                    var link = valueSlug.Links.Self.First().Href;
                    if (_configuration.BlockedLinks.Contains(link))
                        continue;

                    var approved = reviewers != null && reviewers.Any(r => r.Approved && r.User.Name == _configuration.UserName);
                    prOverView.Prs.Add(new PrOverview { Link = link, Name = string.IsNullOrWhiteSpace(valueSlug.Title) ? "foo" : valueSlug.Title, AlreadyApproved = approved });
                    if ((!Prs.Add(link) || Focused) && !NewPrs)
                        continue;

                    prOverView.NewPrs = true;
                    NewPrs = true;
                }
            }

            return View(prOverView);
        }

        public IActionResult Error()
        {
            var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
            return View(context);
        }

        public IActionResult Focus()
        {
            Focused = true;
            NewPrs = false;

            return new OkResult();
        }

        public IActionResult Blur()
        {
            Focused = false;

            return new OkResult();
        }

        private async Task<string> GetContent(string address)
        {
            using (var webClient = new HttpClient())
            {
                var retry = 0;
                HttpResponseMessage response;
                do
                {
                    retry++;
                    if (retry == 3)
                        throw new InvalidOperationException("Can not authenticate");

                    webClient.DefaultRequestHeaders.Clear();
                    webClient.DefaultRequestHeaders.Add(nameof(Cookie), Cookie);
                    response = await webClient.GetAsync(address);
                }
                while (response.StatusCode == HttpStatusCode.Unauthorized && await Authenticate());

                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
        }

        private async Task<bool> Authenticate()
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = false };
            var client = new HttpClient(handler);
            var toSend =
                $"j_username={WebUtility.UrlEncode(_configuration.UserName)}&j_password={WebUtility.UrlEncode(_configuration.Password)}&_atl_remember_me=on&queryString=nextUrl%3D%252Fdashboard&submit=Anmelden";
            var response = await client.PostAsync($"https://{_configuration.Host}/j_atl_security_check", new StringContent(toSend, Encoding.UTF8, "application/x-www-form-urlencoded"));
            var isSetCookie = response.Headers.TryGetValues("Set-Cookie", out var setCookies);
            if (!isSetCookie || setCookies == null)
                return true;

            foreach (var setCookie in setCookies)
            {
                if (!setCookie.StartsWith("BITBUCKETSESSIONID=", StringComparison.OrdinalIgnoreCase))
                    continue;

                var endIndex = setCookie.IndexOf(';');
                var newCookie = setCookie.Substring(0, endIndex);
                Cookie = newCookie;
                return true;
            }

            return true;
        }
    }
}

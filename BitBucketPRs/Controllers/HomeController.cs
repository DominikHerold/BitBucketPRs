using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using BitBucketPRs.Configuration;

using Microsoft.AspNetCore.Mvc;
using BitBucketPRs.Models;

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
        private string _cookie;

        public HomeController(IOptions<PrConfiguration> options)
        {
            _configuration = options.Value;
            _cookie = _configuration.Cookie;
        }

        public async Task<IActionResult> Index()
        {
            var reposResult = await GetContent($"https://{_configuration.Host}/rest/api/latest/projects/{_configuration.ProjectKey}/repos/");
            var repos = JsonConvert.DeserializeObject<RootObject>(reposResult);

            var prOverView = new PrOverviews { Prs = new List<PrOverview>(), LastUpdated = DateTime.Now };
            foreach (var value in repos.Values)
            {
                var slug = value.Slug;
                var repoResult = await GetContent($"https://{_configuration.Host}/rest/api/latest/projects/{_configuration.ProjectKey}/repos/{slug}/pull-requests");
                var repo = JsonConvert.DeserializeObject<RootObject>(repoResult);

                foreach (var valueSlug in repo.Values)
                {
                    var reviewers = valueSlug.Reviewers;
                    var link = valueSlug.Links.Self.First().Href;
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
                webClient.DefaultRequestHeaders.Add(nameof(PrConfiguration.Cookie), _cookie);
                var response = await webClient.GetAsync(address);

                var isSetCookie = response.Headers.TryGetValues("Set-Cookie", out var setCookies);
                if (isSetCookie && setCookies != null)
                {
                    foreach (var setCookie in setCookies)
                    {
                        if (!setCookie.StartsWith("BITBUCKETSESSIONID=", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var endIndex = setCookie.IndexOf(';');
                        var newCookie = setCookie.Substring(0, endIndex);
                        _cookie = newCookie;
                    }
                }

                var content = await response.Content.ReadAsStringAsync();

                return content;
            }
        }
    }
}

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

        public HomeController(IOptions<PrConfiguration> options)
        {
            _configuration = options.Value;
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
                    prOverView.Prs.Add(new PrOverview { Link = valueSlug.Links.Self.First().Href, Name = string.IsNullOrWhiteSpace(valueSlug.Title) ? "foo" : valueSlug.Title });
            }

            return View(prOverView);
        }

        private async Task<string> GetContent(string address)
        {
            using (var webClient = new HttpClient())
            {
                webClient.DefaultRequestHeaders.Add(nameof(PrConfiguration.Cookie), _configuration.Cookie);
                var response = await webClient.GetAsync(address);
                var content = await response.Content.ReadAsStringAsync();

                return content;
            }
        }
    }
}

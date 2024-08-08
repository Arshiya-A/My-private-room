using System.Text.RegularExpressions;
using System.Web;
using GithubProfileViewer;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Extensions;

namespace MyApp.Namespace
{
    [Route("api/[controller]")]
    [ApiController]
    public class GithubController : ControllerBase
    {

        private HttpClient? _httpClient;
        private Uri? _uri;
        private HtmlDocument? _htmlDoc;



        [HttpGet("Profile/{name}")]
        public async Task<ActionResult<string>> GetProfile(string name)
        {
            //houshmand-2005
            var pageResult = await LoadPage(name);
            if (pageResult)
            {
                var iamgenode = _htmlDoc?.DocumentNode.SelectNodes("//img")[2].Attributes["src"];
                var image = iamgenode?.Value;

                return Ok(image);
            }

            return NotFound("User not found in github.");
        }


        [HttpGet("Info/{name}")]
        public async Task<ActionResult<UserInfo>> GetInfo(string name)
        {
            //houshmand-2005
            var pageResult = await LoadPage(name);
            if (pageResult)
            {
                var additionalNameSpan = _htmlDoc.DocumentNode.SelectNodes("//span")[37];
                var nameSpan = _htmlDoc.DocumentNode.SelectNodes("//span")[36];

                var bioDiv_Type1 = _htmlDoc.DocumentNode.SelectNodes("//div")[138];
                var bioDiv_Type2 = _htmlDoc.DocumentNode.SelectNodes("//div")[146];

                string additionalName = GetAdditonalName(additionalNameSpan);

                string username = GetUsername(nameSpan);

                string bio = "User is not have bio";
                bio = GetBio(bioDiv_Type1, bioDiv_Type2);

                var userInfo = new UserInfo()
                {
                    AdditionalName = additionalName,
                    Name = username,
                    Bio = bio,
                };

                return Ok(userInfo);
            }

            return NotFound("User not found in github.");
        }


        [HttpGet("Repo/{name}")]
        public async Task<ActionResult<IEnumerable<RepositoryInfo>>> RepositoryInfo(string name)
        {
            var repoResult = await LoadRepositoryPage(name);
            if (repoResult)
            {
                var ulElement = _htmlDoc.DocumentNode.SelectNodes("//ul[@data-filterable-type='substring']");
                var lis = ulElement.Select(l => l.ChildNodes.Select(i => i.InnerHtml));

                List<string> titles = new();
                List<string> descriptions = new();
                List<string> languages = new();
                List<string> tags = new();
                List<DateTime> dateTimes = new();


                List<RepositoryInfo> repositoryInfos = new();

                RepositoryList(lis, titles, descriptions, languages, dateTimes);

                for (int i = 0; i < titles.Count(); i++)
                {
                    repositoryInfos.Add(
                        new RepositoryInfo()
                        {
                            Name = titles[i],
                            Description = descriptions[i],
                            Languages = languages[i],
                            PublishDate = dateTimes[i],
                        }
                    );
                }
                var result = dateTimes;
                return Ok(repositoryInfos);
            }

            return NotFound();
        }

        private  void RepositoryList(IEnumerable<IEnumerable<string>> lis, List<string> titles, List<string> descriptions, List<string> languages, List<DateTime> dateTimes)
        {
            foreach (var li in lis)
            {
                foreach (var item in li.Skip(1))
                {
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(item);

                    try
                    {

                        var title = htmlDoc.DocumentNode.SelectSingleNode("//a").InnerHtml
                        .Replace("\n", string.Empty)
                        .TrimStart();

                        var repoDescritption = htmlDoc.DocumentNode.
                        SelectSingleNode("//p[@class = 'col-9 d-inline-block color-fg-muted mb-2 pr-4']")
                        .InnerHtml
                        .TrimStart()
                        .Replace("\n", string.Empty)
                        .Replace("  ", string.Empty);

                        var language = htmlDoc.DocumentNode
                        .SelectSingleNode("//span[@itemprop = 'programmingLanguage']")
                        .InnerHtml;

                        var dateTime = htmlDoc.DocumentNode
                        .SelectSingleNode("//relative-time[@datetime]").InnerHtml;

                        if (title is not null)
                            titles.Add(title);

                        descriptions.Add(repoDescritption);
                        languages.Add(language);
                        dateTimes.Add(Convert.ToDateTime(dateTime));
                    }
                    catch
                    {
                        continue;
                    }
                }

            }
        }

        private async Task<bool> LoadPage(string name)
        {
            var uri = "https://github.com/" + name;

            _uri = new Uri(uri);

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = _uri;

            var webPage = new HtmlWeb();
            _htmlDoc = webPage.Load(_httpClient.BaseAddress);
            var pageFounder = _htmlDoc.DocumentNode.SelectSingleNode("//title");

            if (pageFounder.InnerHtml == "Page not found · GitHub · GitHub")
                return false;

            return true;
        }

        private async Task<bool> LoadRepositoryPage(string name)
        {
            var uri = "https://github.com/" + name + "?tab=repositories";

            _uri = new Uri(uri);

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = _uri;

            var webPage = new HtmlWeb();
            _htmlDoc = webPage.Load(_httpClient.BaseAddress);
            var repoFounder = _htmlDoc.DocumentNode.SelectNodes("//h2")[2];
            var repoFounderText = repoFounder.InnerText
            .TrimStart()
            .TrimEnd('\r', '\n');


            if (repoFounderText == name + " doesn’t have any public repositories yet.")
                return false;

            return true;
        }



        private string GetBio(HtmlNode bioDiv_Type1, HtmlNode bioDiv_Type2)
        {
            string bio;
            if (bioDiv_Type1.ChildNodes.Count() > 1)
            {
                var htmlDecode = HttpUtility.HtmlDecode(bioDiv_Type2.InnerHtml);
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlDecode);

                var bioElement = htmlDoc.DocumentNode.SelectNodes("//div")[6];
                bio = bioElement.InnerHtml.TrimEnd('\r', '\n'); ;
            }
            else
                bio = bioDiv_Type1.InnerHtml;


            bio.
            Replace("\n", string.Empty)
            .TrimStart();
            return bio;
        }

        private string GetUsername(HtmlNode nameSpan)
        {
            return nameSpan.InnerHtml
            .Replace("\n", string.Empty)
            .Replace(" ", string.Empty)
            .TrimStart();
        }

        private string GetAdditonalName(HtmlNode additionalNameSpan)
        {
            return additionalNameSpan.InnerHtml
            .Replace("\n", string.Empty)
            .Replace(" ", string.Empty)
            .TrimStart();
        }
    }
}

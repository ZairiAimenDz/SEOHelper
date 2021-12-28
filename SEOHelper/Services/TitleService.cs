using Newtonsoft.Json;
using SEOHelper.Models;

namespace SEOHelper.Service
{
    public class TitleService : ITitleService
    {
        static Random rnd = new Random();
        public string[] Titles;
        public string[] URLs;
        public string[] Headings;
        public TitleService()
        {
            var json = JsonConvert.DeserializeObject<JsonRoot>(File.ReadAllText("examples.json"));
            if (json != null)
            {
                Titles = json.Titles;
                Headings = json.Headings;
                URLs = json.URL;
            }
        }
        public string[] GetHeadings(string[] Keywords,string[]? Post,string[]? Pre)
        {
            var newheadings = new List<string>();
            foreach (var keyword in Keywords)
            {
                Headings.ToList().ForEach(t => newheadings.Add(t.Replace("SUBJECT", keyword)));

                if (Post != null)
                    foreach (var postWord in Post)
                    {
                        Headings.ToList().ForEach(t => newheadings.Add(t.Replace("SUBJECT", keyword) + " " + postWord));
                        if (Pre != null)
                            foreach (var preWord in Pre)
                            {
                                Headings.ToList().ForEach(t => newheadings.Add(t.Replace("SUBJECT", preWord + " " + keyword)));
                                Headings.ToList().ForEach(t => newheadings.Add(t.Replace("SUBJECT", preWord + " " + keyword) + " " + postWord));
                            }
                    }
                else if (Pre != null)
                    foreach (var preWord in Pre)
                    {
                        Headings.ToList().ForEach(t => newheadings.Add(t.Replace("SUBJECT", preWord + " " + keyword)));
                    }
            }

            var resHeadings = new string[5];
            for (int i = 0; i < 5; i++)
            {
                int r = rnd.Next(newheadings.Count());
                resHeadings[i] = newheadings[r];
            }

            return resHeadings;
        }

        public string[] GetTitles(string[] Keywords,string[]? Post,string[]? Pre)
        {
            var newtitles = new List<string>();
            foreach (var keyword in Keywords)
            {
                Titles.ToList().ForEach(t => newtitles.Add(t.Replace("SUBJECT", keyword)));
                if(Post != null)
                    foreach (var postWord in Post)
                    {
                        Headings.ToList().ForEach(t => newtitles.Add(t.Replace("SUBJECT", keyword) + " " + postWord));
                        if (Pre != null)
                            foreach (var preWord in Pre)
                            {
                                Headings.ToList().ForEach(t => newtitles.Add(t.Replace("SUBJECT", preWord + " " + keyword)));
                                Headings.ToList().ForEach(t => newtitles.Add(t.Replace("SUBJECT", preWord + " " + keyword) + " " + postWord));
                            }
                    }
                else if(Pre != null)
                    foreach (var preWord in Pre)
                    {
                        Headings.ToList().ForEach(t => newtitles.Add(t.Replace("SUBJECT", preWord + " " + keyword)));
                    }
            }

            var resTitles = new string[10];
            for (int i = 0; i < 10; i++)
            {
                int r = rnd.Next(newtitles.Count());
                resTitles[i] = newtitles[r];
            }

            return resTitles;
        }

        public string[] GetURL(string baseURL, string[] Keywords, string? title)
        {
            var newUrls = new List<string>();
            foreach (var keyword in Keywords)
            {
                URLs.ToList().ForEach(u => newUrls.Add(baseURL+"/"+ u.Replace("SUBJECT", keyword.Replace(" ","-"))));
            }

            if(!string.IsNullOrEmpty(title))
                newUrls.Add(baseURL + "/" + title.Replace(' ','-'));

            var resURLS = new string[5];
            for (int i = 0; i < 5; i++)
            {
                int r = rnd.Next(newUrls.Count());
                resURLS[i] = newUrls[r];
            }

            return resURLS;
        }
    }
}

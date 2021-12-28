using Google.Ads.GoogleAds;
using Google.Ads.GoogleAds.Lib;
using Google.Ads.GoogleAds.V9.Common;
using Google.Ads.GoogleAds.V9.Errors;
using Google.Ads.GoogleAds.V9.Services;
using SEOHelper.Models;
using System;
namespace SEOHelper.Service
{
    public class KeywordService : IKeywordService
    {
        // TODO : Define The Client
        public GoogleAdsClient client;
        public string customerId;
        public KeywordService()
        {
            client = new GoogleAdsClient();
            customerId = "1777390596";
        }


        public KeywordResult[] GetHashtags(string[] keyword)
        {
            List<KeywordResult> results = new();
            foreach (var item in this.GetKeywords(string.Empty, keyword, null))
            {
                results.Add(new KeywordResult() { Keyword = "#"+item.Keyword.Replace(" ","")
                    ,avgMonthlySearches=item.avgMonthlySearches });
            }
            return results.OrderByDescending(r=>r.avgMonthlySearches).Where(r=>!r.Keyword.ToLower().Contains("nearme")).Take(10).ToArray();
        }

        public KeywordResult[] GetKeywords(string url, string[] keyword,string? location)
        {
            KeywordPlanIdeaServiceClient keywordPlanIdeaService =
                client.GetService(Services.V9.KeywordPlanIdeaService);

            if (keyword.Length == 0 && string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("At least one of keywords or page URL is required, " +
                    "but neither was specified.");
            }

            GenerateKeywordIdeasRequest request = new GenerateKeywordIdeasRequest()
            {
                CustomerId = customerId,
            };

            if (keyword.Length == 0)
            {
                // Only page URL was specified, so use a UrlSeed.
                request.UrlSeed = new UrlSeed()
                {
                    Url = url
                };
            }
            else if (string.IsNullOrEmpty(url))
            {
                // Only keywords were specified, so use a KeywordSeed.
                request.KeywordSeed = new KeywordSeed();
                request.KeywordSeed.Keywords.AddRange(keyword);
            }
            else
            {
                // Both page URL and keywords were specified, so use a KeywordAndUrlSeed.
                request.KeywordAndUrlSeed = new KeywordAndUrlSeed();
                request.KeywordAndUrlSeed.Url = url;
                request.KeywordAndUrlSeed.Keywords.AddRange(keyword);
            }

            if (!string.IsNullOrEmpty(location))
            {
                var ids = GeoLocationService.GetLocationId(location);
                foreach (long locationId in ids)
                {
                    request.GeoTargetConstants.Add(ResourceNames.GeoTargetConstant(locationId));
                }
            }

            try
            {
                // Generate keyword ideas based on the specified parameters.
                var response =
                    keywordPlanIdeaService.GenerateKeywordIdeas(request);

                var results = new List<KeywordResult>();
                // Iterate over the results and print its detail.
                foreach (GenerateKeywordIdeaResult result in response)
                {
                    KeywordPlanHistoricalMetrics metrics = result.KeywordIdeaMetrics;
                    results.Add(new() {Keyword=result.Text,avgMonthlySearches=metrics is null ? 0:metrics.AvgMonthlySearches});
                }
                return results.ToArray();          

            }
            catch (GoogleAdsException e)
            {
                Console.WriteLine("Failure:");
                Console.WriteLine($"Message: {e.Message}");
                Console.WriteLine($"Failure: {e.Failure}");
                Console.WriteLine($"Request ID: {e.RequestId}");
                throw;
            }
        }
    }
}

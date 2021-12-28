using SEOHelper.Models;

namespace SEOHelper.Service
{
    public interface IKeywordService
    {
        public KeywordResult[] GetKeywords(string url,string[] keyword,string? location);
        public KeywordResult[] GetHashtags(string[] keyword);
    }
}

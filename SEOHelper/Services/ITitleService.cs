namespace SEOHelper.Service
{
    public interface ITitleService
    {
        public string[] GetTitles(string[] Keywords,string[]? Post,string[]? Pre);
        public string[] GetHeadings(string[] Keywords,string[]? Post,string[]? Pre);
        public string[] GetURL(string baseURL,string[] Keywords,string? title);
    }
}

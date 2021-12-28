namespace SEOHelper.Service
{
    public interface IContentGenerationService
    {
        public Task<string[]> GenerateMeta(string[] Keywords, string? content);
        public Task<string[]> GenerateContent(string[] Keywords);
    }
}

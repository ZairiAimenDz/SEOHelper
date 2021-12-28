namespace SEOHelper.Models
{
    public class MobileResponsivness
    {
        public Teststatus testStatus { get; set; }
        public string mobileFriendliness { get; set; }
        public Resourceissue[] resourceIssues { get; set; }
    }

    public class Teststatus
    {
        public string status { get; set; }
    }

    public class Resourceissue
    {
        public Blockedresource blockedResource { get; set; }
    }

    public class Blockedresource
    {
        public string url { get; set; }
    }

}

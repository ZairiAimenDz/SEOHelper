namespace SEOHelper.Models
{
    public class JsonRoot
    {
        public string[] Titles { get; set; }
        public string[] URL { get; set; }
        public string[] Headings { get; set; }
    }


    public class TweetJson
    {
        public Datum[] data { get; set; }
        public Meta meta { get; set; }
    }

    public class Meta
    {
        public int total_tweet_count { get; set; }
    }

    public class Datum
    {
        public DateTime end { get; set; }
        public DateTime start { get; set; }
        public int tweet_count { get; set; }
    }

}

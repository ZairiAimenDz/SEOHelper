namespace SEOHelper.Models
{
    public class OpenAIObject
    {
        public string prompt { get; set; }
        public float temperature { get; set; } = 0.2f;
        public int max_tokens { get; set; } = 60;
        public float top_p { get; set; } = 1;
        public float frequency_penalty { get; set; } = 0;
        public float presence_penalty { get; set; } = 0;
    }


    public class APIResult
    {
        public string id { get; set; }
        public string _object { get; set; }
        public int created { get; set; }
        public string model { get; set; }
        public Choice[] choices { get; set; }
    }

    public class Choice
    {
        public string text { get; set; }
        public int index { get; set; }
        public object logprobs { get; set; }
        public string finish_reason { get; set; }
    }


}

namespace historydemo1.Models.Options
{
    public class AIOptions
    {
        public const string SectionName = "OpenAI";
        public string ApiKey { get; set; }
        public string Endpoint { get; set; }
        public string ModelId { get; set; }
    }
}

namespace ElasticLogger
{
    public class ElasticLoggerItem
    {
        public string Index { get; set; }

        public string Type { get; set; }

        public string Json { get; set; }

        public string ToString(string lineBreakChar)
        {
            return "{ \"index\" : { \"_index\": \"" + this.Index + "\", \"_type\": \"" + this.Type + "\" } }" + lineBreakChar + this.Json + lineBreakChar;
        }
    }
}

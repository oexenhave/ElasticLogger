using System;
using Newtonsoft.Json;

namespace ElasticLogger
{
    /// <summary>
    /// Logger item class used for temporary storing the logged object in the queue before transmitting to Elastic Search
    /// </summary>
    public class ElasticLoggerItem
    {
        /// <summary>
        /// Gets or sets the Elastic Search index
        /// </summary>
        public string Index { get; set; }

        /// <summary>
        /// Gets or sets the Elastic Search type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the json string associated with this log item
        /// </summary>
        public string Json { get; set; }

        /// <summary>
        /// Gets or sets the dynamic object to transfer to Elastic Search. Getting or setting this will instantly de-/serialize the dynamic object
        /// </summary>
        public dynamic Object
        {
            get { return JsonConvert.DeserializeObject(this.Json); }
            set { this.Json = JsonConvert.SerializeObject(value); }
        }

        /// <summary>
        /// Gets the json string necessary for the _bulk endpoint in Elastic Search
        /// </summary>
        /// <param name="lineBreakChar">The line break char to use then breaking lines</param>
        /// <param name="indexAppendDateFormat">The optional date pattern to append to the index</param>
        /// <returns>A json string</returns>
        public string ToString(string lineBreakChar, string indexAppendDateFormat)
        {
            var index = this.Index;
            if (!string.IsNullOrWhiteSpace(indexAppendDateFormat))
            {
                index = this.Index + DateTime.Now.ToString(indexAppendDateFormat);
            }

            return "{ \"index\" : { \"_index\": \"" + index + "\", \"_type\": \"" + this.Type + "\" } }" + lineBreakChar + this.Json + lineBreakChar;
        }
    }
}

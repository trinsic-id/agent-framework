using System.Collections.Generic;
using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Records.Search
{
    /// <summary>
    /// Search record result.
    /// </summary>
    public class SearchRecordResult
    {
        /// <summary>
        /// Gets or sets the resulting records.
        /// </summary>
        /// <value>The resulting records.</value>
        [JsonProperty("records")]
        public List<SearchRecordItem> Records { get; set; }
    }
}

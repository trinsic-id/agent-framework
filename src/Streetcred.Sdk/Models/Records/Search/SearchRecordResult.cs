using System.Collections.Generic;
using Newtonsoft.Json;

namespace Streetcred.Sdk.Model.Records.Search
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

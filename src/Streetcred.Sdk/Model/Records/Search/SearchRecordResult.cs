using System.Collections.Generic;
using Newtonsoft.Json;

namespace Streetcred.Sdk.Model.Records.Search
{
    public class SearchRecordResult
    {
        [JsonProperty("records")]
        public List<SearchRecordItem> Records { get; set; }
    }
}

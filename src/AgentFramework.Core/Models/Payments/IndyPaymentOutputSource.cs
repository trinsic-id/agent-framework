using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Payments
{
    public class IndyPaymentOutputSource
    {
        /// <summary>
        /// Gets or sets the recipient.
        /// </summary>
        /// <value>
        /// The recipient.
        /// </value>
        [JsonProperty("recipient")]
        public string Recipient { get; set; }

        /// <summary>
        /// Gets or sets the amount.
        /// </summary>
        /// <value>
        /// The amount.
        /// </value>
        [JsonProperty("amount")]
        public ulong Amount { get; set; }

        /// <summary>
        /// Gets or sets the receipt (utxo source)
        /// </summary>
        [JsonProperty("receipt", NullValueHandling = NullValueHandling.Ignore)]
        public string Receipt { get; set; }

        /// <summary>
        /// Gets or sets extra details
        /// </summary>
        [JsonProperty("extra", NullValueHandling = NullValueHandling.Ignore)]
        public string Extra { get; set; }
    }
}

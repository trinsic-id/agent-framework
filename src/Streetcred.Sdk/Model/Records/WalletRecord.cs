using System.Collections.Generic;
using Newtonsoft.Json;

namespace Streetcred.Sdk.Model.Records
{
    /// <summary>
    /// Wallet record.
    /// </summary>
    public abstract class WalletRecord
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        public abstract string GetId();

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <returns>The type name.</returns>
        public abstract string GetTypeName();

        [JsonIgnore]
        public Dictionary<string, string> Tags { get; } = new Dictionary<string, string>();
    }
}

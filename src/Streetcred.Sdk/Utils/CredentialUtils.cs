using System;
using System.Collections.Generic;
using Hyperledger.Indy.AnonCredsApi;

namespace Streetcred.Sdk.Utils
{
    public class CredentialUtils
    {
        /// <summary>
        /// Formats the credential values into a JSON usable with the <see cref="AnonCreds"/> API
        /// </summary>
        /// <returns>The credential values.</returns>
        /// <param name="values">Values.</param>
        internal static string FormatCredentialValues(Dictionary<string, string> values)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            foreach (var item in values)
            {
                result.Add(item.Key, new Dictionary<string, string>
                {
                    { "raw", item.Value },
                    { "encoded", "1234567890" } // TODO: Add value encoding
                });
            }
            return result.ToJson();
        }
    }
}

using System.Collections.Generic;
using Hyperledger.Indy.AnonCredsApi;
using Newtonsoft.Json.Linq;

namespace AgentFramework.Core.Utils
{
    /// <summary>
    /// Credential utilities
    /// </summary>
    public class CredentialUtils
    {
        /// <summary>
        /// Formats the credential values into a JSON usable with the <see cref="AnonCreds"/> API
        /// </summary>
        /// <returns>The credential values.</returns>
        /// <param name="values">Values.</param>
        public static string FormatCredentialValues(Dictionary<string, string> values)
        {
            if (values == null)
                return null;

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

        public static Dictionary<string, string> GetAttributes(string jsonAttributeValues)
        {
            if (string.IsNullOrEmpty(jsonAttributeValues))
                return new Dictionary<string, string>();

            var attributes = JObject.Parse(jsonAttributeValues);

            var result = new Dictionary<string, string>();
            foreach (var attribute in attributes)
                result.Add(attribute.Key, attribute.Value["raw"].ToString());                

            return result;
        }
    }
}

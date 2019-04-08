using System.Collections.Generic;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Messages.Credentials;
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
        /// <param name="credentialAttributes">The credential attributes.</param>
        public static string FormatCredentialValues(IEnumerable<CredentialPreviewAttribute> credentialAttributes)
        {
            if (credentialAttributes == null)
                return null;

            var result = new Dictionary<string, Dictionary<string, string>>();
            foreach (var item in credentialAttributes)
            {
                switch (item.MimeType)
                {
                    case CredentialMimeTypes.TextMimeType:
                        result.Add(item.Name, FormatStringCredentialAttribute(item));
                        break;
                    default:
                        throw new AgentFrameworkException(ErrorCode.InvalidParameterFormat, $"Mime Type of {item.MimeType} not supported");
                }
            }
            return result.ToJson();
        }

        private static Dictionary<string, string> FormatStringCredentialAttribute(CredentialPreviewAttribute attribute)
        {
            return new Dictionary<string, string>()
            {
                {"raw", (string) attribute.Value},
                {"encoded", "1234567890"} //TODO Add value encoding
            };
        }


        /// <summary>
        /// Validates if the credential preview attribute is valid.
        /// </summary>
        /// <param name="attribute">Credential preview attribute.</param>
        public static void ValidateCredentialPreviewAttribute(CredentialPreviewAttribute attribute)
        {
            switch (attribute.MimeType)
            {
                case CredentialMimeTypes.TextMimeType:
                    break;
                default:
                    throw new AgentFrameworkException(ErrorCode.InvalidParameterFormat, $"Mime Type of {attribute.MimeType} not supported");
            }
        }

        /// <summary>
        /// Validates if the credential preview attributes are valid.
        /// </summary>
        /// <param name="attributes">Credential preview attributes.</param>
        public static void ValidateCredentialPreviewAttributes(IEnumerable<CredentialPreviewAttribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                ValidateCredentialPreviewAttribute(attribute);
            }
        }

        /// <summary>
        /// Gets the attributes.
        /// </summary>
        /// <param name="jsonAttributeValues">The json attribute values.</param>
        /// <returns></returns>
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

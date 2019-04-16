using System;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Messages;

namespace AgentFramework.Core.Utils
{
    /// <summary>
    /// Utilities for handling agent messages.
    /// </summary>
    public static class MessageUtils
    {
        /// <summary>
        /// Encodes a message to a valid URL based format.
        /// </summary>
        /// <typeparam name="T">Type of the agent message.</typeparam>
        /// <param name="baseUrl">Base URL for encoding the message with.</param>
        /// <param name="message">Message to encode.</param>
        /// <returns>Encoded message as a valid URL.</returns>
        public static string EncodeMessageToUrlFormat<T>(string baseUrl, T message) where T : AgentMessage
        {
            if (string.IsNullOrEmpty(baseUrl))
                throw new ArgumentNullException(nameof(baseUrl));

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (!Uri.IsWellFormedUriString(baseUrl, UriKind.Absolute))
                throw new ArgumentException("Not a valid URI", (nameof(baseUrl)));

            return EncodeMessageToUrlFormat(new Uri(baseUrl), message);
        }

        /// <summary>
        /// Encodes a message to a valid URL based format.
        /// </summary>
        /// <typeparam name="T">Type of the agent message.</typeparam>
        /// <param name="baseUrl">Base URL for encoding the message with.</param>
        /// <param name="message">Message to encode.</param>
        /// <returns>Encoded message as a valid URL.</returns>
        public static string EncodeMessageToUrlFormat<T>(Uri baseUrl, T message) where T : AgentMessage
        {
            if (baseUrl == null)
                throw new ArgumentNullException(nameof(baseUrl));

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            return $"{baseUrl}?m={message.ToJson().ToBase64()}";
        }

        /// <summary>
        /// Decodes a message from a valid URL based format.
        /// </summary>
        /// <param name="encodedMessage">Encoded message.</param>
        /// <returns>The agent message as a JSON string.</returns>
        public static string DecodeMessageFromUrlFormat(string encodedMessage)
        {
            if (string.IsNullOrEmpty(encodedMessage))
                throw new ArgumentNullException(nameof(encodedMessage));

            if (!Uri.IsWellFormedUriString(encodedMessage, UriKind.Absolute))
                throw new ArgumentException("Not a valid URI", (nameof(encodedMessage)));

            var uri = new Uri(encodedMessage);
            
            string messageBase64;
            try
            {
                messageBase64 = uri.DecodeQueryParameters()["m"];
            }
            catch (Exception)
            {
                throw new ArgumentException("Unable to find expected query parameter of `m`", (nameof(encodedMessage)));
            }

            return messageBase64.FromBase64();
        }
    }
}

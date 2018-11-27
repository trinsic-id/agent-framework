using System;
using System.Text.RegularExpressions;

namespace AgentFramework.Core.Utils
{
    /// <summary>
    /// Message utilities
    /// </summary>
    public class MessageUtils
    {
        private const string DidPattern = "did:sov:([1-9A-HJ-NP-Za-km-z]{21,22});(.*)$";
        private const string KeyPattern = "([1-9A-HJ-NP-Za-km-z]{43,44});(.*)$";

        public static string FormatDidMessageType(string did, string messageType) => $"did:sov:{did};{messageType}";
        public static string FormatKeyMessageType(string key, string messageType) => $"{key};{messageType}";

        /// <summary>
        /// Parses a message type string and returns the message type identifier and accompanying key or did included in the message type
        /// </summary>
        /// <param name="type">A message type string</param>
        /// <returns>The did or key embedded in the message type and the message type identifier</returns>
        public static (string didOrKey, string messageType) ParseMessageType(string type)
        {
            if (Regex.IsMatch(type, DidPattern))
            {
                var match = Regex.Match(type, DidPattern);
                if (match.Groups.Count == 3)
                    return (match.Groups[1].Value, match.Groups[2].Value);
            }
            else if (Regex.IsMatch(type, KeyPattern))
            {
                var match = Regex.Match(type, KeyPattern);
                if (match.Groups.Count == 3)
                    return (match.Groups[1].Value, match.Groups[2].Value);
            }

            throw new Exception($"Invalid message type {type}");
        }
    }
}
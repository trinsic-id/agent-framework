using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Streetcred.Sdk.Utils
{
    public class MessageUtils
    {
        private const string DidPattern = "did:sov:([1-9A-HJ-NP-Za-km-z]{21,22});(.*)$";
        private const string KeyPattern = "([1-9A-HJ-NP-Za-km-z]{43,44});(.*)$";

        public static string FormatDidMessageType(string did, string messageType) => $"did:sov:{did};{messageType}";
        public static string FormatKeyMessageType(string key, string messageType) => $"{key};{messageType}";

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
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Streetcred.Sdk.Utils
{
    /// <summary>
    /// Did utilities class
    /// </summary>
    public static class DidUtils
    {
        private const string FULL_VERKEY_REGEX = @"^[1-9A-HJ-NP-Za-km-z]{44}$";
        private const string ABREVIATED_VERKEY_REGEX = @"^~[1-9A-HJ-NP-Za-km-z]{22}$";

        /// <summary>
        /// Check a base58 encoded string against a regex expression
        /// to determine if it is a full valid verkey
        /// </summary>
        /// <param name="verkey">Base58 encoded string representation of a verkey</param>
        /// <returns>Boolean indicating if the string is a valid verkey</returns>
        public static bool IsFullVerkey(string verkey)
        {
            return Regex.Matches(verkey, FULL_VERKEY_REGEX).Count == 1;
        }

        /// <summary>
        /// Check a base58 encoded string against a regex expression
        /// to determine if it is a abbreviated valid verkey
        /// </summary>
        /// <param name="verkey">Base58 encoded string representation of a abbreviated verkey</param>
        /// <returns>Boolean indicating if the string is a valid abbreviated verkey</returns>
        public static bool IsAbbreviatedVerkey(string verkey)
        {
            return Regex.Matches(verkey, ABREVIATED_VERKEY_REGEX).Count == 1;
        }

        /// <summary>
        /// Check a base58 encoded string to determine 
        /// if it is a valid verkey
        /// </summary>
        /// <param name="verkey">Base58 encoded string representation of a verkey</param>
        /// <returns>Boolean indicating if the string is a valid verkey</returns>

        public static bool IsVerkey(string verkey)
        {
            return IsAbbreviatedVerkey(verkey)|| IsFullVerkey(verkey);
        }
    }
}

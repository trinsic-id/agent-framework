using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Streetcred.Sdk.Utils
{
    public static class DidUtils
    {
        private const string FULL_VERKEY_REGEX = @"[1-9A-HJ-NP-Za-km-z]{44}$";
        private const string ABREVIATED_VERKEY_REGEX = @"^~[1-9A-HJ-NP-Za-km-z]{22}$";

        public static bool IsFullVerkey(string verkey)
        {
            return Regex.Matches(verkey, FULL_VERKEY_REGEX).Count == 1;
        }

        public static bool IsAbbreviatedVerkey(string verkey)
        {
            return Regex.Matches(verkey, ABREVIATED_VERKEY_REGEX).Count == 1;
        }

        public static bool IsVerkey(string verkey)
        {
            return IsAbbreviatedVerkey(verkey)|| IsFullVerkey(verkey);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;

namespace Streetcred.Sdk.Utils
{
    internal static class EnvironmentUtils
    {
        public static string GetUserPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        public static string GetIndyHomePath()
        {
            return Path.Combine(GetUserPath(), ".indy_client");
        }

        public static string GetIndyHomePath(params string[] paths)
        {
            var pathParts = new List<string>(paths);
            pathParts.Insert(0, GetIndyHomePath());
            return Path.Combine(pathParts.ToArray());
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;

namespace Streetcred.Sdk.Utils
{
    public static class EnvironmentUtils
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

        public static string GetTailsPath()
        {
            return Path.Combine(GetIndyHomePath(), "tails").Replace("\\", "/");
        }

        public static string GetTailsPath(string filename)
        {
            return Path.Combine(GetTailsPath(), filename);
        }
    }
}

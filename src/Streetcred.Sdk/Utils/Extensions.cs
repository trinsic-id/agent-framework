using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Streetcred.Sdk.Utils
{
    public static class Extensions
    {
        /// <summary>
        /// Converts an object to json string using default converter.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public static string ToJson(this object obj) => JsonConvert.SerializeObject(obj);
    }
}

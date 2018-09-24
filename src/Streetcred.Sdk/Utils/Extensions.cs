using System.Text;
using Multiformats.Base;
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

        /// <summary>
        /// Converts a string to base58 representation.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string ToBase58(this string value) =>
            Multibase.Base58.Encode(Encoding.UTF8.GetBytes(value));
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using AgentFramework.Core.Utils;
using Newtonsoft.Json;

namespace AgentFramework.Core.Extensions
{
    public static class FormattingExtensions
    {
        /// <summary>
        /// Decode the array into a string using UTF8 byte mark
        /// </summary>
        /// <param name="array"></param>
        public static string GetUTF8String(this byte[] array)
        {
            return Encoding.UTF8.GetString(array);
        }

        /// <summary>Decode thebyte array and deserialize the JSON string into the specified object</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array">The array.</param>
        /// <returns></returns>
        public static T FromJson<T>(this byte[] array)
        {
            return JsonConvert.DeserializeObject<T>(GetUTF8String(array));
        }

        /// <summary>
        /// Deserializes a JSON string to object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>The json.</returns>
        /// <param name="value">Value.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static T FromJson<T>(this string value)
        {
            return JsonConvert.DeserializeObject<T>(value);
        }

        /// <summary>Encode the string into a byte array using UTF8 byte mark.</summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static byte[] GetUTF8Bytes(this string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        /// <summary>
        /// Serializes an object to byte array using UTF8 byte order
        /// </summary>
        /// <returns>The byte array.</returns>
        /// <param name="value">Value.</param>
        public static byte[] ToByteArray(this object value)
        {
            return value.ToJson().GetUTF8Bytes();
        }

        /// <summary>Converts the specified string, which encodes binary data as base-64 digits,
        /// to an equivalent 8-bit unsigned integer array.</summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static byte[] GetBytesFromBase64(this string value)
        {
            return Convert.FromBase64String(value);
        }

        /// <summary>Converts an array of 8-bit unsigned integers to its equivalent string
        /// representation that is encoded with base-64 digits.</summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string ToBase64String(this byte[] value)
        {
            return Convert.ToBase64String(value);
        }
    }
}

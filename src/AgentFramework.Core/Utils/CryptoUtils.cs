using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AgentFramework.Core.Extensions;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json;

namespace AgentFramework.Core.Utils
{
    internal class CryptoUtils
    {
        /// <summary>Packs a message</summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="recipientKey">The recipient key.</param>
        /// <param name="senderKey">The sender key.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public static Task<byte[]> PackAsync(
            Wallet wallet, string recipientKey, string senderKey, byte[] message) =>
            PackAsync(wallet, new[] {recipientKey}, senderKey, message);

        /// <summary>Packs the asynchronous.</summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="recipientKeys">The recipient keys.</param>
        /// <param name="senderKey">The sender key.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public static Task<byte[]> PackAsync(
            Wallet wallet, string[] recipientKeys, string senderKey, byte[] message) =>
            Crypto.PackMessageAsync(wallet, recipientKeys.ToJson(), senderKey, message);

        /// <summary>Packs the asynchronous.</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="wallet">The wallet.</param>
        /// <param name="recipientKey">The recipient key.</param>
        /// <param name="senderKey">The sender key.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public static Task<byte[]> PackAsync<T>(
            Wallet wallet, string recipientKey, string senderKey, T message) =>
            PackAsync(wallet, new[] {recipientKey}, senderKey, message.ToByteArray());

        /// <summary>Packs the asynchronous.</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="wallet">The wallet.</param>
        /// <param name="recipientKeys">The recipient keys.</param>
        /// <param name="senderKey">The sender key.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public static Task<byte[]> PackAsync<T>(
            Wallet wallet, string[] recipientKeys, string senderKey, T message) =>
            Crypto.PackMessageAsync(wallet, recipientKeys.ToJson(), senderKey, message.ToByteArray());

        /// <summary>Unpacks the asynchronous.</summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public static async Task<UnpackResult> UnpackAsync(Wallet wallet, byte[] message)
        {
            var result = await Crypto.UnpackMessageAsync(wallet, message);
            return result.ToObject<UnpackResult>();
        }

        /// <summary>Unpacks the asynchronous.</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public static async Task<T> UnpackAsync<T>(Wallet wallet, byte[] message)
        {
            var result = await Crypto.UnpackMessageAsync(wallet, message);
            var unpacked = result.ToObject<UnpackResult>();
            return unpacked.Message.ToObject<T>();
        }
    }

    public class UnpackResult
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("sender_verkey")]
        public string SenderVerkey { get; set; }

        [JsonProperty("recipient_verkey")]
        public string RecipientVerkey { get; set; }
    }
}

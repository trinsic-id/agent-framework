using System;
using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Credentials
{
    /// <summary>
    /// A credential offer content message.
    /// </summary>
    public class CredentialOfferMessage : AgentMessage
    {
        /// <inheritdoc />
        public CredentialOfferMessage()
        {
            Id = Guid.NewGuid().ToString();
            Type = MessageTypes.CredentialOffer;
        }
        
        /// <summary>
        /// Gets or sets the offer json.
        /// </summary>
        /// <value>
        /// The offer json.
        /// </value>
        public string OfferJson { get; set; }

        /// <inheritdoc />
        public override string ToString() =>
            $"{GetType().Name}: " +
            $"Id={Id}, " +
            $"Type={Type}, " +
            $"OfferJson={(OfferJson?.Length > 0 ? "[hidden]" : null)}";
    }
}

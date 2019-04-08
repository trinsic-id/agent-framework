using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        /// Gets or sets the comment.
        /// </summary>
        /// <value>
        /// The comment.
        /// </value>
        [JsonProperty("comment", NullValueHandling = NullValueHandling.Ignore)]
        public string Comment { get; set; }
        
        /// <summary>
        /// Gets or sets the offer json.
        /// </summary>
        /// <value>
        /// The offer json.
        /// </value>
        [JsonProperty("offer_json")]
        public string OfferJson { get; set; }

        /// <summary>
        /// Gets or sets the credential preview.
        /// </summary>
        /// <value>
        /// The preview.
        /// </value>
        [JsonProperty("credential_preview")]
        public CredentialPreviewMessage Preview { get; set; }

        /// <inheritdoc />
        public override string ToString() =>
            $"{GetType().Name}: " +
            $"Id={Id}, " +
            $"Type={Type}, " +
            $"OfferJson={(OfferJson?.Length > 0 ? "[hidden]" : null)}";
    }

    /// <summary>
    /// Represents credential preview message
    /// </summary>
    /// <seealso cref="AgentFramework.Core.Messages.AgentMessage" />
    public class CredentialPreviewMessage : AgentMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CredentialPreviewMessage"/> class.
        /// </summary>
        public CredentialPreviewMessage()
        {
            Id = Guid.NewGuid().ToString();
            Type = MessageTypes.CredentialPreview;
        }

        /// <summary>
        /// Gets or sets the context.
        /// </summary>
        /// <value>
        /// The context.
        /// </value>
        [JsonProperty("@context")]
        public JObject Context { get; set; }

        /// <summary>
        /// Gets or sets the attributes.
        /// </summary>
        /// <value>
        /// The attributes.
        /// </value>
        [JsonProperty("attributes")]
        public IEnumerable<CredentialPreviewAttribute> Attributes { get; set; }
    }

    /// <summary>
    /// Represents credential preview attribute
    /// </summary>
    public class CredentialPreviewAttribute
    {
        /// <summary>
        /// String type credential attribute constructor.
        /// </summary>
        /// <param name="name">Name of the credential attribute.</param>
        /// <param name="value">Value of the credential attribute.</param>
        public CredentialPreviewAttribute(string name, string value)
        {
            Name = name;
            Value = value;
            MimeType = CredentialMimeTypes.TextMimeType;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the MIME.
        /// </summary>
        /// <value>
        /// The type of the MIME.
        /// </value>
        [JsonProperty("mime_type")]
        public string MimeType { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        [JsonProperty("value")]
        public object Value { get; set; }
    }

    /// <summary>
    /// Valid Mime types for credential attributes.
    /// </summary>
    public static class CredentialMimeTypes
    {
        /// <summary>
        /// Text mime type attribute.
        /// </summary>
        public const string TextMimeType = "text/plain";
    }
}

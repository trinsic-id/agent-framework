﻿using System;
using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Credentials
{
    /// <summary>
    /// A credential request content message.
    /// </summary>
    public class CredentialRequestMessage : AgentMessage
    {
        /// <inheritdoc />
        public CredentialRequestMessage()
        {
            Id = Guid.NewGuid().ToString();
            Type = MessageTypes.CredentialRequest;
        }

        /// <summary>
        /// Gets or sets the credential request json.
        /// </summary>
        /// <value>
        /// The credential request json.
        /// </value>
        public string CredentialRequestJson { get; set; }

        /// <summary>
        /// Gets or sets the credential values json.
        /// </summary>
        /// <value>
        /// The credential values json.
        /// </value>
        public string CredentialValuesJson { get; set; }

        /// <inheritdoc />
        public override string ToString() =>
            $"{GetType().Name}: " +
            $"Id={Id}, " +
            $"Type={Type}, " +
            $"CredentialRequestJson={(CredentialRequestJson?.Length > 0 ? "[hidden]" : null)}, " +
            $"CredentialValuesJson={(CredentialValuesJson?.Length > 0 ? "[hidden]" : null)}";
    }
}

using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Dids
{
    /// <summary>
    /// Strongly type DID doc key model.
    /// </summary>
    public class DidDocKey
    {
        /// <summary>
        /// The id of the key.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The type of the key.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The owner key.
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// The PEM representation of the key.
        /// </summary>
        public string PublicKeyPem { get; set; }

        //TODO add other public key representations
    }
}
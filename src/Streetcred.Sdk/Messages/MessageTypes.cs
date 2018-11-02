namespace Streetcred.Sdk.Messages
{
    /// <summary>
    /// Represents the supported message types
    /// </summary>
    public class MessageTypes
    {
        // Custom message types
        public const string ConnectionInvitation = "connection_invitation";

        // Sovrin message types
        public const string ConnectionRequest = "spec/connections/1.0/connection_request";
        public const string ConnectionResponse = "spec/connections/1.0/connection_response";
        public const string CredentialOffer = "spec/connections/1.0/credential_offer";
        public const string CredentialRequest = "spec/connections/1.0/credential_request";
        public const string Credential = "spec/connections/1.0/credential";
        public const string ProofRequest = "spec/connections/1.0/proof_request";
        public const string DisclosedProof = "spec/connections/1.0/disclosed_proof";

        public const string ForwardToKey = "spec/routing/1.0/forward_to_key";
        public const string Forward = "spec/routing/1.0/forward";
    }
}

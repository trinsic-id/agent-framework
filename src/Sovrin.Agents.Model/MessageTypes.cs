namespace Sovrin.Agents.Model
{
    public class MessageTypes
    {
        // Custom message types
        public const string ConnectionInvitation = "connection_invitation";

        // Sovrin message types
        public const string ConnectionRequest = "connection_request";
        public const string ConnectionResponse = "connection_response";
        public const string CredentialOffer = "credential_offer";
        public const string CredentialRequest = "credential_request";
        public const string Credential = "credential";
        public const string ProofRequest = "proof_request";
        public const string DisclosedProof = "disclosed_proof";

        public const string ForwardToKey = "spec/routing/1.0/forward_to_key";
        public const string Forward = "spec/routing/1.0/forward";
    }
}

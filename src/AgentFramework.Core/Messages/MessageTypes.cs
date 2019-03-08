namespace AgentFramework.Core.Messages
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Represents the supported message types
    /// </summary>
    public class MessageTypes
    {
        // Connection Messages
        public const string ConnectionInvitation = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/connections/1.0/invitation";
        public const string ConnectionRequest = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/connections/1.0/request";
        public const string ConnectionResponse = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/connections/1.0/response";

        //Credential Messages
        public const string CredentialOffer = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/connections/1.0/credential_offer";
        public const string CredentialRequest = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/connections/1.0/credential_request";
        public const string Credential = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/connections/1.0/credential";

        //Proof Messages
        public const string ProofRequest = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/connections/1.0/proof_request";
        public const string DisclosedProof = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/connections/1.0/disclosed_proof";

        //Ephemeral Challenge Messages
        public const string EphemeralChallenge = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/ephemeral_challenge/1.0/challenge";
        public const string EphemeralChallengeResponse = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/ephemeral_challenge/1.0/challenge_response";

        //Routing Messages
        public const string Forward = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/routing/1.0/forward";
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

}
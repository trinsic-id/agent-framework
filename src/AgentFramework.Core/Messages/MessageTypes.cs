namespace AgentFramework.Core.Messages
{
    /// <summary>
    /// Represents the supported message types
    /// </summary>
    public class MessageTypes
    {
        // Connection Messages
        public const string ConnectionInvitation = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/connections/1.0/invitation";
        public const string ConnectionRequest = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/connections/1.0/request";
        public const string ConnectionResponse = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/connections/1.0/response";

        public const string CredentialOffer = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/connections/1.0/credential_offer";
        public const string CredentialRequest = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/connections/1.0/credential_request";
        public const string Credential = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/connections/1.0/credential";
        public const string ProofRequest = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/connections/1.0/proof_request";
        public const string DisclosedProof = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/connections/1.0/disclosed_proof";

        //Routing Messages
        public const string CreateRoute = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/routing/1.0/create";
        public const string DeleteRoute = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/routing/1.0/delete";
        public const string GetRoutes = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/routing/1.0/get";
        public const string RouteRecord = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/routing/1.0/route";
        public const string RouteRecords = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/routing/1.0/routes";
        public const string ForwardMultiple = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/routing/1.0/forward-multiple";
        public const string Forward = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/routing/1.0/forward";
    }
}

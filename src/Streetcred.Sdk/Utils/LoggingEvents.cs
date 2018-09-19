namespace Streetcred.Sdk.Utils
{
    public class LoggingEvents
    {
        //Credential events
        public const int CreateCredentialOffer = 1000;
        public const int SendCredentialOffer = 1001;
        public const int StoreCredentialRequest = 1002;

        //Proof events
        public const int CreateProofRequest = 2000;
        public const int SendProofRequest = 2001;
        public const int StoreProofRequest = 2002;

        // Connection events
        public const int CreateInvitation = 4000;
        public const int AcceptInvitation = 4001;
        public const int StoreConnectionRequest = 4002;
        public const int AcceptConnectionRequest = 4003;
        public const int AcceptConnectionResponse = 4004;
        public const int GetConnection = 4010;
        public const int ListConnections = 4011;
        public const int DeleteConnection = 4012;

        public const int Forward = 3000;
    }
}

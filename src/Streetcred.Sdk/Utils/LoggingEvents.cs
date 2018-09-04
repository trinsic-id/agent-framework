namespace Streetcred.Sdk.Utils
{
    public class LoggingEvents
    {
        public const int CreateOffer = 1000;
        public const int SendOffer = 1001;
        public const int StoreCredentialRequest = 1002;
        public const int Forward = 3000;

        // Connection events
        public const int CreateInvitation = 4000;
        public const int AcceptInvitation = 4001;
        public const int StoreConnectionRequest = 4002;
        public const int AcceptConnectionRequest = 4003;
        public const int AcceptConnectionResponse = 4004;
        public const int GetConnection = 4010;
        public const int ListConnections = 4011;
    }
}

namespace Streetcred.Sdk.Model.Records
{
    public class RevocationRegistryRecord : WalletRecord
    {
        public override string GetId() => RevocationRegistryId;

        public string RevocationRegistryId
        {
            get;
            set;
        }

        public string TailsFile
        {
            get;
            set;
        }

        public override string GetTypeName() => "RevocationRegistryRecord";
    }
}

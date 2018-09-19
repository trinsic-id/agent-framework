using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json.Linq;

namespace Streetcred.Sdk.Utils
{
    public class ProofUtils
    {
        private static string DefaultProofRequestNameSuffix = "proof_req_";
        private static string DefaultProofRequestVersion = "0.1";
        private static string RequestedAttributePropertyName = "requested_attributes";
        private static string RequestedPredicatePropertyName = "requested_predicates";
        private static string ReferentAttributePropertyName = "name";

        public static async Task<Dictionary<string, string>> GetRequestedAttributes(Wallet wallet, string requestJson)
        {
            var credentialsSearch = await AnonCreds.ProverSearchCredentialsForProofRequestAsync(wallet, requestJson);

            var requestedAtrributes = await GetReferentsAndCredentialIds(credentialsSearch, requestJson);

            credentialsSearch.Dispose();

            return requestedAtrributes;
        }

        public static async Task<Dictionary<string, string>> GetReferentsAndCredentialIds(CredentialSearch search, string proofJson)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            Dictionary<string,string> attribsAndRefs = GetReferentsAndAttributes(proofJson);
            foreach (var attribsAndRef in attribsAndRefs)
            {
                var result = await search.NextAsync(1, attribsAndRef.Key);

                if (!String.IsNullOrEmpty(result))
                    continue;

                var credArray = JArray.Parse(result);

                if (credArray.Count == 0)
                    continue;

                results.Add(attribsAndRef.Key, credArray[0]["referent"].ToString());
            }
            return results;
        }

        public static Dictionary<string, string> GetReferentsAndAttributes(string proofJson)
        {
            JObject obj = JObject.Parse(proofJson);

            Dictionary<string,string> results = new Dictionary<string, string>();

            foreach (var attr in ((JObject)obj[RequestedAttributePropertyName].ToObject(typeof(JObject))).Properties())
            {
                string test = attr.Path;
                results.Add(attr.Name, attr.Value[ReferentAttributePropertyName].ToString());
            }

            return results;
        }

        public static Dictionary<string, string> GetRequestedPredicates(string proofJson)
        {
            throw new NotImplementedException();
        }

        //TODO change requestAttributes into an object list so we can handle restrictions
        public static string CreateProofRequest(IEnumerable<string> requestedAttributes, string name = null, string version = null, string nonce = null)
        {
            JObject proofRequest = new JObject();

            if (string.IsNullOrEmpty(nonce))
                nonce = Guid.NewGuid().ToString(); //TODO proper nonce generator

            proofRequest.Add("nonce", nonce);
            proofRequest.Add("name", name ?? $"{DefaultProofRequestNameSuffix}{nonce}");
            proofRequest.Add("version", version ?? DefaultProofRequestVersion);

            JObject requestedattributesJson = new JObject();

            int count = 1;
            foreach (var requestedAttribute in requestedAttributes)
            {
                JObject attribute = new JObject();
                attribute.Add(ReferentAttributePropertyName, requestedAttribute);
                requestedattributesJson.Add($"attr{count}_referent", attribute);
                count++;
            }

            //TODO add support for predicates

            proofRequest.Add(RequestedAttributePropertyName, requestedattributesJson);

            return proofRequest.ToJson();
        }

        public static string GenerateRequestedCredentials(List<(string credId, string credVal, string credRef)> results)
        {
            //TODO control which attributes are revealed

            //TODO predicates support

            //TODO self attested values support

            JObject requestedattributes = new JObject();

            foreach (var credentialIdForAttribute in results)
            {
                JObject temp = new JObject();

                temp.Add("cred_id", credentialIdForAttribute.credId);
                temp.Add("revealed", true); //TODO add support

                requestedattributes.Add(credentialIdForAttribute.credRef, temp);
            }

            var proof = new JObject();
            proof.Add(RequestedAttributePropertyName, requestedattributes);

            return proof.ToString();
        }
    }
}

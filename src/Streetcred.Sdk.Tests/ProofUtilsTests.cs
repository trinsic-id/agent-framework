using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Utils;
using Xunit;

namespace Streetcred.Sdk.Tests
{
    public class ProofUtilsTests
    {
        private static string ExampleProofRequest = "{" +
                                                    "     \"nonce\":\"123456\",\n" +
                                                    "     \"name\":\"proof_req_123456\",\n" +
                                                    "     \"version\":\"0.1\", " +
                                                    "     \"requested_attributes\": {" +
                                                    "         \"attr1_referent\":{\"name\":\"name\"}," +
                                                    "         \"attr2_referent\":{\"name\":\"sex\"}," +
                                                    "         \"attr3_referent\":{\"name\":\"phone\"}" +
                                                    "     }," +
                                                    "}";

        private static string ExampleProofFulfilmentRequest = "{" +
                                                              "     \"requested_attributes\": {\n" +
                                                              "          \"attr1_referent\":{\"cred_id\":\"c0b2dfa4-cabb-4a04-a0c1-285a0c9dfd10\", \"revealed\":true},\n" +
                                                              "          \"attr2_referent\":{\"cred_id\":\"b1697580-5317-4ce5-9c23-4248eb64df5f\", \"revealed\":true},\n" +
                                                              "          \"attr3_referent\":{\"cred_id\":\"9d827c1b-5d6f-4bd4-9c79-02013eff2735\", \"revealed\":true}\n" +
                                                              "     }\n" +
                                                              "}";


        private static List<string> ExampleRequestedAttributes = new List<string>()
        {
            "name",
            "sex",
            "phone"
        };

        private static Dictionary<string, string> ExampleRequestedAttributesAndReferents = new Dictionary<string, string>()
        {
            {"attr1_referent", "name"},
            {"attr2_referent", "sex"},
            {"attr3_referent", "phone"}
        };

        private static List<(string credId, string credVal, string credRef)> ExampleReferentsAndCredentialIds = new List<(string credId, string credVal, string credRef)>()
        {
            ("c0b2dfa4-cabb-4a04-a0c1-285a0c9dfd10", "", "attr1_referent"),
            ("b1697580-5317-4ce5-9c23-4248eb64df5f", "", "attr2_referent"),
            ("9d827c1b-5d6f-4bd4-9c79-02013eff2735", "", "attr3_referent")
        };


        [Fact]
        //TODO this test will change
        public void CanCreateProofRequest()
        {
            var requestJson = ProofUtils.CreateProofRequest(ExampleRequestedAttributes, null, null, "123456");

            JObject generatedRequest = JsonConvert.DeserializeObject<JObject>(requestJson);
            JObject exampleRequest = JsonConvert.DeserializeObject<JObject>(ExampleProofRequest);

            Assert.Equal(generatedRequest,exampleRequest);
        }

        [Fact]
        public void CanGetRequestedAttributes()
        {
            var results = ProofUtils.GetReferentsAndAttributes(ExampleProofRequest);
            
            Assert.Equal(results, ExampleRequestedAttributesAndReferents);
        }

        [Fact]
        public void CanCreateRequestedCredentialsProofParameter()
        {
            var requestJson = ProofUtils.GenerateRequestedCredentials(ExampleReferentsAndCredentialIds);

            JObject generatedRequest = JsonConvert.DeserializeObject<JObject>(requestJson);
            JObject exampleRequest = JsonConvert.DeserializeObject<JObject>(ExampleProofFulfilmentRequest);

            Assert.Equal(generatedRequest, exampleRequest);
        }
    }
}

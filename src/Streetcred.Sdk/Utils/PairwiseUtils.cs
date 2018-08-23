using System.Threading.Tasks;
using Hyperledger.Indy.PairwiseApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Model;

namespace Streetcred.Sdk.Utils
{
    public class PairwiseUtils
    {
        public static async Task<(string MyDid, Endpoint Endpoint)> GetAsync(Wallet wallet, string theirDid)
        {
            var result = JObject.Parse(await Pairwise.GetAsync(wallet, theirDid));
            var metadata = JsonConvert.DeserializeObject<Endpoint>(result["metadata"].ToObject<string>());

            return (result["my_did"].ToObject<string>(), metadata);
        }
    }
}

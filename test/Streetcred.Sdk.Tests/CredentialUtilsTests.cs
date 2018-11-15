using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Streetcred.Sdk.Utils;
using Xunit;

namespace Streetcred.Sdk.Tests
{
    public class CredentialUtilsTests
    {
        [Fact]
        public void CanFormatCredentialValues()
        {
            Dictionary<string, string> attributeValues = new Dictionary<string, string>()
            {
                {"first_name", "Test"},
                {"last_name", "holder"}
            };

            var expectedResult =
                "{\n  \"first_name\" : {\n    \"raw\" : \"Test\",\n    \"encoded\" : \"1234567890\"\n  },\n  \"last_name\" : {\n    \"raw\" : \"holder\",\n    \"encoded\" : \"1234567890\"\n  }\n}";

            var formatedCredentialUtils = CredentialUtils.FormatCredentialValues(attributeValues);

            var expectedResultObj = JObject.Parse(expectedResult);
            var formatedCredObj = JObject.Parse(formatedCredentialUtils);

            Assert.Equal(expectedResultObj, formatedCredObj);
        }

        [Fact]
        public void CanGetAttributes()
        {
            Dictionary<string, string> expectedResult = new Dictionary<string, string>()
            {
                {"first_name", "Test"},
                {"last_name", "holder"}
            };

            var attributesJson =
                "{\n  \"first_name\" : {\n    \"raw\" : \"Test\",\n    \"encoded\" : \"123456789\"\n  },\n  \"last_name\" : {\n    \"raw\" : \"holder\",\n    \"encoded\" : \"123456789\"\n  }\n}";

            var attributeValues = CredentialUtils.GetAttributes(attributesJson);

            Assert.Equal(expectedResult, attributeValues);
        }
    }
}

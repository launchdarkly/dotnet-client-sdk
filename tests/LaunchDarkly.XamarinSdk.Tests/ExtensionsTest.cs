using Xunit;

namespace LaunchDarkly.Sdk.Xamarin
{
    public class ExtensionsTest
    {
        [Fact]
        public void TestUrlSafeBase64Encode()
        {
            Assert.Equal("eyJrZXkiOiJmb28-YmFyX18_In0=",
                @"{""key"":""foo>bar__?""}".UrlSafeBase64Encode());
        }
    }
}

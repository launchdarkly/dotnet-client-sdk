using Xunit;

namespace LaunchDarkly.Sdk.Xamarin.Internal
{
    public class ExtensionsTest
    {
        [Fact]
        public void TestUrlSafeBase64Encode()
        {
            Assert.Equal("eyJrZXkiOiJmb28-YmFyX18_In0=",
                Base64.UrlSafeEncode(@"{""key"":""foo>bar__?""}"));
        }
    }
}

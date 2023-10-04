using Xunit;

namespace LaunchDarkly.Sdk.Client.Internal
{
    public class Base64Test
    {
        [Fact]
        public void TestUrlSafeBase64Encode()
        {
            Assert.Equal("eyJrZXkiOiJmb28-YmFyX18_In0=",
                Base64.UrlSafeEncode(@"{""key"":""foo>bar__?""}"));
        }

        [Fact]
        public void TestUrlSafeSha256Hash()
        {
            Assert.Equal("something", Base64.UrlSafeSha256Hash("OhYeah?HashThis!!!"));
        }

        // func testSha256base64() throws {
        //     let input = "hashThis!"
        //     let expectedOutput = "sfXg3HewbCAVNQLJzPZhnFKntWYvN0nAYyUWFGy24dQ="
        //     let output = Util.sha256base64(input)
        //     XCTAssertEqual(output, expectedOutput)
        // }
        //
        // func testSha256base64UrlEncoding() throws {
        //     let input = "OhYeah?HashThis!!!" // hash is KzDwVRpvTuf//jfMK27M4OMpIRTecNcJoaffvAEi+as= and it has a + and a /
        //     let expectedOutput = "KzDwVRpvTuf__jfMK27M4OMpIRTecNcJoaffvAEi-as="
        //     let output = Util.sha256(input).base64UrlEncodedString
        //     XCTAssertEqual(output, expectedOutput)
        // }
        //
        // @Test
        // public void testUrlSafeBase64Hash() {
        //     String input = "hashThis!";
        //     String expectedOutput = "sfXg3HewbCAVNQLJzPZhnFKntWYvN0nAYyUWFGy24dQ=";
        //     String output = LDUtil.urlSafeBase64Hash(input);
        //     Assert.assertEquals(expectedOutput, output);
        // }
    }
}

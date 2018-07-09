using System.Threading.Tasks;

namespace LaunchDarkly.Xamarin.Tests
{
    internal class MockFeatureFlagRequestor : IFeatureFlagRequestor
    {
        public void Dispose()
        {

        }

        public Task<WebResponse> FeatureFlagsAsync()
        {
            var jsonText = JSONReader.FeatureFlagJSONFromService();
            var response = new WebResponse(200, jsonText, null);
            return Task.FromResult(response);
        }
    }
}

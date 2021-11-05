using Xunit;

using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;
using static LaunchDarkly.TestHelpers.JsonAssertions;

namespace LaunchDarkly.Sdk.Client
{
    public class AssertHelpers
    {
        public static void DataSetsEqual(FullDataSet expected, FullDataSet actual) =>
            AssertJsonEqual(expected.ToJsonString(), actual.ToJsonString());

        public static void UsersEqualExcludingAutoProperties(User expected, User actual)
        {
            var builder = User.Builder(expected);
            foreach (var autoProp in new string[] { "device", "os" })
            {
                if (!actual.GetAttribute(UserAttribute.ForName(autoProp)).IsNull)
                {
                    builder.Custom(autoProp, actual.GetAttribute(UserAttribute.ForName(autoProp)));
                }
            }
            Assert.Equal(builder.Build(), actual);
        }
    }
}

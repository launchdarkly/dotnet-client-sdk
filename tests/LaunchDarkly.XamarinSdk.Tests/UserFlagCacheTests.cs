using Xunit;

namespace LaunchDarkly.Sdk.Xamarin
{
    public class UserFlagCacheTests : BaseTest
    {
        IUserFlagCache inMemoryCache = new UserFlagInMemoryCache();
        User user1 = User.WithKey("user1Key");
        User user2 = User.WithKey("user2Key");

        [Fact]
        public void CanCacheFlagsInMemory()
        {
            var jsonFlags = @"{""flag1"":{""value"":1},""flag2"":{""value"":2}}";
            var flags = TestUtil.DecodeFlagsJson(jsonFlags);
            inMemoryCache.CacheFlagsForUser(flags, user1);
            var flagsRetrieved = inMemoryCache.RetrieveFlags(user1);
            Assert.Equal(2, flagsRetrieved.Count);
            Assert.Equal(flags["flag1"], flagsRetrieved["flag1"]);
            Assert.Equal(flags["flag2"], flagsRetrieved["flag2"]);
        }
    }
}

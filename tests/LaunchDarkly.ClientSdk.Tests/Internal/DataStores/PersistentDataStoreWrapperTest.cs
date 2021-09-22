using Xunit;
using Xunit.Abstractions;

namespace LaunchDarkly.Sdk.Client.Internal.DataStores
{
    public class PersistentDataStoreWrapperTest : BaseTest
    {
        private static readonly User _basicUser = User.WithKey("user-key");
        private static readonly User _otherUser = User.WithKey("user-key");

        private readonly InMemoryDataStore _inMemoryStore;
        private readonly MockPersistentDataStore _persistentStore;
        private readonly PersistentDataStoreWrapper _wrapper;

        public PersistentDataStoreWrapperTest(ITestOutputHelper testOutput) : base(testOutput)
        {
            _inMemoryStore = new InMemoryDataStore();
            _persistentStore = new MockPersistentDataStore();
            _wrapper = new PersistentDataStoreWrapper(_inMemoryStore, _persistentStore, testLogger);
        }

        [Fact]
        public void PreloadDoesNotInitializeStoreForUserWhoIsNotInPersistentStore()
        {
            _wrapper.Preload(_basicUser);

            Assert.Null(_inMemoryStore.GetAll(_basicUser));
        }

        [Fact]
        public void PreloadDoesNotOverwriteStoreIfDataIsAlreadyCached()
        {
            var inMemoryData = new DataSetBuilder()
                .Add("flag1", 2, LdValue.Of(true), 0)
                .Build();
            _inMemoryStore.Init(_basicUser, inMemoryData);

            var persistedData = new DataSetBuilder()
                .Add("flag1", 1, LdValue.Of(false), 1)
                .Build();
            _persistentStore.Init(_basicUser, DataModelSerialization.SerializeAll(persistedData));

            _wrapper.Preload(_basicUser);

            Assert.Equal(inMemoryData, _inMemoryStore.GetAll(_basicUser));
        }

        [Fact]
        public void PreloadGetsDataFromPersistentStoreIfNotAlreadyCached()
        {
            var persistedData = new DataSetBuilder()
                .Add("flag1", 1, LdValue.Of(false), 1)
                .Build();
            _persistentStore.Init(_basicUser, DataModelSerialization.SerializeAll(persistedData));

            _wrapper.Preload(_basicUser);

            Assert.Equal(persistedData, _inMemoryStore.GetAll(_basicUser));
        }

        [Fact]
        public void InitWritesToBothMemoryAndPersistentStore()
        {
            var data = new DataSetBuilder()
                .Add("flag1", 1, LdValue.Of(true), 0)
                .Build();

            _wrapper.Init(_basicUser, data);

            Assert.Equal(data, _inMemoryStore.GetAll(_basicUser));

            Assert.Equal(data, DataModelSerialization.DeserializeAll(_persistentStore.GetAll(_basicUser)));
        }

        [Fact]
        public void GetReadsOnlyInMemoryStore()
        {
            var flag1a = new FeatureFlagBuilder().Version(2).Value(true).Variation(0).Build();
            var inMemoryData = new DataSetBuilder()
                .Add("flag1", flag1a)
                .Build();
            _inMemoryStore.Init(_basicUser, inMemoryData);

            var flag1b = new FeatureFlagBuilder().Version(1).Value(false).Variation(1).Build();
            var persistedData = new DataSetBuilder()
                .Add("flag1", flag1b)
                .Build();
            _persistentStore.Init(_basicUser, DataModelSerialization.SerializeAll(persistedData));

            Assert.Equal(flag1a.ToItemDescriptor(), _wrapper.Get(_basicUser, "flag1"));
        }

        [Fact]
        public void GetAllReadsOnlyInMemoryStore()
        {
            var inMemoryData = new DataSetBuilder()
                .Add("flag1", 2, LdValue.Of(true), 0)
                .Build();
            _inMemoryStore.Init(_basicUser, inMemoryData);

            var persistedData = new DataSetBuilder()
                .Add("flag1", 1, LdValue.Of(false), 1)
                .Build();
            _persistentStore.Init(_basicUser, DataModelSerialization.SerializeAll(persistedData));

            Assert.Equal(inMemoryData, _wrapper.GetAll(_basicUser));
        }

        [Fact]
        public void UpsertPersistsAllDataAfterUpdatingMemory()
        {
            var flag1a = new FeatureFlagBuilder().Version(100).Value(true).Variation(0).Build();
            var flag1b = new FeatureFlagBuilder().Version(101).Value(false).Variation(1).Build();
            var flag2 = new FeatureFlagBuilder().Version(200).Value(true).Variation(0).Build();
            var initialData = new DataSetBuilder()
                .Add("flag1", flag1a)
                .Add("flag2", flag2)
                .Build();

            _wrapper.Init(_basicUser, initialData);

            _wrapper.Upsert(_basicUser, "flag1", flag1b.ToItemDescriptor());

            Assert.Equal(flag1b.ToItemDescriptor(), _inMemoryStore.Get(_basicUser, "flag1"));
            Assert.Equal(flag2.ToItemDescriptor(), _inMemoryStore.Get(_basicUser, "flag2"));

            var updatedData = new DataSetBuilder()
                .Add("flag1", flag1b)
                .Add("flag2", flag2)
                .Build();

            Assert.Equal(updatedData, DataModelSerialization.DeserializeAll(_persistentStore.GetAll(_basicUser)));
        }
    }
}

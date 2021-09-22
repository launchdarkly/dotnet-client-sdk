using System.Collections.Generic;
using System.Linq;
using Xunit;

using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;

namespace LaunchDarkly.Sdk.Client.Internal.DataStores
{
    public class InMemoryDataStoreTest
    {
        private readonly InMemoryDataStore _store = new InMemoryDataStore();
        private readonly User _basicUser = User.WithKey("user-key");
        private readonly User _otherUser = User.WithKey("other-key");

        [Fact]
        public void GetForUnknownUser()
        {
            Assert.Null(_store.Get(_basicUser, "key1"));
        }

        [Fact]
        public void GetUnknownFlagForKnownUser()
        {
            var flag1 = new FeatureFlagBuilder().Version(100).Value(LdValue.Of(true)).Build();
            var initData = new DataSetBuilder()
                .Add("key1", flag1)
                .Build();
            _store.Init(_basicUser, initData);

            var flag2 = new FeatureFlagBuilder().Version(200).Value(LdValue.Of(false)).Build();
            var otherData = new DataSetBuilder().Add("key2", flag2).Build();
            _store.Init(_otherUser, otherData);

            Assert.Null(_store.Get(_basicUser, "key2"));
        }

        [Fact]
        public void GetDeletedFlagForKnownUser()
        {
            var initData = new DataSetBuilder()
                .AddDeleted("key2", 200)
                .Build();
            _store.Init(_basicUser, initData);

            Assert.Equal(new ItemDescriptor(200, null), _store.Get(_basicUser, "key2"));
        }

        [Fact]
        public void GetAllForUnknownUser()
        {
            Assert.Null(_store.GetAll(_basicUser));
        }

        [Fact]
        public void GetAllForKnownUser()
        {
            var flag1 = new FeatureFlagBuilder().Version(100).Value(LdValue.Of(true)).Build();
            var flag2 = new FeatureFlagBuilder().Version(200).Value(LdValue.Of(true)).Build();
            var initData = new DataSetBuilder()
                .Add("key1", flag1)
                .Add("key2", flag2)
                .AddDeleted("deletedKey", 300)
                .Build();

            _store.Init(_basicUser, initData);

            var otherData = new DataSetBuilder().AddDeleted("key1", 100).Build();
            _store.Init(_otherUser, otherData);

            var result = _store.GetAll(_basicUser);
            Assert.NotNull(result);
            Assert.Equal(initData, result.Value);
        }

        [Fact]
        public void UpsertAddsFlagForUnknownUser()
        {
            var flag1 = new FeatureFlagBuilder().Version(100).Value(LdValue.Of(true)).Build();
            
            var updated = _store.Upsert(_basicUser, "key1", flag1.ToItemDescriptor());
            Assert.True(updated);

            Assert.Equal(flag1.ToItemDescriptor(), _store.Get(_basicUser, "key1"));
        }

        [Fact]
        public void UpsertAddsFlagForKnownUser()
        {
            var flag1 = new FeatureFlagBuilder().Version(100).Value(LdValue.Of(true)).Build();
            var initData = new DataSetBuilder()
                .Add("key1", flag1)
                .Build();

            _store.Init(_basicUser, initData);

            var flag2 = new FeatureFlagBuilder().Version(200).Value(LdValue.Of(true)).Build();
            
            var updated = _store.Upsert(_basicUser, "key2", flag2.ToItemDescriptor());
            Assert.True(updated);

            Assert.Equal(flag2.ToItemDescriptor(), _store.Get(_basicUser, "key2"));
        }

        [Fact]
        public void UpsertUpdatesFlagForKnownUser()
        {
            var flag1a = new FeatureFlagBuilder().Version(100).Value(LdValue.Of(true)).Build();
            var expected = new DataSetBuilder()
                .Add("key1", flag1a)
                .Build();

            _store.Init(_basicUser, expected);

            var flag1b = new FeatureFlagBuilder().Version(101).Value(LdValue.Of(false)).Build();
            
            var updated = _store.Upsert(_basicUser, "key1", flag1b.ToItemDescriptor());
            Assert.True(updated);

            Assert.Equal(flag1b.ToItemDescriptor(), _store.Get(_basicUser, "key1"));
        }

        [Fact]
        public void UpsertDeletesFlagForKnownUser()
        {
            var flag1a = new FeatureFlagBuilder().Version(100).Value(LdValue.Of(true)).Build();
            var expected = new DataSetBuilder()
                .Add("key1", flag1a)
                .Build();

            _store.Init(_basicUser, expected);

            var flag1Deleted = new ItemDescriptor(101, null);

            var updated = _store.Upsert(_basicUser, "key1", flag1Deleted);
            Assert.True(updated);

            Assert.Equal(flag1Deleted, _store.Get(_basicUser, "key1"));
        }

        [Fact]
        public void UpsertUndeletesFlagForKnownUser()
        {
            var expected = new DataSetBuilder()
                .AddDeleted("key1", 100)
                .Build();

            _store.Init(_basicUser, expected);

            var flag1b = new FeatureFlagBuilder().Version(101).Value(LdValue.Of(false)).Build();
            
            var updated = _store.Upsert(_basicUser, "key1", flag1b.ToItemDescriptor());
            Assert.True(updated);

            Assert.Equal(flag1b.ToItemDescriptor(), _store.Get(_basicUser, "key1"));
        }

        [Fact]
        public void UpsertDoesNotUpdateFlagForKnownUserWithLowerVersion()
        {
            var flag1a = new FeatureFlagBuilder().Version(100).Value(LdValue.Of(true)).Build();
            var expected = new DataSetBuilder()
                .Add("key1", flag1a)
                .Build();

            _store.Init(_basicUser, expected);

            var flag1b = new FeatureFlagBuilder().Version(99).Value(LdValue.Of(false)).Build();
            
            var updated = _store.Upsert(_basicUser, "key1", flag1b.ToItemDescriptor());
            Assert.False(updated);

            Assert.Equal(flag1a.ToItemDescriptor(), _store.Get(_basicUser, "key1"));
        }

        [Fact]
        public void UpsertDoesNotDeleteFlagForKnownUserWithLowerVersion()
        {
            var flag1a = new FeatureFlagBuilder().Version(100).Value(LdValue.Of(true)).Build();
            var expected = new DataSetBuilder()
                .Add("key1", flag1a)
                .Build();

            _store.Init(_basicUser, expected);

            var deletedDesc = new ItemDescriptor(99, null);

            var updated = _store.Upsert(_basicUser, "key1", deletedDesc);
            Assert.False(updated);

            Assert.Equal(flag1a.ToItemDescriptor(), _store.Get(_basicUser, "key1"));
        }
    }
}

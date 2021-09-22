using LaunchDarkly.Sdk.Client.Internal.DataStores;
using LaunchDarkly.TestHelpers;
using Xunit;
using Xunit.Abstractions;

using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;

namespace LaunchDarkly.Sdk.Client.Internal.DataSources
{
    public class DataSourceUpdateSinkImplTest : BaseTest
    {
        private readonly InMemoryDataStore _store;
        private readonly FlagChangedEventManager _flagChangedEventManager;
        private readonly DataSourceUpdateSinkImpl _updateSink;
        private readonly User _basicUser = User.WithKey("user-key");
        private readonly User _otherUser = User.WithKey("other-key");

        public DataSourceUpdateSinkImplTest(ITestOutputHelper testOutput) : base(testOutput)
        {
            _store = new InMemoryDataStore();
            _flagChangedEventManager = new FlagChangedEventManager(testLogger);
            _updateSink = new DataSourceUpdateSinkImpl(_store, _flagChangedEventManager);
        }

        [Fact]
        public void InitPassesDataToStore()
        {
            var initData = new DataSetBuilder().Add("key1", new FeatureFlagBuilder().Build()).Build();
            _updateSink.Init(initData, _basicUser);

            Assert.Equal(initData.Items, _store.GetAll(_basicUser).Value.Items);
        }

        [Fact]
        public void UpsertPassesDataToStore()
        {
            var flag1a = new FeatureFlagBuilder().Version(100).Value(LdValue.Of(false)).Build();
            var initData = new DataSetBuilder().Add("key1", flag1a).Build();
            _updateSink.Init(initData, _basicUser);

            var flag1b = new FeatureFlagBuilder().Version(101).Value(LdValue.Of(true)).Build();

            _updateSink.Upsert("key1", flag1b.ToItemDescriptor(), _basicUser);

            Assert.Equal(flag1b.ToItemDescriptor(), _store.Get(_basicUser, "key1"));
        }

        [Fact]
        public void NoEventsAreSentForFirstInit()
        {
            var events = new EventSink<FlagChangedEventArgs>();
            _flagChangedEventManager.FlagChanged += events.Add;

            var initData = new DataSetBuilder().Add("key1", new FeatureFlagBuilder().Build()).Build();
            _updateSink.Init(initData, _basicUser);

            events.ExpectNoValue();
        }

        [Fact]
        public void NoEventsAreSentForUpsertIfNeverInited()
        {
            var events = new EventSink<FlagChangedEventArgs>();
            _flagChangedEventManager.FlagChanged += events.Add;

            _updateSink.Upsert("key1", new FeatureFlagBuilder().Build().ToItemDescriptor(), _basicUser);

            events.ExpectNoValue();
        }

        [Fact]
        public void EventIsSentForChangedFlagOnInit()
        {
            var events = new EventSink<FlagChangedEventArgs>();
            _flagChangedEventManager.FlagChanged += events.Add;

            var initData1 = new DataSetBuilder()
                .Add("key1", new FeatureFlagBuilder().Value(LdValue.Of(true)).Variation(0).Build())
                .Add("key2", new FeatureFlagBuilder().Value(LdValue.Of(true)).Variation(0).Build())
                .Build();
            _updateSink.Init(initData1, _basicUser);

            var initData2 = new DataSetBuilder()
                .Add("key1", new FeatureFlagBuilder().Value(LdValue.Of(false)).Variation(1).Build())
                .Add("key2", new FeatureFlagBuilder().Value(LdValue.Of(true)).Variation(0).Build())
                .Build();
            _updateSink.Init(initData2, _basicUser);

            var e = events.ExpectValue();
            Assert.Equal("key1", e.Key);
            Assert.Equal(LdValue.Of(true), e.OldValue);
            Assert.Equal(LdValue.Of(false), e.NewValue);
            Assert.False(e.FlagWasDeleted);
        }

        [Fact]
        public void EventIsSentForAddedFlagOnInit()
        {
            var events = new EventSink<FlagChangedEventArgs>();
            _flagChangedEventManager.FlagChanged += events.Add;

            var initData1 = new DataSetBuilder()
                .Add("key2", new FeatureFlagBuilder().Value(LdValue.Of(true)).Variation(0).Build())
                .Build();
            _updateSink.Init(initData1, _basicUser);

            var initData2 = new DataSetBuilder()
                .Add("key1", new FeatureFlagBuilder().Value(LdValue.Of(false)).Variation(1).Build())
                .Add("key2", new FeatureFlagBuilder().Value(LdValue.Of(true)).Variation(0).Build())
                .Build();
            _updateSink.Init(initData2, _basicUser);

            var e = events.ExpectValue();
            Assert.Equal("key1", e.Key);
            Assert.Equal(LdValue.Null, e.OldValue);
            Assert.Equal(LdValue.Of(false), e.NewValue);
            Assert.False(e.FlagWasDeleted);
        }

        [Fact]
        public void EventIsSentForDeletedFlagOnInit()
        {
            var events = new EventSink<FlagChangedEventArgs>();
            _flagChangedEventManager.FlagChanged += events.Add;

            var initData1 = new DataSetBuilder()
                .Add("key1", new FeatureFlagBuilder().Value(LdValue.Of(true)).Variation(0).Build())
                .Add("key2", new FeatureFlagBuilder().Value(LdValue.Of(true)).Variation(0).Build())
                .Build();
            _updateSink.Init(initData1, _basicUser);

            var initData2 = new DataSetBuilder()
                .Add("key2", new FeatureFlagBuilder().Value(LdValue.Of(true)).Variation(0).Build())
                .Build();
            _updateSink.Init(initData2, _basicUser);

            var e = events.ExpectValue();
            Assert.Equal("key1", e.Key);
            Assert.Equal(LdValue.Of(true), e.OldValue);
            Assert.Equal(LdValue.Null, e.NewValue);
            Assert.True(e.FlagWasDeleted);
        }

        [Fact]
        public void EventIsSentForChangedFlagOnUpsert()
        {
            var events = new EventSink<FlagChangedEventArgs>();
            _flagChangedEventManager.FlagChanged += events.Add;

            var initData = new DataSetBuilder()
                .Add("key1", new FeatureFlagBuilder().Version(100).Value(LdValue.Of(true)).Variation(0).Build())
                .Add("key2", new FeatureFlagBuilder().Version(200).Value(LdValue.Of(true)).Variation(0).Build())
                .Build();
            _updateSink.Init(initData, _basicUser);

            _updateSink.Upsert("key1",
                new FeatureFlagBuilder().Version(101).Value(LdValue.Of(false)).Variation(1).Build().ToItemDescriptor(),
                _basicUser);

            var e = events.ExpectValue();
            Assert.Equal("key1", e.Key);
            Assert.Equal(LdValue.Of(true), e.OldValue);
            Assert.Equal(LdValue.Of(false), e.NewValue);
            Assert.False(e.FlagWasDeleted);
        }

        [Fact]
        public void EventIsSentForAddedFlagOnUpsert()
        {
            var events = new EventSink<FlagChangedEventArgs>();
            _flagChangedEventManager.FlagChanged += events.Add;

            var initData = new DataSetBuilder()
                .Add("key2", new FeatureFlagBuilder().Version(200).Value(LdValue.Of(true)).Variation(0).Build())
                .Build();
            _updateSink.Init(initData, _basicUser);

            _updateSink.Upsert("key1",
                new FeatureFlagBuilder().Version(100).Value(LdValue.Of(false)).Variation(1).Build().ToItemDescriptor(),
                _basicUser);

            var e = events.ExpectValue();
            Assert.Equal("key1", e.Key);
            Assert.Equal(LdValue.Null, e.OldValue);
            Assert.Equal(LdValue.Of(false), e.NewValue);
            Assert.False(e.FlagWasDeleted);
        }

        [Fact]
        public void EventIsSentForDeletedFlagOnUpsert()
        {
            var events = new EventSink<FlagChangedEventArgs>();
            _flagChangedEventManager.FlagChanged += events.Add;

            var initData = new DataSetBuilder()
                .Add("key1", new FeatureFlagBuilder().Version(100).Value(LdValue.Of(true)).Variation(0).Build())
                .Add("key2", new FeatureFlagBuilder().Version(200).Value(LdValue.Of(true)).Variation(0).Build())
                .Build();
            _updateSink.Init(initData, _basicUser);

            _updateSink.Upsert("key1", new ItemDescriptor(101, null), _basicUser);

            var e = events.ExpectValue();
            Assert.Equal("key1", e.Key);
            Assert.Equal(LdValue.Of(true), e.OldValue);
            Assert.Equal(LdValue.Null, e.NewValue);
            Assert.True(e.FlagWasDeleted);
        }

        [Fact]
        public void EventIsNotSentIfUpsertFailsDueToLowerVersion()
        {
            var events = new EventSink<FlagChangedEventArgs>();
            _flagChangedEventManager.FlagChanged += events.Add;

            var initData = new DataSetBuilder()
                .Add("key1", new FeatureFlagBuilder().Version(100).Value(LdValue.Of(true)).Variation(0).Build())
                .Add("key2", new FeatureFlagBuilder().Version(200).Value(LdValue.Of(true)).Variation(0).Build())
                .Build();
            _updateSink.Init(initData, _basicUser);

            _updateSink.Upsert("key1",
                new FeatureFlagBuilder().Version(99).Value(LdValue.Of(false)).Variation(1).Build().ToItemDescriptor(),
                _basicUser);

            events.ExpectNoValue();
        }

        [Fact]
        public void ValueChangesAreTrackedSeparatelyForEachUser()
        {
            var events = new EventSink<FlagChangedEventArgs>();
            _flagChangedEventManager.FlagChanged += events.Add;

            var initDataForBasicUser = new DataSetBuilder()
                .Add("key1", new FeatureFlagBuilder().Version(100).Value(LdValue.Of("a")).Variation(1).Build())
                .Add("key2", new FeatureFlagBuilder().Version(200).Value(LdValue.Of("b")).Variation(2).Build())
                .Build();
            _updateSink.Init(initDataForBasicUser, _basicUser);

            var initDataForOtherUser = new DataSetBuilder()
                .Add("key1", new FeatureFlagBuilder().Version(100).Value(LdValue.Of("c")).Variation(3).Build())
                .Add("key2", new FeatureFlagBuilder().Version(200).Value(LdValue.Of("d")).Variation(4).Build())
                .Build();
            _updateSink.Init(initDataForOtherUser, _otherUser);

            events.ExpectNoValue();

            _updateSink.Upsert("key1",
                new FeatureFlagBuilder().Version(101).Value(LdValue.Of("c")).Variation(3).Build().ToItemDescriptor(),
                _basicUser);

            var e = events.ExpectValue();

            Assert.Equal("key1", e.Key);
            Assert.Equal(LdValue.Of("a"), e.OldValue);
            Assert.Equal(LdValue.Of("c"), e.NewValue);
        }
    }
}

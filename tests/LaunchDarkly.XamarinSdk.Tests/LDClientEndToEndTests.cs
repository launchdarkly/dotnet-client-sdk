using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using LaunchDarkly.Client;
using LaunchDarkly.Xamarin.PlatformSpecific;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    // Tests of an LDClient instance doing actual HTTP against an embedded server. These aren't intended to cover
    // every possible type of interaction, since the lower-level component tests like FeatureFlagRequestorTests
    // (and the DefaultEventProcessor and StreamManager tests in LaunchDarkly.CommonSdk) cover those more thoroughly.
    // These are more of a smoke test to ensure that the SDK is initializing and using those components in the
    // expected ways.
    public class LdClientEndToEndTests : BaseTest
    {
        private const string _mobileKey = "FAKE_KEY";

        private static readonly User _user = User.WithKey("foo");
        private static readonly User _otherUser = User.WithKey("bar");

        private static readonly IDictionary<string, string> _flagData1 = new Dictionary<string, string>
        {
            { "flag1", "value1" }
        };

        private static readonly IDictionary<string, string> _flagData2 = new Dictionary<string, string>
        {
            { "flag1", "value2" }
        };

        public static readonly IEnumerable<object[]> PollingAndStreaming = new List<object[]>
        {
            { new object[] { UpdateMode.Polling } },
            { new object[] { UpdateMode.Streaming } }
        };

        [Theory]
        [MemberData(nameof(PollingAndStreaming))]
        public void InitGetsFlagsSync(UpdateMode mode)
        {
            WithServer(server =>
            {
                SetupResponse(server, _flagData1, mode);

                var config = BaseConfig(server, mode);
                using (var client = TestUtil.CreateClient(config, _user))
                {
                    VerifyRequest(server, mode);
                    VerifyFlagValues(client, _flagData1);
                }
            });
        }

        [Theory]
        [MemberData(nameof(PollingAndStreaming))]
        public async Task InitGetsFlagsAsync(UpdateMode mode)
        {
            await WithServerAsync(async server =>
            {
                SetupResponse(server, _flagData1, mode);

                var config = BaseConfig(server, mode);
                using (var client = await TestUtil.CreateClientAsync(config, _user))
                {
                    VerifyRequest(server, mode);
                }
            });
        }

        [Fact]
        public void InitCanTimeOutSync()
        {
            WithServer(server =>
            {
                server.ForAllRequests(r => r.WithDelay(TimeSpan.FromSeconds(2)).WithJsonBody(PollingData(_flagData1)));

                using (var log = new LogSinkScope())
                {
                    var config = BaseConfig(server, builder => builder.IsStreamingEnabled(false));
                    using (var client = TestUtil.CreateClient(config, _user, TimeSpan.FromMilliseconds(200)))
                    {
                        Assert.False(client.Initialized);
                        Assert.Null(client.StringVariation(_flagData1.First().Key, null));
                        Assert.Contains(log.Messages, m => m.Level == LogLevel.Warn &&
                            m.Text == "Client did not successfully initialize within 200 milliseconds.");
                    }
                }
            });
        }

        [Theory]
        [MemberData(nameof(PollingAndStreaming))]
        public void InitFailsOn401Sync(UpdateMode mode)
        {
            WithServer(server =>
            {
                server.ForAllRequests(r => r.WithStatusCode(401));

                using (var log = new LogSinkScope())
                {
                    var config = BaseConfig(server, mode);
                    using (var client = TestUtil.CreateClient(config, _user))
                    {
                        Assert.False(client.Initialized);
                    }
                }
            });
        }

        [Theory]
        [MemberData(nameof(PollingAndStreaming))]
        public async Task InitFailsOn401Async(UpdateMode mode)
        {
            await WithServerAsync(async server =>
            {
                server.ForAllRequests(r => r.WithStatusCode(401));

                using (var log = new LogSinkScope())
                {
                    var config = BaseConfig(server, mode);

                    // Currently the behavior of LdClient.InitAsync is somewhat inconsistent with LdClient.Init if there is
                    // an unrecoverable error: LdClient.Init throws an exception, but LdClient.InitAsync returns a task that
                    // will complete successfully with an uninitialized client.
                    using (var client = await TestUtil.CreateClientAsync(config, _user))
                    {
                        Assert.False(client.Initialized);
                    }
                }
            });
        }

        [Fact]
        public async Task InitWithKeylessAnonUserAddsKeyAndReusesIt()
        {
            // Note, we don't care about polling mode vs. streaming mode for this functionality.
            await WithServerAsync(async server =>
            {
                server.ForAllRequests(r => r.WithDelay(TimeSpan.FromSeconds(2)).WithJsonBody(PollingData(_flagData1)));

                var config = BaseConfig(server, UpdateMode.Polling);
                var name = "Sue";
                var anonUser = User.Builder((string)null).Name(name).Anonymous(true).Build();

                // Note, on mobile platforms, the generated user key is the device ID and is stable; on other platforms,
                // it's a GUID that is cached in local storage. Calling ClearCachedClientId() resets the latter.
                ClientIdentifier.ClearCachedClientId();

                string generatedKey = null;
                using (var client = await TestUtil.CreateClientAsync(config, anonUser))
                {
                    Assert.NotNull(client.User.Key);
                    generatedKey = client.User.Key;
                    Assert.Equal(name, client.User.Name);
                }

                using (var client = await TestUtil.CreateClientAsync(config, anonUser))
                {
                    Assert.Equal(generatedKey, client.User.Key);
                }
            });
        }

        [Theory]
        [MemberData(nameof(PollingAndStreaming))]
        public void IdentifySwitchesUserAndGetsFlagsSync(UpdateMode mode)
        {
            WithServer(server =>
            {
                SetupResponse(server, _flagData1, mode);

                var config = BaseConfig(server, mode);
                using (var client = TestUtil.CreateClient(config, _user))
                {
                    VerifyRequest(server, mode);
                    VerifyFlagValues(client, _flagData1);
                    var user1RequestPath = server.GetLastRequest().Path;

                    server.Reset();
                    SetupResponse(server, _flagData2, mode);

                    var success = client.Identify(_otherUser, TimeSpan.FromSeconds(5));
                    Assert.True(success);
                    Assert.True(client.Initialized);
                    Assert.Equal(_otherUser.Key, client.User.Key); // don't compare entire user, because SDK may have added device/os attributes

                    VerifyRequest(server, mode);
                    Assert.NotEqual(user1RequestPath, server.GetLastRequest().Path);
                    VerifyFlagValues(client, _flagData2);
                }
            });
        }

        [Theory]
        [MemberData(nameof(PollingAndStreaming))]
        public async Task IdentifySwitchesUserAndGetsFlagsAsync(UpdateMode mode)
        {
            await WithServerAsync(async server =>
            {
                SetupResponse(server, _flagData1, mode);

                var config = BaseConfig(server, mode);
                using (var client = await TestUtil.CreateClientAsync(config, _user))
                {
                    VerifyRequest(server, mode);
                    VerifyFlagValues(client, _flagData1);
                    var user1RequestPath = server.GetLastRequest().Path;

                    server.Reset();
                    SetupResponse(server, _flagData2, mode);

                    var success = await client.IdentifyAsync(_otherUser);
                    Assert.True(success);
                    Assert.True(client.Initialized);
                    Assert.Equal(_otherUser.Key, client.User.Key); // don't compare entire user, because SDK may have added device/os attributes

                    VerifyRequest(server, mode);
                    Assert.NotEqual(user1RequestPath, server.GetLastRequest().Path);
                    VerifyFlagValues(client, _flagData2);
                }
            });
        }

        [Theory]
        [MemberData(nameof(PollingAndStreaming))]
        public void IdentifyCanTimeOutSync(UpdateMode mode)
        {
            WithServer(server =>
            {
                SetupResponse(server, _flagData1, mode);

                var config = BaseConfig(server, mode);
                using (var client = TestUtil.CreateClient(config, _user))
                {
                    VerifyRequest(server, mode);
                    VerifyFlagValues(client, _flagData1);
                    var user1RequestPath = server.GetLastRequest().Path;

                    server.Reset();
                    server.ForAllRequests(r => r.WithDelay(TimeSpan.FromSeconds(2)).WithJsonBody(PollingData(_flagData1)));

                    var success = client.Identify(_otherUser, TimeSpan.FromMilliseconds(100));
                    Assert.False(success);
                    Assert.False(client.Initialized);
                    Assert.Null(client.StringVariation(_flagData1.First().Key, null));
                }
            });
        }

        [Fact]
        public void OfflineClientUsesCachedFlagsSync()
        {
            WithServer(server =>
            {
                SetupResponse(server, _flagData1, UpdateMode.Polling); // streaming vs. polling should make no difference for this

                ClearCachedFlags(_user);
                try
                {
                    var config = BaseConfig(server, UpdateMode.Polling, builder => builder.PersistFlagValues(true));
                    using (var client = TestUtil.CreateClient(config, _user))
                    {
                        VerifyFlagValues(client, _flagData1);
                    }

                    // At this point the SDK should have written the flags to persistent storage for this user key.
                    // We'll now start over, but with a server that doesn't respond immediately. When the client times
                    // out, we should still see the earlier flag values.

                    server.Reset(); // the offline client shouldn't be making any requests, but just in case
                    var offlineConfig = Configuration.Builder(_mobileKey).Offline(true).Build();
                    using (var client = TestUtil.CreateClient(offlineConfig, _user))
                    {
                        VerifyFlagValues(client, _flagData1);
                    }
                }
                finally
                {
                    ClearCachedFlags(_user);
                }
            });
        }

        [Fact]
        public async Task OfflineClientUsesCachedFlagsAsync()
        {
            await WithServerAsync(async server =>
            {
                SetupResponse(server, _flagData1, UpdateMode.Polling); // streaming vs. polling should make no difference for this

                ClearCachedFlags(_user);
                try
                {
                    var config = BaseConfig(server, UpdateMode.Polling, builder => builder.PersistFlagValues(true));
                    using (var client = await TestUtil.CreateClientAsync(config, _user))
                    {
                        VerifyFlagValues(client, _flagData1);
                    }

                    // At this point the SDK should have written the flags to persistent storage for this user key.
                    // We'll now start over, but with a server that doesn't respond immediately. When the client times
                    // out, we should still see the earlier flag values.

                    server.Reset(); // the offline client shouldn't be making any requests, but just in case
                    var offlineConfig = Configuration.Builder(_mobileKey).Offline(true).Build();
                    using (var client = await TestUtil.CreateClientAsync(offlineConfig, _user))
                    {
                        VerifyFlagValues(client, _flagData1);
                    }
                }
                finally
                {
                    ClearCachedFlags(_user);
                }
            });
        }

        [Fact]
        public async Task BackgroundModeForcesPollingAsync()
        {
            var mockBackgroundModeManager = new MockBackgroundModeManager();
            var backgroundInterval = TimeSpan.FromMilliseconds(50);

            ClearCachedFlags(_user);
            await WithServerAsync(async server =>
            {
                var config = BaseConfig(server, UpdateMode.Streaming, builder => builder
                    .BackgroundModeManager(mockBackgroundModeManager)
                    .BackgroundPollingIntervalWithoutMinimum(backgroundInterval)
                    .PersistFlagValues(false));

                SetupResponse(server, _flagData1, UpdateMode.Streaming);

                using (var client = await TestUtil.CreateClientAsync(config, _user))
                {
                    VerifyFlagValues(client, _flagData1);

                    // Set it up so that when the client switches to background mode and does a polling request, it will
                    // receive _flagData2, and we will be notified of that via a change event. SetupResponse will only
                    // configure the polling endpoint, so if the client makes a streaming request here it'll fail.
                    SetupResponse(server, _flagData2, UpdateMode.Polling);
                    var receivedChangeSignal = new SemaphoreSlim(0, 1);
                    client.FlagChanged += (sender, args) =>
                    {
                        receivedChangeSignal.Release();
                    };

                    mockBackgroundModeManager.UpdateBackgroundMode(true);

                    await receivedChangeSignal.WaitAsync();
                    VerifyFlagValues(client, _flagData2);

                    // Now switch back to streaming
                    SetupResponse(server, _flagData1, UpdateMode.Streaming);
                    mockBackgroundModeManager.UpdateBackgroundMode(false);

                    await receivedChangeSignal.WaitAsync();
                    VerifyFlagValues(client, _flagData1);
                }
            });
        }

        [Fact]
        public async Task BackgroundModePollingCanBeDisabledAsync()
        {
            var mockBackgroundModeManager = new MockBackgroundModeManager();
            var backgroundInterval = TimeSpan.FromMilliseconds(50);
            var hackyUpdateDelay = TimeSpan.FromMilliseconds(200);

            ClearCachedFlags(_user);
            await WithServerAsync(async server =>
            {
                var config = BaseConfig(server, UpdateMode.Streaming, builder => builder
                    .BackgroundModeManager(mockBackgroundModeManager)
                    .EnableBackgroundUpdating(false)
                    .BackgroundPollingInterval(backgroundInterval)
                    .PersistFlagValues(false));

                SetupResponse(server, _flagData1, UpdateMode.Streaming);

                using (var client = await TestUtil.CreateClientAsync(config, _user))
                {
                    VerifyFlagValues(client, _flagData1);

                    // The SDK should *not* hit this polling endpoint, but we're providing some data there so we can
                    // detect whether it does.
                    SetupResponse(server, _flagData2, UpdateMode.Polling);
                    mockBackgroundModeManager.UpdateBackgroundMode(true);

                    await Task.Delay(hackyUpdateDelay);
                    VerifyFlagValues(client, _flagData1);  // we should *not* have done a poll

                    var receivedChangeSignal = new SemaphoreSlim(0, 1);
                    client.FlagChanged += (sender, args) =>
                    {
                        receivedChangeSignal.Release();
                    };

                    // Now switch back to streaming
                    SetupResponse(server, _flagData2, UpdateMode.Streaming);
                    mockBackgroundModeManager.UpdateBackgroundMode(false);

                    await receivedChangeSignal.WaitAsync();
                    VerifyFlagValues(client, _flagData2);
                }
            });
        }

        [Theory]
        [MemberData(nameof(PollingAndStreaming))]
        public async Task OfflineClientGoesOnlineAndGetsFlagsAsync(UpdateMode mode)
        {
            await WithServerAsync(async server =>
            {
                ClearCachedFlags(_user);
                var config = BaseConfig(server, mode, builder => builder.Offline(true).PersistFlagValues(false));
                using (var client = await TestUtil.CreateClientAsync(config, _user))
                {
                    VerifyNoFlagValues(client, _flagData1);

                    SetupResponse(server, _flagData1, mode);

                    await client.SetOfflineAsync(false);

                    VerifyFlagValues(client, _flagData1);
                }
            });
        }

        private Configuration BaseConfig(FluentMockServer server, Func<ConfigurationBuilder, IConfigurationBuilder> extraConfig = null)
        {
            var builderInternal = Configuration.BuilderInternal(_mobileKey)
                .EventProcessor(new MockEventProcessor());
            builderInternal
                .BaseUri(new Uri(server.GetUrl()))
                .StreamUri(new Uri(server.GetUrl()))
                .PersistFlagValues(false);  // unless we're specifically testing flag caching, this helps to prevent test state contamination
            var builder = extraConfig == null ? builderInternal : extraConfig(builderInternal);
            return builder.Build();
        }

        private Configuration BaseConfig(FluentMockServer server, UpdateMode mode, Func<ConfigurationBuilder, IConfigurationBuilder> extraConfig = null)
        {
            return BaseConfig(server, builder =>
            {
                builder.IsStreamingEnabled(mode.IsStreaming);
                return extraConfig == null ? builder : extraConfig(builder);
            });
        }

        // 
        private SemaphoreSlim SetupResponse(FluentMockServer server, IDictionary<string, string> data, UpdateMode mode)
        {
            var signal = new SemaphoreSlim(0, 1);
            server.ResetMappings();
            var resp = Response.Create().WithCallback(req =>
            {
                signal.Release();
                var respBuilder = mode.IsStreaming ?
                    Response.Create().WithEventsBody(StreamingData(data)) :
                    Response.Create().WithJsonBody(PollingData(data));
                return ((Response)respBuilder).ResponseMessage;
            });

            // Note: in streaming mode, since WireMock.Net doesn't seem to support streaming responses, the fake response will close
            // after the end of the data-- so the SDK will enter retry mode and we may get another identical streaming request. For
            // the purposes of these tests, that doesn't matter. The correct processing of a chunked stream is tested in the
            // LaunchDarkly.EventSource tests, and the retry logic is tested in LaunchDarkly.CommonSdk.

            server.Given(Request.Create().WithPath(path => Regex.IsMatch(path, mode.FlagsPathRegex)))
                .RespondWith(resp);
            return signal;
        }

        private void VerifyRequest(FluentMockServer server, UpdateMode mode)
        {
            var req = server.GetLastRequest();
            Assert.Equal("GET", req.Method);

            // Note, we don't check for an exact match of the encoded user string in Req.Path because it is not determinate - the
            // SDK may add custom attributes to the user ("os" etc.) and since we don't canonicalize the JSON representation,
            // properties could be serialized in any order causing the encoding to vary. Also, we don't test REPORT mode here
            // because it is already covered in FeatureFlagRequestorTest.
            Assert.Matches(mode.FlagsPathRegex, req.Path);

            Assert.Equal("", req.RawQuery);
            Assert.Equal(_mobileKey, req.Headers["Authorization"][0]);
            Assert.Null(req.Body);
        }

        private void VerifyFlagValues(ILdClient client, IDictionary<string, string> flags)
        {
            Assert.True(client.Initialized);
            foreach (var e in flags)
            {
                Assert.Equal(e.Value, client.StringVariation(e.Key, null));
            }
        }

        private void VerifyNoFlagValues(ILdClient client, IDictionary<string, string> flags)
        {
            Assert.True(client.Initialized);
            foreach (var e in flags)
            {
                Assert.Null(client.StringVariation(e.Key, null));
            }
        }

        private LdValue FlagJson(string key, string value)
        {
            return LdValue.ObjectFrom(new Dictionary<string, LdValue>
            {
                { "key", LdValue.Of(key) },
                { "value", LdValue.Of(value) }
            });
        }

        private string PollingData(IDictionary<string, string> flags)
        {
            var d = new Dictionary<string, LdValue>();
            foreach (var e in flags)
            {
                d.Add(e.Key, FlagJson(e.Key, e.Value));
            }
            return LdValue.ObjectFrom(d).ToJsonString();
        }

        private string StreamingData(IDictionary<string, string> flags)
        {
            return "event: put\ndata: " + PollingData(flags) + "\n\n";
        }
    }

    public class UpdateMode
    {
        public bool IsStreaming { get; private set; }
        public string FlagsPathRegex { get; private set; }

        public static readonly UpdateMode Streaming = new UpdateMode
        {
            IsStreaming = true,
            FlagsPathRegex = "^/meval/[^/?]+"
        };

        public static readonly UpdateMode Polling = new UpdateMode
        {
            IsStreaming = false,
            FlagsPathRegex = "^/msdk/evalx/users/[^/?]+"
        };

        public override string ToString() => IsStreaming ? "Streaming" : "Polling";
    }
}

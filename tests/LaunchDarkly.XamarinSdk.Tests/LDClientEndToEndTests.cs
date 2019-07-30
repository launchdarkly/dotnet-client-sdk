using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Logging;
using LaunchDarkly.Client;
using LaunchDarkly.Xamarin.PlatformSpecific;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        [Theory(Skip = SkipIfCannotCreateHttpServer)]
        [MemberData(nameof(PollingAndStreaming))]
        public void InitGetsFlagsSync(UpdateMode mode)
        {
            WithServer(server =>
            {
                SetupResponse(server, _flagData1, mode);

                var config = BaseConfig(server).UseReport(false).IsStreamingEnabled(mode.IsStreaming).Build();
                using (var client = TestUtil.CreateClient(config, _user))
                {
                    VerifyRequest(server, mode);
                    VerifyFlagValues(client, _flagData1);
                }
            });
        }

        [Theory(Skip = SkipIfCannotCreateHttpServer)]
        [MemberData(nameof(PollingAndStreaming))]
        public async Task InitGetsFlagsAsync(UpdateMode mode)
        {
            await WithServerAsync(async server =>
            {
                SetupResponse(server, _flagData1, mode);

                var config = BaseConfig(server).UseReport(false).IsStreamingEnabled(mode.IsStreaming).Build();
                using (var client = await TestUtil.CreateClientAsync(config, _user))
                {
                    VerifyRequest(server, mode);
                }
            });
        }

        [Fact(Skip = SkipIfCannotCreateHttpServer)]
        public void InitCanTimeOutSync()
        {
            WithServer(server =>
            {
                server.ForAllRequests(r => r.WithDelay(TimeSpan.FromSeconds(2)).WithJsonBody(PollingData(_flagData1)));

                using (var log = new LogSinkScope())
                {
                    var config = BaseConfig(server).IsStreamingEnabled(false).Build();
                    using (var client = TestUtil.CreateClient(config, _user, TimeSpan.FromMilliseconds(200)))
                    {
                        Assert.False(client.Initialized());
                        Assert.Null(client.StringVariation(_flagData1.First().Key, null));
                        Assert.Contains(log.Messages, m => m.Level == LogLevel.Warn &&
                            m.Text == "Client did not successfully initialize within 200 milliseconds.");
                    }
                }
            });
        }

        [Theory(Skip = SkipIfCannotCreateHttpServer)]
        [MemberData(nameof(PollingAndStreaming))]
        public void InitFailsOn401Sync(UpdateMode mode)
        {
            WithServer(server =>
            {
                server.ForAllRequests(r => r.WithStatusCode(401));

                using (var log = new LogSinkScope())
                {
                    try
                    {
                        var config = BaseConfig(server).UseReport(false).IsStreamingEnabled(mode.IsStreaming).Build();
                        using (var client = TestUtil.CreateClient(config, _user)) { }
                    }
                    catch (Exception e)
                    {
                        // Currently the exact class of this exception is undefined: the polling processor throws
                        // LaunchDarkly.Client.UnsuccessfulResponseException, while the streaming processor throws
                        // a lower-level exception that is defined by LaunchDarkly.EventSource.
                        Assert.Contains("401", e.Message);
                        return;
                    }
                    throw new Exception("Expected exception from LdClient.Init");
                }
            });
        }

        [Theory(Skip = SkipIfCannotCreateHttpServer)]
        [MemberData(nameof(PollingAndStreaming))]
        public async Task InitFailsOn401Async(UpdateMode mode)
        {
            await WithServerAsync(async server =>
            {
                server.ForAllRequests(r => r.WithStatusCode(401));

                using (var log = new LogSinkScope())
                {
                    var config = BaseConfig(server).UseReport(false).IsStreamingEnabled(mode.IsStreaming).Build();

                    // Currently the behavior of LdClient.InitAsync is somewhat inconsistent with LdClient.Init if there is
                    // an unrecoverable error: LdClient.Init throws an exception, but LdClient.InitAsync returns a task that
                    // will complete successfully with an uninitialized client.
                    using (var client = await TestUtil.CreateClientAsync(config, _user))
                    {
                        Assert.False(client.Initialized());
                    }
                }
            });
        }

        [Fact(Skip = SkipIfCannotCreateHttpServer)]
        public async Task InitWithKeylessAnonUserAddsKeyAndReusesIt()
        {
            // Note, we don't care about polling mode vs. streaming mode for this functionality.
            await WithServerAsync(async server =>
            {
                server.ForAllRequests(r => r.WithDelay(TimeSpan.FromSeconds(2)).WithJsonBody(PollingData(_flagData1)));

                var config = BaseConfig(server).UseReport(false).IsStreamingEnabled(false).Build();
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

        [Theory(Skip = SkipIfCannotCreateHttpServer)]
        [MemberData(nameof(PollingAndStreaming))]
        public void IdentifySwitchesUserAndGetsFlagsSync(UpdateMode mode)
        {
            WithServer(server =>
            {
                SetupResponse(server, _flagData1, mode);

                var config = BaseConfig(server).UseReport(false).IsStreamingEnabled(mode.IsStreaming).Build();
                using (var client = TestUtil.CreateClient(config, _user))
                {
                    VerifyRequest(server, mode);
                    VerifyFlagValues(client, _flagData1);
                    var user1RequestPath = server.GetLastRequest().Path;

                    server.Reset();
                    SetupResponse(server, _flagData2, mode);

                    client.Identify(_otherUser);
                    Assert.Equal(_otherUser.Key, client.User.Key); // don't compare entire user, because SDK may have added device/os attributes

                    VerifyRequest(server, mode);
                    Assert.NotEqual(user1RequestPath, server.GetLastRequest().Path);
                    VerifyFlagValues(client, _flagData2);
                }
            });
        }

        [Theory(Skip = SkipIfCannotCreateHttpServer)]
        [MemberData(nameof(PollingAndStreaming))]
        public async Task IdentifySwitchesUserAndGetsFlagsAsync(UpdateMode mode)
        {
            await WithServerAsync(async server =>
            {
                SetupResponse(server, _flagData1, mode);

                var config = BaseConfig(server).UseReport(false).IsStreamingEnabled(mode.IsStreaming).Build();
                using (var client = await TestUtil.CreateClientAsync(config, _user))
                {
                    VerifyRequest(server, mode);
                    VerifyFlagValues(client, _flagData1);
                    var user1RequestPath = server.GetLastRequest().Path;

                    server.Reset();
                    SetupResponse(server, _flagData2, mode);

                    await client.IdentifyAsync(_otherUser);
                    Assert.Equal(_otherUser.Key, client.User.Key); // don't compare entire user, because SDK may have added device/os attributes

                    VerifyRequest(server, mode);
                    Assert.NotEqual(user1RequestPath, server.GetLastRequest().Path);
                    VerifyFlagValues(client, _flagData2);
                }
            });
        }

        [Fact(Skip = SkipIfCannotCreateHttpServer)]
        public void OfflineClientUsesCachedFlagsSync()
        {
            WithServer(server =>
            {
                SetupResponse(server, _flagData1, UpdateMode.Polling); // streaming vs. polling should make no difference for this

                var config = BaseConfig(server).UseReport(false).IsStreamingEnabled(false).Build();
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
            });
        }

        [Fact(Skip = SkipIfCannotCreateHttpServer)]
        public async Task OfflineClientUsesCachedFlagsAsync()
        {
            await WithServerAsync(async server =>
            {
                SetupResponse(server, _flagData1, UpdateMode.Polling); // streaming vs. polling should make no difference for this

                var config = BaseConfig(server).UseReport(false).IsStreamingEnabled(false).Build();
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
            });
        }

        private IConfigurationBuilder BaseConfig(FluentMockServer server)
        {
            return Configuration.BuilderInternal(_mobileKey)
                .EventProcessor(new MockEventProcessor())
                .BaseUri(new Uri(server.GetUrl()))
                .StreamUri(new Uri(server.GetUrl()));
        }

        private void SetupResponse(FluentMockServer server, IDictionary<string, string> data, UpdateMode mode)
        {
            server.ForAllRequests(r =>
                mode.IsStreaming ? r.WithEventsBody(StreamingData(data)) : r.WithJsonBody(PollingData(data)));
            // Note: in streaming mode, since WireMock.Net doesn't seem to support streaming responses, the fake response will close
            // after the end of the data-- so the SDK will enter retry mode and we may get another identical streaming request. For
            // the purposes of these tests, that doesn't matter. The correct processing of a chunked stream is tested in the
            // LaunchDarkly.EventSource tests, and the retry logic is tested in LaunchDarkly.CommonSdk.
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

        private void VerifyFlagValues(ILdMobileClient client, IDictionary<string, string> flags)
        {
            Assert.True(client.Initialized());
            foreach (var e in flags)
            {
                Assert.Equal(e.Value, client.StringVariation(e.Key, null));
            }
        }

        private JToken FlagJson(string key, string value)
        {
            var o = new JObject();
            o.Add("key", key);
            o.Add("value", value);
            return o;
        }

        private string PollingData(IDictionary<string, string> flags)
        {
            var o = new JObject();
            foreach (var e in flags)
            {
                o.Add(e.Key, FlagJson(e.Key, e.Value));
            }
            return JsonConvert.SerializeObject(o);
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
    }
}

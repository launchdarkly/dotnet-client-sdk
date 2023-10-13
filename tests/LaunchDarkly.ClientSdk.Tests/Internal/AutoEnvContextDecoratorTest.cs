using System.Globalization;
using LaunchDarkly.Sdk.Client.Internal.DataStores;
using LaunchDarkly.Sdk.Client.Subsystems;
using LaunchDarkly.Sdk.EnvReporting;
using Xunit;

namespace LaunchDarkly.Sdk.Client.Internal
{
    public class AutoEnvContextDecoratorTest : BaseTest
    {
        [Fact]
        public void AdheresToSchemaTest()
        {
            var envReporter = new EnvironmentReporterBuilder().SetSdkLayer(SdkAttributes.Layer).Build();
            var store = MakeMockDataStoreWrapper();
            var decoratorUnderTest = MakeDecoratorWithPersistence(store, envReporter);

            var input = Context.Builder("aKey").Kind(ContextKind.Of("aKind"))
                .Set("dontOverwriteMeBro", "really bro").Build();
            var output = decoratorUnderTest.DecorateContext(input);

            // Create the expected context after the code runs
            // because there will be persistence side effects
            var applicationKind = ContextKind.Of(AutoEnvContextDecorator.LD_APPLICATION_KIND);
            var expectedApplicationKey = Base64.UrlSafeSha256Hash(envReporter.ApplicationInfo?.ApplicationId ?? "");
            var expectedAppContext = Context.Builder(applicationKind, expectedApplicationKey)
                .Set(AutoEnvContextDecorator.ENV_ATTRIBUTES_VERSION, AutoEnvContextDecorator.SPEC_VERSION)
                .Set(AutoEnvContextDecorator.ATTR_ID, SdkPackage.Name)
                .Set(AutoEnvContextDecorator.ATTR_NAME, SdkPackage.Name)
                .Set(AutoEnvContextDecorator.ATTR_VERSION, SdkPackage.Version)
                .Set(AutoEnvContextDecorator.ATTR_VERSION_NAME, SdkPackage.Version)
                .Build();

            // TODO: We should include ld_device in these tests.  I think that may require a way to mock the platform
            // layer or run on an actual platform that supports getting device information such as Android.

            var expectedOutput = Context.MultiBuilder().Add(input).Add(expectedAppContext)
                .Build();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void CustomCultureInPlatformLayerIsPropagated()
        {
            var platform = new Layer(null, null, null, "en-GB");

            var envReporter = new EnvironmentReporterBuilder().SetPlatformLayer(platform).Build();
            var store = MakeMockDataStoreWrapper();
            var decoratorUnderTest = MakeDecoratorWithPersistence(store, envReporter);

            var input = Context.Builder("aKey").Kind(ContextKind.Of("aKind")).Build();
            var output = decoratorUnderTest.DecorateContext(input);


            Context outContext;
            Assert.True(output.TryGetContextByKind(new ContextKind(AutoEnvContextDecorator.LD_APPLICATION_KIND), out outContext));

            Assert.Equal("en-GB", outContext.GetValue("locale").AsString);
        }

        [Fact]
        public void DoesNotOverwriteCustomerDataTest()
        {
            var envReporter = new EnvironmentReporterBuilder().SetSdkLayer(SdkAttributes.Layer).Build();
            var store = MakeMockDataStoreWrapper();
            var decoratorUnderTest = MakeDecoratorWithPersistence(store, envReporter);

            var input = Context.Builder(ContextKind.Of("ld_application"), "aKey")
                .Set("dontOverwriteMeBro", "really bro").Build();
            var output = decoratorUnderTest.DecorateContext(input);

            // TODO: We should include ld_device in these tests.  I think that may require a way to mock the platform
            // layer or run on an actual platform that supports getting device information such as Android.

            var expectedOutput = Context.MultiBuilder().Add(input).Build();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void DoesNotOverwriteCustomerDataMultiContextTest()
        {
            var envReporter = new EnvironmentReporterBuilder().SetSdkLayer(SdkAttributes.Layer).Build();
            var store = MakeMockDataStoreWrapper();
            var decoratorUnderTest = MakeDecoratorWithPersistence(store, envReporter);

            var input1 = Context.Builder(ContextKind.Of("ld_application"), "aKey")
                .Set("dontOverwriteMeBro", "really bro").Build();
            var input2 = Context.Builder(ContextKind.Of("ld_device"), "anotherKey")
                .Set("AndDontOverwriteThisEither", "bro").Build();
            var multiContextInput = Context.MultiBuilder().Add(input1).Add(input2).Build();
            var output = decoratorUnderTest.DecorateContext(multiContextInput);

            // input and output should be the same
            Assert.Equal(multiContextInput, output);
        }

        [Fact]
        public void GeneratesConsistentKeysAcrossMultipleCalls()
        {
            var envReporter = new EnvironmentReporterBuilder().SetSdkLayer(SdkAttributes.Layer).Build();
            var store = MakeMockDataStoreWrapper();
            var decoratorUnderTest = MakeDecoratorWithPersistence(store, envReporter);

            var input = Context.Builder(ContextKind.Of("aKind"), "aKey")
                .Set("dontOverwriteMeBro", "really bro").Build();

            var output1 = decoratorUnderTest.DecorateContext(input);
            output1.TryGetContextByKind(ContextKind.Of("ld_application"), out var appContext1);
            var key1 = appContext1.Key;

            var output2 = decoratorUnderTest.DecorateContext(input);
            output2.TryGetContextByKind(ContextKind.Of("ld_application"), out var appContext2);
            var key2 = appContext2.Key;

            Assert.Equal(key1, key2);
        }

        private PersistentDataStoreWrapper MakeMockDataStoreWrapper()
        {
            return new PersistentDataStoreWrapper(new MockPersistentDataStore(), BasicMobileKey, testLogger);
        }

        private AutoEnvContextDecorator MakeDecoratorWithPersistence(PersistentDataStoreWrapper store,
            IEnvironmentReporter reporter)
        {
            return new AutoEnvContextDecorator(store, reporter, testLogger);
        }
    }
}

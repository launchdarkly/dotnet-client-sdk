using System.Linq;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Internal.DataStores;
using LaunchDarkly.Sdk.Client.Subsystems;
using LaunchDarkly.Sdk.EnvReporting;
using Xunit;

namespace LaunchDarkly.Sdk.Client.Internal
{
    public class AutoEnvContextDecoratorTest : BaseTest
    {
        // private static readonly ContextKind Kind1 = ContextKind.Of("kind1");
        // private static readonly ContextKind Kind2 = ContextKind.Of("kind2");
        //
        // [Fact]
        // public void SingleKindNonAnonymousContextIsUnchanged()
        // {
        //     var context = Context.Builder("key1").Name("name").Build();
        //
        //     AssertHelpers.ContextsEqual(context,
        //         MakeDecoratorWithoutPersistence().DecorateContext(context));
        // }
        //
        // [Fact]
        // public void SingleKindAnonymousContextIsUnchangedIfConfigOptionIsNotSet()
        // {
        //     var context = Context.Builder("key1").Anonymous(true).Name("name").Build();
        //
        //     AssertHelpers.ContextsEqual(context,
        //         MakeDecoratorWithoutPersistence().DecorateContext(context));
        // }
        //
        // [Fact]
        // public void SingleKindAnonymousContextGetsGeneratedKeyIfConfigOptionIsSet()
        // {
        //     var context = TestUtil.BuildAutoContext().Name("name").Build();
        //
        //     var transformed = MakeDecoratorWithoutPersistence(true).DecorateContext(context);
        //
        //     AssertContextHasBeenTransformedWithNewKey(context, transformed);
        // }
        //
        // [Fact]
        // public void MultiKindContextIsUnchangedIfNoIndividualContextsNeedGeneratedKey()
        // {
        //     var c1 = Context.Builder("key1").Kind(Kind1).Name("name1").Build();
        //     var c2 = Context.Builder("key2").Kind(Kind2).Anonymous(true).Name("name2").Build();
        //
        //     var multiContext = Context.NewMulti(c1, c2);
        //
        //     AssertHelpers.ContextsEqual(multiContext,
        //         MakeDecoratorWithoutPersistence().DecorateContext(multiContext));
        // }
        //
        // [Fact]
        // public void MultiKindContextGetsGeneratedKeyForIndividualContext()
        // {
        //     var c1 = Context.Builder("key1").Kind(Kind1).Name("name1").Build();
        //     var c2 = TestUtil.BuildAutoContext().Kind(Kind2).Anonymous(true).Name("name2").Build();
        //     var multiContext = Context.NewMulti(c1, c2);
        //     var transformedMulti = MakeDecoratorWithoutPersistence(true).DecorateContext(multiContext);
        //
        //     Assert.Equal(multiContext.MultiKindContexts.Select(c => c.Kind).ToList(),
        //         transformedMulti.MultiKindContexts.Select(c => c.Kind).ToList());
        //
        //     transformedMulti.TryGetContextByKind(c1.Kind, out var c1Transformed);
        //     AssertHelpers.ContextsEqual(c1, c1Transformed);
        //
        //     transformedMulti.TryGetContextByKind(c2.Kind, out var c2Transformed);
        //     AssertContextHasBeenTransformedWithNewKey(c2, c2Transformed);
        //
        //     AssertHelpers.ContextsEqual(Context.NewMulti(c1, c2Transformed), transformedMulti);
        // }
        //
        // [Fact]
        // public void MultiKindContextGetsSeparateGeneratedKeyForEachKind()
        // {
        //     var c1 = TestUtil.BuildAutoContext().Kind(Kind1).Anonymous(true).Name("name1").Build();
        //     var c2 = TestUtil.BuildAutoContext().Kind(Kind2).Anonymous(true).Name("name2").Build();
        //     var multiContext = Context.NewMulti(c1, c2);
        //     var transformedMulti = MakeDecoratorWithoutPersistence(true).DecorateContext(multiContext);
        //
        //     Assert.Equal(multiContext.MultiKindContexts.Select(c => c.Kind).ToList(),
        //         transformedMulti.MultiKindContexts.Select(c => c.Kind).ToList());
        //
        //     transformedMulti.TryGetContextByKind(c1.Kind, out var c1Transformed);
        //     AssertContextHasBeenTransformedWithNewKey(c1, c1Transformed);
        //
        //     transformedMulti.TryGetContextByKind(c2.Kind, out var c2Transformed);
        //     AssertContextHasBeenTransformedWithNewKey(c2, c2Transformed);
        //
        //     Assert.NotEqual(c1Transformed.Key, c2Transformed.Key);
        //
        //     AssertHelpers.ContextsEqual(Context.NewMulti(c1Transformed, c2Transformed), transformedMulti);
        // }
        //
        // [Fact]
        // public void GeneratedKeysPersistPerKindIfPersistentStorageIsEnabled()
        // {
        //     var c1 = TestUtil.BuildAutoContext().Kind(Kind1).Anonymous(true).Name("name1").Build();
        //     var c2 = TestUtil.BuildAutoContext().Kind(Kind2).Anonymous(true).Name("name2").Build();
        //     var multiContext = Context.NewMulti(c1, c2);
        //
        //     var store = new MockPersistentDataStore();
        //
        //     var decorator1 = MakeDecoratorWithPersistence(store, true);
        //
        //     var transformedMultiA = decorator1.DecorateContext(multiContext);
        //     transformedMultiA.TryGetContextByKind(c1.Kind, out var c1Transformed);
        //     AssertContextHasBeenTransformedWithNewKey(c1, c1Transformed);
        //     transformedMultiA.TryGetContextByKind(c2.Kind, out var c2Transformed);
        //     AssertContextHasBeenTransformedWithNewKey(c2, c2Transformed);
        //
        //     var decorator2 = MakeDecoratorWithPersistence(store, true);
        //
        //     var transformedMultiB = decorator2.DecorateContext(multiContext);
        //     AssertHelpers.ContextsEqual(transformedMultiA, transformedMultiB);
        // }
        //
        // [Fact]
        // public void GeneratedKeysAreReusedDuringLifetimeOfSdkEvenIfPersistentStorageIsDisabled()
        // {
        //     var c1 = TestUtil.BuildAutoContext().Kind(Kind1).Anonymous(true).Name("name1").Build();
        //     var c2 = TestUtil.BuildAutoContext().Kind(Kind2).Anonymous(true).Name("name2").Build();
        //     var multiContext = Context.NewMulti(c1, c2);
        //
        //     var store = new MockPersistentDataStore();
        //
        //     var decorator = MakeDecoratorWithoutPersistence(true);
        //
        //     var transformedMultiA = decorator.DecorateContext(multiContext);
        //     transformedMultiA.TryGetContextByKind(c1.Kind, out var c1Transformed);
        //     AssertContextHasBeenTransformedWithNewKey(c1, c1Transformed);
        //     transformedMultiA.TryGetContextByKind(c2.Kind, out var c2Transformed);
        //     AssertContextHasBeenTransformedWithNewKey(c2, c2Transformed);
        //
        //     var transformedMultiB = decorator.DecorateContext(multiContext);
        //     AssertHelpers.ContextsEqual(transformedMultiA, transformedMultiB);
        // }
        //
        // [Fact]
        // public void GeneratedKeysAreNotReusedAcrossRestartsIfPersistentStorageIsDisabled()
        // {
        //     var c1 = TestUtil.BuildAutoContext().Kind(Kind1).Anonymous(true).Name("name1").Build();
        //     var c2 = TestUtil.BuildAutoContext().Kind(Kind2).Anonymous(true).Name("name2").Build();
        //     var multiContext = Context.NewMulti(c1, c2);
        //
        //     var decorator1 = MakeDecoratorWithoutPersistence(true);
        //
        //     var transformedMultiA = decorator1.DecorateContext(multiContext);
        //     transformedMultiA.TryGetContextByKind(c1.Kind, out var c1TransformedA);
        //     AssertContextHasBeenTransformedWithNewKey(c1, c1TransformedA);
        //     transformedMultiA.TryGetContextByKind(c2.Kind, out var c2TransformedA);
        //     AssertContextHasBeenTransformedWithNewKey(c2, c2TransformedA);
        //
        //     var decorator2 = MakeDecoratorWithoutPersistence(true);
        //
        //     var transformedMultiB = decorator2.DecorateContext(multiContext);
        //     transformedMultiB.TryGetContextByKind(c1.Kind, out var c1TransformedB);
        //     AssertContextHasBeenTransformedWithNewKey(c1, c1TransformedB);
        //     Assert.NotEqual(c1TransformedA.Key, c1TransformedB.Key);
        //     transformedMultiB.TryGetContextByKind(c2.Kind, out var c2TransformedB);
        //     AssertContextHasBeenTransformedWithNewKey(c2, c2TransformedB);
        //     Assert.NotEqual(c2TransformedA.Key, c2TransformedB.Key);
        // }
        //
        // private AutoEnvContextDecorator MakeDecoratorWithPersistence(IPersistentDataStore store)
        // {
        //     var environmentReporter = new EnvironmentReporterBuilder().Build();
        //     return new AutoEnvContextDecorator(new PersistentDataStoreWrapper(store, BasicMobileKey, testLogger), environmentReporter, testLogger);
        // }
        //
        // private AutoEnvContextDecorator MakeDecoratorWithoutPersistence() =>
        //     MakeDecoratorWithPersistence(new NullPersistentDataStore());
        //
        // private void AssertContextHasBeenTransformedWithNewKey(Context original, Context transformed)
        // {
        //     Assert.NotEqual(original.Key, transformed.Key);
        //     AssertHelpers.ContextsEqual(Context.BuilderFromContext(original).Key(transformed.Key).Build(),
        //         transformed);
        // }
        //
        // public void MultiKindContextIsUnchangedIfNoIndividualContextsNeedGeneratedKey()
        // {
        //     var c1 = Context.Builder("key1").Kind(Kind1).Name("name1").Build();
        //     var c2 = Context.Builder("key2").Kind(Kind2).Anonymous(true).Name("name2").Build();
        //
        //     var multiContext = Context.NewMulti(c1, c2);
        //
        //     AssertHelpers.ContextsEqual(multiContext,
        //         MakeDecoratorWithoutPersistence().DecorateContext(multiContext));
        // }
        //
        // [Fact]
        // public void AdheresToSchemaTest()
        // {
        //     var store = new MockPersistentDataStore();
        //     var decoratorUnderTest = MakeDecoratorWithPersistence(store);
        //
        //     Context input = Context.Builder("aKey").Kind(ContextKind.Of("aKind"))
        //         .Set("dontOverwriteMeBro", "really bro").Build();
        //     LDContext output = underTest.ModifyContext(input);
        //
        //     // Create the expected context after the code runs
        //     // because there will be persistence side effects
        //     ContextKind applicationKind = ContextKind.Of(AutoEnvContextDecorator.LD_APPLICATION_KIND);
        //     string expectedApplicationKey = LDUtil.UrlSafeBase64Hash(reporter.GetApplicationInfo().GetApplicationId());
        //     LDContext expectedAppContext = LDContext.Builder(applicationKind, expectedApplicationKey)
        //         .Set(AutoEnvContextDecorator.ENV_ATTRIBUTES_VERSION, AutoEnvContextDecorator.SPEC_VERSION)
        //         .Set(AutoEnvContextDecorator.ATTR_ID, LDPackageConsts.SDK_NAME)
        //         .Set(AutoEnvContextDecorator.ATTR_NAME, LDPackageConsts.SDK_NAME)
        //         .Set(AutoEnvContextDecorator.ATTR_VERSION, BuildConfig.VERSION_NAME)
        //         .Set(AutoEnvContextDecorator.ATTR_VERSION_NAME, BuildConfig.VERSION_NAME)
        //         .Set(AutoEnvContextDecorator.ATTR_LOCALE, "unknown")
        //         .Build();
        //
        //     ContextKind deviceKind = ContextKind.Of(AutoEnvContextDecorator.LD_DEVICE_KIND);
        //     LDContext expectedDeviceContext = LDContext.Builder(deviceKind, wrapper.GetOrGenerateContextKey(deviceKind))
        //         .Set(AutoEnvContextDecorator.ENV_ATTRIBUTES_VERSION, AutoEnvContextDecorator.SPEC_VERSION)
        //         .Set(AutoEnvContextDecorator.ATTR_MANUFACTURER, "unknown")
        //         .Set(AutoEnvContextDecorator.ATTR_MODEL, "unknown")
        //         .Set(AutoEnvContextDecorator.ATTR_OS, new LdValue.ObjectBuilder()
        //             .Put(AutoEnvContextDecorator.ATTR_FAMILY, "unknown")
        //             .Put(AutoEnvContextDecorator.ATTR_NAME, "unknown")
        //             .Put(AutoEnvContextDecorator.ATTR_VERSION, "unknown")
        //             .Build())
        //         .Build();
        //
        //     LDContext expectedOutput = LDContext.MultiBuilder().Add(input).Add(expectedAppContext).Add(expectedDeviceContext).Build();
        //
        //     Assert.AreEqual(expectedOutput, output);
        // }

    }
}


    //     /**
    //  * Requirement 1.2.2.1 - Schema adherence
    //  * Requirement 1.2.2.3 - Adding all attributes
    //  * Requirement 1.2.2.5 - Schema version in _meta
    //  * Requirement 1.2.2.7 - Adding all context kinds
    //  */
    // @Test
    // public void adheresToSchemaTest() {
    //     PersistentDataStoreWrapper wrapper = TestUtil.makeSimplePersistentDataStoreWrapper();
    //     IEnvironmentReporter reporter = new EnvironmentReporterBuilder().build();
    //     AutoEnvContextModifier underTest = new AutoEnvContextModifier(
    //             wrapper,
    //             reporter,
    //             LDLogger.none()
    //     );
    //
    //     LDContext input = LDContext.builder(ContextKind.of("aKind"), "aKey")
    //             .set("dontOverwriteMeBro", "really bro").build();
    //     LDContext output = underTest.modifyContext(input);
    //
    //     // it is important that we create this expected context after the code runs because there
    //     // will be persistence side effects
    //     ContextKind applicationKind = ContextKind.of(AutoEnvContextModifier.LD_APPLICATION_KIND);
    //     String expectedApplicationKey = LDUtil.urlSafeBase64Hash(reporter.getApplicationInfo().getApplicationId());
    //     LDContext expectedAppContext = LDContext.builder(applicationKind, expectedApplicationKey)
    //             .set(AutoEnvContextModifier.ENV_ATTRIBUTES_VERSION, AutoEnvContextModifier.SPEC_VERSION)
    //             .set(AutoEnvContextModifier.ATTR_ID, LDPackageConsts.SDK_NAME)
    //             .set(AutoEnvContextModifier.ATTR_NAME, LDPackageConsts.SDK_NAME)
    //             .set(AutoEnvContextModifier.ATTR_VERSION, BuildConfig.VERSION_NAME)
    //             .set(AutoEnvContextModifier.ATTR_VERSION_NAME, BuildConfig.VERSION_NAME)
    //             .set(AutoEnvContextModifier.ATTR_LOCALE, "unknown")
    //             .build();
    //
    //     ContextKind deviceKind = ContextKind.of(AutoEnvContextModifier.LD_DEVICE_KIND);
    //     LDContext expectedDeviceContext = LDContext.builder(deviceKind, wrapper.getOrGenerateContextKey(deviceKind))
    //             .set(AutoEnvContextModifier.ENV_ATTRIBUTES_VERSION, AutoEnvContextModifier.SPEC_VERSION)
    //             .set(AutoEnvContextModifier.ATTR_MANUFACTURER, "unknown")
    //             .set(AutoEnvContextModifier.ATTR_MODEL, "unknown")
    //             .set(AutoEnvContextModifier.ATTR_OS, new ObjectBuilder()
    //                     .put(AutoEnvContextModifier.ATTR_FAMILY, "unknown")
    //                     .put(AutoEnvContextModifier.ATTR_NAME, "unknown")
    //                     .put(AutoEnvContextModifier.ATTR_VERSION, "unknown")
    //                     .build())
    //             .build();
    //
    //     LDContext expectedOutput = LDContext.multiBuilder().add(input).add(expectedAppContext).add(expectedDeviceContext).build();
    //
    //     Assert.assertEquals(expectedOutput, output);
    // }
    //
    // /**
    //  *  Requirement 1.2.2.6 - Don't add kind if already exists
    //  *  Requirement 1.2.5.1 - Doesn't change customer provided data
    //  *  Requirement 1.2.7.1 - Log warning when kind already exists
    //  */
    // @Test
    // public void doesNotOverwriteCustomerDataTest() {
    //
    //     PersistentDataStoreWrapper wrapper = TestUtil.makeSimplePersistentDataStoreWrapper();
    //     AutoEnvContextModifier underTest = new AutoEnvContextModifier(
    //             wrapper,
    //             new EnvironmentReporterBuilder().build(),
    //             logging.logger
    //     );
    //
    //     LDContext input = LDContext.builder(ContextKind.of("ld_application"), "aKey")
    //             .set("dontOverwriteMeBro", "really bro").build();
    //     LDContext output = underTest.modifyContext(input);
    //
    //     // it is important that we create this expected context after the code runs because there
    //     // will be persistence side effects
    //     ContextKind deviceKind = ContextKind.of(AutoEnvContextModifier.LD_DEVICE_KIND);
    //     LDContext expectedDeviceContext = LDContext.builder(deviceKind, wrapper.getOrGenerateContextKey(deviceKind))
    //             .set(AutoEnvContextModifier.ENV_ATTRIBUTES_VERSION, AutoEnvContextModifier.SPEC_VERSION)
    //             .set(AutoEnvContextModifier.ATTR_MANUFACTURER, "unknown")
    //             .set(AutoEnvContextModifier.ATTR_MODEL, "unknown")
    //             .set(AutoEnvContextModifier.ATTR_OS, new ObjectBuilder()
    //                     .put(AutoEnvContextModifier.ATTR_FAMILY, "unknown")
    //                     .put(AutoEnvContextModifier.ATTR_NAME, "unknown")
    //                     .put(AutoEnvContextModifier.ATTR_VERSION, "unknown")
    //                     .build())
    //             .build();
    //
    //     LDContext expectedOutput = LDContext.multiBuilder().add(input).add(expectedDeviceContext).build();
    //
    //     Assert.assertEquals(expectedOutput, output);
    //     logging.assertWarnLogged("Unable to automatically add environment attributes for " +
    //             "kind:ld_application. ld_application already exists.");
    // }
    //
    // /**
    //  *  Requirement 1.2.5.1 - Doesn't change customer provided data
    //  */
    // @Test
    // public void doesNotOverwriteCustomerDataMultiContextTest() {
    //
    //     PersistentDataStoreWrapper wrapper = TestUtil.makeSimplePersistentDataStoreWrapper();
    //     AutoEnvContextModifier underTest = new AutoEnvContextModifier(
    //             wrapper,
    //             new EnvironmentReporterBuilder().build(),
    //             LDLogger.none()
    //     );
    //
    //     LDContext input1 = LDContext.builder(ContextKind.of("ld_application"), "aKey")
    //             .set("dontOverwriteMeBro", "really bro").build();
    //     LDContext input2 = LDContext.builder(ContextKind.of("ld_device"), "anotherKey")
    //             .set("AndDontOverwriteThisEither", "bro").build();
    //     LDContext multiContextInput = LDContext.multiBuilder().add(input1).add(input2).build();
    //     LDContext output = underTest.modifyContext(multiContextInput);
    //
    //     // input and output should be the same
    //     Assert.assertEquals(multiContextInput, output);
    // }
    //
    // /**
    //  * Requirement 1.2.6.3 - Generated keys are consistent
    //  */
    // @Test
    // public void generatesConsistentKeysAcrossMultipleCalls() {
    //     PersistentDataStoreWrapper wrapper = TestUtil.makeSimplePersistentDataStoreWrapper();
    //     AutoEnvContextModifier underTest = new AutoEnvContextModifier(
    //             wrapper,
    //             new EnvironmentReporterBuilder().build(),
    //             LDLogger.none()
    //     );
    //
    //     LDContext input = LDContext.builder(ContextKind.of("aKind"), "aKey")
    //             .set("dontOverwriteMeBro", "really bro").build();
    //
    //     LDContext output1 = underTest.modifyContext(input);
    //     String key1 = output1.getIndividualContext("ld_application").getKey();
    //
    //     LDContext output2 = underTest.modifyContext(input);
    //     String key2 = output2.getIndividualContext("ld_application").getKey();
    //
    //     Assert.assertEquals(key1, key2);
    // }
    //
    // /**
    //  * Test that when only myID is supplied, hash is hash(myID:) and not hash(myId:null)
    //  */
    // @Test
    // public void generatedApplicationKeyWithVersionMissing() {
    //     PersistentDataStoreWrapper wrapper = TestUtil.makeSimplePersistentDataStoreWrapper();
    //     ApplicationInfo info = new ApplicationInfo("myID", null, null, null);
    //     EnvironmentReporterBuilder b = new EnvironmentReporterBuilder();
    //     b.setApplicationInfo(info);
    //     IEnvironmentReporter reporter = b.build();
    //     AutoEnvContextModifier underTest = new AutoEnvContextModifier(
    //             wrapper,
    //             reporter,
    //             LDLogger.none()
    //     );
    //
    //     LDContext input = LDContext.builder(ContextKind.of("aKind"), "aKey").build();
    //     LDContext output = underTest.modifyContext(input);
    //
    //     // it is important that we create this expected context after the code runs because there
    //     // will be persistence side effects
    //     ContextKind applicationKind = ContextKind.of(AutoEnvContextModifier.LD_APPLICATION_KIND);
    //     String expectedApplicationKey = LDUtil.urlSafeBase64Hash(reporter.getApplicationInfo().getApplicationId());
    //
    //     LDContext expectedAppContext = LDContext.builder(applicationKind, expectedApplicationKey)
    //             .set(AutoEnvContextModifier.ENV_ATTRIBUTES_VERSION, AutoEnvContextModifier.SPEC_VERSION)
    //             .set(AutoEnvContextModifier.ATTR_ID, "myID")
    //             .set(AutoEnvContextModifier.ATTR_LOCALE, "unknown")
    //             .build();
    //
    //     ContextKind deviceKind = ContextKind.of(AutoEnvContextModifier.LD_DEVICE_KIND);
    //     LDContext expectedDeviceContext = LDContext.builder(deviceKind, wrapper.getOrGenerateContextKey(deviceKind))
    //             .set(AutoEnvContextModifier.ENV_ATTRIBUTES_VERSION, AutoEnvContextModifier.SPEC_VERSION)
    //             .set(AutoEnvContextModifier.ATTR_MANUFACTURER, "unknown")
    //             .set(AutoEnvContextModifier.ATTR_MODEL, "unknown")
    //             .set(AutoEnvContextModifier.ATTR_OS, new ObjectBuilder()
    //                     .put(AutoEnvContextModifier.ATTR_FAMILY, "unknown")
    //                     .put(AutoEnvContextModifier.ATTR_NAME, "unknown")
    //                     .put(AutoEnvContextModifier.ATTR_VERSION, "unknown")
    //                     .build())
    //             .build();
    //
    //     LDContext expectedOutput = LDContext.multiBuilder().add(input).add(expectedAppContext).add(expectedDeviceContext).build();
    //     Assert.assertEquals(expectedOutput, output);
    // }

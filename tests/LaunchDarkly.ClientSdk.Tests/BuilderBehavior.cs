using System;
using Xunit;

// THIS CODE WILL BE MOVED into the dotnet-test-helpers project where it can be shared

namespace LaunchDarkly.TestHelpers
{
    /// <summary>
    /// Factories for helper classes that provide useful test patterns for builder types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These helpers make it easier to provide thorough test coverage of builder types.
    /// It is easy when implementing builders to make basic mistakes like not setting the
    /// right property in a setter, not enforcing desired constraints, or not copying all
    /// the properties in a copy constructor.
    /// </para>
    /// <para>
    /// The general pattern consists of creating a generic helper for the builder type and
    /// the type that it builds, then creating a property helper for each settable property
    /// and performing standard assertions with it. Example:
    /// </para>
    /// <pre><code>
    ///     // This assumes there is a type MyBuilder whose Build method creates an
    ///     // instance of MyType, with properties Height and Weight.
    ///     
    ///     var tester = BuilderBehavior.For(() => new MyBuilder(), b => b.Build());
    ///
    ///     var height = tester.Property(x => x.Height, b => (b, value) => b.Height(value));
    ///     height.AssertDefault(DefaultHeight);
    ///     height.AssertCanSet(72);
    ///     height.AssertSetIsChangedTo(-1, 0); // setter should enforce minimum height of 0
    ///
    ///     var weight = tester.Property(x => x.Weight, b => (b, value) => b.Weight(value));
    ///     weight.AssertDefault(DefaultWeight);
    ///     weight.AssertCanSet(200);
    ///     weight.AssertSetIsChangedTo(-1, 0); // setter should enforce minimum weight of 0
    /// </code></pre>
    /// <para>
    /// It uses Xunit assertion methods.
    /// </para>
    /// </remarks>
    public static class BuilderBehavior
    {
        /// <summary>
        /// Provides a generic <see cref="BuildTester{TBuilder, TBuilt}"/> for testing
        /// methods of a builder against properties of the type it builds.
        /// </summary>
        /// <typeparam name="TBuilder">the builder type</typeparam>
        /// <typeparam name="TBuilt">the type that it builds</typeparam>
        /// <param name="constructor">function that constructs a <typeparamref name="TBuilder"/></param>
        /// <param name="buildMethod">function that creates a <typeparamref name="TBuilt"/>
        ///   from a <typeparamref name="TBuilder"/></param>
        /// <returns>a <see cref="BuilderBehavior.BuildTester{TBuilder, TBuilt}"/> instance</returns>
        public static BuildTester<TBuilder, TBuilt> For<TBuilder, TBuilt>(
            Func<TBuilder> constructor, Func<TBuilder, TBuilt> buildMethod) =>
            new BuildTester<TBuilder, TBuilt>(constructor, buildMethod, null);

        /// <summary>
        /// Provides a generic <see cref="InternalStateTester{TBuilder}"/> for testing
        /// methods of a builder against the builder's own internal state.
        /// </summary>
        /// <remarks>
        /// This can be used in cases where it is not feasible for the test code to actually
        /// call the builder's build method, for instance if it has unwanted side effects.
        /// </remarks>
        /// <typeparam name="TBuilder">the builder type</typeparam>
        /// <param name="constructor">function that constructs a <typeparamref name="TBuilder"/></param>
        /// <returns>an <see cref="InternalStateTester{TBuilder}"/> instance</returns>
        // Use this when we want to test the builder's internal state directly, without
        // calling Build - i.e. if the object is difficult to inspect after it's built.
        public static InternalStateTester<TBuilder> For<TBuilder>(Func<TBuilder> constructor) =>
            new InternalStateTester<TBuilder>(constructor);

        /// <summary>
        /// Helper class that provides useful test patterns for a builder type and the
        /// type that it builds.
        /// </summary>
        /// <remarks>
        /// Create instances of this class with
        /// <see cref="BuilderBehavior.For{TBuilder, TBuilt}(Func{TBuilder}, Func{TBuilder, TBuilt})"/>.
        /// </remarks>
        /// <typeparam name="TBuilder">the builder type</typeparam>
        /// <typeparam name="TBuilt">the type that it builds</typeparam>
        public sealed class BuildTester<TBuilder, TBuilt>
        {
            private readonly Func<TBuilder> _constructor;
            internal readonly Func<TBuilder, TBuilt> _buildMethod;
            internal readonly Func<TBuilt, TBuilder> _copyConstructor;

            internal BuildTester(Func<TBuilder> constructor,
                Func<TBuilder, TBuilt> buildMethod,
                Func<TBuilt, TBuilder> copyConstructor
                )
            {
                _constructor = constructor;
                _buildMethod = buildMethod;
                _copyConstructor = copyConstructor;
            }

            /// <summary>
            /// Creates a helper for testing a specific property of the builder.
            /// </summary>
            /// <typeparam name="TValue">type of the property</typeparam>
            /// <param name="getter">function that gets that property from the built object</param>
            /// <param name="builderSetter">function that sets the property in the builder</param>
            /// <returns></returns>
            public IPropertyAssertions<TValue> Property<TValue>(
                Func<TBuilt, TValue> getter,
                Action<TBuilder, TValue> builderSetter
                ) =>
            new BuildTesterProperty<TBuilder, TBuilt, TValue>(
                this, getter, builderSetter);

            /// <summary>
            /// Creates an instance of the builder.
            /// </summary>
            /// <returns>a new instance</returns>
            public TBuilder New() => _constructor();

            /// <summary>
            /// Adds the ability to test the builder's copy constructor.
            /// </summary>
            /// <remarks>
            /// The effect of this is that all <see cref="Property{TValue}(Func{TBuilt, TValue}, Action{TBuilder, TValue})"/>
            /// assertions created from the resulting helper will also verify that copying
            /// the builder also copies the value of this property.
            /// </remarks>
            /// <param name="copyConstructor">function that should create a new builder with an
            ///   identical state to the existing one</param>
            /// <returns>a copy of the <c>BuilderTestHelper</c> with this additional behavior</returns>
            public BuildTester<TBuilder, TBuilt> WithCopyConstructor(
                Func<TBuilt, TBuilder> copyConstructor
                ) =>
                new BuildTester<TBuilder, TBuilt>(_constructor, _buildMethod, copyConstructor);
        }

        /// <summary>
        /// Similar to <see cref="BuildTester{TBuilder, TBuilt}"/>, but instead of testing the values of
        /// properties in the built object, it inspects the builder directly.
        /// </summary>
        /// <remarks>
        /// Create instances of this class with <see cref="BuilderBehavior.For{TBuilder}(Func{TBuilder})"/>.
        /// </remarks>
        /// <typeparam name="TBuilder">the builder type</typeparam>
        public class InternalStateTester<TBuilder>
        {
            private readonly Func<TBuilder> _constructor;

            internal InternalStateTester(Func<TBuilder> constructor)
            {
                _constructor = constructor;
            }

            /// <summary>
            /// Creates a helper for testing a specific property of the builder.
            /// </summary>
            /// <typeparam name="TValue">type of the property</typeparam>
            /// <param name="builderGetter">function that gets that property from the builder's internal state</param>
            /// <param name="builderSetter">function that sets the property in the builder</param>
            /// <returns></returns>
            public IPropertyAssertions<TValue> Property<TValue>(
                Func<TBuilder, TValue> builderGetter,
                Action<TBuilder, TValue> builderSetter
                ) =>
                new InternalStateTesterProperty<TBuilder, TValue>(this,
                    builderGetter, builderSetter);

            /// <summary>
            /// Creates an instance of the builder.
            /// </summary>
            /// <returns>a new instance</returns>
            public TBuilder New() => _constructor();
        }

        /// <summary>
        /// Assertions provided by the property-specific helpers.
        /// </summary>
        /// <typeparam name="TValue">type of the property</typeparam>
        public interface IPropertyAssertions<TValue>
        {
            /// <summary>
            /// Asserts that the property has the expected value when it has not been set.
            /// </summary>
            /// <param name="defaultValue">the expected value</param>
            void AssertDefault(TValue defaultValue);

            /// <summary>
            /// Asserts that calling the setter for a specific value causes the property
            /// to have that value.
            /// </summary>
            /// <param name="newValue">the expected value</param>
            void AssertCanSet(TValue newValue);

            /// <summary>
            /// Asserts that calling the setter for a specific value causes the property
            /// to have another specific value for the corresponding property.
            /// </summary>
            /// <param name="attemptedValue">the value to pass to the setter</param>
            /// <param name="resultingValue">the expected result value</param>
            void AssertSetIsChangedTo(TValue attemptedValue, TValue resultingValue);
        }

        internal class BuildTesterProperty<TBuilder, TBuilt, TValue> : IPropertyAssertions<TValue>
        {
            private readonly BuildTester<TBuilder, TBuilt> _owner;
            private readonly Func<TBuilt, TValue> _getter;
            private readonly Action<TBuilder, TValue> _builderSetter;

            internal BuildTesterProperty(BuildTester<TBuilder, TBuilt> owner,
                Func<TBuilt, TValue> getter,
                Action<TBuilder, TValue> builderSetter)
            {
                _owner = owner;
                _getter = getter;
                _builderSetter = builderSetter;
            }

            public void AssertDefault(TValue defaultValue)
            {
                var b = _owner.New();
                AssertValue(b, defaultValue);
            }

            public void AssertCanSet(TValue newValue)
            {
                AssertSetIsChangedTo(newValue, newValue);
            }

            public void AssertSetIsChangedTo(TValue attemptedValue, TValue resultingValue)
            {
                var b = _owner.New();
                _builderSetter(b, attemptedValue);
                AssertValue(b, resultingValue);
            }

            private void AssertValue(TBuilder b, TValue v)
            {
                var o = _owner._buildMethod(b);
                Assert.Equal(v, _getter(o));
                if (_owner._copyConstructor != null)
                {
                    var b1 = _owner._copyConstructor(o);
                    var o1 = _owner._buildMethod(b1);
                    Assert.Equal(v, _getter(o1));
                }
            }
        }

        internal class InternalStateTesterProperty<TBuilder, TValue> : IPropertyAssertions<TValue>
        {
            private readonly InternalStateTester<TBuilder> _owner;
            private readonly Func<TBuilder, TValue> _builderGetter;
            private readonly Action<TBuilder, TValue> _builderSetter;

            internal InternalStateTesterProperty(InternalStateTester<TBuilder> owner,
                Func<TBuilder, TValue> builderGetter,
                Action<TBuilder, TValue> builderSetter)
            {
                _owner = owner;
                _builderGetter = builderGetter;
                _builderSetter = builderSetter;
            }

            public void AssertDefault(TValue defaultValue)
            {
                Assert.Equal(defaultValue, _builderGetter(_owner.New()));
            }

            public void AssertCanSet(TValue newValue)
            {
                AssertSetIsChangedTo(newValue, newValue);
            }

            public void AssertSetIsChangedTo(TValue attemptedValue, TValue resultingValue)
            {
                var b = _owner.New();
                _builderSetter(b, attemptedValue);
                Assert.Equal(resultingValue, _builderGetter(b));
            }
        }
    }
}

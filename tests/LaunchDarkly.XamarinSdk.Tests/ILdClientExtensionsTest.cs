using LaunchDarkly.Client;
using Moq;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class ILdClientExtensionsTest
    {
        enum MyEnum
        {
            Red,
            Green,
            Blue
        };

        [Fact]
        public void EnumVariationConvertsStringToEnum()
        {
            var clientMock = new Mock<ILdClient>();
            clientMock.Setup(c => c.StringVariation("key", "Blue")).Returns("Green");
            var client = clientMock.Object;

            var result = client.EnumVariation("key", MyEnum.Blue);
            Assert.Equal(MyEnum.Green, result);
        }

        [Fact]
        public void EnumVariationReturnsDefaultValueForInvalidFlagValue()
        {
            var clientMock = new Mock<ILdClient>();
            clientMock.Setup(c => c.StringVariation("key", "Blue")).Returns("not-a-color");
            var client = clientMock.Object;

            var defaultValue = MyEnum.Blue;
            var result = client.EnumVariation("key", defaultValue);
            Assert.Equal(MyEnum.Blue, defaultValue);
        }

        [Fact]
        public void EnumVariationReturnsDefaultValueForNullFlagValue()
        {
            var clientMock = new Mock<ILdClient>();
            clientMock.Setup(c => c.StringVariation("key", "Blue")).Returns((string)null);
            var client = clientMock.Object;

            var defaultValue = MyEnum.Blue;
            var result = client.EnumVariation("key", defaultValue);
            Assert.Equal(defaultValue, result);
        }

        [Fact]
        public void EnumVariationReturnsDefaultValueForNonEnumType()
        {
            var clientMock = new Mock<ILdClient>();
            clientMock.Setup(c => c.StringVariation("key", "Blue")).Returns("Green");
            var client = clientMock.Object;

            var defaultValue = "this is a string, not an enum";
            var result = client.EnumVariation("key", defaultValue);
            Assert.Equal(defaultValue, result);
        }

        [Fact]
        public void EnumVariationDetailConvertsStringToEnum()
        {
            var clientMock = new Mock<ILdClient>();
            clientMock.Setup(c => c.StringVariationDetail("key", "Blue"))
                .Returns(new EvaluationDetail<string>("Green", 1, EvaluationReason.Fallthrough.Instance));
            var client = clientMock.Object;

            var result = client.EnumVariationDetail("key", MyEnum.Blue);
            var expected = new EvaluationDetail<MyEnum>(MyEnum.Green, 1, EvaluationReason.Fallthrough.Instance);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void EnumVariationDetailReturnsDefaultValueForInvalidFlagValue()
        {
            var clientMock = new Mock<ILdClient>();
            clientMock.Setup(c => c.StringVariationDetail("key", "Blue"))
                .Returns(new EvaluationDetail<string>("not-a-color", 1, EvaluationReason.Fallthrough.Instance));
            var client = clientMock.Object;

            var result = client.EnumVariationDetail("key", MyEnum.Blue);
            var expected = new EvaluationDetail<MyEnum>(MyEnum.Blue, 1, new EvaluationReason.Error(EvaluationErrorKind.WRONG_TYPE));
            Assert.Equal(expected, result);
        }

        [Fact]
        public void EnumVariationDetailReturnsDefaultValueForNullFlagValue()
        {
            var clientMock = new Mock<ILdClient>();
            clientMock.Setup(c => c.StringVariationDetail("key", "Blue"))
                .Returns(new EvaluationDetail<string>(null, 1, EvaluationReason.Fallthrough.Instance));
            var client = clientMock.Object;

            var result = client.EnumVariationDetail("key", MyEnum.Blue);
            var expected = new EvaluationDetail<MyEnum>(MyEnum.Blue, 1, EvaluationReason.Fallthrough.Instance);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void EnumVariationDetailReturnsDefaultValueForNonEnumType()
        {
            var defaultValue = "this is a string, not an enum";
            var clientMock = new Mock<ILdClient>();
            clientMock.Setup(c => c.StringVariationDetail("key", defaultValue))
                .Returns(new EvaluationDetail<string>("Green", 1, EvaluationReason.Fallthrough.Instance));
            var client = clientMock.Object;

            var result = client.EnumVariationDetail("key", defaultValue);
            var expected = new EvaluationDetail<string>(defaultValue, 1, new EvaluationReason.Error(EvaluationErrorKind.WRONG_TYPE));
            Assert.Equal(expected, result);
        }
    }
}

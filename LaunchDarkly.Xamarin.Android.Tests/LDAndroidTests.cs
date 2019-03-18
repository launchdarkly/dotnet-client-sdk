using System;
using NUnit.Framework;

namespace LaunchDarkly.Xamarin.Android.Tests
{
    [TestFixture]
    public class LDAndroidTests
    {
        [SetUp]
        public void Setup() { }


        [TearDown]
        public void Tear() { }

        [Test]
        public void Pass()
        {
            Console.WriteLine("test1");
            Assert.True(true);
        }
    }
}

using System;
using NUnit.Framework;
//using LaunchDarkly.Client;

namespace LaunchDarkly.Xamarin.iOS.Tests
{
    [TestFixture]
    public class LDiOSTests
    {
        [SetUp]
        public void Setup() {
            //LdClient client = LdClient.Init(config, user, TimeSpan.Zero);
        }

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

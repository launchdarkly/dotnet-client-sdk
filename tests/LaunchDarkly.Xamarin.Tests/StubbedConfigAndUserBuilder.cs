using System;
using System.Collections.Generic;
using LaunchDarkly.Client;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Xamarin.Tests
{
    public static class StubbedConfigAndUserBuilder
    {
        public static User UserWithAllPropertiesFilledIn(string key)
        {
            var user = User.WithKey(key);
            user.SecondaryKey = "secondaryKey";
            user.IpAddress = "10.0.0.1";
            user.Country = "US";
            user.FirstName = "John";
            user.LastName = "Doe";
            user.Name = user.FirstName + " " + user.LastName;
            user.Avatar = "images.google.com/myAvatar";
            user.Email = "someEmail@google.com";
            user.Custom = new Dictionary<string, JToken>
            {
                {"somePrivateAttr1", JToken.FromObject("attributeValue1")},
                {"somePrivateAttr2", JToken.FromObject("attributeValue2")},
            };

            return user;
        }
    }

    public class MockPersister : ISimplePersistance
    {
        private IDictionary<string, string> map = new Dictionary<string, string>();

        public string GetValue(string key)
        {
            if (!map.ContainsKey(key))
                return null;
            
            return map[key];
        }

        public void Save(string key, string value)
        {
            map[key] = value;
        }
    }

    public class MockDeviceInfo : IDeviceInfo
    {
        public const string key = "someUniqueKey";

        public string UniqueDeviceId()
        {
            return key;
        }
    }
}

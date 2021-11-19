﻿using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Json;
using Xunit;
using Xunit.Sdk;

using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;
using static LaunchDarkly.TestHelpers.JsonAssertions;

namespace LaunchDarkly.Sdk.Client
{
    public class AssertHelpers
    {
        public static void DataSetsEqual(FullDataSet expected, FullDataSet actual) =>
            AssertJsonEqual(expected.ToJsonString(), actual.ToJsonString());

        public static void DataItemsEqual(ItemDescriptor expected, ItemDescriptor actual)
        {
            AssertJsonEqual(expected.Item is null ? null : expected.Item.ToJsonString(),
                actual.Item is null ? null : actual.Item.ToJsonString());
            Assert.Equal(expected.Version, actual.Version);
        }

        public static void UsersEqual(User expected, User actual) =>
            AssertJsonEqual(LdJsonSerialization.SerializeObject(expected),
                LdJsonSerialization.SerializeObject(actual));

        public static void UsersEqualExcludingAutoProperties(User expected, User actual)
        {
            var builder = User.Builder(expected);
            foreach (var autoProp in new string[] { "device", "os" })
            {
                if (!actual.GetAttribute(UserAttribute.ForName(autoProp)).IsNull)
                {
                    builder.Custom(autoProp, actual.GetAttribute(UserAttribute.ForName(autoProp)));
                }
            }
            UsersEqual(builder.Build(), actual);
        }

        public static void LogMessageRegex(LogCapture logCapture, bool shouldHave, LogLevel level, string pattern)
        {
            if (logCapture.HasMessageWithRegex(level, pattern) != shouldHave)
            {
                ThrowLogMatchException(logCapture, shouldHave, level, pattern, true);
            }
        }

        public static void LogMessageText(LogCapture logCapture, bool shouldHave, LogLevel level, string text)
        {
            if (logCapture.HasMessageWithText(level, text) != shouldHave)
            {
                ThrowLogMatchException(logCapture, shouldHave, level, text, true);
            }
        }

        private static void ThrowLogMatchException(LogCapture logCapture, bool shouldHave, LogLevel level, string text, bool isRegex) =>
            throw new AssertActualExpectedException(shouldHave, !shouldHave,
                string.Format("Expected log {0} the {1} \"{2}\" at level {3}\n\nActual log output follows:\n{4}",
                    shouldHave ? "to have" : "not to have",
                    isRegex ? "pattern" : "exact message",
                    text,
                    level,
                    logCapture.ToString()));
    }
}

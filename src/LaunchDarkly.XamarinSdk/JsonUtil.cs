using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LaunchDarkly.JsonStream;
using LaunchDarkly.Sdk.Json;

namespace LaunchDarkly.Sdk.Xamarin
{
    internal class JsonUtil
    {
        internal static T DecodeJson<T>(string json) where T : IJsonSerializable =>
            LdJsonSerialization.DeserializeObject<T>(json);

        internal static string EncodeJson<T>(T o) where T : IJsonSerializable =>
            LdJsonSerialization.SerializeObject(o);

        public static ImmutableDictionary<string, FeatureFlag> DeserializeFlags(string json)
        {
            var r = JReader.FromString(json);
            try
            {
                var builder = ImmutableDictionary.CreateBuilder<string, FeatureFlag>();
                for (var or = r.Object(); or.Next(ref r);)
                {
                    builder.Add(or.Name.ToString(), FeatureFlag.JsonConverter.ReadJsonValue(ref r));
                }
                return builder.ToImmutable();
            }
            catch (Exception e)
            {
                throw r.TranslateException(e);
            }
        }

        public static string SerializeFlags(IReadOnlyDictionary<string, FeatureFlag> flags)
        {
            var w = JWriter.New();
            using (var ow = w.Object())
            {
                foreach (var kv in flags)
                {
                    FeatureFlag.JsonConverter.WriteJsonValue(kv.Value, ow.Name(kv.Key));
                }
            }
            return w.GetString();
        }
    }
}

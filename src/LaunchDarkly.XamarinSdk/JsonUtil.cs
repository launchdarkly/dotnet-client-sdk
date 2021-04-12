using System;
using System.Collections.Generic;
using LaunchDarkly.Sdk.Json;
using Newtonsoft.Json;

namespace LaunchDarkly.Sdk.Xamarin
{
    internal class JsonUtil
    {
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { LdJsonNet.Converter, new UnixMillisecondTimeConverter() },
            DateParseHandling = DateParseHandling.None
        };

        // Wrapper for JsonConvert.DeserializeObject that ensures we use consistent settings and minimizes our Newtonsoft references.
        internal static T DecodeJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, _jsonSettings);
        }

        // Wrapper for JsonConvert.DeserializeObject that ensures we use consistent settings and minimizes our Newtonsoft references.
        internal static object DecodeJson(string json, Type type)
        {
            return JsonConvert.DeserializeObject(json, type, _jsonSettings);
        }

        // Wrapper for JsonConvert.SerializeObject that ensures we use consistent settings and minimizes our Newtonsoft references.
        internal static string EncodeJson(object o)
        {
            return JsonConvert.SerializeObject(o, _jsonSettings);
        }

        private class UnixMillisecondTimeConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) =>
                objectType == typeof(UnixMillisecondTime) || objectType == typeof(UnixMillisecondTime?);

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (objectType == typeof(UnixMillisecondTime?))
                {
                    if (reader.TokenType == JsonToken.Null)
                    {
                        reader.Skip();
                        return null;
                    }
                }
                return UnixMillisecondTime.OfMillis((long)reader.Value);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value is null)
                {
                    writer.WriteNull();
                }
                else
                {
                    writer.WriteValue(((UnixMillisecondTime)value).Value);
                }
            }
        }
    }
}

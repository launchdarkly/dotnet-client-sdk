using System;
using LaunchDarkly.Client;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Xamarin
{
    internal class ValueType<T>
    {
        public Func<JToken, T> ValueFromJson { get; internal set; }
        public Func<T, JToken> ValueToJson { get; internal set; }
    }

    internal class ValueTypes
    {
        private static ArgumentException BadTypeException()
        {
            return new ArgumentException("unexpected data type");
        }

        public static ValueType<bool> Bool = new ValueType<bool>
        {
            ValueFromJson = json =>
            {
                if (json.Type != JTokenType.Boolean)
                {
                    throw BadTypeException();
                }
                return json.Value<bool>();
            },
            ValueToJson = value => new JValue(value)
        };

        public static ValueType<int> Int = new ValueType<int>
        {
            ValueFromJson = json =>
            {
                if (json.Type != JTokenType.Integer && json.Type != JTokenType.Float)
                {
                    throw BadTypeException();
                }
                return json.Value<int>();
            },
            ValueToJson = value => new JValue(value)
        };

        public static ValueType<float> Float = new ValueType<float>
        {
            ValueFromJson = json =>
            {
                if (json.Type != JTokenType.Integer && json.Type != JTokenType.Float)
                {
                    throw BadTypeException();
                }
                return json.Value<float>();
            },
            ValueToJson = value => new JValue(value)
        };

        public static ValueType<string> String = new ValueType<string>
        {
            ValueFromJson = json =>
            {
                if (json == null || json.Type == JTokenType.Null)
                {
                    return null; // strings are always nullable
                }
                if (json.Type != JTokenType.String)
                {
                    throw BadTypeException();
                }
                return json.Value<string>();
            },
            ValueToJson = value => value == null ? null : new JValue(value)
        };

        public static ValueType<ImmutableJsonValue> Json = new ValueType<ImmutableJsonValue>
        {
            ValueFromJson = json => new ImmutableJsonValue(json),
            ValueToJson = value => value.AsJToken()
            // Note that we are calling the ImmutableJsonValue constructor directly instead of using FromJToken()
            // because we do not need it to deep-copy mutable values immediately - we know that *we* won't be
            // modifying those values. It will deep-copy them if and when the application tries to access them.
        };
    }
}

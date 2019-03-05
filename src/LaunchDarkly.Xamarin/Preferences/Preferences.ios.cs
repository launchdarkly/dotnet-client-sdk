/*
Xamarin.Essentials(https://github.com/xamarin/Essentials) code used under MIT License

The MIT License(MIT)
Copyright(c) Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
associated documentation files (the "Software"), to deal in the Software without restriction, 
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial
portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Globalization;
using Foundation;

namespace LaunchDarkly.Xamarin.Preferences
{
    public static partial class Preferences
    {
        static readonly object locker = new object();

        static bool PlatformContainsKey(string key, string sharedName)
        {
            lock (locker)
            {
                return GetUserDefaults(sharedName)[key] != null;
            }
        }

        static void PlatformRemove(string key, string sharedName)
        {
            lock (locker)
            {
                using (var userDefaults = GetUserDefaults(sharedName))
                {
                    if (userDefaults[key] != null)
                        userDefaults.RemoveObject(key);
                }
            }
        }

        static void PlatformClear(string sharedName)
        {
            lock (locker)
            {
                using (var userDefaults = GetUserDefaults(sharedName))
                {
                    var items = userDefaults.ToDictionary();

                    foreach (var item in items.Keys)
                    {
                        if (item is NSString nsString)
                            userDefaults.RemoveObject(nsString);
                    }
                }
            }
        }

        static void PlatformSet<T>(string key, T value, string sharedName)
        {
            lock (locker)
            {
                using (var userDefaults = GetUserDefaults(sharedName))
                {
                    if (value == null)
                    {
                        if (userDefaults[key] != null)
                            userDefaults.RemoveObject(key);
                        return;
                    }

                    switch (value)
                    {
                        case string s:
                            userDefaults.SetString(s, key);
                            break;
                        case int i:
                            userDefaults.SetInt(i, key);
                            break;
                        case bool b:
                            userDefaults.SetBool(b, key);
                            break;
                        case long l:
                            var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
                            userDefaults.SetString(valueString, key);
                            break;
                        case double d:
                            userDefaults.SetDouble(d, key);
                            break;
                        case float f:
                            userDefaults.SetFloat(f, key);
                            break;
                    }
                }
            }
        }

        static T PlatformGet<T>(string key, T defaultValue, string sharedName)
        {
            object value = null;

            lock (locker)
            {
                using (var userDefaults = GetUserDefaults(sharedName))
                {
                    if (userDefaults[key] == null)
                        return defaultValue;

                    switch (defaultValue)
                    {
                        case int i:
                            value = (int)(nint)userDefaults.IntForKey(key);
                            break;
                        case bool b:
                            value = userDefaults.BoolForKey(key);
                            break;
                        case long l:
                            var savedLong = userDefaults.StringForKey(key);
                            value = Convert.ToInt64(savedLong, CultureInfo.InvariantCulture);
                            break;
                        case double d:
                            value = userDefaults.DoubleForKey(key);
                            break;
                        case float f:
                            value = userDefaults.FloatForKey(key);
                            break;
                        case string s:
                            // the case when the string is not null
                            value = userDefaults.StringForKey(key);
                            break;
                        default:
                            // the case when the string is null
                            if (typeof(T) == typeof(string))
                                value = userDefaults.StringForKey(key);
                            break;
                    }
                }
            }

            return (T)value;
        }

        static NSUserDefaults GetUserDefaults(string sharedName)
        {
            if (!string.IsNullOrWhiteSpace(sharedName))
                return new NSUserDefaults(sharedName, NSUserDefaultsType.SuiteName);
            else
                return NSUserDefaults.StandardUserDefaults;
        }
    }
}

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
using Android.App;
using Android.Content;
using Android.Preferences;

namespace LaunchDarkly.Xamarin.Preferences
{
    internal static partial class Preferences
    {
        static readonly object locker = new object();

        static bool PlatformContainsKey(string key, string sharedName)
        {
            lock (locker)
            {
                using (var sharedPreferences = GetSharedPreferences(sharedName))
                {
                    return sharedPreferences.Contains(key);
                }
            }
        }

        static void PlatformRemove(string key, string sharedName)
        {
            lock (locker)
            {
                using (var sharedPreferences = GetSharedPreferences(sharedName))
                using (var editor = sharedPreferences.Edit())
                {
                    editor.Remove(key).Commit();
                }
            }
        }

        static void PlatformClear(string sharedName)
        {
            lock (locker)
            {
                using (var sharedPreferences = GetSharedPreferences(sharedName))
                using (var editor = sharedPreferences.Edit())
                {
                    editor.Clear().Commit();
                }
            }
        }

        static void PlatformSet<T>(string key, T value, string sharedName)
        {
            lock (locker)
            {
                using (var sharedPreferences = GetSharedPreferences(sharedName))
                using (var editor = sharedPreferences.Edit())
                {
                    if (value == null)
                    {
                        editor.Remove(key);
                    }
                    else
                    {
                        switch (value)
                        {
                            case string s:
                                editor.PutString(key, s);
                                break;
                            case int i:
                                editor.PutInt(key, i);
                                break;
                            case bool b:
                                editor.PutBoolean(key, b);
                                break;
                            case long l:
                                editor.PutLong(key, l);
                                break;
                            case double d:
                                var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
                                editor.PutString(key, valueString);
                                break;
                            case float f:
                                editor.PutFloat(key, f);
                                break;
                        }
                    }
                    editor.Apply();
                }
            }
        }

        static T PlatformGet<T>(string key, T defaultValue, string sharedName)
        {
            lock (locker)
            {
                object value = null;
                using (var sharedPreferences = GetSharedPreferences(sharedName))
                {
                    if (defaultValue == null)
                    {
                        value = sharedPreferences.GetString(key, null);
                    }
                    else
                    {
                        switch (defaultValue)
                        {
                            case int i:
                                value = sharedPreferences.GetInt(key, i);
                                break;
                            case bool b:
                                value = sharedPreferences.GetBoolean(key, b);
                                break;
                            case long l:
                                value = sharedPreferences.GetLong(key, l);
                                break;
                            case double d:
                                var savedDouble = sharedPreferences.GetString(key, null);
                                if (string.IsNullOrWhiteSpace(savedDouble))
                                {
                                    value = defaultValue;
                                }
                                else
                                {
                                    if (!double.TryParse(savedDouble, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var outDouble))
                                    {
                                        var maxString = Convert.ToString(double.MaxValue, CultureInfo.InvariantCulture);
                                        outDouble = savedDouble.Equals(maxString) ? double.MaxValue : double.MinValue;
                                    }

                                    value = outDouble;
                                }
                                break;
                            case float f:
                                value = sharedPreferences.GetFloat(key, f);
                                break;
                            case string s:
                                // the case when the string is not null
                                value = sharedPreferences.GetString(key, s);
                                break;
                        }
                    }
                }

                return (T)value;
            }
        }

        static ISharedPreferences GetSharedPreferences(string sharedName)
        {
            var context = Application.Context;

            return string.IsNullOrWhiteSpace(sharedName) ?
                PreferenceManager.GetDefaultSharedPreferences(context) :
                    context.GetSharedPreferences(sharedName, FileCreationMode.Private);
        }
    }
}

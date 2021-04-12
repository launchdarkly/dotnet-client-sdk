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
using LaunchDarkly.Logging;

namespace LaunchDarkly.Sdk.Xamarin.PlatformSpecific
{
    // Modified for LaunchDarkly: the SDK always serializes values to strings before using this class
    // to store them. Therefore, the overloads for non-string types have been removed, thereby
    // reducing the amount of multi-platform implementation code that won't be used.

    internal static partial class Preferences
    {
        static readonly object locker = new object();

        static bool PlatformContainsKey(string key, string sharedName, Logger log)
        {
            lock (locker)
            {
                using (var sharedPreferences = GetSharedPreferences(sharedName))
                {
                    return sharedPreferences.Contains(key);
                }
            }
        }

        static void PlatformRemove(string key, string sharedName, Logger log)
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

        static void PlatformClear(string sharedName, Logger log)
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

        static void PlatformSet(string key, string value, string sharedName, Logger log)
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
                        editor.PutString(key, value);
                    }
                    editor.Apply();
                }
            }
        }

        static string PlatformGet(string key, string defaultValue, string sharedName, Logger log)
        {
            lock (locker)
            {
                using (var sharedPreferences = GetSharedPreferences(sharedName))
                {
                    return sharedPreferences.GetString(key, defaultValue);
                }
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

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
                return GetUserDefaults(sharedName)[key] != null;
            }
        }

        static void PlatformRemove(string key, string sharedName, Logger log)
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

        static void PlatformClear(string sharedName, Logger log)
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

        static void PlatformSet(string key, string value, string sharedName, Logger log)
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

                    userDefaults.SetString(value, key);
                }
            }
        }

        static string PlatformGet(string key, string defaultValue, string sharedName, Logger log)
        {
            lock (locker)
            {
                using (var userDefaults = GetUserDefaults(sharedName))
                {
                    if (userDefaults[key] == null)
                        return defaultValue;

                    return userDefaults.StringForKey(key);
                }
            }
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

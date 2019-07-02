#if NETSTANDARD1_6
#else
using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using Common.Logging;
using LaunchDarkly.Common;
#endif

namespace LaunchDarkly.Xamarin.PlatformSpecific
{
    // This code is not from Xamarin Essentials, though it implements the same Preferences abstraction.
    //
    // In .NET Standard 2.0, we use the IsolatedStorage API to store per-user data. The .NET Standard implementation
    // of IsolatedStorage puts these files under ~/.local/share/IsolatedStorage followed by a subpath of obfuscated
    // strings that are apparently based on the application and assembly name, so the data should be specific to both
    // the OS user account and the current app.
    //
    // This is based on the Plugin.Settings plugin (which is what Xamarin Essentials uses for preferences), but greatly
    // simplified since we only need one data type. See: https://github.com/jamesmontemagno/SettingsPlugin/blob/master/src/Plugin.Settings/Settings.dotnet.cs
    //
    // In .NET Standard 1.6, there is no data store.

    internal static partial class Preferences
    {
#if NETSTANDARD1_6
        static bool PlatformContainsKey(string key, string sharedName) => false;

        static void PlatformRemove(string key, string sharedName) { }

        static void PlatformClear(string sharedName) { }

        static void PlatformSet(string key, string value, string sharedName) { }

        static string PlatformGet(string key, string defaultValue, string sharedName) => defaultValue;
#else
        private static readonly ILog Log = LogManager.GetLogger(typeof(Preferences));

        private static AtomicBoolean _loggedOSError = new AtomicBoolean(false); // AtomicBoolean is defined in LaunchDarkly.CommonSdk

        private const string ConfigDirectoryName = "LaunchDarkly";

        static bool PlatformContainsKey(string key, string sharedName)
        {
            return WithStore(store => store.FileExists(MakeFilePath(key, sharedName)));
        }

        static void PlatformRemove(string key, string sharedName)
        {
            WithStore(store =>
            {
                try
                {
                    store.DeleteFile(MakeFilePath(key, sharedName));
                }
                catch (IsolatedStorageException) { } // file didn't exist - that's OK
            });
        }

        static void PlatformClear(string sharedName)
        {
            WithStore(store =>
            {
                try
                {
                    store.DeleteDirectory(MakeDirectoryPath(sharedName));
                    // The directory will be recreated next time PlatformSet is called with the same sharedName.
                }
                catch (IsolatedStorageException) { } // directory didn't exist - that's OK
            });
        }

        static void PlatformSet(string key, string value, string sharedName)
        {
            WithStore(store =>
            {
                var path = MakeDirectoryPath(sharedName);
                store.CreateDirectory(path); // has no effect if directory already exists
                using (var stream = store.OpenFile(MakeFilePath(key, sharedName), FileMode.Create, FileAccess.Write))
                {
                    using (var sw = new StreamWriter(stream))
                    {
                        sw.Write(value);
                    }
                }
            });
        }

        static string PlatformGet(string key, string defaultValue, string sharedName)
        {
            return WithStore(store =>
            {
                try
                {
                    using (var stream = store.OpenFile(MakeFilePath(key, sharedName), FileMode.Open))
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
                catch (DirectoryNotFoundException) { } // just return null if no preferences have ever been set
                catch (FileNotFoundException) { } // just return null if this preference was never set
                return null;
            });
        }

        private static T WithStore<T>(Func<IsolatedStorageFile, T> callback)
        {
            try
            {
                // GetUserStoreForDomain returns a storage object that is specific to the current application and OS user.
                var store = IsolatedStorageFile.GetUserStoreForDomain();
                return callback(store);
            }
            catch (Exception e)
            {
                HandleStoreException(e);
                return default;
            }
        }

        private static void HandleStoreException(Exception e)
        {
            if (e is IsolatedStorageException ||
                e is InvalidOperationException)
            {
                // These exceptions are ones that IsolatedStorageFile methods may throw under conditions that are
                // unrelated to our code, e.g. filesystem permissions don't allow the store to be used. Since such a
                // condition is unlikely to change during the application's lifetime, we only want to log it once.
                // We won't log a stacktrace since it'll just point to somewhere in the standard library.

                // Note that we specifically catch IsolatedStorageException in a couple places above, when it would
                // indicate a particular error condition that we want to handle differently. In all other cases it
                // is unexpected and should be considered a platform/configuration issue.

                if (!_loggedOSError.GetAndSet(true))
                {
                    Log.WarnFormat("Persistent storage is unavailable and has been disabled ({0}: {1})", e.GetType(), e.Message);
                }
            }
            else
            {
                // All other errors probably indicate an error in our own code. We don't want to throw these up
                // into the SDK; the Preferences API is expected to either work or silently fail.
                Log.ErrorFormat("Error in accessing persistent storage: {0}: {1}", e.GetType(), e.Message);
                Log.Debug(e.StackTrace);
            }
        }

        private static void WithStore(Action<IsolatedStorageFile> callback)
        {
            WithStore<Boolean>(store =>
            {
                callback(store);
                return true;
            });
        }

        private static string MakeDirectoryPath(string sharedName)
        {
            if (string.IsNullOrEmpty(sharedName))
            {
                return ConfigDirectoryName;
            }
            return ConfigDirectoryName + "." + EscapeFilenameComponent(sharedName);
        }

        private static string MakeFilePath(string key, string sharedName)
        {
            return MakeDirectoryPath(sharedName) + "/" + EscapeFilenameComponent(key);
        }

        private static string EscapeFilenameComponent(string name)
        {
            // In actual usage for LaunchDarkly this should not be an issue, because keys are really feature flag keys
            // which have a very limited character set, and we don't actually use sharedName. It's just good practice.
            StringBuilder buf = null;
            var badChars = Path.GetInvalidFileNameChars();
            const char escapeChar = '%';
            for (var i = 0; i < name.Length; i++)
            {
                var ch = name[i];
                if (badChars.Contains(ch) || ch == escapeChar)
                {
                    if (buf == null) // create StringBuilder lazily since most names will be valid
                    {
                        buf = new StringBuilder(name.Length);
                        buf.Append(name.Substring(0, i));
                    }
                    buf.Append(escapeChar).Append(((int)ch).ToString("X")); // hex value
                }
                else
                {
                    buf?.Append(ch);
                }
            }
            return buf == null ? name : buf.ToString();
        }
#endif
    }
}

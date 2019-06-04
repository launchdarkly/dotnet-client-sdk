#if NETSTANDARD1_6
#else
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
#endif

namespace LaunchDarkly.Xamarin.Preferences
{
    // This code is not from Xamarin Essentials, though it implements the same Preferences abstraction.
    //
    // In .NET Standard 2.0, we use the IsolatedStorage API to store per-user data. The .NET Standard implementation
    // of IsolatedStorage puts these files under ~/.local/share/IsolatedStorage followed by a subpath of obfuscated
    // strings that are apparently based on the application and assembly name, so the data should be specific to both
    // the OS user account and the current app.
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
        private const string ConfigDirectoryName = "LaunchDarkly";

        // GetUserStoreForDomain returns a storage object that is specific to the current application and OS user.
        private static IsolatedStorageFile Store => IsolatedStorageFile.GetUserStoreForDomain();

        static bool PlatformContainsKey(string key, string sharedName)
        {
            return Store.FileExists(MakeFilePath(key, sharedName));
        }

        static void PlatformRemove(string key, string sharedName)
        {
            try
            {
                Store.DeleteFile(MakeFilePath(key, sharedName));
            }
            catch (IsolatedStorageException) {} // Preferences implementations shouldn't throw exceptions except for a code error like a null reference
        }

        static void PlatformClear(string sharedName)
        {
            try
            {
                Store.DeleteDirectory(MakeDirectoryPath(sharedName));
            }
            catch (IsolatedStorageException) {}
        }

        static void PlatformSet(string key, string value, string sharedName)
        {
            try
            {
                var path = MakeDirectoryPath(sharedName);
                if (!Store.DirectoryExists(path))
                {
                    Store.CreateDirectory(path);
                }
            }
            catch (IsolatedStorageException) {}
            using (var stream = Store.OpenFile(MakeFilePath(key, sharedName), FileMode.Create, FileAccess.Write))
            {
                using (var sw = new StreamWriter(stream))
                {
                    sw.Write(value);
                }
            }
        }

        static string PlatformGet(string key, string defaultValue, string sharedName)
        {
            try
            {
                using (var stream = Store.OpenFile(MakeFilePath(key, sharedName), FileMode.Open))
                {
                    using (var sr = new StreamReader(stream))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (IsolatedStorageException) {}
            catch (DirectoryNotFoundException) {}
            catch (FileNotFoundException) {}
            return null;
        }

        static string MakeDirectoryPath(string sharedName)
        {
            if (string.IsNullOrEmpty(sharedName))
            {
                return ConfigDirectoryName;
            }
            return ConfigDirectoryName + "." + EscapeFilenameComponent(sharedName);
        }

        static string MakeFilePath(string key, string sharedName)
        {
            return MakeDirectoryPath(sharedName) + "/" + EscapeFilenameComponent(key);
        }

        static string EscapeFilenameComponent(string name)
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

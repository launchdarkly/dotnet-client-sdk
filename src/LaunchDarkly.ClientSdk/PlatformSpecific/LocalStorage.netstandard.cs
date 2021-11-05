using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using LaunchDarkly.Sdk.Client.Interfaces;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    // In .NET Standard 2.0, we use the IsolatedStorage API to store per-user data. The .NET Standard implementation
    // of IsolatedStorage puts these files under ~/.local/share/IsolatedStorage followed by a subpath of obfuscated
    // strings that are apparently based on the application and assembly name, so the data should be specific to both
    // the OS user account and the current app.
    //
    // This is based on the Plugin.Settings plugin (which is what Xamarin Essentials uses for preferences), but greatly
    // simplified since we only need one data type. See: https://github.com/jamesmontemagno/SettingsPlugin/blob/master/src/Plugin.Settings/Settings.dotnet.cs

    internal sealed partial class LocalStorage : IPersistentDataStore
    {
        private const string ConfigDirectoryName = "LaunchDarkly";

        public string GetValue(string storageNamespace, string key)
        {
            return WithStore(store =>
            {
                try
                {
                    using (var stream = store.OpenFile(MakeFilePath(storageNamespace, key), FileMode.Open))
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
                // ignore exceptions that just indicate no value has been set for this namespace/key
                catch (IsolatedStorageException e) when (e.InnerException is DirectoryNotFoundException) { }
                catch (IsolatedStorageException e) when (e.InnerException is FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
                catch (FileNotFoundException) { }
                return null;
            });
        }

        public void SetValue(string storageNamespace, string key, string value)
        {
            WithStore(store =>
            {
                var filePath = MakeFilePath(storageNamespace, key);
                if (value is null)
                {
                    try
                    {
                        store.DeleteFile(filePath);
                    }
                    catch (IsolatedStorageException) { } // file didn't exist - that's OK
                }
                else
                {
                    var dirPath = MakeDirectoryPath(storageNamespace);
                    store.CreateDirectory(dirPath); // has no effect if directory already exists
                    using (var stream = store.OpenFile(filePath, FileMode.Create, FileAccess.Write))
                    {
                        using (var sw = new StreamWriter(stream))
                        {
                            sw.Write(value);
                        }
                    }
                }
            });
        }

        private T WithStore<T>(Func<IsolatedStorageFile, T> callback)
        {
            // GetUserStoreForDomain returns a storage object that is specific to the current application and OS user.
            using (var store = IsolatedStorageFile.GetUserStoreForDomain())
            {
                return callback(store);
            }
        }

        private void WithStore(Action<IsolatedStorageFile> callback)
        {
            WithStore<bool>(store =>
            {
                callback(store);
                return true;
            });
        }

        private static string MakeDirectoryPath(string storageNamespace) =>
            string.IsNullOrEmpty(storageNamespace)
                ? ConfigDirectoryName
                : (ConfigDirectoryName + "." + EscapeFilenameComponent(storageNamespace));

        private static string MakeFilePath(string storageNamespace, string key) =>
            MakeDirectoryPath(storageNamespace) + "/" + EscapeFilenameComponent(key);

        private static string EscapeFilenameComponent(string name)
        {
            StringBuilder buf = null;
            for (var i = 0; i < name.Length; i++)
            {
                var ch = name[i];
                if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '-'
                    || (ch == '.' && i > 0))
                {
                    buf?.Append(ch);
                }
                else
                {
                    if (buf == null) // create StringBuilder lazily since most names will be valid
                    {
                        buf = new StringBuilder(name.Length + 30);
                        buf.Append(name.Substring(0, i));
                    }
                    buf.Append('%').Append(((int)ch).ToString("X")); // hex value
                }
            }
            return buf == null ? name : buf.ToString();
        }
    }
}

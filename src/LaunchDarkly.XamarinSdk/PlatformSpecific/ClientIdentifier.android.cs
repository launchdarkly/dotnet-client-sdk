using System;
using Common.Logging;
using Android.App;
using Android.Provider;
using Android.OS;
using Android.Runtime;
using Java.Interop;

namespace LaunchDarkly.Xamarin.PlatformSpecific
{
    internal static partial class ClientIdentifier
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ClientIdentifier));
        private static JniPeerMembers buildMembers = new XAPeerMembers("android/os/Build", typeof(Build));

        private static string PlatformGetOrCreateClientId()
        {
            // Based on: https://github.com/jamesmontemagno/DeviceInfoPlugin/blob/master/src/DeviceInfo.Plugin/DeviceInfo.android.cs
            string serialField;
            try
            {
                var value = buildMembers.StaticFields.GetObjectValue("SERIAL.Ljava/lang/String;");
                serialField = JNIEnv.GetString(value.Handle, JniHandleOwnership.TransferLocalRef);
            }
            catch
            {
                serialField = "";
            }
            if (string.IsNullOrWhiteSpace(serialField) || serialField == Build.Unknown || serialField == "0")
            {
                try
                {
                    var context = Android.App.Application.Context;
                    return Settings.Secure.GetString(context.ContentResolver, Settings.Secure.AndroidId);
                }
                catch (Exception ex)
                {
                    Log.WarnFormat("Unable to get client ID: {0}", ex);
                    return null;
                }
            }
            else
            {
                return serialField;
            }
        }
    }
}

using System;
using Android.Provider;
using Android.OS;
using Android.Runtime;
using Java.Interop;
using LaunchDarkly.Logging;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class ClientIdentifier
    {
        private static JniPeerMembers buildMembers = new XAPeerMembers("android/os/Build", typeof(Build));

        private static string PlatformGetOrCreateClientId(Logger log)
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
                    log.Warn("Unable to get client ID: {0}", LogValues.ExceptionSummary(ex));
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

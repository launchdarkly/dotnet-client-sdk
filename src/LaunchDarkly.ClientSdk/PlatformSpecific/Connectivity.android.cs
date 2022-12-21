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
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Content;
using Android.Net;
using Android.OS;
using Debug = System.Diagnostics.Debug;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal partial class Connectivity
    {
        static ConnectivityBroadcastReceiver conectivityReceiver;

        static void StartListeners()
        {
            Permissions.EnsureDeclared(PermissionType.NetworkState);

            conectivityReceiver = new ConnectivityBroadcastReceiver(OnConnectivityChanged);

            Platform.AppContext.RegisterReceiver(conectivityReceiver, new IntentFilter(ConnectivityManager.ConnectivityAction));
        }

        static void StopListeners()
        {
            if (conectivityReceiver == null)
                return;
            try
            {
                Platform.AppContext.UnregisterReceiver(conectivityReceiver);
            }
            catch (Java.Lang.IllegalArgumentException)
            {
                Debug.WriteLine("Connectivity receiver already unregistered. Disposing of it.");
            }
            conectivityReceiver.Dispose();
            conectivityReceiver = null;
        }

        static NetworkAccess IsBetterAccess(NetworkAccess currentAccess, NetworkAccess newAccess) =>
            newAccess > currentAccess ? newAccess : currentAccess;

        static NetworkAccess PlatformNetworkAccess
        {
            get
            {
                Permissions.EnsureDeclared(PermissionType.NetworkState);

                try
                {
                    var currentAccess = NetworkAccess.None;
                    var manager = Platform.ConnectivityManager;

                    if (Platform.HasApiLevel(BuildVersionCodes.Lollipop))
                    {
                        foreach (var network in manager.GetAllNetworks())
                        {
                            try
                            {
                                var capabilities = manager.GetNetworkCapabilities(network);

                                if (capabilities == null)
                                    continue;

                                var info = manager.GetNetworkInfo(network);

                                if (info == null || !info.IsAvailable)
                                    continue;

                                // Check to see if it has the internet capability
                                if (!capabilities.HasCapability(NetCapability.Internet))
                                {
                                    // Doesn't have internet, but local is possible
                                    currentAccess = IsBetterAccess(currentAccess, NetworkAccess.Local);
                                    continue;
                                }

                                ProcessNetworkInfo(info);
                            }
                            catch
                            {
                                // there is a possibility, but don't worry
                            }
                        }
                    }
                    else
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        foreach (var info in manager.GetAllNetworkInfo())
#pragma warning restore CS0618 // Type or member is obsolete
                        {
                            ProcessNetworkInfo(info);
                        }
                    }

                    void ProcessNetworkInfo(NetworkInfo info)
                    {
                        if (info == null || !info.IsAvailable)
                            return;
                        if (info.IsConnected)
                            currentAccess = IsBetterAccess(currentAccess, NetworkAccess.Internet);
                        else if (info.IsConnectedOrConnecting)
                            currentAccess = IsBetterAccess(currentAccess, NetworkAccess.ConstrainedInternet);
                    }

                    return currentAccess;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Unable to get connected state - do you have ACCESS_NETWORK_STATE permission? - error: {0}", e);
                    return NetworkAccess.Unknown;
                }
            }
        }

        static IEnumerable<ConnectionProfile> PlatformConnectionProfiles
        {
            get
            {
                Permissions.EnsureDeclared(PermissionType.NetworkState);

                var manager = Platform.ConnectivityManager;
                if (Platform.HasApiLevel(BuildVersionCodes.Lollipop))
                {
                    foreach (var network in manager.GetAllNetworks())
                    {
                        NetworkInfo info = null;
                        try
                        {
                            info = manager.GetNetworkInfo(network);
                        }
                        catch
                        {
                            // there is a possibility, but don't worry about it
                        }

                        var p = ProcessNetworkInfo(info);
                        if (p.HasValue)
                            yield return p.Value;
                    }
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    foreach (var info in manager.GetAllNetworkInfo())
#pragma warning restore CS0618 // Type or member is obsolete
                    {
                        var p = ProcessNetworkInfo(info);
                        if (p.HasValue)
                            yield return p.Value;
                    }
                }

                ConnectionProfile? ProcessNetworkInfo(NetworkInfo info)
                {
                    if (info == null || !info.IsAvailable || !info.IsConnectedOrConnecting)
                        return null;

                    return GetConnectionType(info.Type, info.TypeName);
                }
            }
        }

        internal static ConnectionProfile GetConnectionType(ConnectivityType connectivityType, string typeName)
        {
            switch (connectivityType)
            {
                case ConnectivityType.Ethernet:
                    return ConnectionProfile.Ethernet;
                case ConnectivityType.Wifi:
                    return ConnectionProfile.WiFi;
                case ConnectivityType.Bluetooth:
                    return ConnectionProfile.Bluetooth;
                case ConnectivityType.Wimax:
                case ConnectivityType.Mobile:
                case ConnectivityType.MobileDun:
                case ConnectivityType.MobileHipri:
                case ConnectivityType.MobileMms:
                    return ConnectionProfile.Cellular;
                case ConnectivityType.Dummy:
                    return ConnectionProfile.Unknown;
                default:
                    if (string.IsNullOrWhiteSpace(typeName))
                        return ConnectionProfile.Unknown;

                    var typeNameLower = typeName.ToLowerInvariant();
                    if (typeNameLower.Contains("mobile"))
                        return ConnectionProfile.Cellular;

                    if (typeNameLower.Contains("wimax"))
                        return ConnectionProfile.Cellular;

                    if (typeNameLower.Contains("wifi"))
                        return ConnectionProfile.WiFi;

                    if (typeNameLower.Contains("ethernet"))
                        return ConnectionProfile.Ethernet;

                    if (typeNameLower.Contains("bluetooth"))
                        return ConnectionProfile.Bluetooth;

                    return ConnectionProfile.Unknown;
            }
        }
    }

    [BroadcastReceiver(Enabled = true, Exported = false, Label = "Essentials Connectivity Broadcast Receiver")]
    class ConnectivityBroadcastReceiver : BroadcastReceiver
    {
        Action onChanged;

        public ConnectivityBroadcastReceiver()
        {
        }

        public ConnectivityBroadcastReceiver(Action onChanged) =>
            this.onChanged = onChanged;

        public override async void OnReceive(Android.Content.Context context, Intent intent)
        {
            if (intent.Action != ConnectivityManager.ConnectivityAction)
                return;

            // await 500ms to ensure that the the connection manager updates
            await Task.Delay(500);
            onChanged?.Invoke();
        }
    }
}

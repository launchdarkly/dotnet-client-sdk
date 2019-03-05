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

namespace LaunchDarkly.Xamarin.Connectivity
{
    public static partial class Connectivity
    {
        static ReachabilityListener listener;

        static void StartListeners()
        {
            listener = new ReachabilityListener();
            listener.ReachabilityChanged += OnConnectivityChanged;
        }

        static void StopListeners()
        {
            if (listener == null)
                return;

            listener.ReachabilityChanged -= OnConnectivityChanged;
            listener.Dispose();
            listener = null;
        }

        static NetworkAccess PlatformNetworkAccess
        {
            get
            {
                var internetStatus = Reachability.InternetConnectionStatus();
                if (internetStatus == NetworkStatus.ReachableViaCarrierDataNetwork || internetStatus == NetworkStatus.ReachableViaWiFiNetwork)
                    return NetworkAccess.Internet;

                var remoteHostStatus = Reachability.RemoteHostStatus();
                if (remoteHostStatus == NetworkStatus.ReachableViaCarrierDataNetwork || remoteHostStatus == NetworkStatus.ReachableViaWiFiNetwork)
                    return NetworkAccess.Internet;

                return NetworkAccess.None;
            }
        }

        static IEnumerable<ConnectionProfile> PlatformConnectionProfiles
        {
            get
            {
                var statuses = Reachability.GetActiveConnectionType();
                foreach (var status in statuses)
                {
                    switch (status)
                    {
                        case NetworkStatus.ReachableViaCarrierDataNetwork:
                            yield return ConnectionProfile.Cellular;
                            break;
                        case NetworkStatus.ReachableViaWiFiNetwork:
                            yield return ConnectionProfile.WiFi;
                            break;
                        default:
                            yield return ConnectionProfile.Unknown;
                            break;
                    }
                }
            }
        }
    }
}

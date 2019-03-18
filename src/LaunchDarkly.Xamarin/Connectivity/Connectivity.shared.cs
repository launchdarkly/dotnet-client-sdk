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
using System.Linq;

namespace LaunchDarkly.Xamarin.Connectivity
{
    public static partial class Connectivity
    {
        static event EventHandler<ConnectivityChangedEventArgs> ConnectivityChangedInternal;

        // a cache so that events aren't fired unnecessarily
        // this is mainly an issue on Android, but we can stiil do this everywhere
        static NetworkAccess currentAccess;
        static List<ConnectionProfile> currentProfiles;

        public static NetworkAccess NetworkAccess => PlatformNetworkAccess;

        public static IEnumerable<ConnectionProfile> ConnectionProfiles => PlatformConnectionProfiles.Distinct();

        public static event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged
        {
            add
            {
                var wasRunning = ConnectivityChangedInternal != null;

                ConnectivityChangedInternal += value;

                if (!wasRunning && ConnectivityChangedInternal != null)
                {
                    SetCurrent();
                    StartListeners();
                }
            }

            remove
            {
                var wasRunning = ConnectivityChangedInternal != null;

                ConnectivityChangedInternal -= value;

                if (wasRunning && ConnectivityChangedInternal == null)
                    StopListeners();
            }
        }

        static void SetCurrent()
        {
            currentAccess = NetworkAccess;
            currentProfiles = new List<ConnectionProfile>(ConnectionProfiles);
        }

        static void OnConnectivityChanged(NetworkAccess access, IEnumerable<ConnectionProfile> profiles)
            => OnConnectivityChanged(new ConnectivityChangedEventArgs(access, profiles));

        static void OnConnectivityChanged()
            => OnConnectivityChanged(NetworkAccess, ConnectionProfiles);

        static void OnConnectivityChanged(ConnectivityChangedEventArgs e)
        {
            if (currentAccess != e.NetworkAccess || !currentProfiles.SequenceEqual(e.ConnectionProfiles))
            {
                SetCurrent();
                MainThread.MainThread.BeginInvokeOnMainThread(() => ConnectivityChangedInternal?.Invoke(null, e));
            }
        }
    }

    public class ConnectivityChangedEventArgs : EventArgs
    {
        public ConnectivityChangedEventArgs(NetworkAccess access, IEnumerable<ConnectionProfile> connectionProfiles)
        {
            NetworkAccess = access;
            ConnectionProfiles = connectionProfiles;
        }

        public NetworkAccess NetworkAccess { get; }

        public IEnumerable<ConnectionProfile> ConnectionProfiles { get; }

        public override string ToString() =>
            $"{nameof(NetworkAccess)}: {NetworkAccess}, " +
            $"{nameof(ConnectionProfiles)}: [{string.Join(", ", ConnectionProfiles)}]";
    }
}

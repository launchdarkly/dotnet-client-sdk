using System;
using System.Collections.Generic;
using System.Text;
using LaunchDarkly.Client;

namespace LaunchDarkly.Xamarin.Tests
{
    class TestUtil
    {
        // Any tests that are going to access the static LdClient.Instance must hold this lock,
        // to avoid interfering with tests that use CreateClient.
        public static readonly object ClientInstanceLock = new object();
        
        // Calls LdClient.Init, but then sets LdClient.Instance to null so other tests can
        // instantiate their own independent clients. Application code cannot do this because
        // the LdClient.Instance setter has internal scope.
        public static LdClient CreateClient(Configuration config, User user)
        {
            lock (ClientInstanceLock)
            {
                LdClient client = LdClient.Init(config, user);
                LdClient.Instance = null;
                return client;
            }
        }
    }
}

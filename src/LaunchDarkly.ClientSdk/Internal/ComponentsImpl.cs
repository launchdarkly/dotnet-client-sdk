using System;
using System.Threading.Tasks;
using LaunchDarkly.Sdk.Client.Interfaces;

namespace LaunchDarkly.Sdk.Client.Internal
{
    internal static class ComponentsImpl
    {
        internal sealed class NullDataSourceFactory : IDataSourceFactory
        {
            public IDataSource CreateDataSource(
                LdClientContext context,
                IDataSourceUpdateSink updateSink,
                User currentUser,
                bool inBackground
                ) =>
                new NullDataSource();
        }

        internal sealed class NullDataSource : IDataSource
        {
            public bool Initialized => true;

            public void Dispose() { }

            public Task<bool> Start() => Task.FromResult(true);
        }
    }
}

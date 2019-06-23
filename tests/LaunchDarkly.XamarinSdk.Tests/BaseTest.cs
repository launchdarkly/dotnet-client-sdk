using System;
using System.Threading.Tasks;
using Common.Logging;
using WireMock.Server;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    [Collection("serialize all tests")]
    public class BaseTest
    {
        public BaseTest()
        {
            LogManager.Adapter = new LogSinkFactoryAdapter();
            TestUtil.ClearClient();
        }

        ~BaseTest()
        {
            TestUtil.ClearClient();
        }

        protected void WithServer(Action<FluentMockServer> a)
        {
            var s = FluentMockServer.Start();
            try
            {
                a(s);
            }
            finally
            {
                s.Stop();
            }
        }

         protected async Task WithServerAsync(Func<FluentMockServer, Task> a)
         {
             var s = FluentMockServer.Start();
             try
             {
                 await a(s);
             }
             finally
             {
                s.Stop();
             }
         }
    }
}

using System.Reflection;
using Android.App;
using Android.OS;
using LaunchDarkly.Sdk.Client.Tests;
using Xunit.Runners.UI;
using Xunit.Sdk;

// For more details about how this test project works, see CONTRIBUTING.md
namespace LaunchDarkly.Sdk.Client.Android.Tests
{
    [Activity(Label = "LaunchDarkly.Sdk.Client.Android.Tests", MainLauncher = true)]
    public class MainActivity : RunnerActivity
    {
        public MainActivity()
        {
            ResultChannel = new XunitConsoleLoggingResultChannel();
        }

        protected override void OnCreate(Bundle bundle)
        {
            AddExecutionAssembly(typeof(ExtensibilityPointFactory).Assembly);
            AddTestAssembly(Assembly.GetExecutingAssembly());

            AutoStart = true; // this is necessary in order for the CI test job to work

            base.OnCreate(bundle);
        }
    }
}

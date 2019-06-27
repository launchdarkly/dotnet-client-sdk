using System.Reflection;

using Android.App;
using Android.OS;
using Xunit.Runners.UI;
using Xunit.Sdk;

namespace LaunchDarkly.Xamarin.Android.Tests
{
    [Activity(Label = "LaunchDarkly.Xamarin.Android.Tests", MainLauncher = true)]
    public class MainActivity : RunnerActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            AddExecutionAssembly(typeof(ExtensibilityPointFactory).Assembly);
            AddTestAssembly(Assembly.GetExecutingAssembly());

            AutoStart = true;
            //TerminateAfterExecution = true;

            base.OnCreate(bundle);
        }
    }
}

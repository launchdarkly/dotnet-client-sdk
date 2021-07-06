using System.Reflection;
using Foundation;
using UIKit;

using Xunit.Runner;
using Xunit.Sdk;

namespace LaunchDarkly.Xamarin.iOS.Tests
{
    // This is based on code that was generated automatically by the xunit.runner.devices package.
    // It configures the test-runner app that is implemented by that package, telling it where to
    // find the tests, and also configuring it to run them immediately and then quit rather than
    // waiting for user input. Output from the test run goes to the system log.

    [Register("AppDelegate")]
    public partial class AppDelegate : RunnerAppDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            AddExecutionAssembly(typeof(ExtensibilityPointFactory).Assembly);
            AddTestAssembly(Assembly.GetExecutingAssembly());

            AutoStart = true; // this is necessary in order for the CI test job to work

            return base.FinishedLaunching(app, options);
        }
    }
}
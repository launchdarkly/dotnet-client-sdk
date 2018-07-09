using System;
using LaunchDarkly.Common;

namespace LaunchDarkly.Xamarin
{
    internal class MobileClientEnvironment : ClientEnvironment
    {
        internal static readonly MobileClientEnvironment Instance =
            new MobileClientEnvironment();

        public override string UserAgentType { get { return "XamarinClient"; } }
    }
}

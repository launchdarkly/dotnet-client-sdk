using System;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Internal;

namespace LaunchDarkly.Sdk.Client.Internal
{
    internal sealed class FlagTrackerImpl : IFlagTracker
    {
        public event EventHandler<FlagValueChangeEvent> FlagValueChanged;

        private readonly Logger _log;

        internal FlagTrackerImpl(
            Logger log
            )
        {
            _log = log;
        }

        internal void FireEvent(FlagValueChangeEvent ev)
        {
            var copyOfHandlers = FlagValueChanged;
            var sender = this;
            if (copyOfHandlers != null)
            {
                foreach (var h in copyOfHandlers.GetInvocationList())
                {
                    // Note, this schedules the listeners separately, rather than scheduling a single task that runs them all.
                    PlatformSpecific.AsyncScheduler.ScheduleAction(() =>
                    {
                        try
                        {
                            h.DynamicInvoke(sender, ev);
                        }
                        catch (Exception e)
                        {
                            LogHelpers.LogException(_log, "Unexpected exception from FlagValueChanged event handler", e);
                        }
                    });
                }
            }
        }
    }
}

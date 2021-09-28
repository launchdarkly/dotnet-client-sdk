using System;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Internal;

namespace LaunchDarkly.Sdk.Client.Internal
{
    internal sealed class FlagTrackerImpl : IFlagTracker
    {
        public event EventHandler<FlagValueChangeEvent> FlagValueChanged;

        private readonly TaskExecutor _taskExecutor;
        private readonly Logger _log;

        internal FlagTrackerImpl(
            TaskExecutor taskExecutor,
            Logger log
            )
        {
            _taskExecutor = taskExecutor;
            _log = log;
        }

        internal void FireEvent(FlagValueChangeEvent ev)
        {
            var copyOfHandlers = FlagValueChanged;
            if (copyOfHandlers != null)
            {
                _taskExecutor.ScheduleEvent(ev, copyOfHandlers);
            }
        }
    }
}

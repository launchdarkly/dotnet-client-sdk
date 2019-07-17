using System;
using System.Linq;
using Common.Logging;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Xamarin
{
    public class FlagChangedEventArgs
    {
        public string Key { get; private set; }

        public JToken NewValue { get; private set; }

        public JToken OldValue { get; private set; }

        public bool FlagWasDeleted { get; private set; }

        public bool NewBoolValue => NewValue.Value<bool>();

        public string NewStringValue => NewValue.Value<string>();

        public int NewIntValue => NewValue.Value<int>();

        public float NewFloatValue => NewValue.Value<float>();

        internal FlagChangedEventArgs(string key, JToken newValue, JToken oldValue, bool flagWasDeleted)
        {
            Key = key;
            NewValue = newValue;
            OldValue = oldValue;
            FlagWasDeleted = flagWasDeleted;
        }
    }

    internal interface IFlagChangedEventManager
    {
        event EventHandler<FlagChangedEventArgs> FlagChanged;
        void FlagWasDeleted(string flagKey, JToken oldValue);
        void FlagWasUpdated(string flagKey, JToken newValue, JToken oldValue);
    }

    internal class FlagChangedEventManager : IFlagChangedEventManager
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(IFlagChangedEventManager));

        public event EventHandler<FlagChangedEventArgs> FlagChanged;

        public bool IsHandlerRegistered(EventHandler<FlagChangedEventArgs> handler)
        {
            return FlagChanged != null && FlagChanged.GetInvocationList().Contains(handler);
        }

        public void FlagWasDeleted(string flagKey, JToken oldValue)
        {
            FireEvent(new FlagChangedEventArgs(flagKey, null, oldValue, true));
        }

        public void FlagWasUpdated(string flagKey, JToken newValue, JToken oldValue)
        {
            FireEvent(new FlagChangedEventArgs(flagKey, newValue, oldValue, false));
        }

        private void FireEvent(FlagChangedEventArgs eventArgs)
        {
            var copyOfHandlers = FlagChanged;
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
                            h.DynamicInvoke(sender, eventArgs);
                        }
                        catch (Exception e)
                        {
                            Log.Warn("Unexpected exception from FlagChanged event handler", e);
                        }
                    });
                }
            }
        }
    }
}

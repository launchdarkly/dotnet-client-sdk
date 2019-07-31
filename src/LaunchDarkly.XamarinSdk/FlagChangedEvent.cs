using System;
using System.Linq;
using Common.Logging;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Xamarin
{
    /// <summary>
    /// An event object that is sent to handlers for the <see cref="ILdMobileClient.FlagChanged"/> event.
    /// </summary>
    public class FlagChangedEventArgs
    {
        /// <summary>
        /// The unique key of the feature flag whose value has changed.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// The updated value of the flag for the current user.
        /// </summary>
        /// <remarks>
        /// Since flag values can be of any JSON type, this property is a <see cref="JToken"/>. The properties
        /// <see cref="NewBoolValue"/>, <see cref="NewIntValue"/>, etc. are shortcuts for accessing its value with a
        /// specific data type.
        ///
        /// Flag evaluations always produce non-null values, but this property could still be null if the flag was
        /// completely deleted or if it could not be evaluated due to an error of some kind.
        ///
        /// Note that in those cases, the <c>Variation</c> methods may return a different result from this property,
        /// because of their "default value" behavior. For instance, if the flag "feature1" has been deleted, the
        /// following expression will return the string "xyz", because that is the default value that you specified
        /// in the method call:
        ///
        /// <code>
        /// client.StringVariation("feature1", "xyz");
        /// </code>
        ///
        /// But when a <c>FlagChangedEvent</c> is sent for the deletion of the flag, it has no way to know that you
        /// would have specified "xyz" as a default value when evaluating the flag, so <c>NewValue</c> will simply
        /// be null.
        /// </remarks>
        public JToken NewValue { get; private set; }

        /// <summary>
        /// The last known value of the flag for the current user prior to the update.
        /// </summary>
        public JToken OldValue { get; private set; }

        /// <summary>
        /// True if the flag was completely removed from the environment.
        /// </summary>
        public bool FlagWasDeleted { get; private set; }

        /// <summary>
        /// Shortcut for converting <see cref="NewValue"/> to a bool. Returns false if the value is null or is not a
        /// boolean (will never throw an exception).
        /// </summary>
        public bool NewBoolValue => AsType(ValueTypes.Bool, false);

        /// <summary>
        /// Shortcut for converting <see cref="NewValue"/> to a string. Returns null if the value is null or is not a
        /// string (will never throw an exception).
        /// </summary>
        public string NewStringValue => AsType(ValueTypes.String, null);

        /// <summary>
        /// Shortcut for converting <see cref="NewValue"/> to an int. Returns 0 if the value is null or is not
        /// numeric (will never throw an exception).
        /// </summary>
        public int NewIntValue => AsType(ValueTypes.Int, 0);

        /// <summary>
        /// Shortcut for converting <see cref="NewValue"/> to a float. Returns 0 if the value is null or is not
        /// numeric (will never throw an exception).
        /// </summary>
        public float NewFloatValue => AsType(ValueTypes.Float, 0);

        private T AsType<T>(ValueType<T> valueType, T defaultValue)
        {
            if (NewValue != null)
            {
                try
                {
                    return valueType.ValueFromJson(NewValue);
                }
                catch (ArgumentException) {}
            }
            return defaultValue;
        }

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
                            Log.Debug(e, e);
                        }
                    });
                }
            }
        }
    }
}

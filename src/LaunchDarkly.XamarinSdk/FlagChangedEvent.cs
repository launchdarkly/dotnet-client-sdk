using System;
using System.Linq;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Internal;

namespace LaunchDarkly.Sdk.Xamarin
{
    /// <summary>
    /// An event object that is sent to handlers for the <see cref="ILdClient.FlagChanged"/> event.
    /// </summary>
    public sealed class FlagChangedEventArgs
    {
        /// <summary>
        /// The unique key of the feature flag whose value has changed.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// The updated value of the flag for the current user.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Since flag values can be of any JSON type, this property is an <see cref="LdValue"/>. You
        /// can use properties and methods of <see cref="LdValue"/> such as <see cref="LdValue.AsBool"/>
        /// to convert it to other types.
        /// </para>
        /// <para>
        /// Flag evaluations always produce non-null values, but this property could still be <see cref="LdValue.Null"/>
        ///  if the flag was completely deleted or if it could not be evaluated due to an error of some kind.
        /// </para>
        /// <para>
        /// Note that in those cases, the Variation methods may return a different result from this property,
        /// because of their "default value" behavior. For instance, if the flag "feature1" has been deleted, the
        /// following expression will return the string "xyz", because that is the default value that you specified
        /// in the method call:
        /// </para>
        /// <code>
        ///     client.StringVariation("feature1", "xyz");
        /// </code>
        /// <para>
        /// But when an event is sent for the deletion of the flag, it has no way to know that you would have
        /// specified "xyz" as a default value when evaluating the flag, so <see cref="NewValue"/> will simply
        /// contain a <see langword="null"/>.
        /// </para>
        /// </remarks>
        public LdValue NewValue { get; private set; }

        /// <summary>
        /// The last known value of the flag for the current user prior to the update.
        /// </summary>
        public LdValue OldValue { get; private set; }

        /// <summary>
        /// True if the flag was completely removed from the environment.
        /// </summary>
        public bool FlagWasDeleted { get; private set; }

        internal FlagChangedEventArgs(string key, LdValue newValue, LdValue oldValue, bool flagWasDeleted)
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
        void FlagWasDeleted(string flagKey, LdValue oldValue);
        void FlagWasUpdated(string flagKey, LdValue newValue, LdValue oldValue);
    }

    internal sealed class FlagChangedEventManager : IFlagChangedEventManager
    {
        private readonly Logger _log;

        public event EventHandler<FlagChangedEventArgs> FlagChanged;

        internal FlagChangedEventManager(Logger log)
        {
            _log = log;
        }

        public bool IsHandlerRegistered(EventHandler<FlagChangedEventArgs> handler)
        {
            return FlagChanged != null && FlagChanged.GetInvocationList().Contains(handler);
        }

        public void FlagWasDeleted(string flagKey, LdValue oldValue)
        {
            FireEvent(new FlagChangedEventArgs(flagKey, LdValue.Null, oldValue, true));
        }

        public void FlagWasUpdated(string flagKey, LdValue newValue, LdValue oldValue)
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
                            LogHelpers.LogException(_log, "Unexpected exception from FlagChanged event handler", e);
                        }
                    });
                }
            }
        }
    }
}

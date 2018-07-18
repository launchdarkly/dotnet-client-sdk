using System.Collections.Generic;
using LaunchDarkly.Client;

namespace LaunchDarkly.Xamarin.Tests
{
    public class MockEventProcessor : IEventProcessor
    {
        public List<Event> Events = new List<Event>();

        public void SendEvent(Event e)
        {
            Events.Add(e);
        }

        public void Flush() { }

        public void Dispose() { }
    }
}

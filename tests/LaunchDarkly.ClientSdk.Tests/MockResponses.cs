using LaunchDarkly.TestHelpers.HttpTest;

using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;

namespace LaunchDarkly.Sdk.Client
{
    public static class MockResponses
    {
        public static Handler Error401Response => Handlers.Status(401);

        public static Handler Error503Response => Handlers.Status(503);

        public static Handler EventsAcceptedResponse => Handlers.Status(202);

        public static Handler PollingResponse(FullDataSet? data = null) =>
            Handlers.BodyJson((data ?? DataSetBuilder.Empty).ToJsonString());

        public static Handler StreamWithInitialData(FullDataSet? data = null) =>
            Handlers.SSE.Start()
                .Then(PutEvent(data))
                .Then(Handlers.SSE.LeaveOpen());

        public static Handler PutEvent(FullDataSet? data = null) =>
            Handlers.SSE.Event(
                "put",
                (data ?? DataSetBuilder.Empty).ToJsonString()
                );
    }
}

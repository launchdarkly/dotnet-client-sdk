using Xunit;

// We must disable all parallel test running by XUnit in order for LogSink to work.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

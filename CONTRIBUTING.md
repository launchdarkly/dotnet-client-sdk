Contributing to the LaunchDarkly Client-side SDK for Xamarin
================================================

LaunchDarkly has published an [SDK contributor's guide](https://docs.launchdarkly.com/docs/sdk-contributors-guide) that provides a detailed explanation of how our SDKs work. See below for additional information on how to contribute to this SDK.

Submitting bug reports and feature requests
------------------

The LaunchDarkly SDK team monitors the [issue tracker](https://github.com/launchdarkly/xamarin-client-sdk/issues) in the SDK repository. Bug reports and feature requests specific to this SDK should be filed in this issue tracker. The SDK team will respond to all newly filed issues within two business days.

Submitting pull requests
------------------

We encourage pull requests and other contributions from the community. Before submitting pull requests, ensure that all temporary or unintended code is removed. Don't worry about adding reviewers to the pull request; the LaunchDarkly SDK team will add themselves. The SDK team will acknowledge all pull requests within two business days.

Build instructions
------------------

### Prerequisites

The .NET Standard 1.6 and 2.0 targets require only the .NET Core 2.0 SDK, while the iOS and Android targets require the corresponding Xamarin SDKs.

To set up the project and dependencies, run the following command in the root SDK directory:

```
dotnet restore
```

### Building

To build the SDK (for all platforms) without running any tests:

```
msbuild src/LaunchDarkly.XamarinSdk/LaunchDarkly.XamarinSdk.csproj
```

### Testing

To build the .NET Standard 2.0 target and run the basic unit tests, which cover all of the non-platform-specific functionality:
```
dotnet build src/LaunchDarkly.XamarinSdk -f netstandard2.0
dotnet test tests/LaunchDarkly.XamarinSdk.Tests/LaunchDarkly.XamarinSdk.Tests.csproj -f netcoreapp2.0
```

Currently the Android and iOS test projects can only be run from Visual Studio in MacOS.

# Change log

All notable changes to the LaunchDarkly Client-side SDK for Xamarin will be documented in this file.
This project adheres to [Semantic Versioning](http://semver.org).

# Note on future releases

The LaunchDarkly SDK repositories are being renamed for consistency. This repository is now `xamarin-client-sdk` rather than `xamarin-client`.

The package name will also change. In the 1.0.0-beta16 release, the published package was `LaunchDarkly.Xamarin`; in all future releases, it will be `LaunchDarkly.XamarinSdk`.

## [1.0.0-beta17] - 2019-05-15
### Changed:
- The NuGet package name for this SDK is now `LaunchDarkly.XamarinSdk`. There are no other changes. Substituting `LaunchDarkly.Xamarin` 1.0.0-beta16 with `LaunchDarkly.XamarinSdk` 1.0.0-beta17 should not affect functionality.

## [1.0.0-beta16] - 2019-04-05
### Added:
- In Android and iOS, when an app is in the background, the SDK should turn off the streaming connection and instead poll for flag updates at an interval determined by `Configuration.BackgroundPollingInterval` (default: 60 minutes).
- The SDK now supports [evaluation reasons](https://docs.launchdarkly.com/docs/evaluation-reasons). See `Configuration.WithEvaluationReasons` and `ILdMobileClient.BoolVariationDetail`.
- The SDK now sends custom attributes called `os` and `device` as part of the user data, indicating the user's platform and OS version. This is the same as what the native Android and iOS SDKs do, except that "iOS" or "Android" is also prepended to the `os` property.
### Changed:
- This is the first version that is built specifically for iOS and Android platforms. There is also still a .NET Standard 1.0 build in the same package.
- The SDK no longer uses Xamarin Essentials.
### Fixed:
- Under some circumstances, a `CancellationTokenSource` object could be leaked.
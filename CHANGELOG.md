# Change log

All notable changes to the LaunchDarkly Client-side SDK for Xamarin will be documented in this file.
This project adheres to [Semantic Versioning](http://semver.org).

## [1.1.0] - 2019-10-10
### Added:
- Added support for upcoming LaunchDarkly experimentation features. See `ILdClient.Track(string, LdValue, double)`.
- `User.AnonymousOptional` and `IUserBuilder.AnonymousOptional` allow treating the `Anonymous` property as nullable (necessary for consistency with other SDKs). See note about this under Fixed.
- Added `LaunchDarkly.Logging.ConsoleAdapter` as a convenience for quickly enabling console logging; this is equivalent to `Common.Logging.Simple.ConsoleOutLoggerFactoryAdapter`, but the latter is not available on some platforms.
 
### Fixed:
- `Configuration.Builder` was not setting a default value for the `BackgroundPollingInterval` property. As a result, if you did not set the property explicitly, the SDK would throw an error when the application went into the background on mobile platforms.
- `IUserBuilder` was incorrectly setting the user's `Anonymous` property to `null` even if it had been explicitly set to `false`. Null and false behave the same in terms of LaunchDarkly's user indexing behavior, but currently it is possible to create a feature flag rule that treats them differently. So `IUserBuilder.Anonymous(false)` now correctly sets it to `false`, just as the deprecated method `UserExtensions.WithAnonymous(false)` would.
- `LdValue.Convert.Long` was mistakenly converting to an `int` rather than a `long`. (CommonSdk [#32](https://github.com/launchdarkly/dotnet-sdk-common/issues/32))

## [1.0.0] - 2019-09-13

First GA release.

For release notes on earlier beta versions, see the [beta changelog](https://github.com/launchdarkly/xamarin-client-sdk/blob/1.0.0-beta24/CHANGELOG.md).

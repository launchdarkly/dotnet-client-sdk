# Change log

All notable changes to the LaunchDarkly Client-Side SDK for Xamarin will be documented in this file.
This project adheres to [Semantic Versioning](http://semver.org).

## [1.2.0] - 2020-01-15
### Added:
- Added `ILdClient` extension methods `EnumVariation` and `EnumVariationDetail`, which convert strings to enums.
- `User.Secondary`, `IUserBuilder.Secondary` (replaces `SecondaryKey`).
- `EvaluationReason` static methods and properties for creating reason instances.
- `LdValue` helpers for dealing with array/object values, without having to use an intermediate `List` or `Dictionary`: `BuildArray`, `BuildObject`, `Count`, `Get`.
- `LdValue.Parse()`.
 
### Changed:
- `EvaluationReason` properties all exist on the base class now, so for instance you do not need to cast to `RuleMatch` to get the `RuleId` property. This is in preparation for a future API change in which `EvaluationReason` will become a struct instead of a base class.
 
### Fixed:
- Calling `Identify` or `IdentifyAsync` with a user that has a null key and `Anonymous(true)`-- which should generate a unique key for the anonymous user-- did not work. The symptom was that the client would fail to retrieve the flags, and the call would either never complete (for `IdentifyAsync`) or time out (for `Identify`). This has been fixed.
- Improved memory usage and performance when processing analytics events: the SDK now encodes event data to JSON directly, instead of creating intermediate objects and serializing them via reflection.
- When parsing arbitrary JSON values, the SDK now always stores them internally as `LdValue` rather than `JToken`. This means that no additional copying step is required when the application accesses that value, if it is of a complex type.
- `LdValue.Equals()` incorrectly returned true for object (dictionary) values that were not equal.
- The SDK now specifies a uniquely identifiable request header when sending events to LaunchDarkly to ensure that events are only processed once, even if the SDK sends them two times due to a failed initial attempt.
 
### Deprecated:
- `IUserBuilder.SecondaryKey`, `User.SecondaryKey`.
- `EvaluationReason` subclasses. Use only the base class properties and methods to ensure compatibility with future versions.

## [1.1.1] - 2019-10-23
### Fixed:
- The JSON serialization of `User` was producing an extra `Anonymous` property in addition to `anonymous`. If Newtonsoft.Json was configured globally to force all properties to lowercase, this would cause an exception when serializing a user since the two properties would end up with the same name. ([#22](https://github.com/launchdarkly/xamarin-client-sdk/issues/22))

## [1.1.0] - 2019-10-17
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

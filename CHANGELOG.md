# Change log

All notable changes to the LaunchDarkly Client-Side SDK for .NET will be documented in this file.
This project adheres to [Semantic Versioning](http://semver.org).

## [2.0.0] - 2022-01-07
This is a major rewrite that introduces a cleaner API design, adds new features, and makes the SDK code easier to maintain and extend. See the [Xamarin 1.x to client-side .NET 2.0 migration guide](https://docs.launchdarkly.com/sdk/client-side/dotnet/migration-1-to-2) for an in-depth look at the changes in 2.0; the following is a summary.

The LaunchDarkly client-side .NET SDK was formerly known as the LaunchDarkly Xamarin SDK. Xamarin for Android and iOS are _among_ its supported platforms, but it can also be used on any platform that supports .NET Core 2+, .NET Standard 2, or .NET 5+. On those platforms, it does not use any Xamarin-specific runtime libraries. To learn more about the distinction between the client-side .NET SDK and the server-side .NET SDK, read: [Client-side and server-side SDKs](https://docs.launchdarkly.com/sdk/concepts/client-side-server-side)

### Added:
- `LdClient.FlagTracker` provides the ability to get notifications when flag values have changed.
- `LdClient.DataSourceStatusProvider` provides information on the status of the SDK's data source (which normally means the streaming connection to the LaunchDarkly service).
- `LdClient.DoubleVariation` and `DoubleVariationDetail` return a numeric flag variation using double-precision floating-point.
- `ConfigurationBuilder.ServiceEndpoints` allows you to override the regular service URIs— as you may want to do if you are using the LaunchDarkly Relay Proxy, for instance— in a single place. Previously, the URIs had to be specified individually for each service (`StreamingDataSource().BaseURI`, `SendEvents().BaseURI`, etc.).
- `HttpConfigurationBuilder.UseReport` tells the SDK to make HTTP `REPORT` requests rather than `GET` requests to the LaunchDarkly service endpoints, which may be desirable in rare circumstances but is not available on all platforms.
- `ConfigurationBuilder.Persistence` and `PersistenceConfigurationBuilder.MaxCachedUsers` allow setting a limit on how many users' flag data can be saved in persistent local storage, or turning off persistence.
- The `LaunchDarkly.Sdk.Json` namespace provides methods for converting types like `User` and `FeatureFlagsState` to and from JSON.
- The `LaunchDarkly.Sdk.UserAttribute` type provides a less error-prone way to refer to user attribute names in configuration, and can also be used to get an arbitrary attribute from a user.
- The `LaunchDarkly.Sdk.UnixMillisecondTime` type provides convenience methods for converting to and from the Unix epoch millisecond time format that LaunchDarkly uses for all timestamp values.
- The SDK now periodically sends diagnostic data to LaunchDarkly, describing the version and configuration of the SDK, the architecture and version of the runtime platform, and performance statistics. No credentials, hostnames, or other identifiable values are included. This behavior can be disabled with `ConfigurationBuilder.DiagnosticOptOut` or configured with `ConfigurationBuilder.DiagnosticRecordingInterval`.

### Changed (requirements/dependencies/build):
- .NET Standard 1.6 is no longer supported.
- The SDK no longer has a dependency on `Common.Logging`. Instead, it uses a similar but simpler logging facade, the [`LaunchDarkly.Logging`](https://github.com/launchdarkly/dotnet-logging) package, which has adapters for various logging destinations.
- The SDK no longer has a dependency on the Json.NET library (a.k.a. `Newtonsoft.Json`), but instead uses a lightweight custom JSON serializer and deserializer. This removes the potential for dependency version conflicts in applications that use Json.NET for their own purposes, and reduces the number of dependencies in applications that do not use Json.NET. If you do use Json.NET and you want to use it with SDK data types like `User` and `LdValue`, see [`LaunchDarkly.CommonSdk.JsonNet`](https://github.com/launchdarkly/dotnet-sdk-common/tree/master/src/LaunchDarkly.CommonSdk.JsonNet). Those types also serialize/deserialize correctly with the `System.Text.Json` API on platforms where that API is available.
 
### Changed (API changes):
- The base namespace has changed: types that were previously in `LaunchDarkly.Client` are now in `LaunchDarkly.Sdk`, and types that were previously in `LaunchDarkly.Xamarin` are now in `LaunchDarkly.Sdk.Client`. The `LaunchDarkly.Sdk` namespace contains types that are not specific to the _client-side_ .NET SDK (that is, they are also used by the server-side .NET SDK): `EvaluationDetail`, `LdValue`, `User`, and `UserBuilder`. Types that are specific to the client-side .NET SDK, such as `Configuration` and `LdClient`, are in `LaunchDarkly.Sdk.Client`.
- `User` and `Configuration` objects are now immutable. To specify properties for these classes, you must now use `User.Builder` and `Configuration.Builder`.
- `Configuration.Builder` now returns a concrete type rather than an interface.
- `EvaluationDetail` is now a struct type rather than a class.
- `EvaluationReason` is now a single struct type rather than a base class with subclasses.
- `EvaluationReasonKind` and `EvaluationErrorKind` constants now use .Net-style naming (`RuleMatch`) rather than Java-style naming (`RULE_MATCH`). Their JSON representations are unchanged.
- The `ILdClient` interface is now in `LaunchDarkly.Sdk.Client.Interfaces` instead of the main namespace.
- The `ILdClientExtensions` methods `EnumVariation<T>` and `EnumVariationDetail<T>` now have type constraints to enforce that `T` really is an `enum` type.

### Changed (behavioral changes):
- The default event flush interval is now 30 seconds on mobile platforms, instead of 5 seconds. This is consistent with the other mobile SDKs and is intended to reduce network traffic.
- Logging now uses a simpler, more stable set of logger names instead of using the names of specific implementation classes that are subject to change. General messages are logged under `LaunchDarkly.Sdk`, while messages about specific areas of functionality are logged under that name plus `.DataSource` (streaming, polling, file data, etc.), `.DataStore` (database integrations), `.Evaluation` (unexpected errors during flag evaluations), or `.Events` (analytics event processing).

### Removed:
- All types and methods that were deprecated as of the last 1.x release have been removed.

## [2.0.0-rc.1] - 2021-11-19
This is the first release candidate version of the LaunchDarkly client-side .NET SDK 2.0-- a major rewrite that introduces a cleaner API design, adds new features, and makes the SDK code easier to maintain and extend. See the [Xamarin 1.x to client-side .NET 2.0 migration guide](https://docs.launchdarkly.com/sdk/client-side/dotnet/migration-1-to-2) for an in-depth look at the changes in 2.0; the following is a summary.

The LaunchDarkly client-side .NET SDK was formerly known as the LaunchDarkly Xamarin SDK. Xamarin for Android and iOS are _among_ its supported platforms, but it can also be used on any platform that supports .NET Core 2+, .NET Standard 2, or .NET 5+. On those platforms, it does not use any Xamarin-specific runtime libraries. To learn more about the distinction between the client-side .NET SDK and the server-side .NET SDK, read: [Client-side and server-side SDKs](https://docs.launchdarkly.com/sdk/concepts/client-side-server-side)

### Added:
- `LdClient.FlagTracker` provides the ability to get notifications when flag values have changed.
- `LdClient.DataSourceStatusProvider` provides information on the status of the SDK's data source (which normally means the streaming connection to the LaunchDarkly service).
- `LdClient.DoubleVariation` and `DoubleVariationDetail` return a numeric flag variation using double-precision floating-point.
- `HttpConfigurationBuilder.UseReport` tells the SDK to make HTTP `REPORT` requests rather than `GET` requests to the LaunchDarkly service endpoints, which may be desirable in rare circumstances but is not available on all platforms.
- `ConfigurationBuilder.Persistence` and `PersistenceConfigurationBuilder.MaxCachedUsers` allow setting a limit on how many users' flag data can be saved in persistent local storage, or turning off persistence.
- The `LaunchDarkly.Sdk.Json` namespace provides methods for converting types like `User` and `FeatureFlagsState` to and from JSON.
- The `LaunchDarkly.Sdk.UserAttribute` type provides a less error-prone way to refer to user attribute names in configuration, and can also be used to get an arbitrary attribute from a user.
- The `LaunchDarkly.Sdk.UnixMillisecondTime` type provides convenience methods for converting to and from the Unix epoch millisecond time format that LaunchDarkly uses for all timestamp values.
- The SDK now periodically sends diagnostic data to LaunchDarkly, describing the version and configuration of the SDK, the architecture and version of the runtime platform, and performance statistics. No credentials, hostnames, or other identifiable values are included. This behavior can be disabled with `ConfigurationBuilder.DiagnosticOptOut` or configured with `ConfigurationBuilder.DiagnosticRecordingInterval`.

### Changed (requirements/dependencies/build):
- .NET Standard 1.6 is no longer supported.
- The SDK no longer has a dependency on `Common.Logging`. Instead, it uses a similar but simpler logging facade, the [`LaunchDarkly.Logging`](https://github.com/launchdarkly/dotnet-logging) package, which has adapters for various logging destinations.
- The SDK no longer has a dependency on the Json.NET library (a.k.a. `Newtonsoft.Json`), but instead uses a lightweight custom JSON serializer and deserializer. This removes the potential for dependency version conflicts in applications that use Json.NET for their own purposes, and reduces the number of dependencies in applications that do not use Json.NET. If you do use Json.NET and you want to use it with SDK data types like `User` and `LdValue`, see [`LaunchDarkly.CommonSdk.JsonNet`](https://github.com/launchdarkly/dotnet-sdk-common/tree/master/src/LaunchDarkly.CommonSdk.JsonNet). Those types also serialize/deserialize correctly with the `System.Text.Json` API on platforms where that API is available.
 
### Changed (API changes):
- The base namespace has changed: types that were previously in `LaunchDarkly.Client` are now in `LaunchDarkly.Sdk`, and types that were previously in `LaunchDarkly.Xamarin` are now in `LaunchDarkly.Sdk.Client`. The `LaunchDarkly.Sdk` namespace contains types that are not specific to the _client-side_ .NET SDK (that is, they are also used by the server-side .NET SDK): `EvaluationDetail`, `LdValue`, `User`, and `UserBuilder`. Types that are specific to the client-side .NET SDK, such as `Configuration` and `LdClient`, are in `LaunchDarkly.Sdk.Client`.
- `User` and `Configuration` objects are now immutable. To specify properties for these classes, you must now use `User.Builder` and `Configuration.Builder`.
- `Configuration.Builder` now returns a concrete type rather than an interface.
- `EvaluationDetail` is now a struct type rather than a class.
- `EvaluationReason` is now a single struct type rather than a base class with subclasses.
- `EvaluationReasonKind` and `EvaluationErrorKind` constants now use .Net-style naming (`RuleMatch`) rather than Java-style naming (`RULE_MATCH`). Their JSON representations are unchanged.
- The `ILdClient` interface is now in `LaunchDarkly.Sdk.Client.Interfaces` instead of the main namespace.
- The `ILdClientExtensions` methods `EnumVariation<T>` and `EnumVariationDetail<T>` now have type constraints to enforce that `T` really is an `enum` type.

### Changed (behavioral changes):
- The default event flush interval is now 30 seconds on mobile platforms, instead of 5 seconds. This is consistent with the other mobile SDKs and is intended to reduce network traffic.
- Logging now uses a simpler, more stable set of logger names instead of using the names of specific implementation classes that are subject to change. General messages are logged under `LaunchDarkly.Sdk.Xamarin.LdClient`, while messages about specific areas of functionality are logged under that name plus `.DataSource` (streaming, polling, file data, etc.), `.DataStore` (database integrations), `.Evaluation` (unexpected errors during flag evaluations), or `.Events` (analytics event processing).
 
### Fixed:
- The SDK was deciding whether to send analytics events based on the `Offline` property of the _original_ SDK configuration, rather than whether the SDK is _currently_ in offline mode or not.
 
### Removed:
- All types and methods that were shown as deprecated/`Obsolete` in the last 1.x release have been removed.

## [1.2.2] - 2021-04-06
### Fixed:
- The SDK was failing to get flags in streaming mode when connecting to a LaunchDarkly Relay Proxy instance.

## [1.2.1] - 2021-03-30
### Fixed:
- Removed unnecessary dependencies on `Xamarin.Android.Support.Core.Utils` and `Xamarin.Android.Support.CustomTabs`. (Thanks, [Vladimir-Mischenchuk](https://github.com/launchdarkly/xamarin-client-sdk/pull/25)!)
- Setting custom base URIs now works correctly even if the base URI includes a path prefix (such as you might use with a reverse proxy that rewrites request URIs). ([#26](https://github.com/launchdarkly/xamarin-client-sdk/issues/26))
- Fixed the base64 encoding of user properties in request URIs to use the URL-safe variant of base64.

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

# Change log

All notable changes to the LaunchDarkly Client-side SDK for Xamarin will be documented in this file.
This project adheres to [Semantic Versioning](http://semver.org).

## [1.0.0-beta24] - 2019-09-13
### Added:
- `ImmutableJsonValue` now has methods for converting to or from a list or dictionary, rather than using the `Newtonsoft.Json` types `JArray` and `JObject`.
- HTML documentation for all public types, methods, and properties is now available [online](https://launchdarkly.github.io/xamarin-client-sdk).
 
### Changed:
- The SDK no longer has a dependency on [`Xam.Plugin.DeviceInfo`](https://github.com/jamesmontemagno/DeviceInfoPlugin).
- When accessing a floating-point flag value with `IntVariation`, or converting a floating-point `ImmutableJsonValue` to an `int`, it will now truncate (round toward zero) rather than rounding to the nearest integer. This is consistent with normal C# behavior and with most other LaunchDarkly SDKs.
 
### Removed:
- All public methods and properties now use `ImmutableJsonValue` instead of `JToken`.

### Fixed:
- Fixed a bug that caused a string flag value that is in an ISO date/time format, like "1970-01-01T00:00:01.001Z", to be treated as an incompatible type by `StringVariation` (because `Newtonsoft.Json` would parse it as a `DateTime` by default).
- On Android, an HTTP connection timeout could leave a connection attempt still happening in the background-- even though the timeout would still happen normally as far as the SDK was concerned-- until the default timeout for the Android HTTP handler elapsed, which is 24 hours. Now it will properly stop the connection attempt after a timeout.
- Fixed an implementation problem that caused excessive overhead for flag evaluations due to unnecessary JSON parsing.

## [1.0.0-beta23] - 2019-08-30
### Added:
- XML documentation comments are now included in the package, so they should be visible in Visual Studio for all LaunchDarkly types and methods.

### Changed:
- The `Online` property of `LdClient` was not useful because it could reflect either a deliberate change to whether the client is allowed to go online (that is, it would be false if you had set `Offline` to true in the configuration), _or_ a change in network availability. It has been removed and replaced with `Offline`, which is only used for explicitly forcing the client to be offline. This is a read-only property; to set it, use `SetOffline` or `SetOfflineAsync`.
- The synchronous `Identify` method now requires a timeout parameter, and returns false if it times out.
- `LdClient.Initialized` is now a property, not a method.
- `LdClient.Version` is now static, since it describes the entire package rather than a client instance.
- In `Configuration` and `IConfigurationBuilder`, `HttpClientTimeout` is now `ConnectionTimeout`.
- There is now more debug-level logging for stream connection state changes.

### Fixed:
- Network availability changes are now detected in both Android and iOS. The SDK should not attempt to connect to LaunchDarkly if the OS has told it that the network is unavailable.
- Background polling was never enabled, even if `Configuration.EnableBackgroundUpdating` was true.
- When changing from offline to online by setting `client.Online = true`, or calling `await client.SetOnlineAsync(true)` (the equivalent now would be `client.Offline = false`, etc.), the SDK was returning too soon before it had acquired flags from LaunchDarkly. The known issues in 1.0.0-beta22 have been fixed.
- If the SDK was online and then was explicitly set to be offline, or if network connectivity was lost, the SDK was still attempting to send analytics events. It will no longer do so.
- If the SDK was originally set to be offline and then was put online, the SDK was _not_ sending analytics events. Now it will.

### Removed:
- `ConfigurationBuilder.UseReport`. Due to [an issue](https://github.com/xamarin/xamarin-android/issues/3544) with the Android implementation of HTTP, the HTTP REPORT method is not currently usable in the Xamarin SDK.
- `IConnectionManager` interface. The SDK now always uses a platform-appropriate implementation of this logic.

## [1.0.0-beta22] - 2019-08-12
### Changed:
- By default, on Android and iOS the SDK now uses Xamarin's platform-specific implementations of `HttpMessageHandler` that are based on native APIs, rather than the basic `System.Net.Http.HttpClientHandler`. This improves performance and stability on mobile platforms.
- The behavior of the `Initialized()` method has changed to be more consistent with the other SDKs. Rather than only being true if there is currently an active connection to LaunchDarkly, it is now also true if you configured it in offline mode so that it will not attempt to connect to LaunchDarkly; in other words, it is only false if it has tried to connect and not yet succeeded.
- Also, instead of always returning default values whenever `Initialized()` is false, the SDK now returns a default value only if it does not already have a cached value for the flag. It will always use cached values if it has obtained any (for the current user).

### Known issues:
- Changing the `Online` property does not wait for the connection state to be updated; that is, if you were offline and then set `Online = true`, flag values may not be immediately available. It is equivalent to calling `SetOnlineAsync(true)` but _not_ waiting for the result.
- Starting with the configuration `Offline(true)` and then going online does not work.
- On mobile platforms, the SDK may not detect when network availability has changed. This means that if the network is unavailable, it may waste some CPU time on trying and failing to reconnect to LaunchDarkly.

## [1.0.0-beta21] - 2019-08-06
### Added:
- `Configuration.Builder` provides a fluent builder pattern for constructing `Configuration` objects. This is now the only method for building a configuration if you want to set properties other than the SDK key.
- `ImmutableJsonValue.Null` (equivalent to `ImmutableJsonValue.Of(null)`).
- `LdClient.PlatformType`.
- Verbose debug logging for stream connections.
 
### Changed:
- `Configuration` objects are now immutable.
- In `Configuration`, `EventQueueCapacity` and `EventQueueFrency` have been renamed to `EventCapacity` and `EventFlushInterval`, for consistency with other LaunchDarkly SDKs.
- `ImmutableJsonValue.FromJToken()` was renamed to `ImmutableJsonValue.Of()`.
- In `FlagChangedEventArgs`, `NewValue` and `OldValue` now have the type `ImmutableJsonValue` instead of `JToken`.
- `ILdMobileClient` is now named `ILdClient`.
 
### Fixed:
- Fixed a bug where setting a user's custom attribute to a null value could cause an exception during JSON serialization of event data.
 
### Removed:
- `ConfigurationExtensions` (use `Configuration.Builder`).
- `Configuration.SamplingInterval`.
- `UserExtensions` (use `User.Builder`).
- `User` constructors (use `User.WithKey` or `User.Builder`).
- `User` property setters.
- `IBaseConfiguration` and `ICommonLdClient` interfaces.

## [1.0.0-beta20] - 2019-08-06
Incomplete release, replaced by beta21.

## [1.0.0-beta19] - 2019-07-31
### Added:
- `User.Builder` provides a fluent builder pattern for constructing `User` objects. This is now the only method for building a user if you want to set properties other than `Key`.
- The `ImmutableJsonValue` type provides a wrapper for the Newtonsoft.Json types that prevents accidentally modifying JSON object properties or array values that are shared by other objects.
- `LdClient.PlatformType` allows you to verify that you have loaded the correct target platform version of the SDK.
 
### Changed:
- `User` objects are now immutable.
- In `User`, `IpAddress` has been renamed to `IPAddress` (standard .NET capitalization for two-letter acronyms).
- Custom attribute values in `User.Custom` are now returned as `ImmutableJsonValue` rather than `JToken`.
- JSON flag variations returned by `JsonVariation`, `JsonVariationDetail`, and `AllFlags`, are now `ImmutableJsonValue` rather than `JToken`.
- Setting additional data in a custom event with `Track` now uses `ImmutableJsonValue` rather than `JToken`.
- The mechanism for specifying a flag change listener has been changed to use the standard .NET event pattern. Instead of `client.RegisterFeatureFlagListener("flag-key", handler)` (where `handler` is an instance of some class that implements a particular interface), it is now `client.FlagChanged += handler` (where `handler` is an event handler method or function that will receive the flag key as part of `FlagChangedEventArgs`).
- Flag change listeners are now invoked asynchronously. This is to avoid the possibility of a deadlock if, for instance, application code triggers an action (such as Identify) that causes flags to be updated, which causes a flag change listener to be called, which then tries to access some resource that is being held by that same application code (e.g. trying to do an action on the main thread). On Android and iOS, these listeners are now guaranteed to be executed on the main thread, but only after any other current action on the main thread has completed. On .NET Standard, they are executed asynchronously with `Task.Run()`.
 
### Fixed:
- Previously, if you created a client with `LdClient.Init` and then called `Dispose()` on the client, it would fail because the SDK would think a singleton instance already exists. Now, disposing of the singleton returns the SDK to a state where you can create a client again.
- Fixed a `NullReferenceException` that could sometimes be thrown when transitioning from background to foreground in Android.
 
### Removed:
- `User` constructors (use `User.WithKey` or `User.Builder`).
- `User.IpAddress` (use `IPAddress`).
- `User` property setters, and the `UserExtension` methods for modifying properties (`AndName()`, etc.).

## [1.0.0-beta18] - 2019-07-02
### Added:
- New `Configuration` property `PersistFlagValues` (default: true) allows you to turn off the SDK's normal behavior of storing flag values locally so they can be used offline.
- Flag values are now stored locally in .NET Standard by default, on the filesystem, using the .NET `IsolatedStorageFile` mechanism.
- Added CI unit test suites that exercise most of the SDK functionality in .NET Standard, Android, and iOS. The tests do not currently cover background-mode detection and network connectivity detection on mobile platforms.

### Changed:
- `Configuration.WithUpdateProcessor` has been replaced with `Configuration.WithUpdateProcessorFactory`. These methods are for testing purposes and will not normally be used.

### Fixed:
- In .NET Standard, if you specify a user with `Key == null` and `Anonymous == true`, the SDK now generates a GUID for a user key and caches it in local storage for future reuse. This is consistent with the other client-side SDKs. Previously, it caused an exception.

### Removed:
- Several low-level component interfaces such as `IDeviceInfo` which had been exposed for testing are now internal.

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
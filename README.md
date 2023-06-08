# LaunchDarkly Client-Side SDK for .NET

[![NuGet](https://img.shields.io/nuget/v/LaunchDarkly.ClientSdk.svg?style=flat-square)](https://www.nuget.org/packages/LaunchDarkly.ClientSdk/)
[![CircleCI](https://circleci.com/gh/launchdarkly/dotnet-client-sdk.svg?style=shield)](https://circleci.com/gh/launchdarkly/dotnet-client-sdk)
[![Documentation](https://img.shields.io/static/v1?label=GitHub+Pages&message=API+reference&color=00add8)](https://launchdarkly.github.io/dotnet-client-sdk)

The LaunchDarkly Client-Side SDK for .NET is designed primarily for use by code that is deployed to an end user, such as in a desktop application or a smart device. It follows the client-side LaunchDarkly model for single-user contexts (much like our mobile or JavaScript SDKs). It is not intended for use in multi-user systems such as web servers and applications.

On supported mobile platforms (Android and iOS), the SDK uses the Xamarin framework which allows .NET code to run on those devices. For that reason, its name was previously "LaunchDarkly Xamarin SDK". However, Xamarin is not the only way to run .NET code in a client-side context (see "Supported platforms" below), so the SDK now has a more general name.

For using LaunchDarkly in *server-side* .NET applications, refer to our [Server-Side .NET SDK](https://github.com/launchdarkly/dotnet-server-sdk).

## LaunchDarkly overview

[LaunchDarkly](https://www.launchdarkly.com) is a feature management platform that serves trillions of feature flags daily to help teams build better software, faster. [Get started](https://docs.launchdarkly.com/home/getting-started) using LaunchDarkly today!
 
[![Twitter Follow](https://img.shields.io/twitter/follow/launchdarkly.svg?style=social&label=Follow&maxAge=2592000)](https://twitter.com/intent/follow?screen_name=launchdarkly)

## Supported platforms

This version of the SDK is built for the following targets:

* Xamarin Android 8.1, for use with Android 8.1 (Android API 27) and higher.
* Xamarin iOS 10, for use with iOS 10 and higher.
* .NET Standard 2.0, for use with any runtime platform that supports .NET Standard 2.0, or in portable .NET Standard library code.

The .NET Standard 2.0 target does not use any Xamarin packages and has no OS-specific code. This allows the SDK to be used in a desktop .NET Framework or .NET 5.0 application, or in a Xamarin MacOS application. However, due to the lack of OS-specific integration, SDK functionality will be limited in those environments: for instance, the SDK will not be able to detect whether networking is turned on or off.

The .NET build tools should automatically load the most appropriate build of the SDK for whatever platform your application or library is targeted to.

## Getting started

Refer to the [SDK documentation](https://docs.launchdarkly.com/sdk/client-side/dotnet) for instructions on getting started with using the SDK.

## Signing

The published version of this assembly is digitally signed with Authenticode and [strong-named](https://docs.microsoft.com/en-us/dotnet/framework/app-domains/strong-named-assemblies). Building the code locally in the default Debug configuration does not use strong-naming and does not require a key file. The public key file is in this repository at `LaunchDarkly.pk` as well as here:

```
Public Key:
0024000004800000940000000602000000240000525341310004000001000100
058a1dbccbc342759dc98b1eaba4467bfdea062629f212cf7c669ff26b4e2ff3
c408292487bc349b8a687d73033ff14dbf861e1eea23303a5b5d13b1db034799
13bd120ba372cf961d27db9f652631565f4e8aff4a79e11cfe713833157ecb5d
cbc02d772967d919f8f06fbee227a664dc591932d5b05f4da1c8439702ecfdb1

Public Key Token: 90b24964a3dfb906
```

## Learn more

Read our [documentation](https://docs.launchdarkly.com) for in-depth instructions on configuring and using LaunchDarkly. You can also head straight to the [complete reference guide for this SDK](https://docs.launchdarkly.com/sdk/client-side/dotnet).

The authoritative description of all types, properties, and methods is in the [generated API documentation](https://launchdarkly.github.io/dotnet-client-sdk/).

## Testing
 
We run integration tests for all our SDKs using a centralized test harness. This approach gives us the ability to test for consistency across SDKs, as well as test networking behavior in a long-running application. These tests cover each method in the SDK, and verify that event sending, flag evaluation, stream reconnection, and other aspects of the SDK all behave correctly.
 
## Contributing
 
We encourage pull requests and other contributions from the community. Check out our [contributing guidelines](CONTRIBUTING.md) for instructions on how to contribute to this SDK.

## About LaunchDarkly
 
* LaunchDarkly is a continuous delivery platform that provides feature flags as a service and allows developers to iterate quickly and safely. We allow you to easily flag your features and manage them from the LaunchDarkly dashboard.  With LaunchDarkly, you can:
    * Roll out a new feature to a subset of your users (like a group of users who opt-in to a beta tester group), gathering feedback and bug reports from real-world use cases.
    * Gradually roll out a feature to an increasing percentage of users, and track the effect that the feature has on key metrics (for instance, how likely is a user to complete a purchase if they have feature A versus feature B?).
    * Turn off a feature that you realize is causing performance problems in production, without needing to re-deploy, or even restart the application with a changed configuration file.
    * Grant access to certain features based on user attributes, like payment plan (eg: users on the ‘gold’ plan get access to more features than users in the ‘silver’ plan). Disable parts of your application to facilitate maintenance, without taking everything offline.
* LaunchDarkly provides feature flag SDKs for a wide variety of languages and technologies. Check out [our documentation](https://docs.launchdarkly.com/docs) for a complete list.
* Explore LaunchDarkly
    * [launchdarkly.com](https://www.launchdarkly.com/ "LaunchDarkly Main Website") for more information
    * [docs.launchdarkly.com](https://docs.launchdarkly.com/  "LaunchDarkly Documentation") for our documentation and SDK reference guides
    * [apidocs.launchdarkly.com](https://apidocs.launchdarkly.com/  "LaunchDarkly API Documentation") for our API documentation
    * [blog.launchdarkly.com](https://blog.launchdarkly.com/  "LaunchDarkly Blog Documentation") for the latest product updates

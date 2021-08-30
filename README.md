# LaunchDarkly Client-Side SDK for .NET

[![NuGet](https://img.shields.io/nuget/v/LaunchDarkly.ClientSdk.svg?style=flat-square)](https://www.nuget.org/packages/LaunchDarkly.ClientSdk/)
[![CircleCI](https://circleci.com/gh/launchdarkly/dotnet-client-sdk.svg?style=shield)](https://circleci.com/gh/launchdarkly/dotnet-client-sdk)
[![Documentation](https://img.shields.io/static/v1?label=GitHub+Pages&message=API+reference&color=00add8)](https://launchdarkly.github.io/dotnet-client-sdk)

The LaunchDarkly Client-Side SDK for .NET is designed primarily for use by code that is deployed to an end user, such as in a desktop application or a smart device. It follows the client-side LaunchDarkly model for single-user contexts (much like our mobile or JavaScript SDKs). It is not intended for use in multi-user systems such as web servers and applications.

On supported mobile platforms (Android and iOS), the SDK uses the Xamarin framework which allows .NET code to run on those devices. For that reason, its name was previously "LaunchDarkly Xamarin SDK". However, Xamarin is not the only way to run .NET code in a client-side context (see "Supported platforms" below), so the SDK now has a more general name.

For using LaunchDarkly in *server-side* .NET applications, refer to our [Server-Side .NET SDK](https://github.com/launchdarkly/dotnet-server-sdk).

## LaunchDarkly overview

[LaunchDarkly](https://www.launchdarkly.com) is a feature management platform that serves over 100 billion feature flags daily to help teams build better software, faster. [Get started](https://docs.launchdarkly.com/home/getting-started) using LaunchDarkly today!
 
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
0024000004800000940000000602000000240000525341310004000001000100f121bbf427e4d7
edc64131a9efeefd20978dc58c285aa6f548a4282fc6d871fbebeacc13160e88566f427497b625
56bf7ff01017b0f7c9de36869cc681b236bc0df0c85927ac8a439ecb7a6a07ae4111034e03042c
4b1569ebc6d3ed945878cca97e1592f864ba7cc81a56b8668a6d7bbe6e44c1279db088b0fdcc35
52f746b4

Public Key Token: f86add69004e6885
```

## Learn more

Check out our [documentation](https://docs.launchdarkly.com) for in-depth instructions on configuring and using LaunchDarkly. You can also head straight to the [complete reference guide for this SDK](https://docs.launchdarkly.com/sdk/client-side/dotnet).

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

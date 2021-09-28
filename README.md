<div style="border: 2px solid yellow; padding: 20px;">
# This project is being renamed

Xamarin is just one of several .NET-based platforms that this SDK supports. The **LaunchDarkly Client-Side SDK for Xamarin** is being renamed to the **LaunchDarkly Client-Side SDK for .NET**.

Future releases of the package on NuGet, starting with 2.0.0, will be named **LaunchDarkly.ClientSdk**. The old package name, **LaunchDarkly.XamarinSdk**, will have only maintenance/patch releases.

The GitHub repository has been renamed from `launchdarkly/xamarin-client-sdk` to `launchdarkly/dotnet-client-sdk`. Links to the old repository are automatically redirected by GitHub.
</div>

# LaunchDarkly Client-Side SDK for .NET

[![NuGet (old)](https://img.shields.io/nuget/v/LaunchDarkly.XamarinSdk.svg?style=flat-square)](https://www.nuget.org/packages/LaunchDarkly.XamarinSdk/)
[![CircleCI](https://circleci.com/gh/launchdarkly/dotnet-client-sdk.svg?style=shield)](https://circleci.com/gh/launchdarkly/dotnet-client-sdk)
[![Documentation](https://img.shields.io/static/v1?label=GitHub+Pages&message=API+reference&color=00add8)](https://launchdarkly.github.io/xamarin-client-sdk)

The LaunchDarkly Client-Side SDK for .NET (formerly "LaunchDarkly SDK for Xamarin") is designed primarily for use by code that is deployed to an end user, such as in a desktop application or a smart device. It follows the client-side LaunchDarkly model for single-user contexts (much like our mobile or JavaScript SDKs). It is not intended for use in multi-user systems such as web servers and applications.

On supported mobile platforms (Android and iOS), the SDK uses the Xamarin framework which allows .NET code to run on those devices. For that reason, the SDK was previously named after Xamarin. However, Xamarin is not the only way to run .NET code in a client-side context (see "Supported platforms" below), so the SDK now has a more general name.

For using LaunchDarkly in *server-side* .NET applications, refer to our [Server-Side .NET SDK](https://github.com/launchdarkly/dotnet-server-sdk).

## LaunchDarkly overview

[LaunchDarkly](https://www.launchdarkly.com) is a feature management platform that serves over 100 billion feature flags daily to help teams build better software, faster. [Get started](https://docs.launchdarkly.com/home/getting-started) using LaunchDarkly today!
 
[![Twitter Follow](https://img.shields.io/twitter/follow/launchdarkly.svg?style=social&label=Follow&maxAge=2592000)](https://twitter.com/intent/follow?screen_name=launchdarkly)

## Supported platforms

This version of the SDK is built for the following targets:

* Xamarin Android 7.1, 8.0, and 8.1, which should also work with later versions of Android.
* Xamarin iOS 10, for use with iOS 10 and higher.
* .NET Standard 1.6 and 2.0, for use with any runtime platform that supports .NET Standard, or in portable .NET Standard library code.

The .NET Standard targets do not use any Xamarin packages and have no OS-specific code. This allows the SDK to be used in a desktop .NET Framework or .NET 5.0 application, or in a Xamarin MacOS application. However, due to the lack of OS-specific integration, SDK functionality will be limited in those environments: for instance, the SDK will not be able to detect whether networking is turned on or off.

The .NET build tools should automatically load the most appropriate build of the SDK for whatever platform your application or library is targeted to.

## Getting started

Refer to the [SDK documentation](https://docs.launchdarkly.com/sdk/client-side/xamarin) for instructions on getting started with using the SDK.

## Learn more

Check out our [documentation](https://docs.launchdarkly.com) for in-depth instructions on configuring and using LaunchDarkly. You can also head straight to the [complete reference guide for this SDK](https://docs.launchdarkly.com/sdk/client-side/xamarin).

The authoritative description of all types, properties, and methods is in the [generated API documentation](https://launchdarkly.github.io/xamarin-client-sdk/).

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

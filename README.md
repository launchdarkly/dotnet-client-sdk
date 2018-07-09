LaunchDarkly SDK [BETA] for Xamarin
===========================
[![CircleCI](https://circleci.com/gh/launchdarkly/xamarin-client/tree/master.svg?style=svg)](https://circleci.com/gh/launchdarkly/.xamarin-client/tree/master)

*This software is a **beta** version and should not be considered ready for production use until tagged at least 1.0.*

Quick setup
-----------

0. Use [NuGet](http://docs.nuget.org/docs/start-here/using-the-package-manager-console) to add the Xamarin SDK to your project:

        Install-Package LaunchDarkly.Xamarin

1. Import the LaunchDarkly package:

        using LaunchDarkly.Xamarin;

2. Initialize the LDClient with your Mobile key and user:

        User user = User.WithKey(username);
        LdClient ldClient = LdClient.Init("YOUR_MOBILE_KEY", username);

Your first feature flag
-----------------------

1. Create a new feature flag on your [dashboard](https://app.launchdarkly.com).
2. In your application code, use the feature's key to check whether the flag is on for each user:

        bool showFeature = ldClient.BoolVariation("your.feature.key");
        if (showFeature) {
          // application code to show the feature 
        }
        else {
          // the code to run if the feature is off
        }

Learn more
-----------

Check out our [documentation](http://docs.launchdarkly.com) for in-depth instructions on configuring and using LaunchDarkly. You can also head straight to the [complete reference guide for this SDK](http://docs.launchdarkly.com/docs/dotnet-sdk-reference).

Testing
-------

We run integration tests for all our SDKs using a centralized test harness. This approach gives us the ability to test for consistency across SDKs, as well as test networking behavior in a long-running application. These tests cover each method in the SDK, and verify that event sending, flag evaluation, stream reconnection, and other aspects of the SDK all behave correctly.

Contributing
------------

See [Contributing](https://github.com/launchdarkly/xamarin-client/blob/master/CONTRIBUTING.md).

Signing
-------
The artifacts generated from this repo are signed by LaunchDarkly. The public key file is in this repo at `LaunchDarkly.Xamarin.pk` as well as here:

```
Public Key:
00240000048000009400000006020000
00240000525341310004000001000100
058a1dbccbc342759dc98b1eaba4467b
fdea062629f212cf7c669ff26b4e2ff3
c408292487bc349b8a687d73033ff14d
bf861e1eea23303a5b5d13b1db034799
13bd120ba372cf961d27db9f65263156
5f4e8aff4a79e11cfe713833157ecb5d
cbc02d772967d919f8f06fbee227a664
dc591932d5b05f4da1c8439702ecfdb1

Public Key Token: 90b24964a3dfb906f86add69004e6885
```

About LaunchDarkly
-----------

* LaunchDarkly is a continuous delivery platform that provides feature flags as a service and allows developers to iterate quickly and safely. We allow you to easily flag your features and manage them from the LaunchDarkly dashboard.  With LaunchDarkly, you can:
    * Roll out a new feature to a subset of your users (like a group of users who opt-in to a beta tester group), gathering feedback and bug reports from real-world use cases.
    * Gradually roll out a feature to an increasing percentage of users, and track the effect that the feature has on key metrics (for instance, how likely is a user to complete a purchase if they have feature A versus feature B?).
    * Turn off a feature that you realize is causing performance problems in production, without needing to re-deploy, or even restart the application with a changed configuration file.
    * Grant access to certain features based on user attributes, like payment plan (eg: users on the ‘gold’ plan get access to more features than users in the ‘silver’ plan). Disable parts of your application to facilitate maintenance, without taking everything offline.
* LaunchDarkly provides feature flag SDKs for
    * [Java](http://docs.launchdarkly.com/docs/java-sdk-reference "Java SDK")
    * [JavaScript](http://docs.launchdarkly.com/docs/js-sdk-reference "LaunchDarkly JavaScript SDK")
    * [PHP](http://docs.launchdarkly.com/docs/php-sdk-reference "LaunchDarkly PHP SDK")
    * [Python](http://docs.launchdarkly.com/docs/python-sdk-reference "LaunchDarkly Python SDK")
    * [Python Twisted](http://docs.launchdarkly.com/docs/python-twisted-sdk-reference "LaunchDarkly Python Twisted SDK")
    * [Go](http://docs.launchdarkly.com/docs/go-sdk-reference "LaunchDarkly Go SDK")
    * [Node.JS](http://docs.launchdarkly.com/docs/node-sdk-reference "LaunchDarkly Node SDK")
    * [.NET](http://docs.launchdarkly.com/docs/dotnet-sdk-reference "LaunchDarkly .Net SDK")
    * [Xamarin](http://docs.launchdarkly.com/docs/xamarin-sdk-reference "LaunchDarkly Xamarin SDK")
    * [Ruby](http://docs.launchdarkly.com/docs/ruby-sdk-reference "LaunchDarkly Ruby SDK")
    * [iOS](http://docs.launchdarkly.com/docs/ios-sdk-reference "LaunchDarkly iOS SDK")
    * [Android](http://docs.launchdarkly.com/docs/android-sdk-reference "LaunchDarkly Android SDK")
* Explore LaunchDarkly
    * [launchdarkly.com](http://www.launchdarkly.com/ "LaunchDarkly Main Website") for more information
    * [docs.launchdarkly.com](http://docs.launchdarkly.com/  "LaunchDarkly Documentation") for our documentation and SDKs
    * [apidocs.launchdarkly.com](http://apidocs.launchdarkly.com/  "LaunchDarkly API Documentation") for our API documentation
    * [blog.launchdarkly.com](http://blog.launchdarkly.com/  "LaunchDarkly Blog Documentation") for the latest product updates
    * [Feature Flagging Guide](https://github.com/launchdarkly/featureflags/  "Feature Flagging Guide") for best practices and strategies

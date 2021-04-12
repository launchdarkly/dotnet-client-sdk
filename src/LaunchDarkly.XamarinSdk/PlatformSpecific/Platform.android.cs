﻿/*
Xamarin.Essentials(https://github.com/xamarin/Essentials) code used under MIT License

The MIT License(MIT)
Copyright(c) Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
associated documentation files (the "Software"), to deal in the Software without restriction, 
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial
portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware;
using Android.Hardware.Camera2;
using Android.Locations;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;

// All commented-out code in this file came from Xamarin Essentials but was removed because it is not used by this SDK.

namespace LaunchDarkly.Sdk.Xamarin.PlatformSpecific
{
    internal static partial class Platform
    {
        //static ActivityLifecycleContextListener lifecycleListener;

        internal static Context AppContext =>
            Application.Context;

        //internal static Activity GetCurrentActivity(bool throwOnNull)
        //{
        //    var activity = lifecycleListener?.Activity;
        //    if (throwOnNull && activity == null)
        //        throw new NullReferenceException("The current Activity can not be detected. Ensure that you have called Init in your Activity or Application class.");

        //    return activity;
        //}

        //public static void Init(Application application)
        //{
        //    lifecycleListener = new ActivityLifecycleContextListener();
        //    application.RegisterActivityLifecycleCallbacks(lifecycleListener);
        //}

        //public static void Init(Activity activity, Bundle bundle)
        //{
        //    Init(activity.Application);
        //    lifecycleListener.Activity = activity;
        //}

        //public static void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults) =>
        //    Permissions.Permissions.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        //internal static bool HasSystemFeature(string systemFeature)
        //{
        //    var packageManager = AppContext.PackageManager;
        //    foreach (var feature in packageManager.GetSystemAvailableFeatures())
        //    {
        //        if (feature.Name.Equals(systemFeature, StringComparison.OrdinalIgnoreCase))
        //            return true;
        //    }
        //    return false;
        //}

        //internal static bool IsIntentSupported(Intent intent)
        //{
        //    var manager = AppContext.PackageManager;
        //    var activities = manager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
        //    return activities.Any();
        //}

        internal static bool HasApiLevel(BuildVersionCodes versionCode) =>
            (int)Build.VERSION.SdkInt >= (int)versionCode;

        //internal static CameraManager CameraManager =>
        //    AppContext.GetSystemService(Context.CameraService) as CameraManager;

        internal static ConnectivityManager ConnectivityManager =>
            AppContext.GetSystemService(Context.ConnectivityService) as ConnectivityManager;

        //internal static Vibrator Vibrator =>
        //    AppContext.GetSystemService(Context.VibratorService) as Vibrator;

        //internal static WifiManager WifiManager =>
        //    AppContext.GetSystemService(Context.WifiService) as WifiManager;

        //internal static SensorManager SensorManager =>
        //    AppContext.GetSystemService(Context.SensorService) as SensorManager;

        //internal static ClipboardManager ClipboardManager =>
        //    AppContext.GetSystemService(Context.ClipboardService) as ClipboardManager;

        //internal static LocationManager LocationManager =>
        //    AppContext.GetSystemService(Context.LocationService) as LocationManager;

        //internal static PowerManager PowerManager =>
            //AppContext.GetSystemService(Context.PowerService) as PowerManager;

        //internal static Java.Util.Locale GetLocale()
        //{
        //    var resources = AppContext.Resources;
        //    var config = resources.Configuration;
        //    if (HasApiLevel(BuildVersionCodes.N))
        //        return config.Locales.Get(0);

        //    return config.Locale;
        //}

//        internal static void SetLocale(Java.Util.Locale locale)
//        {
//            Java.Util.Locale.Default = locale;
//            var resources = AppContext.Resources;
//            var config = resources.Configuration;
//            if (HasApiLevel(BuildVersionCodes.N))
//                config.SetLocale(locale);
//            else
//                config.Locale = locale;

//#pragma warning disable CS0618 // Type or member is obsolete
//            resources.UpdateConfiguration(config, resources.DisplayMetrics);
//#pragma warning restore CS0618 // Type or member is obsolete
        //}
    }

    //internal class ActivityLifecycleContextListener : Java.Lang.Object, Application.IActivityLifecycleCallbacks
    //{
    //    WeakReference<Activity> currentActivity = new WeakReference<Activity>(null);

    //    internal Context Context =>
    //        Activity ?? Application.Context;

    //    internal Activity Activity
    //    {
    //       get => currentActivity.TryGetTarget(out var a) ? a : null;
    //       set => currentActivity.SetTarget(value);
    //    }

    //    void Application.IActivityLifecycleCallbacks.OnActivityCreated(Activity activity, Bundle savedInstanceState) =>
    //        Activity = activity;

    //    void Application.IActivityLifecycleCallbacks.OnActivityDestroyed(Activity activity)
    //    {
    //    }

    //    void Application.IActivityLifecycleCallbacks.OnActivityPaused(Activity activity) =>
    //        Activity = activity;

    //    void Application.IActivityLifecycleCallbacks.OnActivityResumed(Activity activity) =>
    //        Activity = activity;

    //    void Application.IActivityLifecycleCallbacks.OnActivitySaveInstanceState(Activity activity, Bundle outState)
    //    {
    //    }

    //    void Application.IActivityLifecycleCallbacks.OnActivityStarted(Activity activity)
    //    {
    //    }

    //    void Application.IActivityLifecycleCallbacks.OnActivityStopped(Activity activity)
    //    {
    //    }
    //}
}

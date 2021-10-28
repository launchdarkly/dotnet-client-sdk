﻿using System;
using System.Net;
using System.Net.Http;
using LaunchDarkly.Sdk.Internal.Http;
using Xamarin.Android.Net;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class Http
    {
        private static Func<HttpProperties, HttpMessageHandler> PlatformGetHttpMessageHandlerFactory(
            TimeSpan connectTimeout,
            TimeSpan readTimeout,
            IWebProxy proxy
            ) =>
            p => new AndroidClientHandler()
            {
                ConnectTimeout = connectTimeout,
                ReadTimeout = readTimeout,
                Proxy = proxy
            };

        private static Exception PlatformTranslateHttpException(Exception e)
        {
            if (e is Java.Net.SocketTimeoutException)
            {
                return new TimeoutException();
            }
            return e;
        }
    }
}

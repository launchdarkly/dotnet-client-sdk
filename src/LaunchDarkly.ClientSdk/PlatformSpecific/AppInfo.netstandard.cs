/*
Xamarin.Essentials

The MIT License (MIT)

Copyright (c) Microsoft Corporation

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

using System;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static class ExceptionUtils
    {
#if NETSTANDARD1_0 || NETSTANDARD2_0
        internal static NotImplementedInReferenceAssemblyException NotSupportedOrImplementedException =>
            new NotImplementedInReferenceAssemblyException();
#else
        internal static FeatureNotSupportedException NotSupportedOrImplementedException =>
            new FeatureNotSupportedException($"This API is not supported on {DeviceInfo.Platform}");
#endif

    }

    internal class NotImplementedInReferenceAssemblyException : NotImplementedException
    {
        public NotImplementedInReferenceAssemblyException()
            : base(
                "This functionality is not implemented in the portable version of this assembly. You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.")
        {
        }
    }

    internal class PermissionException : UnauthorizedAccessException
    {
        public PermissionException(string message)
            : base(message)
        {
        }
    }

    internal class FeatureNotSupportedException : NotSupportedException
    {
        public FeatureNotSupportedException()
        {
        }

        public FeatureNotSupportedException(string message)
            : base(message)
        {
        }

        public FeatureNotSupportedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    internal class FeatureNotEnabledException : InvalidOperationException
    {
        public FeatureNotEnabledException()
        {
        }

        public FeatureNotEnabledException(string message)
            : base(message)
        {
        }

        public FeatureNotEnabledException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    internal static partial class AppInfo
    {
        static string PlatformGetPackageName() => throw ExceptionUtils.NotSupportedOrImplementedException;

        static string PlatformGetName() => throw ExceptionUtils.NotSupportedOrImplementedException;

        static string PlatformGetVersionString() => throw ExceptionUtils.NotSupportedOrImplementedException;

        static string PlatformGetBuild() => throw ExceptionUtils.NotSupportedOrImplementedException;

    }
}
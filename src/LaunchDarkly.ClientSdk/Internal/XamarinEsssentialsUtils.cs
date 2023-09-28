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
using System.Threading;
using System.Threading.Tasks;

namespace LaunchDarkly.Sdk.Client.Internal
{
    internal class Utils
    {
        internal static Version ParseVersion(string version)
        {
            if (Version.TryParse(version, out var number))
                return number;

            if (int.TryParse(version, out var major))
                return new Version(major, 0);

            return new Version(0, 0);
        }

        internal static CancellationToken TimeoutToken(CancellationToken cancellationToken, TimeSpan timeout)
        {
            // create a new linked cancellation token source
            var cancelTokenSrc = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // if a timeout was given, make the token source cancel after it expires
            if (timeout > TimeSpan.Zero)
                cancelTokenSrc.CancelAfter(timeout);

            // our Cancel method will handle the actual cancellation logic
            return cancelTokenSrc.Token;
        }

        internal static async Task<T> WithTimeout<T>(Task<T> task, TimeSpan timeSpan)
        {
            var retTask = await Task.WhenAny(task, Task.Delay(timeSpan))
                .ConfigureAwait(false);

            return retTask is Task<T> ? task.Result : default(T);
        }
    }
}
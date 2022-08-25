#!/bin/bash

set -eu

# Run the .NET Standard 2.0 unit tests. (Android and iOS tests are run by regular CI jobs)

TESTFRAMEWORK=net6.0 dotnet test tests/LaunchDarkly.ClientSdk.Tests/LaunchDarkly.ClientSdk.Tests.csproj -f net6.0

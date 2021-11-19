#!/bin/bash

set -eu

# Run the .NET Standard 2.0 unit tests. (Android and iOS tests are run by regular CI jobs)

dotnet test tests/LaunchDarkly.ClientSdk.Tests/LaunchDarkly.ClientSdk.Tests.csproj -f net5.0

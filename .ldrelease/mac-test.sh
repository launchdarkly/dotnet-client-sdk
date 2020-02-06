#!/bin/bash

set -eu

# Run the .NET Standard 2.0 unit tests. (Android and iOS tests are run by regular CI jobs)

dotnet test tests/LaunchDarkly.XamarinSdk.Tests/LaunchDarkly.XamarinSdk.Tests.csproj -f netcoreapp2.0

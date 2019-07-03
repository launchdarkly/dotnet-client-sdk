#!/bin/bash
set -e

# Usage: ./scripts/package.sh [debug|release]

# This script performs a clean build for all target platforms, which produces both the DLLs and the .nupkg,
# and also runs the .NET Standard unit tests. It is used in the LaunchDarkly release process. It must be run
# on MacOS, since iOS is one of the targets.

# msbuild expects word-capitalization of this parameter
CONFIG=`echo $1 | awk '{print toupper(substr($0,0,1))tolower(substr($0,2))}'`
if [[ -z "$CONFIG" ]]; then
  CONFIG=Debug  # currently we're releasing debug builds by default
fi

# Remove any existing build products.

msbuild /t:clean
rm -f ./src/LaunchDarkly.XamarinSdk/bin/Debug/*.nupkg
rm -f ./src/LaunchDarkly.XamarinSdk/bin/Release/*.nupkg

# Build the project for all target frameworks. This includes building the .nupkg, because of
# the <GeneratePackageOnBuild> directive in our project file.

msbuild /restore /p:Configuration:$CONFIG src/LaunchDarkly.XamarinSdk/LaunchDarkly.XamarinSdk.csproj

# Run the .NET Standard 2.0 unit tests. (Android and iOS tests are run by CI jobs in config.yml)

export ASPNETCORE_SUPPRESSSTATUSMESSAGES=true  # suppresses some annoying test output
dotnet test tests/LaunchDarkly.XamarinSdk.Tests/LaunchDarkly.XamarinSdk.Tests.csproj -f netcoreapp2.0

#!/bin/bash
set -e

# Usage: ./scripts/release.sh [debug|release]

# This script calls package.sh to create a NuGet package, and then uploads it to NuGet. It is used in
# the LaunchDarkly release process. It must be run on MacOS, since iOS is one of the targets.

# msbuild expects word-capitalization of this parameter
CONFIG=`echo $1 | awk '{print toupper(substr($0,0,1))tolower(substr($0,2))}'`
if [[ -z "$CONFIG" ]]; then
  CONFIG=Debug  # currently we're releasing debug builds by default
fi

./scripts/package.sh $CONFIG

# Since package.sh does a clean build, whichever .nupkg file now exists in the output directory
# is the one we want to upload.

nuget push ./src/LaunchDarkly.XamarinSdk/bin/$CONFIG/LaunchDarkly.XamarinSdk.*.nupkg -Source https://www.nuget.org

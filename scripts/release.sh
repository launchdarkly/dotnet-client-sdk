#!/bin/bash
set -e

# Usage: ./scripts/release.sh [debug|release]

# msbuild expects word-capitalization of this parameter
CONFIG=`echo $1 | awk '{print toupper(substr($0,0,1))tolower(substr($0,2))}'`
if [[ -z "$CONFIG" ]]; then
	CONFIG=Debug  # currently we're releasing debug builds by default
fi

./scripts/package.sh $CONFIG

# Since package.sh does a clean build, whichever .nupkg file now exists in the output directory
# is the one we want to upload.

nuget push ./src/LaunchDarkly.XamarinSdk/bin/$CONFIG/LaunchDarkly.XamarinSdk.*.nupkg -Source https://www.nuget.org

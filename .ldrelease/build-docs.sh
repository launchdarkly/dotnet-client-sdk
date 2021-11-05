#!/bin/bash

set -eu

# This script is only run in the Dockerized Linux job that is used for building
# documentation.

# This is a hack to deal with the conflict between two requirements:
# 1. Currently Xamarin projects must be built with msbuild, so the project file
#    must use "MSBuild.Sdk.Extras".
# 2. Our documentation build process uses the dotnet CLI command, so it expects
#    the project file to use "Microsoft.NET.Sdk" instead.
# As long as we are not building any Xamarin-specific targets, but only the .NET
# Standard target, it is OK to use "Microsoft.NET.Sdk". This script modifies the
# project file (only within the Docker container, not in source control) so it
# will build with "Microsoft.NET.Sdk" during this job.

PROJECT_FILE=./src/LaunchDarkly.ClientSdk/LaunchDarkly.ClientSdk.csproj

cp "${PROJECT_FILE}" "${PROJECT_FILE}.bak"
sed "s/MSBuild.Sdk.Extras/Microsoft.NET.Sdk/g" "${PROJECT_FILE}" > "${PROJECT_FILE}.tmp"
mv "${PROJECT_FILE}.tmp" "${PROJECT_FILE}"

# Now run the actual build-docs script from Releaser's project template
${LD_RELEASE_TEMP_DIR}/../template/build-docs.sh

# Change the project file back
mv "${PROJECT_FILE}.bak" "${PROJECT_FILE}"

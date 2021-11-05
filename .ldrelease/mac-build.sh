#!/bin/bash

set -eu

# Build the project for all target frameworks. This includes building the .nupkg, because of
# the <GeneratePackageOnBuild> directive in our project file.

msbuild /restore /p:Configuration=Release src/LaunchDarkly.ClientSdk/LaunchDarkly.ClientSdk.csproj

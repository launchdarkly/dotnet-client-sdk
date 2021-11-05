#!/bin/bash

set -eu

NUGET_KEY=$(cat "${LD_RELEASE_SECRETS_DIR}/dotnet_nuget_api_key")

for pkg in $(find ./src/LaunchDarkly.ClientSdk/bin/Release -name '*.nupkg' -o -name '*.snupkg'); do
  echo "publishing $pkg"
  nuget push "$pkg" -ApiKey "${NUGET_KEY}" -Source https://www.nuget.org
  echo "published $pkg"
done

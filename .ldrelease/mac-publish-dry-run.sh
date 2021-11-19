#!/bin/bash

set -eu

for pkg in $(find ./src/LaunchDarkly.ClientSdk/bin/Release -name '*.nupkg' -o -name '*.snupkg'); do
  cp "$pkg" "${LD_RELEASE_ARTIFACTS_DIR}"
done

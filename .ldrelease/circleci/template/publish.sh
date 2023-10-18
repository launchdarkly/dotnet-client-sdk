#!/bin/bash

# See: https://github.com/launchdarkly/project-releaser/blob/main/docs/templates/dotnet-linux.md

set -eu

$(dirname $0)/build-all-packages.sh

nuget_api_key=$(cat "${LD_RELEASE_SECRETS_DIR}/dotnet_nuget_api_key")

projects=$(ls ./src)
for project in ${projects}; do
  release_products_dir="./src/${project}/bin/Release"
  for pkg in $(find "${release_products_dir}" -name '*.nupkg' -o -name '*.snupkg'); do
    echo "[$project]: publishing $pkg"
    dotnet nuget push "${pkg}" --source "https://www.nuget.org" --api-key "${nuget_api_key}"
    echo "[$project]: published $pkg"
  done
done

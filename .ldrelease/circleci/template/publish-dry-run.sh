#!/bin/bash

# See: https://github.com/launchdarkly/project-releaser/blob/master/docs/templates/dotnet-linux.md

set -eu

echo "DRY RUN: not publishing to NuGet, only building package"

$(dirname $0)/build-all-packages.sh

projects=$(ls ./src)
for project in ${projects}; do
  release_products_dir="./src/${project}/bin/Release"
  for pkg in $(find "${release_products_dir}" -name '*.nupkg' -o -name '*.snupkg'); do
    cp "${pkg}" "${LD_RELEASE_ARTIFACTS_DIR}"
  done
done

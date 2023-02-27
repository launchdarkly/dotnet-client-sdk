#!/bin/bash

# See: https://github.com/launchdarkly/project-releaser/blob/main/docs/templates/dotnet-linux.md

set -eu

# Microsoft's Docker containers set NUGET_XMLDOC_MODE=skip which causes XML doc files *not* to be
# extracted from NuGet dependencies as they normally would be (https://github.com/NuGet/Home/issues/7805).
# We want them to be extracted in case we need them in the build-docs step.
export NUGET_XMLDOC_MODE=none

projects=$(ls ./src || true)
if [[ -z "${projects}" ]]; then
  echo "No projects were found under ./src; does this solution use the standard directory layout?" >&2
  exit 1
fi

echo "Building all projects in Debug configuration"
echo "Projects to be built: ${projects}"
echo

# Suppress the "Welcome to .NET Core!" message that appears the first time you run dotnet
dotnet help >/dev/null

# If we downloaded any .snk files for strong-naming, copy them to the root project directory which is
# where we assume the project will want them to be
find "${LD_RELEASE_SECRETS_DIR}" -name '*.snk' -exec cp {} . \;

for project in ${projects}; do
  dotnet build -c Debug "./src/${project}/${project}.csproj"
done

#!/bin/bash

# See: https://github.com/launchdarkly/project-releaser/blob/master/docs/templates/dotnet-linux.md

set -ue

# Besides the secrets that were already specified in the project template's secrets.properties,
# we may also need to tell Releaser to download additional key files for strong-naming, based on
# the project properties. We do this by writing to ${LD_RELEASE_SECRETS_DIR}/secrets.properties,
# which Releaser will check after calling this script.

project_files=$(find ./src -name "*.csproj")
for project_file in ${project_files}; do
  key_file=$(sed <"${project_file}" -n -e 's#.*<AssemblyOriginatorKeyFile> *[./\\]*\([^ <]*\) *</AssemblyOriginatorKeyFile>.*#\1#p')
  if [[ -n "${key_file}" ]]; then
    echo "Will download ${key_file} for strong-naming"
    echo "${key_file}=blob:/dotnet/${key_file}" >> ${LD_RELEASE_SECRETS_DIR}/secrets.properties
  fi
done

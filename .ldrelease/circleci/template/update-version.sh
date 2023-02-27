#!/bin/bash

# See: https://github.com/launchdarkly/project-releaser/blob/main/docs/templates/dotnet-linux.md

set -ue

project_files=$(find ./src -name "*.csproj")
for project_file in ${project_files}; do
  echo "Setting version in ${project_file} to ${LD_RELEASE_VERSION}"
  temp_file="${project_file}.tmp"
  sed "s#^\( *\)<Version>[^<]*</Version>#\1<Version>${LD_RELEASE_VERSION}</Version>#g" "${project_file}" > "${temp_file}"
  mv "${temp_file}" "${project_file}"
done

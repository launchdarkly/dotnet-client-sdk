#!/bin/bash

# See: https://github.com/launchdarkly/project-releaser/blob/main/docs/templates/dotnet-linux.md

set -eu

test_projects=$(ls ./test || true)
if [[ -z "${test_projects}" ]]; then
  echo "No projects were found under ./test; skipping test step"
  exit 0
fi

echo "Running tests"
echo "Test projects: ${test_projects}"
echo

for project in ${test_projects}; do
  test_project_file="./test/${project}/${project}.csproj"
  dotnet build -c Debug "${test_project_file}" -f net6.0
  dotnet test -c Debug "${test_project_file}" -f net6.0
done

#!/bin/bash

# See: https://github.com/launchdarkly/project-releaser/blob/master/docs/templates/dotnet-linux.md

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
  dotnet test -c Debug "${test_project_file}" -f net5.0
done

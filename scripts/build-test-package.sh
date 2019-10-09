#!/bin/bash

set -e

# build-test-package.sh
# Temporarily changes the project version to a unique prerelease version string based on the current
# date and time, builds a .nupkg package, and places the package in ./test-packages. You can then use
# the test-packages directory as a package source to use this package in our testing tools.

TEST_VERSION="0.0.1-$(date +%Y%m%d.%H%M%S)"
PROJECT_FILE=src/LaunchDarkly.XamarinSdk/LaunchDarkly.XamarinSdk.csproj
SAVE_PROJECT_FILE="${PROJECT_FILE}.orig"
TEST_PACKAGE_DIR=./test-packages

mkdir -p "${TEST_PACKAGE_DIR}"

cp "${PROJECT_FILE}" "${SAVE_PROJECT_FILE}"

"$(dirname "$0")/update-version.sh" "${TEST_VERSION}"

trap 'mv "${SAVE_PROJECT_FILE}" "${PROJECT_FILE}"' EXIT

msbuild /restore

NUPKG_FILE="src/LaunchDarkly.XamarinSdk/bin/Debug/LaunchDarkly.XamarinSdk.${TEST_VERSION}.nupkg"
if [ -f "${NUPKG_FILE}" ]; then
	mv "${NUPKG_FILE}" "${TEST_PACKAGE_DIR}"
	echo; echo; echo "Success! Created test package version ${TEST_VERSION} in ${TEST_PACKAGE_DIR}"
else
	echo; echo "Unknown problem - did not build the expected package file"
	exit 1
fi

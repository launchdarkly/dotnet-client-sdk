#!/bin/bash

# This script gets run on the Releaser host, not in the Mac or Windows CI jobs

set -eu

PROJECT_FILE=./src/LaunchDarkly.XamarinSdk/LaunchDarkly.XamarinSdk.csproj
TEMP_FILE="${PROJECT_FILE}.tmp"

sed "s#^\( *\)<Version>[^<]*</Version>#\1<Version>${LD_RELEASE_VERSION}</Version>#g" "${PROJECT_FILE}" > "${TEMP_FILE}"
mv "${TEMP_FILE}" "${PROJECT_FILE}"

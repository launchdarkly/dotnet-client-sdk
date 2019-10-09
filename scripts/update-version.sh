#!/bin/bash

# update-version.sh <version string>
# Updates the version string in the project file.

NEW_VERSION="$1"

PROJECT_FILE=./src/LaunchDarkly.XamarinSdk/LaunchDarkly.XamarinSdk.csproj
TEMP_FILE="${PROJECT_FILE}.tmp"

sed "s#^\( *\)<Version>[^<]*</Version>#\1<Version>${NEW_VERSION}</Version>#g" "${PROJECT_FILE}" > "${TEMP_FILE}"
mv "${TEMP_FILE}" "${PROJECT_FILE}"

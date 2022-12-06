#!/bin/bash

set -eu
set +o pipefail

# Download and build the osslsigncode tool. In our other .NET projects, code signing
# is handled by our standard project template scripts for .NET, which use a Docker
# image that already has osslsigncode installed. But here we're using a CircleCI job
# to do the build, so we need to do the code-signing in the same place and download
# the tool as needed. Unfortunately it has to be built from source.

SIGNCODE_DOWNLOAD_URL=https://github.com/mtrojnar/osslsigncode/releases/download/2.5/osslsigncode-2.5-macOS.zip
SIGNCODE_ARCHIVE="${LD_RELEASE_TEMP_DIR}/osslsigncode.zip"
SIGNCODE_DIR="${LD_RELEASE_TEMP_DIR}/osslsigncode"

echo ""
echo "Downloading osslsigncode..."
curl --fail --silent -L "${SIGNCODE_DOWNLOAD_URL}" >"${SIGNCODE_ARCHIVE}"
mkdir -p "${SIGNCODE_DIR}"
pushd "${SIGNCODE_DIR}" && unzip "${SIGNCODE_ARCHIVE}"
popd
chmod a+x "${SIGNCODE_DIR}/bin/osslsigncode"

# Copy the strong-naming key that was downloaded due to our secrets.properties declaration
cp "${LD_RELEASE_SECRETS_DIR}/LaunchDarkly.ClientSdk.snk" .

# Install the Xamarin frameworks; this will take a while
echo ""
echo "Installing Xamarin..."
./.circleci/scripts/macos-install-xamarin.sh android ios
./.circleci/scripts/macos-install-android-sdk.sh 25 26 27

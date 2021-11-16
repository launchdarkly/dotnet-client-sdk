#!/bin/bash

set -eu
set +o pipefail

./.circleci/scripts/macos-install-xamarin.sh android ios
./.circleci/scripts/macos-install-android-sdk.sh 25 26 27

# Download and build the osslsigncode tool. In our other .NET projects, code signing
# is handled by our standard project template scripts for .NET, which use a Docker
# image that already has osslsigncode installed. But here we're using a CircleCI job
# to do the build, so we need to do the code-signing in the same place and download
# the tool as needed. Unfortunately it has to be built from source.

SIGNCODE_DOWNLOAD_URL=https://github.com/mtrojnar/osslsigncode/releases/download/2.1/osslsigncode-2.1.0.tar.gz
SIGNCODE_ARCHIVE="${LD_RELEASE_TEMP_DIR}/osslsigncode-2.1.0.tar.gz"
SIGNCODE_DIR="${LD_RELEASE_TEMP_DIR}/osslsigncode"
SIGNCODE="${SIGNCODE_DIR}/osslsigncode"

echo ""
echo "Downloading osslsigncode..."
curl --fail --silent -L "${SIGNCODE_DOWNLOAD_URL}" >"${SIGNCODE_ARCHIVE}"
mkdir -p "${SIGNCODE_DIR}"
tar xfz "${SIGNCODE_ARCHIVE}" -C "${SIGNCODE_DIR}"

echo ""
echo "Building osslsigncode..."
HOMEBREW_NO_AUTO_UPDATE=1 brew install pkg-config
export PKG_CONFIG=$(which pkg-config)
export PKG_CONFIG_PATH=/usr/local/Cellar/openssl@1.1/1.1.1i/lib/pkgconfig
pushd "${SIGNCODE_DIR}/osslsigncode-2.1.0"
./configure
make
cp osslsigncode ..
popd

#!/bin/bash

set -eu
set +o pipefail

./scripts/macos-install-xamarin.sh android ios
./scripts/macos-install-android-sdk.sh 25 26 27

export HOMEBREW_NO_AUTO_UPDATE=1
brew install awscli

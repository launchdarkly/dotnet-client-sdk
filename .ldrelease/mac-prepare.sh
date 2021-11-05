#!/bin/bash

set -eu
set +o pipefail

./.circleci/scripts/macos-install-xamarin.sh android ios
./.circleci/scripts/macos-install-android-sdk.sh 25 26 27

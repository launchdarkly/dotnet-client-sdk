#!/bin/bash

# Standard steps for installing the desired version(s) of the Android SDK tools on
# a MacOS system. This is used both in our CI build and in releases.
#
# This updates the path and other variables in $BASH_ENV; to get the new values,
# run "source $BASH_ENV" (not necessary in CircleCI because CircleCI starts a new
# shell for each step).

# Usage:
# macos-install-android-sdk.sh 27     - installs Android API 27
# macos-install-android-sdk.sh 27 28  - installs Android API 27 & 28... etc.

set -e

if [ -z "$1" ]; then
  echo "must specify at least one Android API version" >&2
  exit 1
fi

ANDROID_SDK_CMDLINE_TOOLS_DOWNLOAD_URL=https://dl.google.com/android/repository/commandlinetools-mac-6858069_latest.zip
ANDROID_BUILD_TOOLS_VERSION=26.0.2

if [ -z "$ANDROID_HOME" ]; then
  export ANDROID_HOME=/usr/local/share/android-sdk
  echo "export ANDROID_HOME=$ANDROID_HOME" >> $BASH_ENV
fi
if [ -z "$ANDROID_SDK_HOME" ]; then
  export ANDROID_SDK_HOME=$ANDROID_HOME
  echo "export ANDROID_SDK_HOME=$ANDROID_SDK_HOME" >> $BASH_ENV
fi
if [ -z "$ANDROID_SDK_ROOT" ]; then
  export ANDROID_SDK_ROOT=$ANDROID_HOME
  echo "export ANDROID_SDK_ROOT=$ANDROID_SDK_ROOT" >> $BASH_ENV
fi

for addpath in \
    /usr/local/share/android-sdk/cmdline-tools/latest/bin \
    /usr/local/share/android-sdk/tools/bin \
    /usr/local/share/android-sdk/platform-tools
do
  echo "export PATH=\$PATH:$addpath" >> $BASH_ENV
done
source $BASH_ENV

# Download the core Android SDK command-line tools - we don't use Homebrew for this,
# because the version they had (as of March 2021) was out of date and incompatible
# with Java 11.
mkdir -p $ANDROID_HOME
mkdir -p $ANDROID_HOME/cmdline-tools
sdk_temp_dir=/tmp/android-sdk-download
rm -rf $sdk_temp_dir
mkdir -p $sdk_temp_dir
echo "Downloading Android tools from $ANDROID_SDK_CMDLINE_TOOLS_DOWNLOAD_URL"
curl "$ANDROID_SDK_CMDLINE_TOOLS_DOWNLOAD_URL" >$sdk_temp_dir/android-sdk.zip
cd $sdk_temp_dir
unzip android-sdk.zip
mv cmdline-tools $ANDROID_HOME/cmdline-tools/latest

sdkmanager_args="platform-tools emulator"
sdkmanager_args="$sdkmanager_args extras;intel;Hardware_Accelerated_Execution_Manager"
sdkmanager_args="$sdkmanager_args build-tools;$ANDROID_BUILD_TOOLS_VERSION"
for apiver in "$@"; do
  sdkmanager_args="$sdkmanager_args platforms;android-$apiver"
  sdkmanager_args="$sdkmanager_args system-images;android-$apiver;default;x86"
done

echo "Installing Android SDK packages: $sdkmanager_args"
yes | sdkmanager $sdkmanager_args | grep -v = || true
yes | sdkmanager --licenses >/dev/null

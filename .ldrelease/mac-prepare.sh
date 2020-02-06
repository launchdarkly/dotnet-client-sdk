#!/bin/bash

set -eu
set +o pipefail

export HOMEBREW_NO_AUTO_UPDATE=1

brew tap isen-ng/dotnet-sdk-versions

brew cask install \
	android-sdk \
	dotnet-sdk-2.2.400 \
	mono-mdk \
	xamarin \
	xamarin-android \
	xamarin-ios

brew install awscli

for path_component in /Library/Frameworks/Mono.framework/Commands /usr/local/share/android-sdk/tools/bin /usr/local/share/android-sdk/platform-tools; do
  echo "export PATH=\"\$PATH:$path_component\"" >> $BASH_ENV
done

echo "export ANDROID_HOME=/usr/local/share/android-sdk" >> $BASH_ENV
echo "export ANDROID_SDK_HOME=/usr/local/share/android-sdk" >> $BASH_ENV
echo "export ANDROID_SDK_ROOT=/usr/local/share/android-sdk" >> $BASH_ENV

source $BASH_ENV

sudo mkdir -p /usr/local/android-sdk-linux/licenses
yes | sdkmanager "platform-tools" "platforms;android-25" "platforms;android-26" "platforms;android-27" "build-tools;26.0.2"
yes | sdkmanager --licenses

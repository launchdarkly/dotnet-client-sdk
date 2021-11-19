#!/bin/bash

# Standard steps for setting up Xamarin command-line build tools on a MacOS system,
# without using the interactive Visual Studio installer. This is used both in our CI
# build and in releases.
#
# This updates the path and other variables in $BASH_ENV; to get the new values,
# run "source $BASH_ENV" (not necessary in CircleCI because CircleCI starts a new
# shell for each step).

# Formerly, we used Homebrew to install the .NET SDK, Mono, and Xamarin. This had the
# advantage of having stable package identifiers rather than download URLs. However,
# these Homebrew installers seem to be no longer maintained. So we are now downloading
# the lower-level installer packages directly from their current locations online,
# which will undoubtedly need to be updated in the future.

# Usage:
# macos-install-xamarin.sh android       - installs Xamarin.Android
# macos-install-xamarin.sh ios           - installs Xamarin.iOS
# macos-install-xamarin.sh android ios   - installs both

set -e

# The .NET SDK 5.0 installer is pinned to a specific version
# See: https://dotnet.microsoft.com/download/dotnet/5.0
DOTNET_SDK_INSTALLER_URL=https://download.visualstudio.microsoft.com/download/pr/88bc1553-e90f-4a4f-9574-65d9a5065cd2/1d5646e1abb8b4d4a61ba0b0be976047/dotnet-sdk-5.0.402-osx-x64.pkg

# Currently we are also pinning the rest of the installers to specific version URLs.
# Alternately, we could use the "latest stable" mode of boots:
#    boots --stable Mono
#    boots --stable Xamarin.Android
#    boots --stable Xamarin.iOS
# Download URLs were found here:
#    https://www.mono-project.com/download/stable/
#    https://github.com/xamarin/xamarin-android
#    https://github.com/xamarin/xamarin-macios
MONO_INSTALLER_URL=https://download.mono-project.com/archive/6.12.0/macos-10-universal/MonoFramework-MDK-6.12.0.122.macos10.xamarin.universal.pkg
XAMARIN_ANDROID_INSTALLER_URL=https://aka.ms/xamarin-android-commercial-d16-8-macos
XAMARIN_IOS_INSTALLER_URL=https://download.visualstudio.microsoft.com/download/pr/7b60a920-c8b1-4798-b660-ae1a7294eb6d/bbdc2a9c6705520fd0a6d04f71e5ed3e/xamarin.ios-14.2.0.12.pkg

for addpath in \
    /Library/Frameworks/Mono.framework/Commands \
    /usr/local/share/dotnet \
    $HOME/.dotnet/tools
do
  echo "export PATH=\$PATH:$addpath" >> $BASH_ENV
done
source $BASH_ENV

# Install .NET SDK
curl -s "$DOTNET_SDK_INSTALLER_URL" >/tmp/dotnet-sdk.pkg
sudo installer -package /tmp/dotnet-sdk.pkg -target /
rm /tmp/dotnet-sdk.pkg

# The "boots" tool is a shortcut for downloading and running package installers. Since
# it is a .NET tool, we can't install it until we've already installed .NET above.
dotnet tool install --global boots

# Install the basic Mono tools (including msbuild)
boots --url "$MONO_INSTALLER_URL"

for arg in "$@"
do
  case "$arg" in
    android)
      boots --url "$XAMARIN_ANDROID_INSTALLER_URL"
      ;;
    ios)
      boots --url "$XAMARIN_IOS_INSTALLER_URL"
      ;;
    *)
      echo "unsupported parameter: $arg" >&2
      exit 1
  esac
done

# In the CircleCI environment, /Applications/Xcode.app might be a symlink. The build
# tools can be confused by this (https://github.com/xamarin/xamarin-macios/issues/11006)
# so we'll just move things around so that is a real file.

if [[ -L /Applications/Xcode.app ]]; then
  cd /Applications
  real_xcode=`readlink Xcode.app`
  rm Xcode.app
  mv "$real_xcode" Xcode.app
  sudo xcode-select --switch /Applications/Xcode.app
fi

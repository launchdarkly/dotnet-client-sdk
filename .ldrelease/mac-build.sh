#!/bin/bash

set -eu

PROJECT_FILE=src/LaunchDarkly.ClientSdk/LaunchDarkly.ClientSdk.csproj

# Build the project for all target frameworks. This does not include building the
# .nupkg, because we didn't set <GeneratePackageOnBuild> in our project file; we
# don't want to create the package until we've signed the assemblies.

msbuild /restore /p:Configuration=Release "${PROJECT_FILE}"

# Sign the code with osslsigncode (which was installed in mac-prepare.sh)

SIGNCODE_DIR="${LD_RELEASE_TEMP_DIR}/osslsigncode"
SIGNCODE="${SIGNCODE_DIR}/osslsigncode"

echo ""
echo "Signing assemblies..."
for dll in $(find ./src/LaunchDarkly.ClientSdk/bin/Release -name LaunchDarkly.ClientSdk.dll); do
  echo -n "${dll}: "
  ${SIGNCODE} sign \
    -certs "${LD_RELEASE_SECRETS_DIR}/dotnet_code_signing_certificate" \
    -key "${LD_RELEASE_SECRETS_DIR}/dotnet_code_signing_private_key" \
    -readpass "${LD_RELEASE_SECRETS_DIR}/dotnet_code_signing_private_key_passphrase" \
    -h sha1 \
    -n "LaunchDarkly" \
    -i "https://www.launchdarkly.com/" \
    -t "http://timestamp.comodoca.com/authenticode" \
    -in "${dll}" \
    -out "${dll}.signed" || (echo "Signing failed; see log for error" >&2; exit 1)
  mv ${dll}.signed ${dll}
done

echo ""
echo "Creating NuGet package"
msbuild /t:pack /p:NoBuild=true /p:Configuration=Release "${PROJECT_FILE}"

#!/bin/bash

set -eu

# Since we are currently publishing in Debug configuration, we can push the .nupkg that build.sh already built.

export AWS_DEFAULT_REGION=us-east-1
NUGET_KEY=$(aws ssm get-parameter --name /production/common/services/nuget/api_key --with-decryption --query "Parameter.Value" --output text)

nuget push "./src/LaunchDarkly.ClientSdk/bin/Debug/LaunchDarkly.XamarinSdk.${LD_RELEASE_VERSION}.nupkg" \
  -ApiKey "${NUGET_KEY}" -Source https://www.nuget.org

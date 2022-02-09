#/!bin/bash

# This script is not an independent build step; it is called from publish.sh or publish-dry-run.sh.
# On successful exit, each project's build directory will contain a .nupkg and possibly also a .snupkg

# For code signing, the osslsigncode command must be present (https://github.com/mtrojnar/osslsigncode).
# This is preinstalled in our dotnet5-release Docker image.

set -eu

projects=$(ls ./src || true)
if [[ -z "${projects}" ]]; then
  echo "No projects were found under ./src; does this solution use the standard directory layout?" >&2
  exit 1
fi

if [ -n "${LD_RELEASE_SKIP_SIGNING:-}" ]; then
  echo "Will skip code signing because LD_RELEASE_SKIP_SIGNING was set"
else
  cert_file_path="${LD_RELEASE_SECRETS_DIR}/dotnet_code_signing_certificate"
  cert_key_file_path="${LD_RELEASE_SECRETS_DIR}/dotnet_code_signing_private_key"
  cert_password_file_path="${LD_RELEASE_SECRETS_DIR}/dotnet_code_signing_private_key_passphrase"
fi

for project in ${projects}; do
  echo
  echo "[$project]: building in Release configuration"
  project_file_path="./src/${project}/${project}.csproj"
  release_products_dir="./src/${project}/bin/Release"

  dotnet clean "${project_file_path}" >/dev/null
  dotnet build -c Release "${project_file_path}"

  if [ -z "${LD_RELEASE_SKIP_SIGNING:-}" ]; then
    dlls=$(find "${release_products_dir}" -name "${project}.dll")
    echo
    echo "[$project]: signing assemblies"
    for dll in ${dlls}; do
      echo -n "$dll: "
      osslsigncode sign \
        -certs "${cert_file_path}" \
        -key "${cert_key_file_path}" \
        -readpass "${cert_password_file_path}" \
        -h sha1 \
        -n "LaunchDarkly" \
        -i "https://www.launchdarkly.com/" \
        -t "http://timestamp.comodoca.com/authenticode" \
        -in "${dll}" \
        -out "${dll}.signed" || (echo "Signing failed; see log for error" >&2; exit 1)
      mv ${dll}.signed ${dll}
    done
  fi

  echo
  echo "[$project]: creating package"
  rm -rf "${release_products_dir}/*.nupkg" "${release_products_dir}/*.snupkg"
  dotnet pack -c Release --no-build "${project_file_path}"
done

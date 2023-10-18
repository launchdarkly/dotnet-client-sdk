#!/bin/bash

# See: https://github.com/launchdarkly/project-releaser/blob/main/docs/templates/dotnet6-linux.md

# This script expects the docfx tool to be in the PATH. It is, as long as we use our pre-built
# ldcircleci/dotnet6-release image (see https://github.com/launchdarkly/sdks-ci-docker).

set -eu

docs_title="${LD_RELEASE_DOCS_TITLE:-}"
if [[ -z "${docs_title}" ]]; then
  echo "LD_RELEASE_DOCS_TITLE was not set - skipping build-docs step"
  exit 0
fi
target_framework="${LD_RELEASE_DOCS_TARGET_FRAMEWORK:-net6.0}"
assembly_names_to_document="${LD_RELEASE_DOCS_ASSEMBLIES:-}"

project_names=$(ls ./src || true)
if [[ -z "${project_names}" ]]; then
  echo "No projects were found under ./src; does this solution use the standard directory layout?" >&2
  exit 1
fi

source "$(dirname $0)/build-docs-helpers.sh"  # provides the functions used below

# Find the assemblies to be documented. If one of them is a dependency rather
# than a project in this directory, its DLL should still have been copied into
# the build products of one of the projects here (as long as the project file
# specified <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>).
if [[ -z "${assembly_names_to_document}" ]]; then
  assembly_names_to_document="${project_names}"
fi
assembly_paths=""
for assembly_name in ${assembly_names_to_document}; do
  dll_path="$(get_documentation_input_path "${assembly_name}" "${target_framework}" ${project_names})"
  if [[ -z "${dll_path}" ]]; then
    echo "Could not find ${assembly_name}.dll in build products" >&2
    exit 1
  fi
  assembly_paths="${assembly_paths} ${dll_path}"
done

# Create the input files for DocFX
temp_docs_dir="${LD_RELEASE_TEMP_DIR}/build-docs"
mkdir -p "${temp_docs_dir}"
make_docs_home_page "${docs_title}" > "${temp_docs_dir}/index.md"
make_docs_nav_bar_data > "${temp_docs_dir}/toc.yml"
make_docs_overwrites > "${temp_docs_dir}/overwrites.md"
make_docfx_config ${assembly_paths} > "${temp_docs_dir}/docfx.json"
cp "$(dirname $0)/docfx-base-links.yml" "${temp_docs_dir}"

# Run the documentation generator
pushd "${temp_docs_dir}"
docfx docfx.json
popd

cp -r ${temp_docs_dir}/build/html/* "${LD_RELEASE_DOCS_DIR}"

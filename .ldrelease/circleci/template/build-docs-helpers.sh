
function get_documentation_input_path() {
  assembly_name="$1"
  target_framework="$2"
  shift; shift
  project_names="$@"
  # We want to locate assembly_name.dll and its corresponding assembly_name.xml docs file. This
  # could be either one of the projects we just built, or a dependency that is being merged into
  # the docs by setting LD_RELEASE_DOCS_ASSEMBLIES. In the latter case, the dependency .dll should
  # be in with the build products of one of the projects we built (assuming the project used the
  # <CopyLocalLockFileAssemblies> option), but we might have to look in the NuGet cache to find
  # the xml file.
  for project_name in ${project_names}; do
    bin_path="src/${project_name}/bin/Debug/${target_framework}"
    dll_path="${bin_path}/${assembly_name}.dll"
    if [ -f "${dll_path}" ]; then
      xml_path="${bin_path}/${assembly_name}.xml"
      if [ ! -f "${xml_path}" ]; then
        # Try the NuGet cache
        lowercase_assembly_name="$(tr '[:upper:]' '[:lower:]' <<< "${assembly_name}")"
        nuget_cache_path="$HOME/.nuget/packages/${lowercase_assembly_name}"
        xml_path="$(find "${nuget_cache_path}" -name "${assembly_name}.xml")"
        if [ -z "${xml_path}" ]; then
          echo "Trying to include ${assembly_name} in the HTML documentation failed because no XML file for it was in the NuGet cache" >&2
          exit 1
        fi
        if [ "$(wc <<< "${xml_path}")" != "1" ]; then
          path_for_target_framework="$(find "${nuget_cache_path}" -name "${target_framework}")"
          if [ -n "${path_for_target_framework}" ]; then
            xml_path="${path_for_target_framework}/${assembly_name}.xml"
          else
            xml_path="$(head -n 1 <<<"${xml_path}")"  # take first result
          fi
        fi
        if [ ! -f "${xml_path}" ]; then
          echo "Can't find ${xml_path}" >&2
          exit 1
        fi
        # Put the .xml file in the same place that the .dll is, so DocFX can find it.
        cp "${xml_path}" "${bin_path}"
      fi
      echo "${dll_path}"
      exit 0
    fi
  done
}

function make_docs_home_page() {
  title="$1"
  echo "# $title"
  echo
  echo "**(version ${LD_RELEASE_VERSION})**"
  echo

  cat "./docs-src/index.md" 2>/dev/null || true  # this file is optional
  echo

  # There's no single entry point for the API documentation, and the sidebar of
  # namespaces doesn't show up on the home page in DocFX's default template, so we'll
  # add a list of them here if they were documented under ./docs-src/namespaces.
  namespace_files="$(ls ./docs-src/namespaces 2>/dev/null || true)"
  if [[ -n "${namespace_files}" ]]; then
    echo
    echo "## Namespaces"
    echo
    for namespace_file in ${namespace_files}; do
      namespace="${namespace_file%.*}"
      summary="$(head -n 1 "./docs-src/namespaces/${namespace_file}")"
      echo "**<xref:${namespace}>**: ${summary}"
      echo
    done
  fi
}

function make_docs_nav_bar_data() {
  # Create a minimal navigation list that just points to the root of the API. The path
  # "build/api" does not exist in the built HTML docs-- it refers to the intermediate
  # build metadata; when DocFX resolves this link during HTML generation, it will become
  # a link to the first namespace in the API. Unfortunately, that link resolution appears
  # to only work in the navbar and not in Markdown pages.
  echo "    - name: API Documentation"
  echo "      href: build/api/"
}

function make_docs_overwrites() {
  # Create a YAML document containing any "overwrite" data that we want to inject into
  # the generated docs, based on the files (if any) that are in the project under docs-src/.
  namespace_files="$(ls ./docs-src/namespaces 2>/dev/null || true)"
  for namespace_file in ${namespace_files}; do
    namespace="${namespace_file%.*}"
    summary="$(head -n 1 "./docs-src/namespaces/${namespace_file}")"
    remarks="$(tail -n +2 "./docs-src/namespaces/${namespace_file}")"
    echo "---"
    echo "uid: ${namespace}"
    echo "summary: *content"
    echo "---"
    echo "${summary}"
    echo
    echo "---"
    echo "uid: ${namespace}"
    echo "remarks: *content"
    echo "---"
    echo "${remarks}"
    echo
  done
}

function make_docfx_config() {
  assembly_paths="$@"
  project_dir="$(pwd)"
  json_files_list=""
  for assembly_path in ${assembly_paths}; do
    if [[ -n "${json_files_list}" ]]; then
      json_files_list="${json_files_list}, "
    fi
    json_files_list="${json_files_list}\"${assembly_path}\""
  done

  cat <<EOF
{
  "metadata": [
    {
      "src": [
        {
          "src": "${project_dir}",
          "files": [ ${json_files_list} ]
        }
      ],
      "dest": "build/api",
      "disableGitFeatures": true,
      "disableDefaultFilter": false
    }
  ],
  "build": {
    "content": [
      {
        "src": "build/api",
        "files": [ "**.yml" ],
        "dest": "api"
      },
      {
        "src": ".",
        "files": [ "toc.yml", "index.md" ]
      }
    ],
    "overwrite": [
      "overwrites.md"
    ],
    "xref": [
      "docfx-base-links.yml"
    ],
    "dest": "build/html",
    "globalMetadata": {
      "_disableContribution": true
    },
    "template": [ "default" ],
    "disableGitFeatures": true
  }
}
EOF
}

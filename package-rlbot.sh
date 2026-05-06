#!/usr/bin/env bash
set -euo pipefail

# package-rlbot.sh - Package RLBotServer binary for multiple Linux distributions
#
# Usage: ./package-rlbot.sh <binary_path> [distro_list]
#
# If no distribution list is provided, it defaults to packaging for ubuntu and fedora
# Supported distributions: ubuntu, fedora
#
# Examples:
#   ./package-rlbot.sh ./RLBotServer ubuntu
#   ./package-rlbot.sh ./RLBotServer  # Defaults to ubuntu and fedora
#
# This script packages the RLBotServer binary for various Linux distributions,
# creating appropriate metadata and package structures.

DEFAULT_DISTROS=("ubuntu" "fedora")

usage() {
  cat <<'EOF'
Usage: ./package-rlbot.sh <binary_path> [distro_list]

If no distribution list is provided, it defaults to packaging all supported distributions.
Supported distributions: ubuntu, fedora.

Examples:
  ./package-rlbot.sh ./RLBotServer ubuntu
  ./package-rlbot.sh ./RLBotServer  # Defaults to ubuntu and fedora

Environment overrides:
  OUTPUT_DIR      Output directory for packages (default: ./dist)
EOF
}

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1" >&2
    return 1
  fi
}

get_git_version() {
  require_cmd git

  local repo_root="$1"
  local latest_tag

  latest_tag="$(git -C "$repo_root" describe --tags --abbrev=0 2>/dev/null || true)"
  if [[ -z "$latest_tag" ]]; then
    echo "Unable to determine version: no git tags found in $repo_root" >&2
    return 1
  fi

  echo "${latest_tag#v}"
}

normalize_rpm_version() {
  local input="$1"

  if [[ -z "$input" ]]; then
    echo "RPM version is empty" >&2
    return 1
  fi

  echo "${input//-/.}"
}

resolve_path() {
  local input_path="$1"
  local dir
  dir="$(cd "$(dirname "$input_path")" && pwd)"
  echo "${dir}/$(basename "$input_path")"
}

ensure_x86_64() {
  local arch_raw="$1"
  case "$arch_raw" in
    x86_64|amd64)
      return 0
      ;;
    *)
      echo "Unsupported architecture: ${arch_raw}. Only x86-64 is supported." >&2
      return 1
      ;;
  esac
}

package_deb() {
  require_cmd dpkg-deb

  local pkg_name="rlbotserver"
  local pkg_dir="${tmp_root}/deb/${pkg_name}_${version}"
  local deb_path="${out_dir}/${pkg_name}_${version}_${deb_arch}.deb"

  rm -rf "$pkg_dir"
  mkdir -p "$pkg_dir/DEBIAN" "$pkg_dir/usr/local/bin"

  install -m 0755 "$binary_path" "$pkg_dir/usr/local/bin/$binary_name"

  cat > "$pkg_dir/DEBIAN/control" <<EOF
Package: ${pkg_name}
Version: ${version}
Section: utils
Priority: optional
Architecture: ${deb_arch}
Maintainer: RLBot Contributors
Description: RLBotServer binary
 RLBotServer is the backend server for RLBot v5, allowing custom bots and scripts to interface with Rocket League.
EOF

  dpkg-deb --build "$pkg_dir" "$deb_path" >/dev/null
  echo "Created ${deb_path}"
}

package_rpm() {
  require_cmd rpmbuild
  require_cmd tar

  local pkg_name="rlbotserver"
  local rpm_root="${tmp_root}/rpm"
  local src_dir="${tmp_root}/${pkg_name}-${rpm_version}"
  local spec_file="${rpm_root}/SPECS/${pkg_name}.spec"

  mkdir -p "${rpm_root}"/{BUILD,RPMS,SOURCES,SPECS,SRPMS}
  rm -rf "$src_dir"
  mkdir -p "$src_dir"

  install -m 0755 "$binary_path" "$src_dir/$binary_name"
  tar -C "$tmp_root" -czf "$rpm_root/SOURCES/${pkg_name}-${rpm_version}.tar.gz" "${pkg_name}-${rpm_version}"

  cat > "$spec_file" <<EOF
Name:           ${pkg_name}
Version:        ${rpm_version}
Release:        1%{?dist}
Summary:        RLBotServer binary

License:        MIT
BuildArch:      ${rpm_arch}
Source0:        %{name}-%{version}.tar.gz

%description
RLBotServer is the backend server for RLBot v5, allowing custom bots and scripts to interface with Rocket League.

%prep
%setup -q

%build

%install
mkdir -p %{buildroot}/usr/local/bin
install -m 0755 ${binary_name} %{buildroot}/usr/local/bin/${binary_name}

%files
/usr/local/bin/${binary_name}

%changelog
* $(date -u "+%a %b %d %Y") RLBot Contributors - ${version}-1
- Packaged RLBotServer binary
EOF

  rpmbuild --define "_topdir ${rpm_root}" -bb "$spec_file" >/dev/null 2>&1

  local rpm_file
  rpm_file="$(find "$rpm_root/RPMS" -type f -name "*.rpm" | head -n 1)"
  if [[ -z "$rpm_file" ]]; then
    echo "RPM build failed: no package found in ${rpm_root}/RPMS" >&2
    return 1
  fi

  cp "$rpm_file" "$out_dir/"
  echo "Created ${out_dir}/$(basename "$rpm_file")"
}

main() {
  if [[ ${1:-} == "-h" || ${1:-} == "--help" ]]; then
    usage
    exit 0
  fi

  if [[ $# -lt 1 ]]; then
    usage
    exit 1
  fi

  binary_path="$1"
  shift

  script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

  if [[ $# -eq 0 ]]; then
    distros=("${DEFAULT_DISTROS[@]}")
  else
    if [[ $# -eq 1 && "$1" == *","* ]]; then
      IFS=',' read -r -a distros <<< "$1"
    else
      distros=("$@")
    fi
  fi

  binary_path="$(resolve_path "$binary_path")"
  if [[ ! -f "$binary_path" ]]; then
    echo "Binary not found: $binary_path" >&2
    exit 1
  fi
  if [[ ! -x "$binary_path" ]]; then
    echo "Binary is not executable: $binary_path" >&2
    exit 1
  fi

  binary_name="$(basename "$binary_path")"

  version="$(get_git_version "$script_dir")"
  rpm_version="$(normalize_rpm_version "$version")"
  out_dir="${OUTPUT_DIR:-dist}"
  mkdir -p "$out_dir"

  arch_raw="$(uname -m)"
  ensure_x86_64 "$arch_raw"
  deb_arch="amd64"
  rpm_arch="x86_64"

  tmp_root="$(mktemp -d)"
  trap 'rm -rf "$tmp_root"' EXIT

  for distro in "${distros[@]}"; do
    distro="${distro,,}"
    case "$distro" in
      ubuntu)
        package_deb
        ;;
      fedora)
        package_rpm
        ;;
      *)
        echo "Unknown distribution: $distro" >&2
        echo "Supported distributions: ubuntu, fedora" >&2
        exit 1
        ;;
    esac
  done
}

main "$@"

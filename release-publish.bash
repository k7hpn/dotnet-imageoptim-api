#!/usr/bin/env bash

set -Eeuo pipefail
trap cleanup SIGINT SIGTERM ERR EXIT

script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" &>/dev/null && pwd -P)

usage() {
  cat <<EOF
Usage: $(basename "${BASH_SOURCE[0]}") [-h] [-v] [-f] -p param_value arg1 [arg2...]

Script description here.

Available options:

-h, --help      Print this help and exit
-v, --verbose   Print script debug info

Environment variables:

- BLD_RELEASE_VERSION - required - version of this build

EOF
  exit
}

cleanup() {
  trap - SIGINT SIGTERM ERR EXIT
}

setup_colors() {
  if [[ -t 2 ]] && [[ -z "${NO_COLOR-}" ]] && [[ "${TERM-}" != "dumb" ]]; then
    NOFORMAT='\033[0m' RED='\033[0;31m' GREEN='\033[0;32m' ORANGE='\033[0;33m' BLUE='\033[0;34m' PURPLE='\033[0;35m' CYAN='\033[0;36m' YELLOW='\033[1;33m'
  else
    NOFORMAT='' RED='' GREEN='' ORANGE='' BLUE='' PURPLE='' CYAN='' YELLOW=''
  fi
}

msg() {
  echo >&2 -e "${1-}"
}

die() {
  local msg=$1
  local code=${2-1} # default exit status 1
  msg "$msg"
  exit "$code"
}

parse_params() {
  while :; do
    case "${1-}" in
    -h | --help) usage ;;
    -v | --verbose) set -x ;;
    --no-color) NO_COLOR=1 ;;
    -?*) die "Unknown option: $1" ;;
    *) break ;;
    esac
    shift
  done

  args=("$@")

  return 0
}

parse_params "$@"
setup_colors

# script logic here

readonly PUB_STARTAT=$SECONDS

msg "${BLUE}===${NOFORMAT} Copying NuGet package..."

mkdir -p "/package" && \
cp /app/ImageOptimApi*.nupkg /package

if [[ -z $NUGET_API_KEY ]]; then
  msg "${RED}===${NOFORMAT} Not uploading NuGet package, API key not set"
else
  msg "${BLUE}===${NOFORMAT} Publishing to NuGet..."
  dotnet nuget push /package/ImageOptimApi.*.nupkg --api-key "${NUGET_API_KEY}" --source https://api.nuget.org/v3/index.json
fi

msg "${PURPLE}===${NOFORMAT} Publish script complete in $((SECONDS - PUB_STARTAT)) seconds."

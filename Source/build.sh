#!/usr/bin/env bash
# Builds 1.5/ and 1.6/Assemblies/TrashbrickBurning.dll with mono's mcs against the
# Krafs.Rimworld.Ref reference assemblies from NuGet, so the game doesn't need to be installed.
set -euo pipefail
cd "$(dirname "$0")/.."
REF_DIR="${REF_DIR:-${TMPDIR:-/tmp}/stb-rimworld-ref}"

build() {
  local game="$1" pkg="$2" extra="${3:-}"
  local dir="$REF_DIR/$pkg"
  if [ ! -d "$dir/ref/net472" ]; then
    mkdir -p "$dir"
    curl -sSL "https://api.nuget.org/v3-flatcontainer/krafs.rimworld.ref/$pkg/krafs.rimworld.ref.$pkg.nupkg" -o "$dir.nupkg"
    (cd "$dir" && unzip -qo "../$pkg.nupkg")
  fi
  local r="$dir/ref/net472"
  mkdir -p "$game/Assemblies"
  mcs -target:library -optimize+ -nostdlib -noconfig \
    -r:"$r/mscorlib.dll" -r:"$r/System.dll" -r:"$r/System.Core.dll" $extra \
    -r:"$r/Assembly-CSharp.dll" -r:"$r/UnityEngine.CoreModule.dll" \
    -out:"$game/Assemblies/TrashbrickBurning.dll" Source/TrashbrickBurning/*.cs
  echo "built $game"
}

build 1.5 1.5.4409
build 1.6 1.6.4871 "-r:$REF_DIR/1.6.4871/ref/net472/netstandard.dll"

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
    -r:"$VEF_DIR/$game/Assemblies/PipeSystem.dll" -r:"$HARMONY" \
    -out:"$game/Assemblies/TrashbrickBurning.dll" Source/TrashbrickBurning/*.cs
  echo "built $game"
}

# Vanilla Expanded Framework's PipeSystem, for the pressurised hot water network. VEF is always
# present at runtime: Vanilla Recycling Expanded, a hard dependency, requires it.
VEF_DIR="$REF_DIR/vef"
if [ ! -f "$VEF_DIR/1.6/Assemblies/PipeSystem.dll" ]; then
  rm -rf "$VEF_DIR"
  git clone -q --depth 1 --filter=blob:none --sparse https://github.com/Vanilla-Expanded/VanillaExpandedFramework.git "$VEF_DIR"
  git -C "$VEF_DIR" sparse-checkout set 1.5/Assemblies 1.6/Assemblies
fi

# Harmony, for the other-fuels patch. At runtime it comes from the Harmony mod, which VEF needs too.
HARMONY="$REF_DIR/harmony/lib/net472/0Harmony.dll"
if [ ! -f "$HARMONY" ]; then
  mkdir -p "$REF_DIR/harmony"
  curl -sSL "https://api.nuget.org/v3-flatcontainer/lib.harmony/2.3.3/lib.harmony.2.3.3.nupkg" -o "$REF_DIR/harmony.nupkg"
  (cd "$REF_DIR/harmony" && unzip -qo ../harmony.nupkg)
fi

build 1.5 1.5.4409
build 1.6 1.6.4871 "-r:$REF_DIR/1.6.4871/ref/net472/netstandard.dll"

# The Dubs Bad Hygiene bridge. Compiled against DBH's own assembly from its public repo, and
# shipped under Mods/DubsBadHygiene so it is only ever loaded alongside DBH.
DBH_DIR="$REF_DIR/dbh"
if [ ! -f "$DBH_DIR/1.6/Assemblies/BadHygiene.dll" ]; then
  rm -rf "$DBH_DIR"
  git clone -q --depth 1 --filter=blob:none --sparse https://github.com/Dubwise56/Dubs-Bad-Hygiene.git "$DBH_DIR"
  git -C "$DBH_DIR" sparse-checkout set 1.5/Assemblies 1.6/Assemblies
fi

build_dbh() {
  local game="$1" pkg="$2" extra="${3:-}"
  local r="$REF_DIR/$pkg/ref/net472"
  local out="Mods/DubsBadHygiene/$game/Assemblies"
  mkdir -p "$out"
  mcs -target:library -optimize+ -nostdlib -noconfig \
    -r:"$r/mscorlib.dll" -r:"$r/System.dll" -r:"$r/System.Core.dll" $extra \
    -r:"$r/Assembly-CSharp.dll" -r:"$r/UnityEngine.CoreModule.dll" \
    -r:"$DBH_DIR/$game/Assemblies/BadHygiene.dll" -r:"$DBH_DIR/$game/Assemblies/0DubCore.dll" -r:"$game/Assemblies/TrashbrickBurning.dll" \
    -out:"$out/TrashbrickBurning.DBH.dll" Source/TrashbrickBurning.DBH/*.cs
  echo "built $out"
}

build_dbh 1.5 1.5.4409
build_dbh 1.6 1.6.4871 "-r:$REF_DIR/1.6.4871/ref/net472/netstandard.dll"

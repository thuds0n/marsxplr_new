#!/usr/bin/env bash
# Editor-free C# compile check for the Unity6 project.
#
# Compiles the project's C# scripts with the Roslyn compiler bundled inside a
# Unity installation, against that installation's engine and editor reference
# assemblies. It approximates Unity's predefined assemblies:
#
#   firstpass  Standard Assets/, Pro Standard Assets/ and Plugins/ (no Editor/)
#   runtime    every other script outside an Editor/ folder
#   editor     every script inside an Editor/ folder
#
# Runtime assemblies are compiled as a macOS player (no UNITY_EDITOR) so that
# editor-only API use outside #if UNITY_EDITOR is caught. Passing this check
# does not prove that Unity compiles the project, imports assets, or plays.
#
# Usage: tools/compile-check.sh [path/to/Unity.app]
# Default editor: /Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app
# Outputs are written to Unity6/Temp/CompileCheck/ (ignored by Git).

set -euo pipefail

repo_root="$(cd "$(dirname "$0")/.." && pwd)"
unity_app="${1:-/Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app}"
scripting="$unity_app/Contents/Resources/Scripting"
dotnet="$scripting/NetCoreRuntime/dotnet"
csc="$scripting/DotNetSdkRoslyn/csc.dll"
managed="$scripting/Managed"
assets="$repo_root/Unity6/Assets"
out="$repo_root/Unity6/Temp/CompileCheck"

for required in "$dotnet" "$csc" "$managed/UnityEngine/UnityEngine.CoreModule.dll"; do
    if [[ ! -e "$required" ]]; then
        echo "error: missing $required (is $unity_app a Unity 6 installation?)" >&2
        exit 2
    fi
done

mkdir -p "$out"

common_args=(
    -nologo -noconfig -nostdlib+ -target:library -langversion:9.0
    -nowarn:0169,0414,0618,0649,0672
    "-reference:$scripting/NetStandard/ref/2.1.0/netstandard.dll"
    "-reference:$assets/Ionic.Zip.dll"
)
for dll in "$managed"/UnityEngine/UnityEngine*.dll; do
    common_args+=("-reference:$dll")
done
for dll in "$scripting"/NetStandard/compat/2.1.0/shims/netfx/*.dll; do
    common_args+=("-reference:$dll")
done

player_defines="-define:UNITY_STANDALONE;UNITY_STANDALONE_OSX;UNITY_6000_3_OR_NEWER;UNITY_2022_3_OR_NEWER;ENABLE_MONO;NET_STANDARD_2_1"
editor_defines="-define:UNITY_EDITOR;UNITY_EDITOR_OSX;UNITY_STANDALONE;UNITY_STANDALONE_OSX;UNITY_6000_3_OR_NEWER;UNITY_2022_3_OR_NEWER;NET_STANDARD_2_1"

collect() {
    # $1: find predicate group selecting files; prints NUL-separated paths.
    (cd "$assets" && eval "find . -name '*.cs' $1 -print0")
}

is_firstpass='\( -path "./Standard Assets/*" -o -path "./Pro Standard Assets/*" -o -path "./Plugins/*" \)'
is_editor='-path "*/Editor/*"'

firstpass=(); runtime=(); editor=()
while IFS= read -r -d '' f; do firstpass+=("$assets/${f#./}"); done < <(collect "$is_firstpass -not $is_editor")
while IFS= read -r -d '' f; do runtime+=("$assets/${f#./}"); done < <(collect "-not $is_firstpass -not $is_editor")
while IFS= read -r -d '' f; do editor+=("$assets/${f#./}"); done < <(collect "$is_editor")

status=0
compile() {
    local name="$1"; shift
    echo "== $name"
    if "$dotnet" "$csc" "${common_args[@]}" "$@" -out:"$out/$name.dll"; then
        echo "   ok"
    else
        status=1
    fi
}

compile Assembly-CSharp-firstpass "$player_defines" "${firstpass[@]}"
compile Assembly-CSharp "$player_defines" "-reference:$out/Assembly-CSharp-firstpass.dll" "${runtime[@]}"

# Editor scripts see editor-only members, so build editor-flavoured copies of
# the runtime assemblies for them to reference.
compile Assembly-CSharp-firstpass.Editor "$editor_defines" "-reference:$managed/UnityEditor.dll" "${firstpass[@]}"
compile Assembly-CSharp.Editor "$editor_defines" "-reference:$managed/UnityEditor.dll" \
    "-reference:$out/Assembly-CSharp-firstpass.Editor.dll" "${runtime[@]}"
compile Assembly-CSharp-Editor "$editor_defines" "-reference:$managed/UnityEditor.dll" \
    "-reference:$out/Assembly-CSharp-firstpass.Editor.dll" "-reference:$out/Assembly-CSharp.Editor.dll" "${editor[@]}"

exit $status

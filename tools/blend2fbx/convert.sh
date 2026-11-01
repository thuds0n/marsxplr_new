#!/bin/sh
# Replaces the Blender (.blend) models in Unity6/Assets with FBX files, so the
# project no longer needs Blender to import them.
#
# Unity imports a .blend by running Blender with Unity's own
# Unity-BlenderToFBX.py and importing the FBX it writes; this script runs that
# same exporter. Each FBX is checked against its .blend (blenddump.py inside
# Blender, then verify.py blend), put next to it, the .blend.meta becomes the
# .fbx.meta (same GUID and mesh IDs, so prefab and scene references hold), and
# the .blend is removed. The originals stay in unity/.
#
# Before replacing anything, Unity's import of the .blend files is dumped with
# ModelImportProbe so verify.sh can compare Unity's import of the FBX files
# with it. Set BASELINE to an existing dump to skip that batch run (for
# example one taken from the open editor).
#
# Needs Blender (5.2.3 was used) and the Unity editor, closed unless BASELINE is set.
# Usage: tools/blend2fbx/convert.sh [work-dir]   (default: $TMPDIR/marsxplr-blend2fbx)
set -eu

ROOT=$(cd "$(dirname "$0")/../.." && pwd)
UNITY_APP=${UNITY_APP:-/Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app}
UNITY=${UNITY:-$UNITY_APP/Contents/MacOS/Unity}
EXPORTER=$UNITY_APP/Contents/Resources/AssetImporting/Unity-BlenderToFBX.py
BLENDER=${BLENDER:-/Applications/Blender.app/Contents/MacOS/Blender}
WORK=${1:-${TMPDIR:-/tmp}/marsxplr-blend2fbx}
rm -rf "$WORK/fbx"
mkdir -p "$WORK/fbx"

cd "$ROOT/Unity6"
MODELS=$(find Assets -iname '*.blend' ! -name '._*' | sort)
if [ -z "$MODELS" ]; then
    echo "No .blend files left in Unity6/Assets."
    exit 0
fi
printf '%s\n' "$MODELS" > "$WORK/models.txt"

if [ -n "${BASELINE:-}" ]; then
    cp "$BASELINE" "$WORK/blend-import.json"
else
    "$UNITY" -batchmode -projectPath "$ROOT/Unity6" \
        -executeMethod ModelImportProbe.DumpBatchMode \
        -probeModels "$(printf '%s\n' "$MODELS" | tr '\n' ';')" -probeReimport 1 \
        -probeOut "$WORK/blend-import.json" -logFile "$WORK/baseline.log"
fi

failed=0
printf '%s\n' "$MODELS" | while IFS= read -r blend; do
    name=$(basename "$blend" .blend)
    echo "== $blend"
    "$BLENDER" -b "$blend" --python "$EXPORTER" -- "$WORK/fbx/$name.fbx" > "$WORK/fbx/$name.export.log" 2>&1
    "$BLENDER" -b "$blend" --python "$ROOT/tools/blend2fbx/blenddump.py" -- "$WORK/fbx/$name.json" > /dev/null 2>&1
    python3 -I "$ROOT/tools/blend2fbx/verify.py" blend "$WORK/fbx/$name.json" "$WORK/fbx/$name.fbx" || exit 1
done || failed=1
if [ "$failed" -ne 0 ]; then
    echo "Verification failed; nothing replaced."
    exit 1
fi

printf '%s\n' "$MODELS" | while IFS= read -r blend; do
    fbx="${blend%.blend}.fbx"
    cp "$WORK/fbx/$(basename "$blend" .blend).fbx" "$fbx"
    mv "$blend.meta" "$fbx.meta"
    rm "$blend"
    echo "Replaced $blend"
done
echo "Now run tools/blend2fbx/verify.sh $WORK"

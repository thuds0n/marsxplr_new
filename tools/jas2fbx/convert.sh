#!/bin/sh
# Replaces the Cheetah 3D (.jas) models in Unity6/Assets with the FBX files
# Cheetah 3D writes for Unity, so the project no longer needs Cheetah 3D to
# import them.
#
# Unity imports a .jas by having Cheetah 3D export Temp/ExportedCheetahFBXFile.fbx
# and importing that; ModelImportProbe copies each of those files out during a
# forced reimport. Each FBX is then put next to its .jas, the .jas.meta becomes
# the .fbx.meta (same GUID and mesh IDs, so prefab and scene references hold),
# and the .jas is removed. The originals stay in unity/.
#
# Before replacing anything, each captured FBX is checked against its .jas
# (verify.py jas), and the probe's dump of Unity's import of the .jas files is
# kept so verify.sh can compare Unity's import of the FBX files with it.
#
# Needs Cheetah 3D (the free version is enough) and the Unity editor, closed.
# Usage: tools/jas2fbx/convert.sh [work-dir]   (default: $TMPDIR/marsxplr-jas2fbx; not under Unity6/Temp,
#                                              which Unity clears on start)
set -eu

ROOT=$(cd "$(dirname "$0")/../.." && pwd)
UNITY=${UNITY:-/Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app/Contents/MacOS/Unity}
WORK=${1:-${TMPDIR:-/tmp}/marsxplr-jas2fbx}
rm -rf "$WORK/fbx"
mkdir -p "$WORK"

cd "$ROOT/Unity6"
MODELS=$(find Assets -iname '*.jas' ! -name '._*' | sort)
if [ -z "$MODELS" ]; then
    echo "No .jas files left in Unity6/Assets."
    exit 0
fi

LIST=$(printf '%s\n' "$MODELS" | tr '\n' ';')
printf '%s\n' "$MODELS" > "$WORK/models.txt"
"$UNITY" -batchmode -projectPath "$ROOT/Unity6" \
    -executeMethod ModelImportProbe.DumpBatchMode \
    -probeModels "$LIST" -probeReimport 1 -captureCheetahFbx "$WORK/fbx" \
    -probeOut "$WORK/jas-import.json" -logFile "$WORK/capture.log"

failed=0
printf '%s\n' "$MODELS" | while IFS= read -r jas; do
    name=$(basename "$jas" .jas)
    echo "== $jas"
    python3 -I "$ROOT/tools/jas2fbx/verify.py" jas "$jas" "$WORK/fbx/$name.fbx" || exit 1
done || failed=1
if [ "$failed" -ne 0 ]; then
    echo "Verification failed; nothing replaced."
    exit 1
fi

printf '%s\n' "$MODELS" | while IFS= read -r jas; do
    fbx="${jas%.jas}.fbx"
    cp "$WORK/fbx/$(basename "$jas" .jas).fbx" "$fbx"
    mv "$jas.meta" "$fbx.meta"
    rm "$jas"
    echo "Replaced $jas"
done
echo "Now run tools/jas2fbx/verify.sh $WORK"

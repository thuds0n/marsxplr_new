#!/bin/sh
# Second check after convert.sh: has Unity imported each FBX exactly as it
# imported the .jas it replaced? Reimports the converted FBX files, dumps the
# result with ModelImportProbe, and compares it with the dump convert.sh took
# of the .jas imports (hierarchy, transforms, mesh names and IDs, vertices,
# normals, UV sets, triangles).
#
# Usage: tools/jas2fbx/verify.sh [work-dir]   (default: $TMPDIR/marsxplr-jas2fbx; not under Unity6/Temp,
#                                              which Unity clears on start)
set -eu

ROOT=$(cd "$(dirname "$0")/../.." && pwd)
UNITY=${UNITY:-/Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app/Contents/MacOS/Unity}
WORK=${1:-${TMPDIR:-/tmp}/marsxplr-jas2fbx}

LIST=$(sed 's/\.jas$/.fbx/' "$WORK/models.txt" | tr '\n' ';')
"$UNITY" -batchmode -projectPath "$ROOT/Unity6" \
    -executeMethod ModelImportProbe.DumpBatchMode \
    -probeModels "$LIST" -probeReimport 1 \
    -probeOut "$WORK/fbx-import.json" -logFile "$WORK/verify.log"

python3 -I "$ROOT/tools/jas2fbx/verify.py" unity "$WORK/jas-import.json" "$WORK/fbx-import.json"

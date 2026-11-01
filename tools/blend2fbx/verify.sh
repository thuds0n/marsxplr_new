#!/bin/sh
# Second check after convert.sh: has Unity imported each FBX exactly as it
# imported the .blend it replaced? Reimports the converted FBX files, dumps the
# result with ModelImportProbe, and compares it with convert.sh's dump of the
# .blend imports (../jas2fbx/verify.py unity).
#
# Needs the Unity editor closed.
# Usage: tools/blend2fbx/verify.sh [work-dir]   (default: $TMPDIR/marsxplr-blend2fbx)
set -eu

ROOT=$(cd "$(dirname "$0")/../.." && pwd)
UNITY=${UNITY:-/Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app/Contents/MacOS/Unity}
WORK=${1:-${TMPDIR:-/tmp}/marsxplr-blend2fbx}

LIST=$(sed 's/\.blend$/.fbx/' "$WORK/models.txt" | tr '\n' ';')
"$UNITY" -batchmode -projectPath "$ROOT/Unity6" \
    -executeMethod ModelImportProbe.DumpBatchMode \
    -probeModels "$LIST" -probeReimport 1 \
    -probeOut "$WORK/fbx-import.json" -logFile "$WORK/verify.log"

python3 -I "$ROOT/tools/jas2fbx/verify.py" unity "$WORK/blend-import.json" "$WORK/fbx-import.json"

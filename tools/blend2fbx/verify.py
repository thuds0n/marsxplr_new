#!/usr/bin/env python3
"""Check a converted model against its .blend source.

  verify.py blend SOURCE.json CONVERTED.fbx
      SOURCE.json is blenddump.py's dump of the .blend, read by Blender itself;
      the FBX is read independently with ../jas2fbx/fbxread.py. Checks that
      the FBX holds every mesh object with the same name, parent and local
      transform (converted to FBX's Y-up axes), and the same polygons: corner
      positions, UV sets and material slot.

Unity's import of the FBX against its import of the .blend is checked with
../jas2fbx/verify.py unity. Exits with status 1 on any difference.
"""

import math
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "jas2fbx"))

import fbxread
from verify import close, euler_matrix, fbx_props, fbx_scene, match_polygons

import json

# Blender (Z up) to FBX (Y up) as Unity-BlenderToFBX.py writes it: geometry
# stays in Blender's local axes and units, a root object's transform gains a
# -90 degree X rotation, and positions map (x, y, z) -> (x, z, -y).
AXIS = [[1, 0, 0], [0, 0, 1], [0, -1, 0]]


def mul(a, b):
    return [[sum(a[i][k] * b[k][j] for k in range(3)) for j in range(3)] for i in range(3)]


def transpose(a):
    return [list(r) for r in zip(*a)]


def expected_local(blend, is_root):
    m = blend["matrix"]
    rot = [row[:3] for row in m[:3]]
    pos = [m[0][3], m[1][3], m[2][3]]
    if not is_root:
        return rot, pos
    # FBX root matrix = AXIS * M * AXIS^T * Rx(-90): the object's own axes stay Blender's.
    rx = [[1, 0, 0], [0, 0, 1], [0, -1, 0]]
    return mul(mul(mul(AXIS, rot), transpose(AXIS)), rx), [sum(AXIS[i][k] * pos[k] for k in range(3)) for i in range(3)]


def fbx_local(model):
    props = fbx_props(model)
    pos = list(props.get("Lcl Translation", [0.0, 0.0, 0.0]))
    rot = euler_matrix(props.get("Lcl Rotation", [0.0, 0.0, 0.0]))
    scale = props.get("Lcl Scaling", [1.0, 1.0, 1.0])
    rot = [[rot[3 * i + j] * scale[j] for j in range(3)] for i in range(3)]
    return rot, pos


def fbx_polygons(geometry):
    flat = geometry.find("Vertices").props[0]
    verts = [tuple(flat[i:i + 3]) for i in range(0, len(flat), 3)]
    polys, cur = [], []
    for i in geometry.find("PolygonVertexIndex").props[0]:
        cur.append(~i if i < 0 else i)
        if i < 0:
            polys.append(cur)
            cur = []
    layers = []
    for layer in geometry.findall("LayerElementUV"):
        uv, index = layer.find("UV").props[0], layer.find("UVIndex").props[0]
        layers.append([(uv[2 * k], uv[2 * k + 1]) for k in index])
    element = geometry.find("LayerElementMaterial")
    materials = element.find("Materials").props[0] if element else [0]
    if len(materials) == 1:
        materials = materials * len(polys)
    out, corner = [], 0
    for n, poly in enumerate(polys):
        out.append(([(verts[v],) + tuple(layer[corner + k] for layer in layers) for k, v in enumerate(poly)],
                    materials[n]))
        corner += len(poly)
    return out


def check_blend(json_path, fbx_path):
    source = json.load(open(json_path))
    models = fbx_scene(fbx_path)
    by_name = {path.split("/")[-1]: (path, model, geometry) for path, (model, geometry) in models.items()}
    ok = True
    for name, blend in sorted(source.items()):
        if blend["modifiers"]:
            print("  %s: has modifiers %s; not checked" % (name, blend["modifiers"]))
            ok = False
            continue
        if name not in by_name or by_name[name][2] is None:
            print("  %s: missing from FBX" % name)
            ok = False
            continue
        path, model, geometry = by_name[name]
        parent = path.split("/")[-2] if "/" in path else None
        if parent != blend["parent"]:
            print("  %s: parent %s, expected %s" % (name, parent, blend["parent"]))
            ok = False
        want_rot, want_pos = expected_local(blend, blend["parent"] is None)
        got_rot, got_pos = fbx_local(model)
        if not (close(sum(want_rot, []), sum(got_rot, []), 1e-5) and close(want_pos, got_pos)):
            print("  %s: transform differs\n    want %s %s\n    got  %s %s" % (name, want_rot, want_pos, got_rot, got_pos))
            ok = False
        expected = [([tuple(tuple(x) for x in c) for c in corners], mat) for corners, mat in blend["polys"]]
        missing, extra = match_polygons(expected, fbx_polygons(geometry))
        print("  %s (mesh %s): %d polygons, %d missing, %d extra"
              % (name, blend["mesh"], len(expected), len(missing), len(extra)))
        ok = ok and not missing and not extra
    return ok


def main(argv):
    if len(argv) != 4 or argv[1] != "blend":
        print(__doc__)
        return 2
    return 0 if check_blend(argv[2], argv[3]) else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))

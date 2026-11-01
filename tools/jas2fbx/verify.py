#!/usr/bin/env python3
"""Check a converted model against its source.

  verify.py jas  SOURCE.jas CONVERTED.fbx
      Reads both files independently and checks that the FBX holds every
      exportable object of the .jas with the same hierarchy, transform and
      pivot, and the same polygons: corner positions, both UV sets and
      material slot, including the copies that SYMMETRYMOD modifiers mirror.

  verify.py unity BEFORE.json AFTER.json
      Compares two ModelImportProbe dumps (what Unity imported) asset by
      asset, matched by file name without extension: hierarchy, transforms,
      mesh names and local IDs, vertices, normals, UV sets and triangles.

Exits with status 1 on any difference.
"""

import collections
import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import fbxread
import jasread

EXPORTED = ("NGON", "FOLDER")
UNIT = 100.0  # Cheetah writes FBX in centimetres
TOL = 1e-4    # in .jas units (metres)


def close(a, b, tol=TOL):
    return all(math.isclose(x, y, rel_tol=1e-5, abs_tol=tol) for x, y in zip(a, b)) and len(a) == len(b)


def close_uv(source, converted):
    # A few degenerate polygons in the originals carry NaN UVs; Cheetah writes
    # the whole pair as NaN.
    if any(math.isnan(x) for x in source):
        return all(math.isnan(x) for x in converted)
    return close(source, converted)


def euler_matrix(angles):
    """Row-major 3x3 matrix for FBX's default XYZ order (X applied first)."""
    x, y, z = (math.radians(a) for a in angles)
    cx, sx, cy, sy, cz, sz = math.cos(x), math.sin(x), math.cos(y), math.sin(y), math.cos(z), math.sin(z)
    return [cy * cz, sx * sy * cz - cx * sz, cx * sy * cz + sx * sz,
            cy * sz, sx * sy * sz + cx * cz, cx * sy * sz - sx * cz,
            -sy, sx * cy, cx * cy]


# ---------------------------------------------------------------- FBX side

def fbx_scene(path):
    _, root = fbxread.read(path)
    objects = root.find("Objects")
    by_id = {o.props[0]: o for o in objects.children}
    children = collections.defaultdict(list)
    parent = {}
    for c in root.find("Connections").children:
        if c.props[0] == "OO":
            children[c.props[2]].append(c.props[1])
            parent[c.props[1]] = c.props[2]
    models = {}
    for m in objects.findall("Model"):
        names = []
        node = m
        while node is not None:
            names.append(node.props[1].split("\0")[0])
            node = by_id.get(parent.get(node.props[0]))
            if node is not None and node.name != "Model":
                node = None
        geometry = [by_id[i] for i in children[m.props[0]] if i in by_id and by_id[i].name == "Geometry"]
        models["/".join(reversed(names))] = (m, geometry[0] if geometry else None)
    return models


def fbx_props(model):
    props = {}
    p70 = model.find("Properties70")
    for p in p70.children if p70 else []:
        props[p.props[0]] = p.props[4:]
    return props


def fbx_polygons(geometry):
    flat = geometry.find("Vertices").props[0]
    verts = [tuple(v / UNIT for v in flat[i:i + 3]) for i in range(0, len(flat), 3)]
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
    materials = geometry.find("LayerElementMaterial").find("Materials").props[0]
    if len(materials) == 1:
        materials = materials * len(polys)
    out, corner = [], 0
    for n, poly in enumerate(polys):
        corners = []
        for k, v in enumerate(poly):
            corners.append((verts[v],) + tuple(layer[corner + k] for layer in layers))
        corner += len(poly)
        out.append((corners, materials[n]))
    return out, len(verts)


# ---------------------------------------------------------------- .jas side

def mirror(point, axis, origin):
    p = list(point)
    p[axis] = 2 * origin - p[axis]
    return tuple(p)


def snap(point, axis, origin, radius):
    # Strictly inside: a vertex exactly one radius from the plane stays put.
    if abs(point[axis] - origin) >= radius:
        return point
    p = list(point)
    p[axis] = origin
    return tuple(p)


def jas_polygons(obj):
    """Polygons as the FBX should hold them: winding reversed, V flipped, and
    the material tag's UV scale and offset applied to the first UV set."""
    # The last matching tag wins; its slot is that tag's position in the list.
    tags = obj.shader_tags
    for tag in tags:
        if tag.get("shaderTagUVRotation", 0.0):
            raise NotImplementedError("%s: material tag with UV rotation" % obj.path)
    tag_slot = [None] * len(obj.polygons)
    for index, tag in enumerate(tags):
        sel = tag.get("shaderTagSelection", 0)
        for i in range(len(obj.polygons)):
            if sel == 0 or (obj.selection[i] >> (sel - 1)) & 1:
                tag_slot[i] = index
    out = []
    for i, poly in enumerate(obj.polygons):
        tag = tags[tag_slot[i]] if tag_slot[i] is not None else {}
        su, sv = tag.get("shaderTagUVScale", [1.0, 1.0])
        ou, ov = tag.get("shaderTagUVOffset", [0.0, 0.0])
        corners = [(tuple(obj.vertices[v]), (uv[0] * su + ou, 1 - (uv[1] * sv + ov)), (uv[2], 1 - uv[3]))
                   for v, uv in zip(poly, obj.uvs[i])]
        slot = -1 if tag_slot[i] is None else tag_slot[i]
        out.append((list(reversed(corners)), slot))
    for sym in obj.symmetry:
        axis = sym.raw.get("symmetryPlain", 0)
        origin = sym.position[axis]
        if sym.raw.get("symmetryMelt", False):
            # Melt: vertices within the radius of the plane are moved onto it,
            # so the two halves share them.
            radius = sym.raw.get("symmetryRadius", 0.0)
            out = [([(snap(c[0], axis, origin, radius),) + c[1:] for c in corners], slot)
                   for corners, slot in out]
        out += [([(mirror(c[0], axis, origin),) + c[1:] for c in reversed(corners)], slot)
                for corners, slot in list(out)]
    return out


# ---------------------------------------------------------------- comparison

def canonical(corners):
    """Rotate a polygon so its smallest corner (by rounded position) leads."""
    keys = [tuple(round(x, 3) for x in c[0]) for c in corners]
    start = keys.index(min(keys))
    return corners[start:] + corners[:start]


def poly_key(corners):
    # Order-free, so polygons smaller than the rounding still share a bucket.
    return tuple(sorted(tuple(round(x, 3) for x in c[0]) for c in corners))


def same_polygon(a, b):
    """Same material and corners in the same cyclic order (winding), from any start."""
    ca, cb = a[0], b[0]
    if a[1] != b[1] or len(ca) != len(cb):
        return False
    for start in range(len(cb)):
        rotated = cb[start:] + cb[:start]
        if all(close(x[0], y[0]) and all(close_uv(u, w) for u, w in zip(x[1:], y[1:]))
               for x, y in zip(ca, rotated)):
            return True
    return False


def match_polygons(expected, actual):
    buckets = collections.defaultdict(list)
    for p in actual:
        buckets[poly_key(p[0])].append(p)
    missing = []
    for p in expected:
        candidates = buckets.get(poly_key(p[0]), [])
        hit = next((c for c in candidates if same_polygon(p, c)), None)
        if hit is None:
            missing.append(p)
        else:
            candidates.remove(hit)
    extra = [p for cs in buckets.values() for p in cs]
    # Rounding can put a matching pair in different buckets: pair the leftovers directly.
    for p in list(missing):
        hit = next((c for c in extra if same_polygon(p, c)), None)
        if hit is not None:
            missing.remove(p)
            extra.remove(hit)
    return missing, extra


def check_jas(jas_path, fbx_path):
    _, roots, _ = jasread.read(jas_path)
    models = fbx_scene(fbx_path)
    problems = []
    seen = set()

    def walk(obj, prefix):
        if obj.type not in EXPORTED:
            return
        path = prefix + obj.name
        seen.add(path)
        if path not in models:
            problems.append("missing object %s" % path)
            return
        model, geometry = models[path]
        props = fbx_props(model)
        for name, expected in (("Lcl Translation", [v * UNIT for v in obj.position]),
                               ("Lcl Scaling", obj.scale),
                               ("RotationPivot", [v * UNIT for v in obj.pivot]),
                               ("ScalingPivot", [v * UNIT for v in obj.pivot])):
            actual = props.get(name, [1.0] * 3 if name == "Lcl Scaling" else [0.0] * 3)
            if not close(actual, expected, 1e-3):
                problems.append("%s: %s %s, expected %s" % (path, name, list(actual), expected))
        # Cheetah stores heading, pitch, bank; FBX stores X (pitch), Y (heading),
        # Z (bank). Compare as rotations, since (180, 360, 180) equals (0, 180, 0).
        heading, pitch, bank = obj.rotation
        expected_rotation = [pitch, heading, bank]
        rotation = props.get("Lcl Rotation", [0.0] * 3)
        if not close(euler_matrix(rotation), euler_matrix(expected_rotation), 1e-5):
            problems.append("%s: Lcl Rotation %s, expected %s" % (path, list(rotation), expected_rotation))
        if obj.type == "NGON":
            if geometry is None:
                problems.append("%s: no geometry" % path)
            else:
                expected = jas_polygons(obj)
                actual, vertex_count = fbx_polygons(geometry)
                missing, extra = match_polygons(expected, actual)
                if missing or extra:
                    problems.append("%s: %d of %d polygons unmatched, %d unexpected" %
                                    (path, len(missing), len(expected), len(extra)))
                print("  %-40s %6d polygons, %6d vertices%s" %
                      (path, len(actual), vertex_count, " (symmetry)" if obj.symmetry else ""))
        for child in obj.children:
            walk(child, path + "/")

    for root in roots:
        walk(root, "")
    for path in models:
        if path not in seen:
            problems.append("unexpected object %s" % path)
    return problems


def check_unity(before_path, after_path):
    def by_name(path):
        with open(path) as f:
            data = json.load(f)
        return {os.path.splitext(os.path.basename(k))[0]: v for k, v in data.items()}

    before, after = by_name(before_path), by_name(after_path)
    problems = []
    for name in sorted(set(before) | set(after)):
        a, b = before.get(name), after.get(name)
        if a is None or b is None:
            problems.append("%s: present in only one dump" % name)
            continue
        if a["nodes"] != b["nodes"]:
            problems.append("%s: hierarchy or transforms differ" % name)
        meshes_a = {m["name"]: m for m in a["meshes"]}
        meshes_b = {m["name"]: m for m in b["meshes"]}
        if set(meshes_a) != set(meshes_b):
            problems.append("%s: meshes %s vs %s" % (name, sorted(meshes_a), sorted(meshes_b)))
        for mesh in sorted(set(meshes_a) & set(meshes_b)):
            for field in ("localId", "vertices", "normals", "uv", "uv2", "submeshes", "bounds"):
                if meshes_a[mesh][field] != meshes_b[mesh][field]:
                    problems.append("%s/%s: %s differ" % (name, mesh, field))
        print("  %-24s %d nodes, %d meshes" % (name, len(b["nodes"]), len(meshes_b)))
    return problems


def main(argv):
    if len(argv) != 4 or argv[1] not in ("jas", "unity"):
        print(__doc__)
        return 2
    problems = check_jas(argv[2], argv[3]) if argv[1] == "jas" else check_unity(argv[2], argv[3])
    for p in problems:
        print("  FAIL " + p)
    print("  %s" % ("OK" if not problems else "%d problem(s)" % len(problems)))
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))

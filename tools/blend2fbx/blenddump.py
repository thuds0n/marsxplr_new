# Run inside Blender: blender -b SOURCE.blend --python blenddump.py -- OUT.json
# Writes every mesh object of the .blend as Blender stores it: local matrix,
# mesh name, and each polygon's corners (position, every UV set) and
# material slot. verify.py compares this with the converted FBX.
import bpy
import json
import sys

out = sys.argv[sys.argv.index("--") + 1]
result = {}
for o in bpy.data.objects:
    if o.type != 'MESH':
        continue
    m = o.data
    uvs = [layer.data for layer in m.uv_layers]
    polys = []
    for p in m.polygons:
        corners = [[list(m.vertices[m.loops[i].vertex_index].co)] + [list(u[i].uv) for u in uvs]
                   for i in p.loop_indices]
        polys.append([corners, p.material_index])
    result[o.name] = {
        "parent": o.parent.name if o.parent else None,
        "mesh": m.name,
        "matrix": [list(row) for row in o.matrix_local],
        "modifiers": [mod.type for mod in o.modifiers],
        "polys": polys,
    }
with open(out, "w") as f:
    json.dump(result, f)

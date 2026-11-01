"""Reader for Cheetah 3D .jas scenes (Apple binary property lists).

Only what the Mars Explorer models use: FOLDER and NGON (polygon mesh)
objects, their transforms and pivots, SYMMETRYMOD modifiers, and material
assignment through SHADERTAG polygon selections. Cameras and lights are
returned as objects but carry no geometry.

Layout of an NGON's binary fields (all big-endian):
  vertex          4 floats per vertex (x, y, z, w)
  polygons        per polygon: -n, then n vertex indices
  uvcoords        4 floats per polygon corner (u0, v0, u1, v1)
  polygonselection one int32 bitmask per polygon; SHADERTAG shaderTagSelection
                  n (n > 0) applies to polygons with bit n-1 set, 0 to all
"""

import plistlib
import struct


class JasObject:
    def __init__(self, raw, parent):
        self.raw = raw
        self.parent = parent
        self.type = raw["type"]
        self.name = raw["name"]
        self.position = raw["position"]
        self.rotation = raw["rotation"]
        self.scale = raw["scale"]
        self.pivot = raw["pivot"]
        self.children = [JasObject(c, self) for c in raw.get("childs", [])]
        self.vertices = []
        self.polygons = []
        self.uvs = []
        self.selection = []
        if self.type == "NGON":
            self._read_mesh()

    def _read_mesh(self):
        raw = self.raw
        count = raw["vertexcount"]
        flat = struct.unpack(">%df" % (count * 4), raw["vertex"])
        self.vertices = [flat[i * 4:i * 4 + 3] for i in range(count)]

        ints = struct.unpack(">%di" % (len(raw["polygons"]) // 4), raw["polygons"])
        pos = 0
        while pos < len(ints):
            n = -ints[pos]
            if n <= 0:
                raise ValueError("%s: bad polygon header %d" % (self.name, ints[pos]))
            self.polygons.append(list(ints[pos + 1:pos + 1 + n]))
            pos += 1 + n
        if len(self.polygons) != raw["polygoncount"]:
            raise ValueError("%s: %d polygons, header says %d" % (self.name, len(self.polygons), raw["polygoncount"]))

        corners = sum(len(p) for p in self.polygons)
        uv = struct.unpack(">%df" % (corners * 4), raw["uvcoords"][:corners * 16])
        pos = 0
        for poly in self.polygons:
            self.uvs.append([uv[(pos + k) * 4:(pos + k) * 4 + 4] for k in range(len(poly))])
            pos += len(poly)

        sel = raw.get("polygonselection", b"")
        self.selection = list(struct.unpack(">%di" % (len(sel) // 4), sel)) if sel else [0] * len(self.polygons)

    @property
    def shader_tags(self):
        return [t for t in self.raw.get("tags", []) if t["type"] == "SHADERTAG"]

    @property
    def symmetry(self):
        return [c for c in self.children if c.type == "SYMMETRYMOD"]

    def walk(self):
        yield self
        for c in self.children:
            yield from c.walk()

    @property
    def path(self):
        names = []
        node = self
        while node is not None:
            names.append(node.name)
            node = node.parent
        return "/".join(reversed(names))


def read(path):
    with open(path, "rb") as f:
        doc = plistlib.load(f)
    roots = [JasObject(o, None) for o in doc["Objects"]]
    materials = {m["ID"]: m["name"] for m in doc.get("Materials3", [])}
    return doc["Version"].strip(), roots, materials

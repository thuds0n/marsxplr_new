"""Minimal reader for binary FBX (versions 7.x), enough to check geometry.

Returns a tree of Node(name, props, children). Arrays are decoded into Python
lists. Only the Python standard library is used.
"""

import struct
import zlib


class Node:
    def __init__(self, name, props, children):
        self.name = name
        self.props = props
        self.children = children

    def find(self, name):
        return next((c for c in self.children if c.name == name), None)

    def findall(self, name):
        return [c for c in self.children if c.name == name]

    def __repr__(self):
        return "Node(%s, %d props, %d children)" % (self.name, len(self.props), len(self.children))


_ARRAY = {"f": ("f", 4), "d": ("d", 8), "l": ("q", 8), "i": ("i", 4), "b": ("?", 1)}
_SCALAR = {"Y": ("h", 2), "C": ("?", 1), "I": ("i", 4), "F": ("f", 4), "D": ("d", 8), "L": ("q", 8)}


def _prop(data, pos):
    kind = chr(data[pos])
    pos += 1
    if kind in _SCALAR:
        fmt, size = _SCALAR[kind]
        return struct.unpack_from("<" + fmt, data, pos)[0], pos + size
    if kind in _ARRAY:
        fmt, size = _ARRAY[kind]
        count, encoding, length = struct.unpack_from("<III", data, pos)
        pos += 12
        raw = data[pos:pos + length]
        if encoding == 1:
            raw = zlib.decompress(raw)
        return list(struct.unpack("<%d%s" % (count, fmt), raw[:count * size])), pos + length
    if kind in "SR":
        (length,) = struct.unpack_from("<I", data, pos)
        pos += 4
        raw = data[pos:pos + length]
        return (raw.decode("utf-8", "replace") if kind == "S" else raw), pos + length
    raise ValueError("unknown FBX property type %r at %d" % (kind, pos - 1))


def _node(data, pos, wide):
    if wide:
        end, count, _, name_len = struct.unpack_from("<QQQB", data, pos)
        pos += 25
    else:
        end, count, _, name_len = struct.unpack_from("<IIIB", data, pos)
        pos += 13
    if end == 0:
        return None, pos
    name = data[pos:pos + name_len].decode("ascii")
    pos += name_len
    props = []
    for _ in range(count):
        value, pos = _prop(data, pos)
        props.append(value)
    children = []
    sentinel = 25 if wide else 13
    while pos < end - sentinel or (pos < end and any(data[pos:end])):
        child, pos = _node(data, pos, wide)
        if child is None:
            break
        children.append(child)
    return Node(name, props, children), end


def read(path):
    with open(path, "rb") as f:
        data = f.read()
    if not data.startswith(b"Kaydara FBX Binary  \x00"):
        raise ValueError("%s is not a binary FBX file" % path)
    (version,) = struct.unpack_from("<I", data, 23)
    wide = version >= 7500
    pos, top = 27, []
    while pos < len(data):
        node, pos = _node(data, pos, wide)
        if node is None:
            break
        top.append(node)
    return version, Node("", [], top)

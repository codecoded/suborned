"""Store glTF images beside each Godot mesh so a fresh import can compress them first."""

import argparse
import json
import struct
from pathlib import Path


def pad(value, byte):
    return value + byte * (-len(value) % 4)


def split_images(path):
    source = path.read_bytes()
    length = struct.unpack_from("<I", source, 12)[0]
    doc = json.loads(source[20 : 20 + length])
    binary = source[28 + length :]
    image_views = set()
    for image in doc.get("images", []):
        if "bufferView" not in image:
            continue
        index = image.pop("bufferView")
        view = doc["bufferViews"][index]
        start = view.get("byteOffset", 0)
        data = binary[start : start + view["byteLength"]]
        suffix = {"image/png": ".png", "image/jpeg": ".jpg"}[image.pop("mimeType")]
        filename = path.stem + "_" + image["name"] + suffix
        if Path(filename).name != filename:
            raise ValueError(f"Invalid embedded image name {filename}")
        (path.parent / filename).write_bytes(data)
        image["uri"] = filename
        image_views.add(index)
    if not image_views:
        return
    compact = bytearray()
    indices = {}
    views = []
    for index, view in enumerate(doc.pop("bufferViews")):
        if index in image_views:
            continue
        indices[index] = len(views)
        begin = view.get("byteOffset", 0)
        view["byteOffset"] = len(compact)
        compact.extend(pad(binary[begin : begin + view["byteLength"]], b"\0"))
        views.append(view)

    def remap(node):
        if isinstance(node, list):
            for item in node:
                remap(item)
        elif isinstance(node, dict):
            for key, value in node.items():
                if key == "bufferView":
                    node[key] = indices[value]
                else:
                    remap(value)

    remap(doc)
    doc["bufferViews"] = views
    doc["buffers"][0]["byteLength"] = len(compact)
    metadata = pad(json.dumps(doc, separators=(",", ":")).encode(), b" ")
    total = 28 + len(metadata) + len(compact)
    with path.open("wb") as output:
        output.write(struct.pack("<4sII", b"glTF", 2, total))
        output.write(struct.pack("<I4s", len(metadata), b"JSON") + metadata)
        output.write(struct.pack("<I4s", len(compact), b"BIN\0") + compact)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mesh", nargs="+", type=Path)
    for mesh in parser.parse_args().mesh:
        split_images(mesh)

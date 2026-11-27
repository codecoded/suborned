"""Inventory source meshes and material slots with Blender's FBX reader.

Run Blender in background with --python-exit-code 1 --python tools/decokit/audit_fbx.py
and pass -- <output.json> [asset-directory]. Each mesh and each authored LOD is a separate row.
"""
import argparse
import hashlib
import json
import sys
from pathlib import Path

import bpy
import numpy as np


def child(node, name):
    return next((element for element in node.elems if element.id == name), None)


def mesh_row(geometry):
    corners = np.asarray(child(geometry, b"PolygonVertexIndex").props[0])
    lengths = np.diff(np.r_[-1, np.flatnonzero(corners < 0)])
    triangles = np.maximum(lengths - 2, 0)
    materials = child(geometry, b"LayerElementMaterial")
    assignments = child(materials, b"Materials") if materials else None
    slots = np.asarray(assignments.props[0]) if assignments else np.array([0])
    if len(slots) == len(triangles):
        counts = {str(s): int(triangles[slots == s].sum()) for s in np.unique(slots)}
    elif len(slots) <= 1:
        counts = {str(slots[0] if len(slots) else 0): int(triangles.sum())}
    else:
        raise ValueError("Unrecognised polygon material assignment")
    return {
        "mesh": geometry.props[1].split(b"\x00")[0].decode(),
        "triangles": int(triangles.sum()),
        "trianglesBySlot": counts,
        "uvSets": sum(e.id == b"LayerElementUV" for e in geometry.elems),
    }


def inventory(root):
    for scripts in bpy.utils.script_paths():
        addons = Path(scripts) / "addons_core"
        if (addons / "io_scene_fbx").is_dir():
            sys.path.insert(0, str(addons))
            break
    from io_scene_fbx import parse_fbx

    files = []
    cache = {}
    for path in sorted(p for p in root.rglob("*") if p.suffix.lower() == ".fbx"):
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        if digest not in cache:
            document, _ = parse_fbx.parse(str(path))
            objects = child(document, b"Objects")
            cache[digest] = [
                mesh_row(g) for g in (objects.elems if objects else ())
                if g.id == b"Geometry" and g.props[2] == b"Mesh" and child(g, b"PolygonVertexIndex")
            ]
        files.append({"file": path.relative_to(root).as_posix(), "sha256": digest, "meshes": cache[digest]})
    return {"files": files, "uniqueFileContents": len(cache)}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Inventory FBX source geometry and material slots.")
    parser.add_argument("output", type=Path)
    parser.add_argument("assets", type=Path, nargs="?", default=Path(__file__).resolve().parents[2] / "unity/Assets/Suborned")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    if not args.assets.is_dir():
        parser.error(f"Missing asset directory {args.assets}")
    result = inventory(args.assets)
    output = args.output
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, indent=2) + "\n")
    print(f"Audited {len(result['files'])} FBX files, {result['uniqueFileContents']} distinct files")

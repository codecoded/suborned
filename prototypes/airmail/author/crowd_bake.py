"""Bake Harbour's retargeted LOD1 figures into one instanced mesh library and a pose texture."""

import argparse
import io
import json
import struct
from pathlib import Path

import numpy as np
from PIL import Image
from scipy.spatial.transform import Rotation, Slerp

FIGURES = ["d2m", "d4", "cafe_w2", "d5c", "d6", "el1", "cafe_w5", "d4b"]
CLIPS = [("walk", 16), ("idle", 12), ("talk", 16)]
WIDTH = 512
ROWS = sum(count + 1 for _, count in CLIPS)
LOW = np.array([-1.05, -0.04, -0.95])
SPAN = np.array([2.10, 2.15, 1.90])
TYPES = {5121: "u1", 5123: "<u2", 5125: "<u4", 5126: "<f4"}
DIMS = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}


class Figure:
    def __init__(self, path):
        data = path.read_bytes()
        length = struct.unpack_from("<I", data, 12)[0]
        self.doc = json.loads(data[20 : 20 + length])
        self.binary = data[28 + length :]
        self.nodes = self.doc["nodes"]
        self.primitive = self.doc["meshes"][0]["primitives"][0]
        self.attributes = {
            k: self.array(v) for k, v in self.primitive["attributes"].items()
        }
        skin = self.doc["skins"][0]
        self.joints = skin["joints"]
        self.bind = (
            self.array(skin["inverseBindMatrices"]).reshape(-1, 4, 4).transpose(0, 2, 1)
        )
        self.parents = {}
        self.tree_order = []
        for root in self.doc["scenes"][0]["nodes"]:
            self.visit(root, None)

    def visit(self, index, parent):
        self.parents[index] = parent
        self.tree_order.append(index)
        for child in self.nodes[index].get("children", []):
            self.visit(child, index)

    def array(self, idx):
        a = self.doc["accessors"][idx]
        v = self.doc["bufferViews"][a["bufferView"]]
        dtype = np.dtype(TYPES[a["componentType"]])
        dims = DIMS[a["type"]]
        return np.ndarray(
            (a["count"], dims),
            dtype,
            self.binary,
            v.get("byteOffset", 0) + a.get("byteOffset", 0),
            (v.get("byteStride", dims * dtype.itemsize), dtype.itemsize),
        )

    def albedo(self):
        image = self.doc["images"][0]
        v = self.doc["bufferViews"][image["bufferView"]]
        start = v.get("byteOffset", 0)
        return Image.open(
            io.BytesIO(self.binary[start : start + v["byteLength"]])
        ).convert("RGB")

    def sample(self, clip, count):
        a = next(a for a in self.doc["animations"] if a["name"] == clip)
        end = max(self.array(s["input"])[-1, 0] for s in a["samplers"])
        times = np.arange(count) * end / count
        components = {}
        for key, default in [
            ("rotation", [0, 0, 0, 1]),
            ("translation", [0, 0, 0]),
            ("scale", [1, 1, 1]),
        ]:
            components[key] = np.tile(
                [n.get(key, default) for n in self.nodes], (count, 1, 1)
            ).astype(float)
        for chan in a["channels"]:
            s = a["samplers"][chan["sampler"]]
            t, v = self.array(s["input"]).ravel(), self.array(s["output"])
            key, bone = chan["target"]["path"], chan["target"]["node"]
            sample_at = np.clip(times, t[0], t[-1])
            if len(t) == 1 or s.get("interpolation") == "STEP":
                values = v[np.searchsorted(t, sample_at, side="right") - 1]
            elif key == "rotation":
                values = Slerp(t, Rotation.from_quat(v))(sample_at).as_quat()
            else:
                values = np.stack(
                    [np.interp(sample_at, t, col) for col in v.T], axis=-1
                )
            components[key][:, bone] = values
        rot = Rotation.from_quat(components["rotation"].reshape(-1, 4)).as_matrix()
        world = np.tile(np.eye(4), (count, len(self.nodes), 1, 1))
        world[:, :, :3, :3] = (
            rot.reshape(count, -1, 3, 3) * components["scale"][:, :, None, :]
        )
        world[:, :, :3, 3] = components["translation"]
        for n in self.tree_order:
            parent = self.parents[n]
            if parent is not None:
                world[:, n] = world[:, parent] @ world[:, n]
        bones = world[:, self.joints] @ self.bind
        attr = self.attributes
        weights = attr["WEIGHTS_0"].astype(float)
        weights /= weights.sum(axis=1, keepdims=True)
        blend = (bones[:, attr["JOINTS_0"]] * weights[None, :, :, None, None]).sum(
            axis=2
        )
        pos = (
            np.einsum("fvij,vj->fvi", blend[:, :, :3, :3], attr["POSITION"])
            + blend[:, :, :3, 3]
        )
        pos[:, :, 1] -= pos[:, :, 1].min(axis=1, keepdims=True)
        normals = np.einsum("fvij,vj->fvi", blend[:, :, :3, :3], attr["NORMAL"])
        normals /= np.maximum(1e-8, np.linalg.norm(normals, axis=-1, keepdims=True))
        return pos, normals


class Library:
    def __init__(self):
        self.data = bytearray()
        self.doc = dict(
            asset={"version": "2.0"},
            scene=0,
            scenes=[{"nodes": []}],
            nodes=[],
            meshes=[],
            buffers=[],
            bufferViews=[],
            accessors=[],
        )

    def attribute(self, values, shape, kind):
        data = np.ascontiguousarray(values, dtype=TYPES[kind])
        self.data.extend(bytes(-len(self.data) % 4))
        views = self.doc["bufferViews"]
        views.append(dict(buffer=0, byteOffset=len(self.data), byteLength=data.nbytes))
        self.data.extend(data.tobytes())
        self.doc["accessors"].append(
            dict(
                bufferView=len(views) - 1,
                componentType=kind,
                count=len(data),
                type=shape,
                min=data.min(axis=0).tolist(),
                max=data.max(axis=0).tolist(),
            )
        )
        return len(self.doc["accessors"]) - 1

    def add(self, name, figure, uv, lookup):
        a = figure.attributes
        fields = {
            "POSITION": self.attribute(a["POSITION"], "VEC3", 5126),
            "NORMAL": self.attribute(a["NORMAL"], "VEC3", 5126),
            "TEXCOORD_0": self.attribute(uv, "VEC2", 5126),
            "TEXCOORD_1": self.attribute(lookup, "VEC2", 5126),
        }
        indices = self.attribute(
            figure.array(figure.primitive["indices"]), "SCALAR", 5123
        )
        idx = len(self.doc["meshes"])
        self.doc["meshes"].append(
            dict(name=name, primitives=[dict(attributes=fields, indices=indices)])
        )
        self.doc["nodes"].append(dict(name=name, mesh=idx))
        self.doc["scenes"][0]["nodes"].append(idx)

    def save(self, path):
        self.data.extend(bytes(-len(self.data) % 4))
        self.doc["buffers"] = [{"byteLength": len(self.data)}]
        header = json.dumps(self.doc, separators=(",", ":")).encode()
        header += b" " * (-len(header) % 4)
        path.write_bytes(
            struct.pack(
                "<4sIIII",
                b"glTF",
                2,
                28 + len(header) + len(self.data),
                len(header),
                0x4E4F534A,
            )
            + header
            + struct.pack("<II", len(self.data), 0x004E4942)
            + self.data
        )


def bake(source, out):
    figures = [Figure(source / (name + ".glb")) for name in FIGURES]
    chunks = [(len(f.attributes["POSITION"]) + WIDTH - 1) // WIDTH for f in figures]
    height = sum(chunks) * ROWS
    motion = np.zeros((2 * height, WIDTH, 3), dtype=np.uint8)
    atlas = Image.new("RGB", (2048, 1024))
    lib = Library()
    base = 0
    for i, (name, f) in enumerate(zip(FIGURES, figures)):
        col, row = i % 4, i // 4
        # The two-pixel gutter survives filtering at UV seams and mip transitions.
        tile = np.array(f.albedo().resize((508, 508), Image.Resampling.LANCZOS))
        atlas.paste(
            Image.fromarray(np.pad(tile, ((2, 2), (2, 2), (0, 0)), mode="edge")),
            (512 * col, 512 * row),
        )
        uv = (
            np.clip(f.attributes["TEXCOORD_0"], 0, 1) * 508
            + [512 * col + 2, 512 * row + 2]
        ) / [2048, 1024]
        verts = np.arange(len(uv))
        x = verts % WIDTH
        y = (base + verts // WIDTH) * ROWS
        lookup = np.stack(((x + 0.5) / WIDTH, (base + verts // WIDTH) / 64.0), axis=1)
        lib.add(name, f, uv, lookup)
        offset = 0
        for clip, count in CLIPS:
            pos, normals = f.sample(clip, count)
            rgb = (pos - LOW) / SPAN
            if not np.isfinite(rgb).all() or rgb.min() < 0 or rgb.max() > 1:
                raise ValueError(f"{name} {clip} left the pose texture bounds")
            for j in range(count + 1):
                yy = y + offset + j
                motion[yy, x] = np.rint(255 * rgb[j % count]).astype("u1")
                motion[yy + height, x] = np.rint(
                    127.5 * (1 + normals[j % count])
                ).astype("u1")
            offset += count + 1
        base += chunks[i]
    lib.save(out / "street_extras.glb")
    atlas.save(out / "street_wardrobe.jpg", quality=92, subsampling=0)
    Image.fromarray(motion).save(out / "street_poses.png", optimize=True)
    print(
        f"{len(figures)} Harbour outfits, {sum(count for _, count in CLIPS)} sampled poses each, {WIDTH} x {2 * height} pose texture"
    )


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("out", type=Path)
    args = parser.parse_args()
    bake(args.source, args.out)

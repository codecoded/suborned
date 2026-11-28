"""Build a packed, editable Blender version of the finished postal kit from the same gameplay maps."""

from pathlib import Path
import argparse
import subprocess
import sys
import tempfile

ROOMS = ("Sorting hall", "Reservations", "Pacific air office", "Parcel lockers")


def swatches(source, destination):
    import numpy as np
    from PIL import Image

    def read(name):
        return np.asarray(Image.open(source / (name + ".png")).convert("RGBA"), dtype=float) / 255

    def srgb(linear):
        return np.where(
            linear < 0.0031308, linear * 12.92, 1.055 * np.maximum(linear, 0) ** (1 / 2.4) - 0.055
        )

    def write(name, linear, slope, rough, metal, ao=1.0, encoded=False):
        def save(channel, pixels):
            a = np.rint(np.clip(pixels, 0, 1) * 255).astype(np.uint8)
            Image.fromarray(a).save(destination / f"{name}-{channel}.png")

        save("color", linear if encoded else srgb(linear))
        xy = slope * 2.0 - 1.0
        nz = np.sqrt(np.maximum(0.0, 1.0 - np.sum(xy * xy, axis=2)))
        save("normal", np.dstack((slope, 0.5 + 0.5 * nz)))
        save("orm", np.stack(np.broadcast_arrays(ao, rough, metal), axis=-1))

    def mix(a, b, q):
        return np.array(a) * (1.0 - q[..., None]) + np.array(b) * q[..., None]

    s = read("finish_stone")
    stone = np.array([0.695, 0.612, 0.458]) * mix([0.67, 0.64, 0.58], [1.32, 1.31, 1.36], s[..., 2])
    write("Cut stone", stone, s[..., :2], s[..., 3] + 0.035, 0.0)
    f = read("finish_faience")
    base = mix([0.018, 0.083, 0.078], [0.123, 0.368, 0.293], np.clip((f[..., 2] - 0.22) * 2.8, 0, 1))
    y, x = np.mgrid[: base.shape[0], : base.shape[1]]
    z = 0.72 * (1 - y / base.shape[0])
    band = np.abs(z - 0.67)
    wave = np.abs((x / base.shape[1] * 6) % 1 - 0.5) * 0.05
    oxide = (np.maximum(abs(band - 0.054) < 0.0024, abs(band - wave - 0.008) < 0.0024)) * (f[..., 2] > 0.38)
    write("Celadon dado", mix(base, [0.55, 0.43, 0.23], oxide * 0.9), f[..., :2], f[..., 3], 0.0)
    m = read("finish_metal")
    for label, color in [("Drawn brass", [0.62, 0.40, 0.18]), ("Nickel silver", [0.61, 0.61, 0.55])]:
        write(
            label,
            np.array(color) * (0.84 + 0.32 * m[..., 2, None]),
            0.5 + (m[..., :2] - 0.5) * 0.26,
            m[..., 3],
            0.92,
        )
    roof = read("finish_roof")
    write(
        "Folded copper",
        mix([0.036, 0.141, 0.135], [0.24, 0.443, 0.356], np.clip((roof[..., 2] - 0.25) * 2, 0, 1)),
        0.5 + (roof[..., :2] - 0.5) * 0.78,
        roof[..., 3],
        0.43,
    )
    coat, detail = read("collection_coat"), read("collection_finish")
    write(
        "Collection enamel",
        coat[..., :3],
        detail[..., :2],
        detail[..., 3],
        coat[..., 3],
        detail[..., 2],
        True,
    )
    plates = read("postal_plates")
    for name, half in [("Air post roundel", 0), ("Collection plate", 1)]:
        data = plates[:, half * 512 : (half + 1) * 512]
        gilt = np.clip((data[..., 0] - 0.44) / 0.12, 0, 1)
        ink = np.clip((data[..., 1] - 0.44) / 0.12, 0, 1)
        color = mix(mix([0.014, 0.045, 0.052], [0.80, 0.57, 0.27], gilt), [0.86, 0.79, 0.59], ink)
        write(name, color, data[..., 2:4], 0.29 - 0.04 * gilt, 0.11 + 0.76 * gilt)
    rooms = read("sorting_rooms")
    for i, name in enumerate(ROOMS):
        y, x = divmod(i, 2)
        rgb = rooms[y * 512 : y * 512 + 512, x * 512 : x * 512 + 512, :3]
        write(name, rgb, np.full((*rgb.shape[:2], 2), 0.5), np.full(rgb.shape[:2], 0.57), 0.035, encoded=True)


def dress_kit(maps, target):
    import bpy

    def material(name):
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        mat.asset_mark()
        nt = mat.node_tree
        shader = nt.nodes.get("Principled BSDF")
        uv = nt.nodes.new("ShaderNodeUVMap")
        uv.uv_map = "Finish metres"
        uv.location = (-760, 0)

        def texture(tag, y):
            node = nt.nodes.new("ShaderNodeTexImage")
            node.location = (-530, y)
            node.image = bpy.data.images.load(str(maps / f"{name}-{tag}.png"))
            if tag != "color":
                node.image.colorspace_settings.name = "Non-Color"
            nt.links.new(uv.outputs[0], node.inputs["Vector"])
            return node.outputs["Color"]

        color = texture("color", 280)
        nt.links.new(color, shader.inputs["Base Color"])
        if name in ROOMS:
            nt.links.new(color, shader.inputs["Emission Color"])
            shader.inputs["Emission Strength"].default_value = 0.17
        normal = nt.nodes.new("ShaderNodeNormalMap")
        normal.uv_map = "Finish metres"
        normal.location = (-200, -80)
        nt.links.new(texture("normal", -10), normal.inputs["Color"])
        nt.links.new(normal.outputs["Normal"], shader.inputs["Normal"])
        split = nt.nodes.new("ShaderNodeSeparateColor")
        split.location = (-200, -320)
        nt.links.new(texture("orm", -290), split.inputs[0])
        nt.links.new(split.outputs["Green"], shader.inputs["Roughness"])
        nt.links.new(split.outputs["Blue"], shader.inputs["Metallic"])
        return mat

    finishes = {p.name[:-10]: material(p.name[:-10]) for p in sorted(maps.glob("*-color.png"))}
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH" or not obj.get("part"):
            continue
        mesh = obj.data
        atlas = mesh.uv_layers["Surface"]
        service = mesh.uv_layers["Postal service"]
        metric = mesh.uv_layers.new(name="Finish metres")
        labels = {}
        for poly in mesh.polygons:
            idx = poly.loop_indices[0]
            u, v = atlas.data[idx].uv
            cell = int(u * 8) + 8 * int((1 - v) * 4)
            nx, ny, nz = poly.normal
            x, y, z = poly.center
            roof = cell == 2 and z > 3.49 and nz > 0.12
            top = abs(nz) > max(abs(nx), abs(ny))
            width = height = 1.0
            if cell == 0:
                label, width, height = "Cut stone", 2.4, 2.4
            elif cell in (9, 29) or (cell == 7 and not top and z < 1.09):
                label, width, height = "Celadon dado", 1.44, 0.72
            elif cell == 2 and service.data[idx].uv.x > 0.001:
                label = "Collection enamel"
            elif cell in (3, 4):
                label, width, height = ("Drawn brass" if cell == 4 else "Nickel silver"), 0.41, 0.32
            elif roof:
                label, width, height = "Folded copper", 3.2, 3.2
            elif cell in (16, 23):
                label = "Air post roundel" if cell == 16 else "Collection plate"
            elif 24 <= cell <= 27:
                label = ROOMS[cell - 24]
            else:
                continue
            if label not in labels:
                labels[label] = len(mesh.materials)
                mesh.materials.append(finishes[label])
            poly.material_index = labels[label]
            for i in poly.loop_indices:
                x, y, z = mesh.vertices[mesh.loops[i].vertex_index].co
                a, b = (x, -y) if top else ((-y, z) if abs(nx) > abs(ny) else (x, z))
                uv = (a / width, (b + 0.16) / height if not top else b / height)
                if roof:
                    uv = (-y / width, -(x * 0.55 + (1 if nx < 0 else -1) * z * 0.83) / height)
                if label == "Collection enamel":
                    uv = service.data[i].uv
                if cell in (16, 23) or 24 <= cell <= 27:
                    u, v = atlas.data[i].uv
                    uv = ((u * 8 % 1 - 0.021) / 0.958, (v * 4 % 1 - 0.021) / 0.958)
                metric.data[i].uv = uv
        mesh.uv_layers.active_index = 0
        atlas.active_render = True
    bpy.ops.file.pack_all()
    missing = [im.name for im in bpy.data.images if im.source == "FILE" and im.users and not im.packed_file]
    if missing:
        raise ValueError(f"Unpacked texture dependencies {missing}")
    bpy.ops.wm.save_as_mainfile(filepath=str(target), compress=True)


if __name__ == "__main__":
    if "--" in sys.argv:
        a, b = sys.argv[sys.argv.index("--") + 1 :]
        dress_kit(Path(a), Path(b))
    else:
        parser = argparse.ArgumentParser(description=__doc__)
        parser.add_argument("source_blend", type=Path)
        parser.add_argument("assets", type=Path)
        parser.add_argument("destination", type=Path)
        parser.add_argument("--blender", default="blender")
        a = parser.parse_args()
        with tempfile.TemporaryDirectory() as directory:
            swatches(a.assets, Path(directory))
            subprocess.run(
                [
                    a.blender,
                    "-b",
                    str(a.source_blend.resolve()),
                    "--python-exit-code",
                    "1",
                    "-P",
                    str(Path(__file__).resolve()),
                    "--",
                    directory,
                    str(a.destination.resolve()),
                ],
                check=True,
            )

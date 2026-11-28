"""Bake the arcade's diffuse sky visibility and first sun bounce in world space using Blender's BVH."""

import json
import math
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree


# X and Z cover the permanent postal frontage common to both street layouts.
FIRST = (-24.0, 0.22, -8.0)
SIZE = (50, 12, 20)
STEP = (1.0, 1.0, 1.0)
SAMPLES = 100
PITCH, YAW = math.radians(27.0), math.radians(-108.0)
SUN = Vector((math.cos(PITCH) * math.sin(YAW), math.sin(PITCH), math.cos(PITCH) * math.cos(YAW)))
GROUND = (0.55, 0.45, 0.31)


def read_meshes(source):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(source))
    atlas = next(image for image in bpy.data.images if "meridian_base" in image.name or image.name == "base")
    pixels = np.empty(len(atlas.pixels), dtype=np.float32)
    atlas.pixels.foreach_get(pixels)
    rgb = pixels.reshape(atlas.size[1], atlas.size[0], 4)[:, :, :3]
    catalog = {}
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH" or not obj.name.endswith("_L0"):
            continue
        mesh = obj.data
        mesh.calc_loop_triangles()
        positions = [(v.co.x, v.co.z, -v.co.y) for v in mesh.vertices]
        faces, finishes = [], []
        uv = mesh.uv_layers.active.data
        for triangle in mesh.loop_triangles:
            faces.append(tuple(triangle.vertices))
            center = sum((uv[i].uv for i in triangle.loops), Vector((0.0, 0.0))) / 3
            u = int(center.x * rgb.shape[1]) % rgb.shape[1]
            v = int(center.y * rgb.shape[0]) % rgb.shape[0]
            finishes.append(tuple(rgb[v, u]))
        catalog[obj.name[:-3]] = (positions, faces, finishes)
    return catalog


def acceleration(catalog, instances):
    vertices, faces, surfaces = [], [], []
    for name, x, y, z, yaw in instances:
        if abs(x) > 65 or abs(z) > 45:
            continue
        coords, triangles, colors = catalog[name]
        start = len(vertices)
        co, si = math.cos(yaw), math.sin(yaw)
        vertices.extend((x + co * u + si * w, y + v, z - si * u + co * w) for u, v, w in coords)
        faces.extend(tuple(start + i for i in tri) for tri in triangles)
        surfaces.extend(colors)
    # Two curbs and the road feed different coloured light back into the canopies.
    for za, zb, h, colour in [
        (-90, 6.88, 0.15, GROUND),
        (6.88, 19.62, 0.0, (0.10, 0.09, 0.07)),
        (19.62, 90, 0.15, GROUND),
    ]:
        start = len(vertices)
        vertices.extend([(-90, h, za), (-90, h, zb), (90, h, zb), (90, h, za)])
        faces.extend([(start, start + 1, start + 2), (start, start + 2, start + 3)])
        surfaces.extend([colour, colour])
    return BVHTree.FromPolygons(vertices, faces, all_triangles=True), np.asarray(surfaces, dtype=float)


def bake(tree, colours):
    # A Fibonacci sphere gives equal solid angles. Rays and their shadow tests are deterministic.
    directions = []
    for k in range(SAMPLES):
        y = 1 - 2 * (k + 0.5) / SAMPLES
        r, t = math.sqrt(1 - y * y), k * math.pi * (3 - math.sqrt(5))
        directions.append(Vector((r * math.cos(t), y, r * math.sin(t))))
    sky_weight = sum(0.35 + 0.65 * v.y for v in directions if v.y > 0)
    nx, ny, nz = SIZE
    out = np.zeros((nz, ny, nx, 4), dtype=np.uint8)
    for iz in range(nz):
        for iy in range(ny):
            for ix in range(nx):
                pos = Vector(tuple(a + b * c for a, b, c in zip(FIRST, STEP, (ix, iy, iz))))
                bounce = np.zeros(3)
                visible = 0.0
                for ray in directions:
                    hit, normal, face, distance = tree.ray_cast(pos, ray, 48.0)
                    if hit is None:
                        if ray.y > 0:
                            visible += 0.35 + 0.65 * ray.y
                        continue
                    # Only the side facing this receiver can reflect light back to it.
                    if normal.dot(ray) >= 0:
                        continue
                    incidence = normal.dot(SUN)
                    if incidence <= 0 or tree.ray_cast(hit + normal * 0.035, SUN, 100.0)[0] is not None:
                        continue
                    bounce += colours[face] * incidence
                # Store in linear space. The shader supplies the incident sun colour.
                out[iz, iy, ix, :3] = np.rint(np.clip(bounce / SAMPLES * 2.0, 0, 1) * 255)
                out[iz, iy, ix, 3] = round(min(1, visible / sky_weight) * 255)
    return out


if __name__ == "__main__":
    source, locations, destination = (Path(s).resolve() for s in sys.argv[sys.argv.index("--") + 1 :])
    grid = bake(*acceleration(read_meshes(source), json.loads(locations.read_text())))
    destination.write_bytes(grid.tobytes())
    print(
        "Baked", SIZE, "irradiance samples;", SAMPLES, "rays per point;", destination.stat().st_size, "bytes"
    )

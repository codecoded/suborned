"""Project six street views into the longitude convention of Godot's sky shader."""

import argparse
from pathlib import Path

import numpy as np
from PIL import Image
from scipy.ndimage import map_coordinates


def panorama(directory):
    directions = np.array([(1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1)])
    up = np.array([(0, 1, 0), (0, 1, 0), (0, 0, -1), (0, 0, 1), (0, 1, 0), (0, 1, 0)])
    right = np.cross(directions, up)
    x = (np.arange(1024) + 0.5) * (np.pi * 2 / 1024)
    y = ((np.arange(512) + 0.5) * (np.pi / 512))[:, None]
    rays = np.stack(np.broadcast_arrays(np.sin(y) * np.sin(x), np.cos(y), -np.sin(y) * np.cos(x)), axis=2)
    face_ids = np.argmax(rays @ directions.T, axis=2)
    result = np.empty((512, 1024, 3), dtype=np.uint8)
    for i in range(6):
        source = np.asarray(Image.open(directory / f"{i}.png").convert("RGB"))
        rows, cols = np.where(face_ids == i)
        rays_here = rays[rows, cols]
        view_z = rays_here @ directions[i]
        u = (1 + rays_here @ right[i] / view_z) * source.shape[1] / 2 - 0.5
        v = (1 - rays_here @ up[i] / view_z) * source.shape[0] / 2 - 0.5
        for channel in range(3):
            result[rows, cols, channel] = map_coordinates(
                source[:, :, channel], (v, u), order=1, mode="nearest"
            )
    return Image.fromarray(result)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path)
    parser.add_argument("destination", type=Path)
    args = parser.parse_args()
    panorama(args.directory).save(args.destination, quality=93, subsampling=0)

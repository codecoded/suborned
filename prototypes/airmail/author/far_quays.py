"""Extract the architecture from Harbour's painted shore for the sky at walking height."""

import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage


def cut_quays(source, output):
    painting = Image.open(source).convert("RGBA")
    r, g, b, alpha = np.asarray(painting, dtype=np.float32).transpose(2, 0, 1)
    height = r.shape[0]
    y = np.arange(height)[:, None]
    # The sunlit stone is warm against the blue hills. Only keep marks connected
    # to the quays, so isolated sunlit mountain ridges do not become rooftops.
    stone = (r - b > 8) & (r - g > 1) & (alpha > 205)
    stone = ndimage.binary_closing(stone, structure=np.ones((3, 3)))
    connected = ndimage.binary_dilation(stone, iterations=2) | (y > height * 0.86)
    footprint = ndimage.binary_propagation(
        np.broadcast_to(y > height * 0.86, r.shape), mask=connected
    )
    edge = np.where(stone & footprint, y, height - 28).min(axis=0)
    edge = ndimage.median_filter(edge, size=3, mode="nearest")
    edge = ndimage.minimum_filter1d(edge, size=3, mode="nearest") - 1.15
    coverage = np.minimum(alpha, np.clip(y - edge + 0.5, 0, 1) * 255)
    painting.putalpha(Image.fromarray(coverage.astype(np.uint8)))
    painting.save(output, optimize=True)


if __name__ == "__main__":
    cut_quays(*map(Path, sys.argv[1:]))

"""Shared top-left pixel rectangles on the 2k Lantern architectural sheet."""

SIZE = 2048
REGIONS = {
    "bureau": (0, 0, 1024, 512),
    "flowers": (1024, 0, 2048, 512),
    "studio": (0, 512, 1024, 1024),
    "produce": (1024, 512, 2048, 1024),
    "exchange_name": (0, 1024, 1024, 1152),
    "flower_name": (0, 1152, 1024, 1280),
    "market_name": (0, 1280, 1024, 1408),
    "passage_name": (0, 1408, 1024, 1536),
    "clock": (1024, 1024, 1536, 1536),
    "air_poster": (1536, 1024, 1792, 1536),
    "sea_poster": (1792, 1024, 2048, 1536),
}
SURFACES = (
    "stone",
    "cream",
    "jade",
    "wine",
    "nickel",
    "bronze",
    "shadow",
    "soffit",
    "glass",
    "shade",
    "plinth",
    "roof",
    "opal",
    "leaf",
    "petals",
    "terracotta",
)
for i, name in enumerate(SURFACES):
    x, y = i % 8 * 256, 1536 + i // 8 * 256
    REGIONS[name] = (x, y, x + 256, y + 256)


def atlas_uv(region, u, v):
    x, y, right, bottom = REGIONS[region]
    edge = 5
    return (
        (x + edge + (right - x - 2 * edge) * u) / SIZE,
        1 - (bottom - edge - (bottom - y - 2 * edge) * v) / SIZE,
    )

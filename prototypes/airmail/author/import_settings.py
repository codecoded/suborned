"""Keep authored LOD selection and fingerprinted VRAM import settings reproducible."""

from pathlib import Path
import re, sys

root = Path(sys.argv[1])
paths = (
    [root / name for name in sys.argv[2:]]
    if len(sys.argv) > 2
    else sorted(root.glob("*.import"))
)
for path in paths:
    text = path.read_text()
    if path.name in ("meridian.glb.import", "yard_trades.glb.import"):
        text = text.replace("meshes/generate_lods=true", "meshes/generate_lods=false")
    elif path.name == "yard_ambience.wav.import":
        text = re.sub(r"(?m)^edit/loop_mode=\d+", "edit/loop_mode=1", text)
    elif path.name == "street_poses.png.import":
        # Adjacent columns hold different skin vertices. Block compression,
        # colour conversion and mip filtering would move vertices off their paths.
        text = re.sub(r"(?m)^compress/mode=\d+", "compress/mode=0", text)
        text = re.sub(r"(?m)^compress/normal_map=\d+", "compress/normal_map=2", text)
        text = re.sub(
            r"(?m)^detect_3d/compress_to=\d+", "detect_3d/compress_to=0", text
        )
        text = text.replace("mipmaps/generate=true", "mipmaps/generate=false")
    elif path.name.endswith((".png.import", ".jpg.import")):
        text = re.sub(r"(?m)^compress/mode=\d+", "compress/mode=2", text)
        text = text.replace("mipmaps/generate=false", "mipmaps/generate=true")
        if "normal." in path.name or "_n." in path.name:
            text = re.sub(
                r"(?m)^compress/normal_map=\d+", "compress/normal_map=1", text
            )
        if path.name.startswith(
            ("finish_", "collection_", "postal_plates.", "sorting_rooms.")
        ):
            text = text.replace(
                "compress/high_quality=false", "compress/high_quality=true"
            )
            text = text.replace(
                "process/fix_alpha_border=true", "process/fix_alpha_border=false"
            )
            text = re.sub(
                r"(?m)^compress/normal_map=\d+", "compress/normal_map=2", text
            )
    if text != path.read_text():
        path.write_text(text)

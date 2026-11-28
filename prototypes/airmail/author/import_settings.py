"""Keep authored LOD selection and fingerprinted VRAM import settings reproducible."""

from pathlib import Path
import re, sys

root = Path(sys.argv[1])
for path in sorted(root.glob("*.import")):
    text = path.read_text()
    if path.name == "meridian.glb.import":
        text = text.replace("meshes/generate_lods=true", "meshes/generate_lods=false")
    elif path.name.endswith((".png.import", ".jpg.import")):
        text = re.sub(r"(?m)^compress/mode=\d+", "compress/mode=2", text)
        text = text.replace("mipmaps/generate=false", "mipmaps/generate=true")
        if "normal." in path.name or "_n." in path.name:
            text = re.sub(r"(?m)^compress/normal_map=\d+", "compress/normal_map=1", text)
        if path.name.startswith(("finish_", "collection_", "postal_plates.")):
            text = text.replace("compress/high_quality=false", "compress/high_quality=true")
            text = text.replace("process/fix_alpha_border=true", "process/fix_alpha_border=false")
            text = re.sub(r"(?m)^compress/normal_map=\d+", "compress/normal_map=2", text)
    if text != path.read_text():
        path.write_text(text)

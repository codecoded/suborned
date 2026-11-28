"""Export Godot's official universal template as a compact signed Apple Silicon test player."""

import argparse, plistlib, subprocess, tempfile
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("destination", type=Path)
args = parser.parse_args()
game = Path(__file__).resolve().parents[1] / "game"
app = args.destination.resolve()
if app.suffix != ".app":
    raise SystemExit("Choose an .app destination")
app.parent.mkdir(parents=True, exist_ok=True)


def run(*command):
    subprocess.run(list(map(str, command)), check=True)


run("godot", "--headless", "--path", game, "--export-release", "macOS", app)
with (app / "Contents/Info.plist").open("rb") as f:
    info = plistlib.load(f)
binary = app / "Contents/MacOS" / info["CFBundleExecutable"]
with tempfile.TemporaryDirectory(dir=app.parent) as d:
    thin = Path(d) / "player"
    run("lipo", binary, "-thin", "arm64", "-output", thin)
    thin.chmod(0o755)
    thin.replace(binary)
run("codesign", "--force", "--sign", "-", "--deep", app)
run("codesign", "--deep", "--strict", "--verify", app)
zip_path = app.with_suffix(".zip")
run("ditto", "-c", "-k", "--keepParent", app, zip_path)
print(app, zip_path.stat().st_size, "bytes in zip")

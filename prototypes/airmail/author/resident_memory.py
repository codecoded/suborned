"""Sample the standalone Mac player's resident set while it alternates camera routes."""

import argparse
import json
import subprocess
from pathlib import Path
from time import monotonic, sleep


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("player", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--seconds", type=int, default=125)
    args = parser.parse_args()
    directory = args.output.resolve()
    directory.mkdir(parents=True, exist_ok=True)
    samples = []
    with (directory / "player.log").open("w") as log:
        process = subprocess.Popen(
            [
                str(args.player.resolve()),
                "--resolution",
                "1280x720",
                "--disable-vsync",
                "--",
                "out=" + str(directory),
                "raster=1080",
                "soak=" + str(args.seconds),
            ],
            stdout=log,
            stderr=log,
        )
        start = monotonic()
        while process.poll() is None:
            result = subprocess.run(
                ["ps", "-o", "rss=", "-p", str(process.pid)], capture_output=True, text=True
            )
            rss = result.stdout.strip()
            if rss.isdecimal():
                samples.append([round(monotonic() - start, 3), int(rss) * 1024])
            sleep(1)
        if process.returncode:
            raise SystemExit(process.returncode)
    (directory / "resident.json").write_text(json.dumps(samples) + "\n")


if __name__ == "__main__":
    main()

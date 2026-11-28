"""Summarize captured frame intervals and Godot counters from the fixed-route release suite."""

import argparse
import csv
import gzip
import hashlib
import io
import json
import math
from collections import defaultdict
from pathlib import Path
from statistics import fmean, median


def percentile(values, percent):
    position = (len(values) - 1) * percent / 100
    low, high = math.floor(position), math.ceil(position)
    return values[low] + (values[high] - values[low]) * (position - low)


def summarize(repeats):
    samples = [sample for run in repeats for sample in run["samples"]]
    columns = dict(zip(repeats[0]["columns"], zip(*samples)))
    wall = sorted(columns["wall_ms"])
    return {
        "frames": len(wall),
        "mean_fps": round(1000 / fmean(wall), 1),
        "median_ms": round(median(wall), 2),
        "p95_ms": round(percentile(wall, 95), 2),
        "p99_ms": round(percentile(wall, 99), 2),
        "within_60fps_budget_pct": round(100 * sum(v <= 1000 / 60 for v in wall) / len(wall), 1),
        "repeat_p95_ms": [round(percentile(sorted(row[0] for row in r["samples"]), 95), 2) for r in repeats],
        "median_visible_draws": median(columns["visible_draws"]),
        "median_visible_primitives": median(columns["visible_primitives"]),
        "median_shadow_draws": median(columns["shadow_draws"]),
        "median_shadow_primitives": median(columns["shadow_primitives"]),
        "median_total_draws": median(columns["total_draws"]),
        "median_render_cpu_ms": round(median(columns["render_cpu_ms"]), 3),
        "median_gpu_ms": round(median(columns["gpu_ms"]), 3) if any(columns["gpu_ms"]) else None,
    }


def load_suite(directory, finishes=False, resolution=(1920, 1080)):
    groups = defaultdict(list)
    variant = "lighting" if finishes else "mode"
    expected_viewport = "(%.1f, %.1f)" % resolution
    for path in sorted(directory.glob("*/run.json")):
        run = json.loads(path.read_text())
        options = run["options"]
        if run["viewport"] != expected_viewport or run["render_scale"] != 1 or run["vsync"] != 0:
            raise ValueError(f"Unexpected resolution, scale or VSync at {path}")
        if len(run["samples"]) != 220:
            raise ValueError(f"Unexpected sample count at {path}")
        key = "/".join(str(options[k]) for k in (variant, "corner", "view"))
        groups[key].append(run)
    if len(groups) != 9 or any(
        sorted(int(r["options"]["repeat"]) for r in runs) != [0, 1] for runs in groups.values()
    ):
        raise ValueError(f"Expected two passes through all nine conditions in {directory}")
    if finishes and set(groups) != {
        f"{look}/turned/{view}"
        for look in ("original", "arcade", "surfaced")
        for view in ("street", "threshold", "iso")
    }:
        raise ValueError(f"Incomplete lighting and material comparison in {directory}")
    one = next(iter(groups.values()))[0]
    return {
        "adapter": one["device"],
        "renderer": one["renderer"],
        "driver": one["driver"],
        "viewport": list(resolution),
        "frames_total": sum(len(run["samples"]) for group in groups.values() for run in group),
        "cases": {name: summarize(runs) for name, runs in groups.items()},
    }


def write_frames(path, sources, finishes=False):
    metrics = [
        "wall_ms",
        "render_cpu_ms",
        "visible_draws",
        "visible_primitives",
        "shadow_draws",
        "shadow_primitives",
        "total_draws",
    ]
    with path.open("wb") as binary, gzip.GzipFile(filename="", fileobj=binary, mode="wb", mtime=0) as stream:
        with io.TextIOWrapper(stream, encoding="utf-8", newline="") as text:
            writer = csv.writer(text)
            variant = "lighting" if finishes else "mode"
            writer.writerow(["machine", variant, "corner", "view", "repeat", "frame"] + metrics)
            for machine, directory in sources:
                if directory is None:
                    continue
                for run_path in sorted(directory.glob("*/run.json")):
                    run = json.loads(run_path.read_text())
                    case = [run["options"][key] for key in (variant, "corner", "view", "repeat")]
                    columns = [run["columns"].index(name) for name in metrics]
                    for frame, sample in enumerate(run["samples"]):
                        writer.writerow([machine, *case, frame, *(sample[index] for index in columns)])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--studio", type=Path, required=True)
    parser.add_argument("--air", type=Path)
    parser.add_argument(
        "--air-window", type=Path, help="Optional matched series at the 1280 by 720 play window"
    )
    parser.add_argument(
        "--finishes", action="store_true", help="Compare the original, lighting and material treatments"
    )
    parser.add_argument("--soak", type=Path)
    parser.add_argument("--resident", type=Path)
    parser.add_argument("--archive", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--csv-output", type=Path)
    args = parser.parse_args()
    report = {
        "engine": "Godot 4.7.2.stable.official.ed1daf0bf",
        "player_sha256": hashlib.sha256(args.archive.read_bytes()).hexdigest(),
        "player_zip_bytes": args.archive.stat().st_size,
        "studio": load_suite(args.studio, args.finishes),
        "air": load_suite(args.air, args.finishes) if args.air else None,
    }
    if args.air_window:
        report["air_window"] = load_suite(args.air_window, args.finishes, (1280, 720))
    if args.soak:
        raw = json.loads(args.soak.read_text())
        stable = [row for row in raw if row[0] >= 25]
        # Some Metal releases return unsigned underflow for this monitor.
        valid_texture = all(0 < row[1] < 2**48 for row in stable)
        report["studio_soak"] = {
            "duration_seconds": round(raw[-1][0]),
            "warmup_seconds": 25,
            "first_texture_mib": round(stable[0][1] / 2**20, 1) if valid_texture else None,
            "last_texture_mib": round(stable[-1][1] / 2**20, 1) if valid_texture else None,
            "max_texture_mib": round(max(row[1] for row in stable) / 2**20, 1) if valid_texture else None,
            "resources_first_last": [stable[0][2], stable[-1][2]],
        }
        if args.resident:
            rss = [row[1] / 2**20 for row in json.loads(args.resident.read_text()) if row[0] >= 25]
            report["studio_soak"]["macos_rss_mib"] = {
                "first": round(rss[0], 1),
                "last": round(rss[-1], 1),
                "max": round(max(rss), 1),
            }
    args.output.write_text(json.dumps(report, indent=2) + "\n")
    if args.csv_output:
        write_frames(
            args.csv_output,
            [("studio", args.studio), ("air", args.air), ("air_window", args.air_window)],
            args.finishes,
        )


if __name__ == "__main__":
    main()

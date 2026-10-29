"""Exercise the real managed SwapMesh code without launching Unity or changing a scene."""
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[4]
version = re.search(r"m_EditorVersion: (\S+)", (root / "unity/ProjectSettings/ProjectVersion.txt").read_text()).group(1)
managed = Path("/Applications/Unity/Hub/Editor") / version / "Unity.app/Contents/Resources/Scripting/Managed"
scratch = root / "out/scratch/aster/tram-degenerate"
scratch.mkdir(parents=True, exist_ok=True)
project = ET.Element("Project", Sdk="Microsoft.NET.Sdk")
props = ET.SubElement(project, "PropertyGroup")
for key, value in {"TargetFramework": "net10.0", "OutputType": "Exe", "DefineConstants": "UNITY_EDITOR",
                   "EnableDefaultCompileItems": "false", "Nullable": "disable"}.items():
    ET.SubElement(props, key).text = value
items = ET.SubElement(project, "ItemGroup")
for path in ["unity/Assets/Suborned/Editor/OldTown/Swap/SwapMesh.cs", "briefs/075/recovery/tram-degenerate/Probe.cs"]:
    ET.SubElement(items, "Compile", Include=str(root / path))
ET.SubElement(items, "Reference", Include=str(managed / "UnityEngine/*.dll"))
path = scratch / "Probe.csproj"
ET.ElementTree(project).write(path)
subprocess.run(["dotnet", "run", "--project", str(path), "-c", "Release"], cwd=root, check=True)

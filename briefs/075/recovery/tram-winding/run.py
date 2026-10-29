from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[4]
out=root/'out/scratch/aster/tram-winding'
out.mkdir(parents=True,exist_ok=True)
tree=ET.parse(root/'briefs/075/recovery/tram-settled/winding-check.csproj')
project=tree.getroot()
for p in project.findall('PropertyGroup'):
    for k in list(p):
        if k.tag=='BaseIntermediateOutputPath':k.text=str(out/'obj')+'/'
        if k.tag=='OutputPath':k.text=str(out/'bin')+'/'
for c in project.findall('ItemGroup/Compile'):
    if c.get('Include')=='WindingCheck.cs':c.set('Include',str(Path(__file__).with_name('Probe.cs')))
path=out/'Probe.csproj';tree.write(path)
subprocess.run(['dotnet','run','--project',str(path)],cwd=root,check=True)

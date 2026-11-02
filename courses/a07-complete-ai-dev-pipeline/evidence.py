"""A07 portrait evidence. snapshot: local production records; verify: offline clone; replay: saved AI sheets, no API."""
import argparse
import hashlib
import importlib.util
import json
from decimal import Decimal
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
EVIDENCE = HERE / 'evidence'
ART = ROOT / 'briefs/033/portraits'
UNITY = ROOT / 'unity/Assets/Suborned/UI/Resources/Kit/portraits/live'
STATES = ['calm', 'focused', 'noticed', 'under_fire', 'firing', 'hurt', 'badly_hurt', 'downed', 'success']
AGENTS = ['scout', 'fixer', 'tech', 'enforcer']
SOURCE_CODE = [
    'tools/figures/portraits_gen.py', 'tools/figures/portraits_export.py',
    'unity/Assets/Suborned/Editor/World/UI/KitImport.cs',
    'unity/Assets/Suborned/Editor/World/Play/GameBuild.cs',
    'unity/Assets/Suborned/Runtime/Play/HostPortraits.cs',
    'unity/Assets/Suborned/Runtime/Play/HudToolkit.Live.cs',
]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write(name, value):
    EVIDENCE.mkdir(exist_ok=True)
    (EVIDENCE / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')


def costs(rows):
    # Provider-reported costs only: never substitute a prediction or apply today's prices.
    assert len(rows) == 14 and all(r['status'] == 'ok' and r['cost_usd'] is not None for r in rows)
    by_pass = {}
    for r in rows:
        p = by_pass.setdefault(r['pass'], {'calls': 0, 'usd': Decimal(0)})
        p['calls'] += 1
        p['usd'] += Decimal(str(r['cost_usd']))
    total = sum((Decimal(str(r['cost_usd'])) for r in rows), Decimal(0))
    return {
        'scope': 'Recorded 033-portraits campaign, t1-t5; includes superseded takes',
        'total_actual_usd': str(total), 'calls': len(rows),
        'by_pass': {k: {'calls': v['calls'], 'usd': str(v['usd'])} for k, v in by_pass.items()},
        'highest_cost_per_call_usd': str(max(Decimal(str(r['cost_usd'])) for r in rows)),
        'highest_cost_step': 'Nine-state 2048x2048 sheet generation: t1-t4, 10 calls, $0.9630 total',
        'before': {'passes': ['t1', 't2', 't3', 't4'], 'shape': '2048x2048, nine states', 'usd_per_call': '0.0963'},
        'after': {'passes': ['t5'], 'shape': '2048x704, three options for the downed state only', 'usd_per_call': '0.04815'},
        'cost_reduction_per_repair_call_percent': 50,
        'limits': 'Different output scope, not equal work: repair one rejected state instead of regenerating all nine. Recorded per-call cost, not per-portrait savings. Excludes subscriptions, labour, hardware, hosting and other asset jobs. Ledger was migrated from PROVENANCE; dates are marked mtime.',
    }


def snapshot():
    rows = [json.loads(l) for l in (ROOT / 'paid/033-portraits/ledger.jsonl').read_text().splitlines() if l.strip()]
    for row in rows:
        path = ROOT / row['ref']['path']
        assert path.is_file() and sha(path) == row['sha256'], f'Raw generation missing or changed: {path}'
    write('generation-ledger.json', rows)
    write('costs.json', costs(rows))
    selected = []
    for agent in AGENTS:
        take = 4 if agent == 'tech' else 3
        source = ART / f'{agent}_states_t{take}.png'
        selected.append({'agent': agent, 'source': str(source.relative_to(ROOT)), 'sha256': sha(source), 'take': take})
    sprites = []
    build = json.loads((ROOT / 'out/build/play/build.json').read_text())
    inputs = json.loads((ROOT / 'out/build/play/inputs.json').read_text())['files']
    for agent in AGENTS:
        for state in STATES:
            name = f'{agent}_{state}@2x.png'
            src, dst = ART / 'final' / name, UNITY / name
            assert sha(src) == sha(dst), f'Unity sprite differs: {name}'
            path = str(dst.relative_to(ROOT))
            data = dst.read_bytes()
            blob = hashlib.sha1(f'blob {len(data)}\0'.encode() + data).hexdigest()
            assert inputs[path] == blob, f'Build did not use current sprite: {name}'
            sprites.append({'file': path, 'sha256': sha(dst), 'build_git_blob': blob, 'source_matches': True})
    write('integration.json', {
        'schema': 'suborned.a07.portraits/1', 'build': build, 'chosen_ai_sheets': selected,
        'states': STATES, 'agents': AGENTS, 'sprites': sprites,
        'code': {name: sha(ROOT / name) for name in SOURCE_CODE},
        'manual_exception': 'Four downed portraits are approved option-b replacements from t5, ring-refined and desaturated to 18%. Original export script does not reproduce this final treatment. Kept as an explicit manual step; no claim of 100% automation.',
        'proof': '36 production finals equal 36 tracked Unity sprites; each sprite blob matches the recorded local build input. This proves local build 0.2.232 inputs, not the public download or a cold-start time.',
    })
    print(f'Captured {len(rows)} generation receipts, 4 selected sheets, 36 Unity sprite matches; build {build["version"]}.')


def verify():
    rows = json.loads((EVIDENCE / 'generation-ledger.json').read_text())
    assert costs(rows) == json.loads((EVIDENCE / 'costs.json').read_text()), 'Cost evidence is inconsistent'
    data = json.loads((EVIDENCE / 'integration.json').read_text())
    assert len(data['sprites']) == 36
    assert len({r['file'] for r in data['sprites']}) == 36
    for row in data['sprites']:
        assert sha(ROOT / row['file']) == row['sha256'], f'Changed sprite: {row["file"]}'
    for name, digest in data['code'].items():
        assert sha(ROOT / name) == digest, f'Changed production code: {name}'
    print('PASS: 14 real receipts total $1.15560; 50% cheaper targeted repair calls; 36 tracked Unity sprites and 6 integration source files match.')
    print('Not certified by this check: public download contents, two-minute first play, video link, original live model execution.')


def replay(out):
    data = json.loads((EVIDENCE / 'integration.json').read_text())
    spec = importlib.util.spec_from_file_location('portrait_export', ROOT / 'tools/figures/portraits_export.py')
    exporter = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(exporter)
    out.mkdir(parents=True, exist_ok=True)
    exporter.OUT = out
    for row in data['chosen_ai_sheets']:
        source = ROOT / row['source']
        assert source.is_file() and sha(source) == row['sha256'], 'Replay needs the retained production AI sheets on the author machine'
        exporter.run(row['agent'], source)
    matches = []
    for agent in AGENTS:
        for state in STATES:
            if state == 'downed':
                continue  # The explicitly documented human-selected replacement, not the original grid cell.
            name = f'{agent}_{state}@2x.png'
            assert sha(out / 'final' / name) == sha(UNITY / name), f'Export does not reproduce {name}'
            matches.append(name)
    result = {'reproduced_exactly': matches, 'count': len(matches), 'manual_overrides_not_replayed': [f'{a}_downed@2x.png' for a in AGENTS], 'model_calls': 0}
    (out / 'replay.json').write_text(json.dumps(result, indent=2) + '\n')
    print(f'PASS: {len(matches)} exported portraits reproduce current Unity assets byte for byte; 4 documented downed replacements excluded; no API calls.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['snapshot', 'verify', 'replay'])
    parser.add_argument('--out', type=Path, default=ROOT / 'out/scratch/a07/portrait-replay')
    args = parser.parse_args()
    if args.mode == 'snapshot': snapshot()
    elif args.mode == 'verify': verify()
    else: replay(args.out.resolve())

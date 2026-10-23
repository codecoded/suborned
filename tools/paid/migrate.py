"""One-off migration of the paid-API history into paid/ (brief 053). Record of what was done; refuses to run twice.
  python3 -I tools/paid/migrate.py [--dry-run]
- 047 (loading art) and 052 (teaser): move the git-ignored paid media into paid/<job>/, leave relative symlinks at the old paths,
  convert PROVENANCE.json into ledger.jsonl (dates = the file's modification time, kept by the move; rows without a file inherit the previous
  row's time, marked at_source 'inferred'), mark what was used.
- 007, 020, 033: leave the media where it is and index it in place (ref.where says where it sits: committed, repo-local, store).
The old PROVENANCE.json files are left untouched as the frozen record."""
import datetime, hashlib, json, os, re, subprocess, sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
import paid

ROOT, PD = paid.ROOT, paid.PAID_DIR
DRY = '--dry-run' in sys.argv
STORE = Path('~/Projects/the-suborned-store').expanduser()
iso = lambda t: datetime.datetime.fromtimestamp(t).astimezone().isoformat(timespec='seconds')
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest()
tracked = set(subprocess.run(['git', '-C', str(ROOT), 'ls-files'], capture_output=True, text=True).stdout.splitlines())


def busy():
    out = subprocess.run(['ps', '-axo', 'pid,command'], capture_output=True, text=True).stdout.splitlines()   # read-only: nothing is killed
    return [l for l in out if re.search(r'briefs/052/(gen|edit\d*)\.py|\b(gen|edit\d*)\.py', l) and 'migrate.py' not in l and 'grep' not in l]


def move(src, dst, whole_dir):
    """Move src to dst and leave a relative symlink at src."""
    src, dst = Path(src), Path(dst)
    if src.is_symlink():
        return
    if DRY:
        return
    dst.parent.mkdir(parents=True, exist_ok=True)
    assert not dst.exists(), f'{dst} exists'
    os.rename(src, dst)                      # same filesystem: instant, keeps mtime and birth time
    os.symlink(os.path.relpath(dst, src.parent), src)


def write(job, rows, passes):
    d = PD / job
    if (d / 'ledger.jsonl').exists():
        raise SystemExit(f'{job}: ledger exists, migration already done')
    if DRY:
        return
    d.mkdir(parents=True, exist_ok=True)
    with open(d / 'ledger.jsonl', 'w') as f:
        for i, r in enumerate(rows, 1):
            f.write(json.dumps({'id': i, **r}, ensure_ascii=False) + '\n')
    (d / 'passes.json').write_text(json.dumps(passes, indent=1))


def mtime_of(abs_path):
    return iso(Path(abs_path).stat().st_mtime) if Path(abs_path).exists() else None


def fill_times(rows):
    last = next((r['at'] for r in rows if r.get('at')), None)
    for r in rows:
        if r.get('at'):
            last = r['at']
        else:
            r['at'], r['at_source'] = last, 'inferred'
    return rows


def pass_budget(prior, note):
    """Migrated passes are closed (budget_usd null): more calls on them need `open --raise` (Sky's OK). prior = the old ceiling, kept for the record."""
    d = {'budget_usd': None, 'opened': 'pre-helper', 'note': note, 'history': []}
    if prior: d['prior_budget_usd'] = prior
    return d


# ------------------------------------------------------------------ 047
def mig_047():
    B = ROOT / 'briefs/047'; J = PD / '047-loading-art'
    prov = json.loads((B / 'PROVENANCE.json').read_text())
    final = json.loads((B / 'fix/FINAL.json').read_text())
    final_files = {}                       # fixes/<name> -> letters whose FINAL chain it is part of
    for L in 'ABCD':
        for n in re.findall(r'[A-D]_[A-Za-z0-9_]+\.png', final[L]['made_from']):
            final_files.setdefault(n, set()).add(L)
    picks = {}                             # take file -> label
    for p in sorted((B / 'picks').glob('*.png')):
        m = re.match(r'([A-D])_(pick|alt)_(.+\.png)$', p.name)
        assert sha(p) == sha(B / 'takes' / m.group(3)) if (B / 'takes' / m.group(3)).exists() else True
        picks['takes/' + m.group(3)] = f'047 loading art {m.group(1)}: {"pick" if m.group(2) == "pick" else "alt pick"} ({p.name})'
    rows = []
    for e in prov:
        ph = 'fix' if e.get('phase') == 'fix' else 'takes'
        used = []
        if e['file'] in picks: used.append(picks[e['file']])
        name = e['file'].split('/')[-1]
        if e['file'].startswith('fixes/') and name in final_files:
            used += [f'047 FINAL {L}: made_from chain (fix/{L}_after.png)' for L in sorted(final_files[name])]
        rows.append({'at': mtime_of(B / e['file']), 'at_source': 'mtime', 'service': e.get('service', 'Runware'), 'model': e['model'], 'task': 'image', 'pass': ph,
                     'status': 'ok', 'cost_usd': e['cost_usd'], 'cost_est': None, 'file': e['file'], 'sha256': e['sha256'], 'bytes': (B / e['file']).stat().st_size,
                     'prompt': e['prompt'], 'refs': e.get('refs', []), 'seed': e.get('seed'), 'size': e.get('size'), 'duration_s': None,
                     'params': {k: e[k] for k in ('composition', 'variant', 'phase') if e.get(k)}, 'used_in': used, 'note': e.get('note', ''), 'task_uuid': None})
    # derived finals (no cost): the composite and the four FINAL pictures
    a = 'fixes/A_fixed.png'
    extra = [(a, 'fix', 'derived: A_face_f3 edit with the pick\'s own bollard pasted back over a camera the edit added (local composite, no call)', ['047 FINAL A: composite (fix/A_after.png)'])]
    extra += [(f'fix/{L}_after.png', 'fix', f'derived: FINAL {L}. ' + final[L]['made_from'], [f'047 FINAL {L}: loading screen picture']) for L in 'ABCD']
    for f, ph, note, used in extra:
        rows.append({'at': mtime_of(B / f), 'at_source': 'mtime', 'service': 'local', 'model': None, 'task': 'derived', 'pass': ph, 'status': 'ok', 'cost_usd': 0, 'file': f,
                     'sha256': sha(B / f), 'bytes': (B / f).stat().st_size, 'prompt': '', 'refs': [], 'seed': None, 'size': None, 'params': {}, 'used_in': used, 'note': note})
    fill_times(rows)
    write('047-loading-art', rows, {'takes': pass_budget(None, 'closed: before the helper; budget not recorded'),
                                    'fix': pass_budget(None, 'closed: before the helper; "Fix spend cap $5" in fix.py')})
    for d in ('takes', 'picks', 'fix'):
        move(B / d, J / d, True)
    for p in sorted((B / 'fixes').glob('*.png')):               # fixes/ also holds the scripts: link the pictures one by one
        move(p, J / 'fixes' / p.name, False)
    return len(rows)


# ------------------------------------------------------------------ 052
def mig_052():
    B = ROOT / 'briefs/052'; J = PD / '052-teaser'
    prov = json.loads((B / 'PROVENANCE.json').read_text())
    edit_ix = [i for i, e in enumerate(prov) if e.get('kind') == 'edit']
    pass_of, cur, k = {}, 1, 0
    for i, e in enumerate(prov):                                # edit rows close a pass; an explicit pass field wins
        pass_of[i] = str(e['pass']) if e.get('pass') is not None else str(cur)
        if i in edit_ix:
            cur += 1
    used = {}                                                   # file -> [labels]
    for i in edit_ix:
        e = prov[i]; film = e['file']
        for c in eval(e['cuts']) if isinstance(e['cuts'], str) else e['cuts']:
            used.setdefault(c['file'], []).append(f'{film} ({c["shot"]})')
    vids = {e['file']: e for e in prov if e.get('kind') == 'video'}
    for f, labels in list(used.items()):                        # the keyframe a used clip grew from feeds it
        v = vids.get(f)
        if v:
            for kf in (v.get('keyframe'), v.get('last_frame')):
                if kf and kf in {e.get('file') for e in prov}:
                    used.setdefault(kf, []).append(f'via {f.split("/")[-1]}')
    rows = []
    for i, e in enumerate(prov):
        kind, src = e.get('kind'), e.get('file')
        base = {'service': e.get('service', 'Runware'), 'model': e.get('model'), 'pass': pass_of[i], 'cost_usd': float(e.get('cost_usd') or 0), 'note': e.get('note', ''),
                'refs': e.get('refs', []), 'prompt': e.get('prompt', ''), 'seed': e.get('seed'), 'size': e.get('size'), 'duration_s': e.get('duration_s'), 'used_in': used.get(src, [])}
        params = {k2: e[k2] for k2 in ('shot', 'frame', 'mode', 'resolution', 'keyframe', 'last_frame', 'audio', 'seconds_wall', 'worst_case_cost_usd') if e.get(k2) is not None}
        if kind in ('image', 'video', 'audio'):
            rows.append({**base, 'at': mtime_of(B / src), 'at_source': 'mtime', 'task': kind, 'status': 'ok', 'cost_est': None, 'file': src, 'sha256': e['sha256'],
                         'bytes': (B / src).stat().st_size, 'params': params, 'task_uuid': None})
        elif kind in ('rejected', 'unreturned', 'error', 'blocked'):
            task = 'image' if kind == 'error' else 'video'
            rows.append({**base, 'task': task, 'status': 'blocked' if kind in ('rejected', 'blocked') else 'failed', 'model': e.get('model') or ('minimax:h3@0' if kind == 'blocked' else 'bytedance:seedream@5.0-pro'),
                         'file': None, 'params': {**params, 'kind': kind}, 'at': None})
        elif kind == 'note':
            rows.append({**base, 'task': 'note', 'status': 'ok', 'file': None, 'params': {}, 'at': None})
        elif kind == 'edit':
            edit_file = 'edits/' + src
            rows.append({**base, 'at': mtime_of(B / src), 'at_source': 'mtime', 'task': 'edit', 'service': 'ffmpeg', 'model': None, 'status': 'ok', 'file': edit_file, 'sha256': sha(B / src),
                         'bytes': (B / src).stat().st_size, 'params': {'tool': e.get('tool'), 'spec': e.get('spec'), 'cuts': e.get('cuts')}, 'used_in': ['final film cut (no cost)'],
                         'note': 'recorded in PROVENANCE.json at the time of the edit; the file on disk may be newer if the edit was re-run (sha256 is the file now)'})
        else:
            raise SystemExit(f'unknown row kind {kind}')
    fill_times(rows)
    write('052-teaser', rows, {'1': pass_budget(40, 'closed: gen.py CAP comment "pass 1 closed at $14.61 of $40"'), '2': pass_budget(30, 'closed: gen.py CAP comment "pass 2 (Sky\'s props sheet) has its own $30 ceiling"'),
                               '3': pass_budget(None, 'closed: budget not recorded'), '4': pass_budget(None, 'closed: budget not recorded'),
                               '5': pass_budget(35, 'closed: gen.py CAP comment "v5 pass: 43.65 spent before + $35 ceiling"')})
    for d in ('keyframes', 'shots', 'audio'):
        move(B / d, J / d, True)
    for i in edit_ix:
        move(B / prov[i]['file'], J / 'edits' / prov[i]['file'], False)
    return len(rows)


# ------------------------------------------------------------------ in place: 007, 020, 033
def where(abs_path):
    p = Path(abs_path)
    try:
        rel = str(p.relative_to(ROOT))
    except ValueError:
        return 'store', str(p).replace(str(Path.home()), '~')
    return ('repo-committed' if rel in tracked else 'repo-local'), rel


def in_place(job, entries, passes):
    rows = []
    for e in entries:
        abs_p = Path(e['path']); w, pth = where(abs_p)
        rows.append({'at': mtime_of(e.get('mtime_from', abs_p)), 'at_source': 'mtime', 'service': 'Runware', 'model': e['model'], 'task': 'image', 'pass': e['pass'], 'status': 'ok', 'cost_usd': e['cost'],
                     'cost_est': None, 'ref': {'where': w, 'path': pth}, 'sha256': e.get('sha256'), 'prompt': e.get('prompt', ''), 'refs': [e['reference']] if e.get('reference') else [],
                     'seed': e.get('seed'), 'size': e.get('size'), 'duration_s': None, 'params': {}, 'used_in': e.get('used_in', []), 'note': e.get('note', ''), 'task_uuid': None})
    fill_times(rows)
    write(job, rows, {k: pass_budget(None, 'closed: before the helper; indexed in place') for k in passes})
    return len(rows)


def mig_007():
    ents = []
    for sub in ('Blocks', 'AtomicAge'):
        for e in json.loads((ROOT / f'briefs/007/concepts/{sub}/PROVENANCE.json').read_text()):
            name = e['file']; cand = [ROOT / f'briefs/007/concepts/{sub}/{name}', STORE / f'concepts/{sub}/{name}']
            p = next((c for c in cand if c.exists()), cand[1])
            ents.append({**e, 'path': str(p), 'cost': e['cost'], 'pass': sub.lower() + ('-bakeoff' if name.startswith('bakeoff/') else '')})
    return in_place('007-concepts', ents, sorted({e['pass'] for e in ents}))


def mig_020():
    ents = []
    for e in json.loads((ROOT / 'briefs/020/people/sheets/PROVENANCE.json').read_text()):
        copy = ROOT / e['copy']; orig = STORE / 'concepts/Cell' / e['file']
        ents.append({**e, 'path': str(copy if copy.exists() else orig), 'pass': '1', 'used_in': ['tools/figures reads the committed copy ' + e['copy']],
                     'note': f'store original {e["file"]}; repo copy {e["copy"]}'})
        if orig.exists(): ents[-1]['mtime_from'] = str(orig)
    return in_place('020-people-sheets', ents, ['1'])


def mig_033():
    ents = []
    for e in json.loads((ROOT / 'briefs/033/portraits/PROVENANCE.json').read_text()):
        t = re.search(r'_t(\d+)', e['file']).group(1)
        ents.append({**e, 'cost': e['cost_usd'], 'path': str(ROOT / 'briefs/033/portraits' / e['file']), 'pass': f't{t}'})
    return in_place('033-portraits', ents, sorted({e['pass'] for e in ents}))


if __name__ == '__main__':
    b = busy()
    if b:
        raise SystemExit('a gen.py/edit*.py process is running; wait for it (nothing was changed):\n' + '\n'.join(b))
    for name, fn in (('007-concepts', mig_007), ('020-people-sheets', mig_020), ('033-portraits', mig_033), ('047-loading-art', mig_047), ('052-teaser', mig_052)):
        print(f'{name}: {fn()} ledger rows' + (' (dry run)' if DRY else ''))
    if not DRY:
        print(json.dumps({k: (v['total_usd'], v['calls']) for k, v in paid.refresh()['jobs'].items()}))

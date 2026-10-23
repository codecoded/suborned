"""Paid-API helper (brief 053, Splice, 2026-10-23). Every paid generation call goes through here.

  Library (the gen scripts):
      sys.path.insert(0, str(REPO / 'tools/paid')); import paid
      p = paid.Pass('052-teaser', '6', budget=12.0)            # refuses without a budget
      p.image(model, prompt, 2560, 1440, name='V6S1_t1.png', subdir='keyframes', inputs=..., refs_log=[...], est=0.12)
      p.video(task_fields, name='V6S1_hailuo_t1.mp4', subdir='shots', est=0.49, prompt=..., ...)
      p.audio(task_fields, name='score_t2.mp3', subdir='audio', est=0.2, prompt=...)
  CLI:
      python3 -I tools/paid/paid.py open <job> <pass> --budget USD [--raise] [--note text]
      python3 -I tools/paid/paid.py status [job]        spend, budget and what is left, per pass
      python3 -I tools/paid/paid.py refresh             rebuild the committed rollup tools/paid/spend.json
      python3 -I tools/paid/paid.py view                refresh + thumbnails + paid/index.html   (= bun run paid)
      python3 -I tools/paid/paid.py codex <job> <pass> --name N.png --prompt TEXT [--ref IMG]... [--subdir D]
                                                        a still from the Codex CLI's image generation: free on Sky's ChatGPT plan, so no budget;
                                                        logged at $0 in the same ledger and viewer (Sky, 2026-10-23: "should be using chatgpt cli imagegen
                                                        as that's free"). Stills go here first; Runware is for what Codex can't make (video, music, 3D).

Layout: paid/<job>/ holds the media; paid/<job>/ledger.jsonl is append-only (one line per call: time, service, model, task, cost_usd,
file, prompt, refs, seed, sha256, status ok|blocked|failed, pass, used_in); paid/<job>/passes.json holds the budget of each pass.
paid/ is git-ignored; the rollup tools/paid/spend.json (no media) is committed so spend history survives. The Runware key is read
at runtime by the old repo's client (workbench/Tools/Concept/runware.py from its .env.local); it is never copied here.
Rules: a pass has an explicit budget (opened once with `open`, raised only with --raise, which needs Sky's OK); the helper prints the
estimate before the first call of a run and stops when spent + in-flight + estimate would pass the budget."""
import base64, collections, datetime, fcntl, hashlib, html, io, json, os, re, subprocess, sys, time, uuid
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PAID_DIR = Path(os.environ.get('SUBORNED_PAID', ROOT / 'paid'))
SPEND_PATH = Path(os.environ.get('SUBORNED_PAID_SPEND', ROOT / 'tools/paid/spend.json'))
OLD = Path(os.environ.get('SUBORNED_OLD', '~/Projects/the-suborned')).expanduser() / 'workbench/Tools/Concept'
CALL_TASKS = ('image', 'video', 'audio')           # rows that are provider calls; other tasks (edit, derived, note) are bookkeeping
STATUSES = ('ok', 'blocked', 'failed')
SCHEMA = 'suborned.paid-spend/1'


class BudgetError(SystemExit):
    """Raised (as a SystemExit, so a gen script just stops with the message) when a call would pass the pass budget or none is set."""


def now():
    return datetime.datetime.now().astimezone().isoformat(timespec='seconds')


# ---------------------------------------------------------------- ledger (append-only jsonl, one per job)
def job_dir(job, paid_dir=None):
    if not re.fullmatch(r'\d{3}-[a-z0-9][a-z0-9-]*', job):
        raise ValueError(f'job must look like <brief>-<slug> (e.g. 052-teaser): {job!r}')
    return Path(paid_dir or PAID_DIR) / job


class _Lock:
    def __init__(self, d):
        d.mkdir(parents=True, exist_ok=True); self.f = open(d / '.lock', 'w')
    def __enter__(self):
        fcntl.flock(self.f, fcntl.LOCK_EX); return self
    def __exit__(self, *a):
        fcntl.flock(self.f, fcntl.LOCK_UN); self.f.close()


def read_ledger(job, paid_dir=None):
    p = job_dir(job, paid_dir) / 'ledger.jsonl'
    return [json.loads(l) for l in p.read_text().splitlines() if l.strip()] if p.exists() else []


def append_row(job, row, paid_dir=None):
    """Append one line (locked); assigns id and time. Returns the stored row."""
    d = job_dir(job, paid_dir)
    with _Lock(d):
        p = d / 'ledger.jsonl'
        n = sum(1 for l in p.read_text().splitlines() if l.strip()) if p.exists() else 0
        row = {'id': n + 1, **{k: v for k, v in row.items() if k != 'id'}}
        row.setdefault('at', now()); row.setdefault('at_source', 'call')
        with open(p, 'a') as f:
            f.write(json.dumps(row, ensure_ascii=False) + '\n')
    return row


def row_cost(r):
    """Dollars for budget purposes: the reported cost, else the estimate (a call with no reported cost still counts)."""
    if r.get('task') not in CALL_TASKS:
        return 0.0
    c = r.get('cost_usd')
    return float(c if c is not None else (r.get('cost_est') or 0))


# ---------------------------------------------------------------- passes (budgets) and in-flight reservations
def read_passes(job, paid_dir=None):
    p = job_dir(job, paid_dir) / 'passes.json'
    return json.loads(p.read_text()) if p.exists() else {}


def open_pass(job, name, budget_usd, note='', allow_raise=False, paid_dir=None):
    """Declare (or change) a pass budget. A missing, zero or negative budget is refused; a raise needs allow_raise (Sky's OK first)."""
    if budget_usd is None or not isinstance(budget_usd, (int, float)) or isinstance(budget_usd, bool) or budget_usd <= 0:
        raise BudgetError(f'paid: pass {job}/{name} needs an explicit budget in dollars (> 0); refusing to run without one')
    d = job_dir(job, paid_dir)
    with _Lock(d):
        passes = read_passes(job, paid_dir); cur = passes.get(str(name))
        if cur and budget_usd > (cur.get('budget_usd') or 0) and not allow_raise:
            was = f'has budget ${cur["budget_usd"]:.2f}' if cur.get('budget_usd') else 'is closed'
            raise BudgetError(f'paid: pass {job}/{name} {was}; raising or reopening it (to ${budget_usd:.2f}) needs --raise and Sky\'s OK first. A new pass name needs neither')
        hist = (cur or {}).get('history', [])
        if not cur or cur.get('budget_usd') != budget_usd:
            hist = hist + [{'at': now(), 'budget_usd': budget_usd}]
        passes[str(name)] = {**{k: v for k, v in (cur or {}).items() if k == 'prior_budget_usd'}, 'budget_usd': budget_usd, 'opened': (cur or {}).get('opened', now()),
                             'note': note or (cur or {}).get('note', ''), 'history': hist}
        (d / 'passes.json').write_text(json.dumps(passes, indent=1))
    return passes[str(name)]


def pass_spent(job, name, paid_dir=None):
    return sum(row_cost(r) for r in read_ledger(job, paid_dir) if str(r.get('pass')) == str(name))


def _inflight(d):
    p = d / 'inflight.json'
    items = json.loads(p.read_text()) if p.exists() else []
    live = []
    for it in items:
        try:
            os.kill(it['pid'], 0); live.append(it)       # liveness probe only (signal 0 sends nothing)
        except (ProcessLookupError, KeyError):
            pass
        except PermissionError:
            live.append(it)
    return live


# ---------------------------------------------------------------- client (the old repo's Runware client; key stays in its env file)
class RunwareClient:
    def __init__(self):
        sys.path.insert(0, str(OLD))
        import runware           # noqa: E402  (key read from the old repo's .env.local at call time, never printed or copied)
        self._rw = runware
    def call(self, tasks, timeout=300):
        return self._rw.call(tasks, timeout=timeout)
    def fetch(self, url):
        return self._rw.fetch(url)


KIND = {  # task type, result url key, uuid key, output format, poll every s, give up after s
    'image': ('imageInference', 'imageURL', 'imageUUID', 'PNG', 4.0, 900.0),
    'video': ('videoInference', 'videoURL', 'videoUUID', 'MP4', 8.0, 1500.0),
    'audio': ('audioInference', 'audioURL', 'audioUUID', 'MP3', 8.0, 900.0),
}


def sha256(b):
    return hashlib.sha256(b).hexdigest()


def data_uri(path, size=None, quality=92, fmt='JPEG'):
    """Reference image as a data URI (resized if asked); gen scripts use it for inputs."""
    from PIL import Image
    im = Image.open(path).convert('RGB')
    if size:
        im = im.resize(size, Image.LANCZOS)
    b = io.BytesIO(); im.save(b, fmt, quality=quality)
    return f'data:image/{fmt.lower()};base64,' + base64.b64encode(b.getvalue()).decode()


class Pass:
    """One budgeted pass of one job. Every provider call is `generate` (via image/video/audio)."""
    def __init__(self, job, name, budget=None, *, client=None, paid_dir=None, sleep=time.sleep, allow_raise=False, note='', quiet=False, spend_path=None):
        self.job, self.name, self.paid_dir, self.sleep, self.quiet = job, str(name), paid_dir, sleep, quiet
        self.dir = job_dir(job, paid_dir); self.dir.mkdir(parents=True, exist_ok=True)
        self.spend_path = spend_path
        self._client, self._announced = client, False
        have = read_passes(job, paid_dir).get(self.name)
        if budget is not None:
            have = open_pass(job, self.name, budget, note=note, allow_raise=allow_raise, paid_dir=paid_dir)
        if not have or not have.get('budget_usd'):
            raise BudgetError(f'paid: pass {job}/{self.name} has no budget. Open it first: python3 -I tools/paid/paid.py open {job} {self.name} --budget <USD> '
                              f'(estimate the pass; anything over $10 needs Sky\'s OK)')
        self.budget = float(have['budget_usd'])

    @property
    def client(self):
        if self._client is None:
            self._client = RunwareClient()
        return self._client

    def spent(self):
        return pass_spent(self.job, self.name, self.paid_dir)

    def remaining(self):
        return self.budget - self.spent()

    def _reserve(self, est, label):
        with _Lock(self.dir):
            live = _inflight(self.dir)
            spent, held = self.spent(), sum(i['est'] for i in live)
            if not self._announced and not self.quiet:
                print(f'paid: {self.job}/{self.name}: estimate ${est:.3f} for {label}; spent ${spent:.2f} of ${self.budget:.2f} budget (${self.budget - spent - held:.2f} left)', flush=True)
            self._announced = True
            if spent + held + est > self.budget + 1e-9:
                raise BudgetError(f'paid: budget stop: {self.job}/{self.name} spent ${spent:.3f} + in flight ${held:.3f} + estimate ${est:.3f} > budget ${self.budget:.2f}')
            tok = uuid.uuid4().hex
            (self.dir / 'inflight.json').write_text(json.dumps(live + [{'tok': tok, 'pid': os.getpid(), 'est': est, 'at': now()}]))
        return tok

    def _release(self, tok):
        with _Lock(self.dir):
            (self.dir / 'inflight.json').write_text(json.dumps([i for i in _inflight(self.dir) if i['tok'] != tok]))

    def _log(self, row):
        r = append_row(self.job, {'service': 'Runware', 'pass': self.name, **row}, self.paid_dir)
        try:
            refresh(self.paid_dir, self.spend_path)
        except Exception as e:      # the rollup is a convenience; the ledger line is already safe
            print(f'paid: rollup refresh failed ({e}); run `paid.py refresh`', file=sys.stderr)
        return r

    def generate(self, kind, task, *, name, est, subdir='', prompt='', refs=None, link=None, used_in=None, note='', n=1, params=None, size=None, duration_s=None):
        """Submit task (a Runware task dict without the plumbing keys), wait, save into the job folder, append ledger rows. Returns the rows."""
        ttype, urlkey, uuidkey, fmt, every, limit = KIND[kind]
        if link and Path(link).exists() and not Path(link).is_symlink():       # checked before any spend: a real file there is indexed in place, never replaced
            raise SystemExit(f'paid: {link} is a real file (committed or indexed in place); use a new take name so it is not replaced')
        tok = self._reserve(est, f'{kind} {name}')
        task = {'taskType': ttype, 'taskUUID': str(uuid.uuid4()), 'outputFormat': fmt, 'includeCost': True, 'deliveryMethod': 'async', **task}
        if kind == 'image':
            task.setdefault('outputType', 'URL'); task['numberResults'] = n
        base = dict(task=kind, model=task.get('model'), prompt=prompt or task.get('positivePrompt', ''), refs=refs or [], size=size, duration_s=duration_s,
                    params=params or {}, used_in=used_in or [], note=note, task_uuid=task['taskUUID'])
        t0 = time.time()
        try:
            self.client.call([task])
            got = {}
            while time.time() - t0 < limit:
                self.sleep(every)
                for d in self.client.call([{'taskType': 'getResponse', 'taskUUID': task['taskUUID']}]):
                    if d.get('status') == 'error':
                        raise RuntimeError(json.dumps(d)[:600])
                    if d.get('status') == 'success' and d.get(urlkey):
                        got[d.get(uuidkey) or d[urlkey]] = d
                if len(got) >= (n if kind == 'image' else 1):
                    break
            if not got:
                raise TimeoutError(f'{kind} task {task["taskUUID"]} did not return in {limit:.0f} s')
            rows = []
            for i, d in enumerate(got.values()):
                try:
                    data = self.client.fetch(d[urlkey])
                except Exception as e:          # the provider has already charged: keep the cost and the url so the output can be fetched again
                    self._log({**base, 'status': 'failed', 'cost_usd': d.get('cost'), 'cost_est': None if d.get('cost') is not None else est, 'file': None, 'seed': d.get('seed'),
                               'note': (note + ' ' if note else '') + f'fetch failed ({str(e)[:120]}); result url {d[urlkey]}'})
                    raise SystemExit(f'paid: {kind} generated but fetch failed: {e}')
                nm = name if len(got) == 1 else re.sub(r'(\.\w+)$', rf'_{i}\1', name)
                rel = self._save(subdir, nm, data, link)
                rows.append(self._log({**base, 'status': 'ok', 'cost_usd': d.get('cost'), 'cost_est': None if d.get('cost') is not None else est,
                                       'file': rel, 'sha256': sha256(data), 'bytes': len(data), 'seed': d.get('seed'), 'seconds_wall': round(time.time() - t0)}))
                if not self.quiet:
                    print(f'paid: {rel}  ${d.get("cost")}  pass {self.name}: ${self.spent():.3f} of ${self.budget:.2f}', flush=True)
            return rows
        except BudgetError:
            raise
        except Exception as e:
            timeout = isinstance(e, TimeoutError)
            self._log({**base, 'status': 'failed', 'cost_usd': None if timeout else 0, 'cost_est': est if timeout else None, 'file': None,
                       'note': (note + ' ' if note else '') + f'{"unreturned" if timeout else "error"}: {str(e)[:400]}'})
            raise SystemExit(f'paid: {kind} call failed: {e}')
        finally:
            self._release(tok)

    def _save(self, subdir, name, data, link):
        d = self.dir / subdir if subdir else self.dir
        d.mkdir(parents=True, exist_ok=True)
        p = d / name; k = 1
        while p.exists():                       # never overwrite paid output: a repeated take name keeps the older file
            k += 1; p = d / re.sub(r'(\.\w+)$', rf'__r{k}\1', name)
        p.write_bytes(data)
        if link:
            link = Path(link); link.parent.mkdir(parents=True, exist_ok=True)
            if link.is_symlink():
                link.unlink()
            os.symlink(os.path.relpath(p, link.parent), link)
        return str(p.relative_to(self.dir))

    # ---- the three kinds ----
    def image(self, model, prompt, width, height, *, name, est, subdir='keyframes', inputs=None, n=1, seed=None, negative=None, refs_log=None, link=None, note='', used_in=None, extra=None):
        t = {'model': model, 'positivePrompt': prompt, 'width': width, 'height': height, **(extra or {})}
        if inputs: t['inputs'] = inputs
        if seed: t['seed'] = int(seed)
        if negative: t['negativePrompt'] = negative
        return self.generate('image', t, name=name, est=est, subdir=subdir, prompt=prompt, refs=refs_log, link=link, note=note, n=n, size=f'{width}x{height}', used_in=used_in)

    def video(self, task, *, name, est, subdir='shots', prompt='', refs_log=None, link=None, note='', used_in=None, params=None, duration_s=None):
        return self.generate('video', task, name=name, est=est, subdir=subdir, prompt=prompt or task.get('positivePrompt', ''), refs=refs_log, link=link, note=note,
                             used_in=used_in, params=params, duration_s=duration_s or task.get('duration'))

    def audio(self, task, *, name, est, subdir='audio', prompt='', refs_log=None, link=None, note='', used_in=None, params=None):
        return self.generate('audio', task, name=name, est=est, subdir=subdir, prompt=prompt or task.get('positivePrompt', ''), refs=refs_log, link=link, note=note,
                             used_in=used_in, params=params)


# ---------------------------------------------------------------- free stills: Codex CLI image generation (ChatGPT plan, $0)
CODEX_IMAGES = Path(os.environ.get('CODEX_HOME', '~/.codex')).expanduser() / 'generated_images'


def codex_image(job, pass_name, prompt, name, *, refs=(), subdir='codex', note='', paid_dir=None, spend_path=None, timeout=900, runner=subprocess.run):
    """Generate with `codex exec` (its built-in image tool), copy every image it made into paid/<job>/<subdir>/ and log each at $0. Returns the rows."""
    d = job_dir(job, paid_dir); d.mkdir(parents=True, exist_ok=True)
    t0 = time.time()
    ask = ('Use your image generation tool to make exactly one image for the request below' + (', working from the attached reference image(s)' if refs else '') +
           '. Do not run shell commands and do not write files; just generate the image.\n\n' + prompt)
    cmd = ['codex', 'exec', '--skip-git-repo-check', '-s', 'read-only', '--enable', 'image_generation']
    for r in refs: cmd += ['-i', str(r)]
    # stdin closed: with an open pipe codex exec waits on it after answering. Images land in generated_images/<session id>/, so only this
    # session's are taken (other Codex sessions may be generating at the same time); the time window is the fallback if no id is printed.
    r = runner(cmd + ['--', ask], capture_output=True, text=True, timeout=timeout, cwd=str(d), stdin=subprocess.DEVNULL)
    sid = re.search(r'session id:\s*([0-9a-f-]{36})', (r.stdout or '') + (r.stderr or ''))
    pool = (CODEX_IMAGES / sid.group(1)).glob('*') if sid else (f for f in CODEX_IMAGES.glob('*/*') if f.stat().st_mtime >= t0 - 1)
    made = sorted((f for f in pool if f.suffix.lower() in ('.png', '.jpg', '.jpeg', '.webp')), key=lambda f: f.stat().st_mtime)
    base = {'service': 'Codex (ChatGPT plan)', 'model': 'codex:image_gen', 'task': 'image', 'pass': str(pass_name), 'prompt': prompt,
            'refs': [str(x) for x in refs], 'cost_usd': 0.0, 'used_in': [], 'note': note}
    if not made:
        row = append_row(job, {**base, 'status': 'failed', 'file': None, 'note': (note + ' ' if note else '') + f'codex exit {r.returncode}, no image: {(r.stderr or r.stdout)[-300:]}'}, paid_dir)
        raise SystemExit(f'paid: codex made no image (ledger row {row["id"]})')
    rows = []
    for i, f in enumerate(made):
        data = f.read_bytes(); nm = name if len(made) == 1 else re.sub(r'(\.\w+)$', rf'_{i}\1', name)
        out = d / subdir / nm; out.parent.mkdir(parents=True, exist_ok=True); k = 1
        while out.exists():
            k += 1; out = d / subdir / re.sub(r'(\.\w+)$', rf'__r{k}\1', nm)
        out.write_bytes(data)
        try:
            from PIL import Image
            with Image.open(out) as im: size = f'{im.width}x{im.height}'
        except Exception:
            size = None
        rows.append(append_row(job, {**base, 'status': 'ok', 'file': str(out.relative_to(d)), 'sha256': sha256(data), 'bytes': len(data), 'size': size,
                                     'seconds_wall': round(time.time() - t0), 'params': {'codex_file': str(f)}}, paid_dir))
        print(f'paid: {rows[-1]["file"]}  $0 (codex)', flush=True)
    refresh(paid_dir, spend_path)
    return rows


# ---------------------------------------------------------------- rollup (the committed tools/paid/spend.json)
def _bucket(d, key, cost):
    b = d.setdefault(key, {'usd': 0.0, 'calls': 0}); b['usd'] += cost; b['calls'] += 1


def _used(r):
    return any(not str(u).startswith('via ') for u in r.get('used_in') or [])


def rollup_job(rows, passes=None):
    j = {'total_usd': 0.0, 'calls': 0, 'ok': 0, 'failed': 0, 'blocked': 0, 'used': 0, 'used_usd': 0.0, 'unknown_cost_calls': 0,
         'by_pass': {}, 'by_model': {}, 'by_day': {}}
    for r in rows:
        if r.get('task') not in CALL_TASKS:
            continue
        c = float(r.get('cost_usd') or 0)
        j['total_usd'] += c; j['calls'] += 1; j[r.get('status', 'ok')] = j.get(r.get('status', 'ok'), 0) + 1
        if r.get('cost_usd') is None:
            j['unknown_cost_calls'] += 1
        if r.get('status') == 'ok' and _used(r):
            j['used'] += 1; j['used_usd'] += c
        _bucket(j['by_pass'], str(r.get('pass') or '-'), c); _bucket(j['by_model'], r.get('model') or '-', c); _bucket(j['by_day'], (r.get('at') or '')[:10] or '-', c)
    for k, p in (passes or {}).items():                    # passes with a budget but no calls yet still show
        b = j['by_pass'].setdefault(str(k), {'usd': 0.0, 'calls': 0})
        b['budget_usd'] = p.get('budget_usd')
        if p.get('prior_budget_usd'):
            b['prior_budget_usd'] = p['prior_budget_usd']
    j['cost_per_used'] = round(j['total_usd'] / j['used'], 5) if j['used'] else None
    return _round(j)


def _round(o):
    if isinstance(o, float): return round(o, 5)
    if isinstance(o, dict): return {k: _round(v) for k, v in sorted(o.items())}
    return o


def _merge_buckets(a, b):
    for k, v in b.items():
        t = a.setdefault(k, {'usd': 0.0, 'calls': 0}); t['usd'] += v['usd']; t['calls'] += v['calls']


def rollup(paid_dir=None, spend_path=None):
    """Recompute every job that has a ledger here; keep the committed summary of jobs whose ledger is not on this machine."""
    paid_dir = Path(paid_dir or PAID_DIR); spend_path = Path(spend_path or SPEND_PATH)
    old = json.loads(spend_path.read_text()).get('jobs', {}) if spend_path.exists() else {}
    jobs = dict(old)
    if paid_dir.exists():
        for d in sorted(paid_dir.iterdir()):
            if d.is_dir() and (d / 'ledger.jsonl').exists():
                jobs[d.name] = rollup_job(read_ledger(d.name, paid_dir), read_passes(d.name, paid_dir))
    out = {'schema': SCHEMA, 'total_usd': 0.0, 'calls': 0, 'failed': 0, 'blocked': 0, 'used': 0, 'by_model': {}, 'by_day': {}, 'jobs': dict(sorted(jobs.items()))}
    for j in jobs.values():
        for k in ('total_usd', 'calls', 'failed', 'blocked', 'used'):
            out[k] += j.get(k, 0)
        _merge_buckets(out['by_model'], j['by_model']); _merge_buckets(out['by_day'], j['by_day'])
    return _round(out)


def refresh(paid_dir=None, spend_path=None):
    spend_path = Path(spend_path or SPEND_PATH)
    out = rollup(paid_dir, spend_path)
    txt = json.dumps(out, indent=1) + '\n'
    if not spend_path.exists() or spend_path.read_text() != txt:
        spend_path.parent.mkdir(parents=True, exist_ok=True); spend_path.write_text(txt)
    return out


# ---------------------------------------------------------------- viewer (paid/index.html)
MEDIA = {'.png': 'image', '.jpg': 'image', '.jpeg': 'image', '.webp': 'image', '.mp4': 'video', '.mov': 'video', '.mp3': 'audio', '.wav': 'audio'}


def asset_path(row, job, paid_dir=None):
    """Absolute path of a row's media, or None. Job-owned rows carry `file`; indexed-in-place rows carry ref {where, path}."""
    if row.get('file'):
        return job_dir(job, paid_dir) / row['file']
    ref = row.get('ref')
    if ref and ref.get('path'):
        return Path(os.path.expanduser(ref['path'])) if ref['path'].startswith('~') else ROOT / ref['path']
    return None


def _thumb(src, dest):
    if dest.exists():
        return True
    dest.parent.mkdir(parents=True, exist_ok=True)
    kind = MEDIA.get(src.suffix.lower())
    try:
        if kind == 'image':
            from PIL import Image
            with Image.open(src) as im:
                im.draft('RGB', (1280, 1280)); im = im.convert('RGB'); im.thumbnail((640, 640)); im.save(dest, 'JPEG', quality=78)
            return True
        if kind == 'video':
            r = subprocess.run(['ffmpeg', '-v', 'error', '-y', '-ss', '0.6', '-i', str(src), '-frames:v', '1', '-vf', 'scale=640:-2', '-q:v', '5', str(dest)], capture_output=True)
            if r.returncode or not dest.exists():           # clip shorter than 0.6 s
                subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(src), '-frames:v', '1', '-vf', 'scale=640:-2', '-q:v', '5', str(dest)], capture_output=True)
            return dest.exists()
    except Exception:
        return False
    return False


def view_data(paid_dir=None, spend_path=None, make_thumbs=True):
    paid_dir = Path(paid_dir or PAID_DIR)
    spend = refresh(paid_dir, spend_path)
    rows, todo = [], []
    for d in sorted(paid_dir.iterdir()) if paid_dir.exists() else []:
        if not (d.is_dir() and (d / 'ledger.jsonl').exists()):
            continue
        passes = read_passes(d.name, paid_dir)
        for r in read_ledger(d.name, paid_dir):
            src = asset_path(r, d.name, paid_dir)
            v = {**r, 'job': d.name, 'where': (r.get('ref') or {}).get('where', 'paid') if r.get('ref') else 'paid', 'pass': str(r.get('pass') or '-')}
            v['pass_budget'] = (passes.get(v['pass']) or {}).get('budget_usd')
            if src is not None:
                v['media'] = MEDIA.get(src.suffix.lower(), 'file'); v['exists'] = src.exists()
                v['src'] = os.path.relpath(src, paid_dir) if src.exists() else None
                if src.exists() and make_thumbs and v['media'] in ('image', 'video'):
                    st = src.stat(); key = hashlib.sha1(f'{src}|{st.st_size}|{int(st.st_mtime)}'.encode()).hexdigest()[:20]
                    v['thumb'] = f'.thumbs/{key}.jpg'; todo.append((src, paid_dir / v['thumb'], v))
            rows.append(v)
    if todo:
        with ThreadPoolExecutor(8) as ex:
            for ok, (_, _, v) in zip(ex.map(lambda t: _thumb(t[0], t[1]), todo), todo):
                if not ok: v.pop('thumb', None)
    return {'spend': spend, 'rows': rows, 'built': now()}


def build_view(paid_dir=None, spend_path=None, out=None):
    paid_dir = Path(paid_dir or PAID_DIR)
    data = view_data(paid_dir, spend_path)
    tpl = (Path(__file__).parent / 'viewer.html').read_text()
    blob = json.dumps(data, ensure_ascii=False).replace('</', '<\\/')
    out = Path(out or paid_dir / 'index.html'); out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(tpl.replace('/*DATA*/null', blob))
    return out, data


# ---------------------------------------------------------------- CLI
def _arg(a, k, d=None):
    return a[a.index(k) + 1] if k in a else d


def main(a):
    if not a or a[0] in ('-h', '--help'):
        print(__doc__); return 0
    if a[0] == 'open':
        p = open_pass(a[1], a[2], float(_arg(a, '--budget')) if _arg(a, '--budget') else None, note=_arg(a, '--note', ''), allow_raise='--raise' in a)
        print(f'paid: {a[1]}/{a[2]} budget ${p["budget_usd"]:.2f}'); return 0
    if a[0] == 'status':
        spend = refresh()
        for job, j in spend['jobs'].items():
            if len(a) > 1 and a[1] != job: continue
            print(f'{job}: ${j["total_usd"]:.2f} in {j["calls"]} calls ({j["failed"]} failed, {j["blocked"]} blocked, {j["used"]} used)')
            for k, b in j['by_pass'].items():
                bud, was = b.get('budget_usd'), b.get('prior_budget_usd'); print(f'   pass {k}: ${b["usd"]:.2f} / {("$%.2f" % bud) if bud else "closed" + (" (was $%.2f)" % was if was else "")}  {b["calls"]} calls')
        print(f'TOTAL ${spend["total_usd"]:.2f} in {spend["calls"]} calls'); return 0
    if a[0] == 'refresh':
        s = refresh(); print(f'paid: ${s["total_usd"]:.2f} in {s["calls"]} calls -> {SPEND_PATH.relative_to(ROOT)}'); return 0
    if a[0] == 'view':
        out, data = build_view()
        s = data['spend']; print(f'paid: ${s["total_usd"]:.2f} in {s["calls"]} calls, {len(data["rows"])} ledger rows -> {out}')
        return 0
    if a[0] == 'codex':
        refs = [a[i + 1] for i, x in enumerate(a) if x == '--ref']
        if not _arg(a, '--name') or not _arg(a, '--prompt'): raise SystemExit('paid: codex needs --name and --prompt')
        codex_image(a[1], a[2], _arg(a, '--prompt'), _arg(a, '--name'), refs=[Path(x).resolve() for x in refs], subdir=_arg(a, '--subdir', 'codex'), note=_arg(a, '--note', ''))
        return 0
    print(f'paid: unknown command {a[0]!r}', file=sys.stderr); return 2


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))

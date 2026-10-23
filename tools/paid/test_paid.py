"""Tests for the paid helper (brief 053): ledger, rollup maths, budget refusal. No network and no real provider call anywhere:
a fake client stands in for Runware (a Pass never builds the real client when one is given)."""
import io, json, os, sys, tempfile, unittest
from contextlib import redirect_stdout
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
import paid

def _png():
    from PIL import Image
    b = io.BytesIO(); Image.new('RGB', (8, 8), (200, 180, 160)).save(b, 'PNG'); return b.getvalue()
PNG = _png()


class Fake:
    """Answers submit with nothing and getResponse with the programmed results."""
    def __init__(self, results=None, cost=0.1, fail=None, never=False):
        self.results, self.cost, self.fail, self.never, self.calls, self.fetched = results, cost, fail, never, [], []
    def call(self, tasks, timeout=300):
        self.calls.append(tasks[0])
        t = tasks[0]
        if t['taskType'] != 'getResponse' or self.never:
            return []
        if self.fail:
            return [{'status': 'error', 'message': self.fail}]
        n = t.get('n', 1)
        return self.results if self.results is not None else [{'status': 'success', 'imageURL': 'https://x/i.png', 'imageUUID': 'u1', 'cost': self.cost, 'seed': 7}]
    def fetch(self, url):
        self.fetched.append(url); return PNG


class T(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.dir = Path(self.tmp.name) / 'paid'; self.spend = Path(self.tmp.name) / 'spend.json'
        self.addCleanup(self.tmp.cleanup)

    def mk(self, budget=1.0, client=None, name='1', **kw):
        return paid.Pass('900-test', name, budget=budget, client=client or Fake(), paid_dir=self.dir, sleep=lambda s: None, spend_path=self.spend, quiet=True, **kw)

    def img(self, p, name='a.png', est=0.12, **kw):
        return p.image('bytedance:seedream@5.0-pro', 'a prompt', 64, 64, name=name, est=est, refs_log=['refs/x.png'], **kw)

    # ---- budget refusal
    def test_refuses_without_budget(self):
        with self.assertRaises(paid.BudgetError):
            paid.Pass('900-test', '1', client=Fake(), paid_dir=self.dir, spend_path=self.spend)
        for bad in (0, -1, None, True):
            with self.assertRaises(paid.BudgetError):
                paid.open_pass('900-test', 'x', bad, paid_dir=self.dir)

    def test_no_call_is_made_without_budget(self):
        fake = Fake()
        with self.assertRaises(paid.BudgetError):
            paid.Pass('900-test', '1', client=fake, paid_dir=self.dir, spend_path=self.spend)
        self.assertEqual(fake.calls, [])
        self.assertFalse((self.dir / '900-test/ledger.jsonl').exists())

    def test_budget_is_remembered_and_raise_needs_flag(self):
        self.mk(budget=2.0)
        p = paid.Pass('900-test', '1', client=Fake(), paid_dir=self.dir, spend_path=self.spend, quiet=True)   # later run, no --budget
        self.assertEqual(p.budget, 2.0)
        with self.assertRaises(paid.BudgetError):
            self.mk(budget=3.0)
        self.assertEqual(self.mk(budget=3.0, allow_raise=True).budget, 3.0)
        self.assertEqual(len(paid.read_passes('900-test', self.dir)['1']['history']), 2)
        self.assertEqual(self.mk(budget=1.0).budget, 1.0)             # lowering is always fine

    def test_closed_pass_refuses_until_reopened_with_raise(self):
        d = self.dir / '900-test'; d.mkdir(parents=True)
        (d / 'passes.json').write_text(json.dumps({'old': {'budget_usd': None, 'prior_budget_usd': 40}}))
        with self.assertRaises(paid.BudgetError):
            paid.Pass('900-test', 'old', client=Fake(), paid_dir=self.dir, spend_path=self.spend)
        with self.assertRaises(paid.BudgetError):
            self.mk(budget=5.0, name='old')
        self.assertEqual(self.mk(budget=5.0, name='old', allow_raise=True).budget, 5.0)

    def test_stops_at_budget(self):
        fake = Fake(cost=0.4); p = self.mk(budget=1.0, client=fake)
        self.img(p, 'a.png', est=0.4); self.img(p, 'b.png', est=0.4)
        self.assertAlmostEqual(p.spent(), 0.8)
        n = len(fake.calls)
        with self.assertRaises(paid.BudgetError) as e:
            self.img(p, 'c.png', est=0.4)                              # 0.8 + 0.4 > 1.0
        self.assertIn('budget stop', str(e.exception))
        self.assertEqual(len(fake.calls), n)                           # nothing was sent
        self.assertFalse((self.dir / '900-test/keyframes/c.png').exists())
        self.img(p, 'd.png', est=0.2)                                  # 0.8 + 0.2 fits exactly

    def test_estimate_printed_before_first_call_only(self):
        p = paid.Pass('900-test', '1', budget=1.0, client=Fake(), paid_dir=self.dir, sleep=lambda s: None, spend_path=self.spend)
        buf = io.StringIO()
        with redirect_stdout(buf):
            self.img(p, 'a.png', est=0.12); self.img(p, 'b.png', est=0.12)
        out = buf.getvalue()
        self.assertEqual(out.count('estimate $0.120'), 1)
        self.assertIn('of $1.00 budget', out)
        self.assertLess(out.index('estimate'), out.index('a.png'))

    def test_in_flight_reservation_counts(self):
        p = self.mk(budget=1.0)
        (self.dir / '900-test/inflight.json').write_text(json.dumps([{'tok': 'x', 'pid': os.getpid(), 'est': 0.95, 'at': 'now'}]))
        with self.assertRaises(paid.BudgetError):
            self.img(p, est=0.12)
        (self.dir / '900-test/inflight.json').write_text(json.dumps([{'tok': 'x', 'pid': 2 ** 22 + 12345, 'est': 0.95, 'at': 'now'}]))   # dead pid: ignored
        self.img(p, est=0.12)

    # ---- ledger
    def test_call_writes_file_ledger_and_rollup(self):
        p = self.mk(client=Fake(cost=0.0963)); [row] = self.img(p, 'A_t1.png', note='n')
        f = self.dir / '900-test/keyframes/A_t1.png'
        self.assertEqual(f.read_bytes(), PNG)
        [led] = paid.read_ledger('900-test', self.dir)
        for k, v in dict(id=1, service='Runware', model='bytedance:seedream@5.0-pro', task='image', status='ok', cost_usd=0.0963, file='keyframes/A_t1.png', seed=7,
                         prompt='a prompt', refs=['refs/x.png'], used_in=[], note='n').items():
            self.assertEqual(led[k], v, k)
        self.assertEqual(led['sha256'], paid.sha256(PNG)); self.assertEqual(led['pass'], '1'); self.assertTrue(led['at'])
        s = json.loads(self.spend.read_text())
        self.assertEqual(s['jobs']['900-test']['total_usd'], 0.0963); self.assertEqual(s['total_usd'], 0.0963)

    def test_ledger_is_append_only_and_ids_grow(self):
        p = self.mk(); self.img(p, 'a.png'); self.img(p, 'b.png')
        first = (self.dir / '900-test/ledger.jsonl').read_text().splitlines()
        self.img(p, 'c.png')
        now = (self.dir / '900-test/ledger.jsonl').read_text().splitlines()
        self.assertEqual(now[:2], first); self.assertEqual([json.loads(l)['id'] for l in now], [1, 2, 3])

    def test_repeated_name_keeps_older_file(self):
        p = self.mk(); self.img(p, 'a.png'); self.img(p, 'a.png')
        names = sorted(x.name for x in (self.dir / '900-test/keyframes').iterdir())
        self.assertEqual(names, ['a.png', 'a__r2.png'])

    def test_failed_call_is_logged_with_zero_cost(self):
        p = self.mk(client=Fake(fail='moderation'))
        with self.assertRaises(SystemExit):
            self.img(p)
        [led] = paid.read_ledger('900-test', self.dir)
        self.assertEqual((led['status'], led['cost_usd'], led['file']), ('failed', 0, None)); self.assertIn('moderation', led['note'])
        self.assertEqual(p.spent(), 0)
        self.assertEqual(json.loads(self.spend.read_text())['jobs']['900-test']['failed'], 1)

    def test_unreturned_call_counts_its_estimate_toward_the_budget(self):
        p = self.mk(client=Fake(never=True)); orig = paid.KIND['image']; paid.KIND['image'] = orig[:5] + (0.0,)
        try:
            with self.assertRaises(SystemExit):
                self.img(p, est=0.3)
        finally:
            paid.KIND['image'] = orig
        [led] = paid.read_ledger('900-test', self.dir)
        self.assertIsNone(led['cost_usd']); self.assertEqual(led['cost_est'], 0.3); self.assertEqual(p.spent(), 0.3)

    def test_missing_cost_uses_estimate(self):
        p = self.mk(client=Fake(results=[{'status': 'success', 'imageURL': 'u', 'imageUUID': 'k'}])); self.img(p, est=0.2)
        self.assertEqual(p.spent(), 0.2)
        j = json.loads(self.spend.read_text())['jobs']['900-test']
        self.assertEqual((j['total_usd'], j['unknown_cost_calls']), (0, 1))

    def test_link_symlinks_and_never_replaces_a_real_file(self):
        p = self.mk(); link = Path(self.tmp.name) / 'consumer/a.png'
        self.img(p, 'a.png', link=link)
        self.assertTrue(link.is_symlink()); self.assertEqual(link.read_bytes(), PNG)
        self.assertFalse(os.path.isabs(os.readlink(link)))
        real = Path(self.tmp.name) / 'consumer/b.png'; real.write_bytes(b'mine')
        fake = p.client
        with self.assertRaises(SystemExit):
            self.img(p, 'b.png', link=real)
        self.assertEqual(real.read_bytes(), b'mine')
        self.assertEqual(sum(1 for c in fake.calls if c['taskType'] == 'imageInference'), 1)     # the refused one sent nothing

    # ---- rollup maths
    ROWS = [
        dict(task='image', pass_='1', model='m1', at='2026-10-20T10:00:00+01:00', status='ok', cost_usd=0.10, used_in=['film']),
        dict(task='image', pass_='1', model='m1', at='2026-10-20T11:00:00+01:00', status='ok', cost_usd=0.20, used_in=['via clip']),
        dict(task='video', pass_='2', model='m2', at='2026-10-21T09:00:00+01:00', status='ok', cost_usd=0.50, used_in=['film (shot)']),
        dict(task='video', pass_='2', model='m2', at='2026-10-21T09:30:00+01:00', status='blocked', cost_usd=0, used_in=[]),
        dict(task='image', pass_='2', model='m1', at='2026-10-21T10:00:00+01:00', status='failed', cost_usd=None, cost_est=0.3, used_in=[]),
        dict(task='audio', pass_='2', model='m3', at='2026-10-22T10:00:00+01:00', status='ok', cost_usd=0.04, used_in=[]),
        dict(task='edit', pass_='2', model=None, at='2026-10-22T11:00:00+01:00', status='ok', cost_usd=0, used_in=['final']),
        dict(task='note', pass_='2', model=None, at='2026-10-22T12:00:00+01:00', status='ok', cost_usd=9.99, used_in=[]),
    ]

    def rows(self):
        return [{**{k: v for k, v in r.items() if k != 'pass_'}, 'pass': r['pass_']} for r in self.ROWS]

    def test_rollup_job_maths(self):
        j = paid.rollup_job(self.rows(), {'1': {'budget_usd': 5.0}, '3': {'budget_usd': None, 'prior_budget_usd': 7}})
        self.assertAlmostEqual(j['total_usd'], 0.84)                  # notes and edits are not calls; None cost counts 0
        self.assertEqual((j['calls'], j['ok'], j['failed'], j['blocked']), (6, 4, 1, 1))
        self.assertEqual((j['used'], j['unknown_cost_calls']), (2, 1))   # 'via' feeds do not count as used; failed/blocked never do
        self.assertAlmostEqual(j['used_usd'], 0.60); self.assertAlmostEqual(j['cost_per_used'], 0.42)
        self.assertEqual(j['by_pass']['1'], {'usd': 0.3, 'calls': 2, 'budget_usd': 5.0})
        self.assertEqual(j['by_pass']['2']['calls'], 4); self.assertAlmostEqual(j['by_pass']['2']['usd'], 0.54)
        self.assertEqual(j['by_pass']['3'], {'usd': 0, 'calls': 0, 'budget_usd': None, 'prior_budget_usd': 7})
        self.assertAlmostEqual(j['by_model']['m1']['usd'], 0.3); self.assertEqual(j['by_model']['m1']['calls'], 3)
        self.assertEqual(sorted(j['by_day']), ['2026-10-20', '2026-10-21', '2026-10-22'])
        self.assertAlmostEqual(j['by_day']['2026-10-21']['usd'], 0.5); self.assertEqual(j['by_day']['2026-10-21']['calls'], 3)

    def test_rollup_totals_and_survival_of_jobs_without_a_ledger(self):
        for job in ('900-a', '900-b'):
            d = self.dir / job; d.mkdir(parents=True)
            with open(d / 'ledger.jsonl', 'w') as f:
                for r in self.rows(): f.write(json.dumps(r) + '\n')
        s = paid.refresh(self.dir, self.spend)
        self.assertAlmostEqual(s['total_usd'], 1.68); self.assertEqual(s['calls'], 12); self.assertEqual((s['failed'], s['blocked'], s['used']), (2, 2, 4))
        self.assertAlmostEqual(s['by_model']['m1']['usd'], 0.6); self.assertAlmostEqual(s['by_day']['2026-10-20']['usd'], 0.6)
        # a fresh clone has the committed rollup but no paid/ media or ledger: history is kept, not wiped
        import shutil; shutil.rmtree(self.dir / '900-b')
        s2 = paid.refresh(self.dir, self.spend)
        self.assertEqual(s2, s)
        shutil.rmtree(self.dir)
        self.assertEqual(paid.refresh(self.dir, self.spend), s)

    def test_rollup_is_deterministic(self):
        d = self.dir / '900-a'; d.mkdir(parents=True)
        (d / 'ledger.jsonl').write_text(''.join(json.dumps(r) + '\n' for r in self.rows()))
        paid.refresh(self.dir, self.spend); a = self.spend.read_text(); paid.refresh(self.dir, self.spend)
        self.assertEqual(a, self.spend.read_text()); self.assertNotIn('built', a)

    # ---- viewer
    def test_view_builds_html_with_totals_and_relative_media(self):
        p = self.mk(client=Fake(cost=0.25)); self.img(p, 'a.png')
        (self.dir / 'index.html').unlink(missing_ok=True)
        out, data = paid.build_view(self.dir, self.spend)
        html = out.read_text()
        self.assertEqual(out, self.dir / 'index.html'); self.assertIn('900-test', html); self.assertNotIn('/*DATA*/', html)
        [row] = data['rows']
        self.assertEqual((row['src'], row['media'], row['exists']), ('900-test/keyframes/a.png', 'image', True))
        self.assertTrue(row['thumb'].startswith('.thumbs/') and (self.dir / row['thumb']).exists())
        self.assertEqual(data['spend']['total_usd'], 0.25)

    def test_asset_path_for_rows_indexed_in_place(self):
        r = {'ref': {'where': 'repo-local', 'path': 'briefs/x/y.png'}}
        self.assertEqual(paid.asset_path(r, '900-test', self.dir), paid.ROOT / 'briefs/x/y.png')
        self.assertEqual(paid.asset_path({'file': 'k/a.png'}, '900-test', self.dir), self.dir / '900-test/k/a.png')
        with self.assertRaises(ValueError):
            paid.job_dir('Bad Job')

    # ---- free stills through the Codex CLI
    def test_codex_image_copies_new_images_and_logs_zero_cost(self):
        gen = Path(self.tmp.name) / 'codex_images'; (gen / 'old').mkdir(parents=True); (gen / 'old/x.png').write_bytes(PNG)
        os.utime(gen / 'old/x.png', (1, 1))                      # made before this call: not picked up
        seen = []
        def runner(cmd, **kw):
            seen.append(cmd); sid = '01a168a8-b8cb-7722-80aa-93859f4ede5f'
            (gen / sid).mkdir(exist_ok=True); (gen / sid / 'a.png').write_bytes(PNG)
            (gen / 'other').mkdir(exist_ok=True); (gen / 'other/b.png').write_bytes(PNG)      # another session, same moment: not taken
            self.assertIs(kw.get('stdin'), paid.subprocess.DEVNULL)
            return type('R', (), {'returncode': 0, 'stdout': '', 'stderr': f'session id: {sid}\n'})()
        old, paid.CODEX_IMAGES = paid.CODEX_IMAGES, gen
        try:
            with redirect_stdout(io.StringIO()):
                rows = paid.codex_image('900-test', 'c1', 'a prompt', 'f.png', refs=[Path('/r/ref.png')], paid_dir=self.dir, spend_path=self.spend, runner=runner)
        finally:
            paid.CODEX_IMAGES = old
        self.assertEqual(len(rows), 1)
        self.assertIn('-i', seen[0]); self.assertIn('/r/ref.png', seen[0])
        r = paid.read_ledger('900-test', self.dir)[0]
        self.assertEqual((r['cost_usd'], r['model'], r['status'], r['pass'], r['file'], r['size']), (0.0, 'codex:image_gen', 'ok', 'c1', 'codex/f.png', '8x8'))
        self.assertEqual((self.dir / '900-test/codex/f.png').read_bytes(), PNG)
        self.assertEqual(json.loads(self.spend.read_text())['total_usd'], 0)

    def test_codex_image_with_no_output_logs_a_failed_row(self):
        gen = Path(self.tmp.name) / 'empty'; gen.mkdir()
        old, paid.CODEX_IMAGES = paid.CODEX_IMAGES, gen
        try:
            with self.assertRaises(SystemExit):
                paid.codex_image('900-test', 'c1', 'p', 'f.png', paid_dir=self.dir, spend_path=self.spend,
                                 runner=lambda cmd, **kw: type('R', (), {'returncode': 1, 'stdout': '', 'stderr': 'boom'})())
        finally:
            paid.CODEX_IMAGES = old
        self.assertEqual(paid.read_ledger('900-test', self.dir)[0]['status'], 'failed')


if __name__ == '__main__':
    unittest.main()

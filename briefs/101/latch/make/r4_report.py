"""Brief 101 steps 4-6: the report for Sky and Aster (briefs/101/latch/r4.html), from coverage-r4.json and the pictures r4.py made (git-ignored under r4/).
The words are in report-r4.json beside it (one place to edit them). Latch. Usage: python3 -I r4_report.py"""
import html, json, os

R = '/Users/sky/Projects/suborned/'
D = R + 'briefs/101/latch/'
cov = json.load(open(D + 'coverage-r4.json'))
words = json.load(open(D + 'make/report-r4.json'))
e = html.escape


def para(t): return f'<p>{e(t)}</p>'


def ul(items): return '<ul>' + ''.join(f'<li>{e(i)}</li>' for i in items) + '</ul>'


def table(head, rows):
    h = ''.join(f'<th>{e(x)}</th>' for x in head)
    b = ''.join('<tr>' + ''.join(f'<td>{e(str(x))}</td>' for x in r) + '</tr>' for r in rows)
    return f'<table><tr>{h}</tr>{b}</table>'


def fig(src, cap): return f'<figure><img src="{e(src)}" alt="{e(cap)}"><figcaption>{e(cap)}</figcaption></figure>' if os.path.exists(D + src) else para(f'(picture missing: {src})')


css = ('body{font:15px/1.45 -apple-system,system-ui,sans-serif;margin:0;padding:24px;max-width:1180px;color:#17302f;background:#f7f4ec}'
       'h1{font-size:22px;margin:0 0 4px}h2{font-size:17px;margin:28px 0 8px;border-bottom:1px solid #d2e6e2;padding-bottom:4px}'
       'p.sub{color:#4a6461;margin:0 0 16px}table{border-collapse:collapse;width:100%;font-size:13.5px}'
       'td,th{border-bottom:1px solid #d2e6e2;padding:5px 6px;text-align:left;vertical-align:top}th{background:#e8f1ee}'
       'code{font-size:12.5px;background:#e8f1ee;padding:0 3px;border-radius:3px}figure{margin:0 0 18px}figure img{width:100%;border:1px solid #d2e6e2}'
       'figcaption{font-size:12.5px;color:#4a6461}')
out = [f'<!doctype html><meta charset="utf-8"><title>{e(words["title"])}</title><style>{css}</style>', f'<h1>{e(words["title"])}</h1>', f'<p class="sub">{e(words["sub"])}</p>']
out.append('<h2>The captures beside the mocks</h2>')
for key, row in cov['rows'].items():
    out.append(fig(f'r4/side-{key}.png', f'{row["what"]}. This build {row["now"]} % of the screen; the S mock {row["mock"]} %; the build before steps 4-6 {row["before"]} %.'))
out.append(fig('r4/close-busy.png', 'The faces and the lens at 2x, the busiest moment: this build (left), the S mock (right).'))
out.append(fig('r4/close-rest.png', 'The faces and the lens at 2x, at rest.'))
out.append('<h2>Room on screen</h2>')
out.append(para(cov['method'] + '.'))
out.append(table(['moment', 'S mock', 'before (0.2.280)', 'this build'], [[r['what'], f"{r['mock']} %", f"{r['before']} %", f"{r['now']} %"] for r in cov['rows'].values()]))
for sec in words['sections']:
    out.append(f'<h2>{e(sec["h"])}</h2>')
    for b in sec['body']: out.append(ul(b) if isinstance(b, list) else para(b))
open(D + 'r4.html', 'w').write('\n'.join(out) + '\n')
print('wrote', D + 'r4.html')

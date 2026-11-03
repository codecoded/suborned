# The city's name lists beyond the national statistics offices (brief 089 follow-up, Pulse): the heritages the figures show that no open national
# frequency list covers (East Asian, South Asian, West African), cut from English Wikipedia (CC BY-SA 4.0) as data: ranked surname tables where
# Wikipedia has them (Japan, Korea, China), else its categories of given names and surnames by language (unranked: every name weighs the same).
# Also the US Census surnames split by the census's own shares (white, black, Asian and Pacific Islander), so an American is drawn a surname as common
# among people who look as their figure does. No model reads, writes or picks a name. The downloads are untrusted data, read only as text:
#   <venv>/bin/python -I tools/people/names/extract_wiki.py <downloads dir> <out dir>
# where <downloads dir>/wiki/ holds the MediaWiki API answers (action=parse wikitext, list=categorymembers) and <downloads dir>/us/census/ the census table.
import csv, hashlib, json, os, re, sys, unicodedata

SRC, OUT = sys.argv[1], sys.argv[2]
DECADES = [1940, 1950, 1960, 1970, 1980, 1990, 2000]
NAME = re.compile(r"^[A-Z][a-z]+(?:[-'][A-Za-z][a-z]+)?$")  # one word, Latin letters (a hyphen in Korean names: Seo-jun)
files = {}


def sha(p):
    return hashlib.sha256(open(p, 'rb').read()).hexdigest()


def use(cc, rel):
    p = os.path.join(SRC, rel); files.setdefault(cc, {})[rel] = sha(p); return p


def plain(s):
    return unicodedata.normalize('NFC', ''.join(c for c in unicodedata.normalize('NFD', s) if not unicodedata.combining(c)))


def clean(n):
    """A display name as the city writes it: no parenthesised note, no tone marks or macrons (Satō → Sato, Wáng → Wang), one word."""
    n = plain(re.sub(r'\s*\(.*?\)', '', n).split('/')[0].strip())
    return n if NAME.match(n) else None


def wikitext(rel, cc):
    d = json.load(open(use(cc, rel), encoding='utf-8'))
    return d['parse']['wikitext'], d['parse']['revid'], d['parse']['title']


def detemplate(t):
    t = re.sub(r'<ref[^>]*/>', '', t); t = re.sub(r'<ref.*?</ref>', '', t, flags=re.S)
    out, depth, i = [], 0, 0
    while i < len(t):
        if t.startswith('{{', i): depth += 1; i += 2; continue
        if t.startswith('}}', i) and depth: depth -= 1; i += 2; continue
        if not depth: out.append(t[i])
        i += 1
    return ''.join(out)


def link_text(cell):
    m = re.search(r'\[\[([^\]|]+)(?:\|([^\]]+))?\]\]', cell)
    return (m.group(2) or m.group(1)) if m else cell.strip()


def ranked_rows(section):
    """[(name, count or None)] of a wikitable section, in rank order: the first link (or text) after the rank that reads as a name, and the largest number in the row."""
    out = []
    for row in detemplate(section).split('\n|-'):
        cells = [c.strip() for c in re.split(r'\n\||\|\|', row) if c.strip()]
        cells = [re.sub(r'^(?:colspan|rowspan|style|align)=[^|]*\|', '', c).strip() for c in cells]
        if not cells or not re.match(r'^\d+$', cells[0]): continue
        name = next((x for x in (clean(link_text(c)) for c in cells[1:]) if x), None)
        nums = [int(n.replace(',', '')) for n in re.findall(r'\b\d{1,3}(?:,\d{3})+\b|\b\d{4,}\b', row)]
        if name: out.append((name, max(nums) if nums else None))
    return out


def section(t, title):
    i = re.search(r'^==\s*' + re.escape(title) + r'\s*==\s*$', t, re.M)
    if not i: raise SystemExit(f'no section {title}')
    j = re.search(r'^==[^=]', t[i.end():], re.M)
    return t[i.end(): i.end() + j.start()] if j else t[i.end():]


def weighted(rows):
    """Counts where the table gives them; where it gives only the rank, Zipf's 1/rank (scaled to a million): the order is the source's, the size an estimate."""
    seen, out = set(), []
    for r, (n, k) in enumerate(rows, 1):
        if n in seen: continue
        seen.add(n); out.append([n, k if k else round(1_000_000 / r)])
    return out


def cat(cc, name):
    d = json.load(open(use(cc, f'wiki/cat_{name}.json'), encoding='utf-8'))
    return [x for x in (clean(t) for t in d['titles'] if not t.startswith('List')) if x]


def flat(names):
    """An unranked list: every name once, each weighing 1, in alphabetical order (no top to skip: the generator reads `ranked`)."""
    return [[n, 1] for n in sorted(set(names))]


def same_every_decade(lst):
    return {str(d): lst for d in DECADES}


def jp():
    t, rev, _ = wikitext('wiki/List_of_common_Japanese_surnames.json', 'jp')
    fam = weighted(ranked_rows(t))
    g = {'f': flat(cat('jp', 'Japanese_feminine_given_names')), 'm': flat(cat('jp', 'Japanese_masculine_given_names'))}
    return {'given': same_every_decade(g), 'family': {'any': fam}, 'ranked': {'given': False, 'family': True}}


def kr():
    t, _, _ = wikitext('wiki/List_of_most_common_surnames_in_Asian_countries.json', 'kr')
    fam = weighted(ranked_rows(section(t, 'Korea')))
    p, _, _ = wikitext('wiki/List_of_the_most_popular_given_names_in_South_Korea.json', 'kr')
    g = {'f': {}, 'm': {}}
    for m in re.finditer(r'^==\s*(\d{4})\s*==\s*$', p, re.M):
        year = int(m.group(1))
        if not 1940 <= year <= 2009: continue
        body = section(p, m.group(1))
        for tab in body.split('{| class="wikitable')[1:]:
            sex = 'm' if re.search(r'\|\+\s*Boys', tab) else 'f' if re.search(r'\|\+\s*Girls', tab) else None
            if not sex: continue
            for row in detemplate(tab).split('\n|-')[1:]:
                cells = [c.strip() for c in re.split(r'\n\||\|\|', row) if c.strip() and not c.strip().startswith('!')]
                n = clean(link_text(cells[0])) if cells else None
                if n: g[sex][n] = g[sex].get(n, 0) + 1  # years the name is in the top list
    gl = {s: [[n, k] for n, k in sorted(v.items(), key=lambda x: (-x[1], x[0]))] for s, v in g.items()}
    return {'given': same_every_decade(gl), 'family': {'any': fam}, 'ranked': {'given': False, 'family': True}}


def cn():
    t, _, _ = wikitext('wiki/List_of_most_common_surnames_in_Asian_countries.json', 'cn')
    fam = weighted(ranked_rows(section(t, 'China')))
    g = {'f': flat(cat('cn', 'Chinese_feminine_given_names')), 'm': flat(cat('cn', 'Chinese_masculine_given_names'))}
    return {'given': same_every_decade(g), 'family': {'any': fam}, 'ranked': {'given': False, 'family': True}}


def india():
    fam = flat(sum((cat('in', c) for c in ['Punjabi-language_surnames', 'Telugu-language_surnames', 'Marathi-language_surnames', 'Tamil-language_surnames', 'Bengali-language_surnames']), []))
    g = {'f': flat(cat('in', 'Indian_feminine_given_names')), 'm': flat(cat('in', 'Indian_masculine_given_names'))}
    return {'given': same_every_decade(g), 'family': {'any': fam}, 'ranked': {'given': False, 'family': False}}


def ng():
    fam = flat(sum((cat('ng', c) for c in ['Yoruba-language_surnames', 'Igbo-language_surnames', 'Hausa-language_surnames']), []))
    g = {'f': flat(cat('ng', 'Nigerian_feminine_given_names') + cat('ng', 'Yoruba_feminine_given_names')),
         'm': flat(cat('ng', 'Nigerian_masculine_given_names') + cat('ng', 'Yoruba_masculine_given_names'))}
    return {'given': same_every_decade(g), 'family': {'any': fam}, 'ranked': {'given': False, 'family': False}}


def us_shares(known):
    """The census's surnames weighted by its own shares: count × pctwhite, × pctblack, × pctapi (a suppressed share, "(S)", is 0). Most common first.
    A name is cased as extract.py cased it in `any` (O'Brien, McDonald), else plainly."""
    by, case = {'white': [], 'black': [], 'api': []}, {k.upper().replace("'", ''): k for k in known}
    with open(use('us', 'us/census/Names_2010Census.csv'), encoding='utf-8') as f:
        for r in csv.DictReader(f):
            if r['rank'] == '0': continue
            n = case.get(r['name']) or (r['name'][0] + r['name'][1:].lower())
            if n.startswith('Mc') and len(n) > 3: n = 'Mc' + n[2].upper() + n[3:]
            for k, col in (('white', 'pctwhite'), ('black', 'pctblack'), ('api', 'pctapi')):
                p = float(r[col]) if r[col] not in ('(S)', '') else 0.0
                if p > 0: by[k].append((n, int(r['count']) * p / 100))
    return {k: [[n, round(c)] for n, c in sorted(v, key=lambda x: (-x[1], x[0]))[:800] if round(c) > 0] for k, v in by.items()}


os.makedirs(OUT, exist_ok=True)
for cc, fn in (('jp', jp), ('kr', kr), ('cn', cn), ('in', india), ('ng', ng)):
    data = fn()
    body = {'schema': 'suborned.names/1', 'country': cc, 'decades': DECADES, 'files': files[cc], **data}
    open(os.path.join(OUT, f'{cc}.json'), 'w', encoding='utf-8').write(json.dumps(body, ensure_ascii=False, separators=(',', ':')) + '\n')
    print(cc, 'given f/m', len(data['given']['1970']['f']), len(data['given']['1970']['m']), 'family', len(data['family']['any']), 'top', [x[0] for x in data['family']['any'][:5]])
# the US file gains the census's shares beside `any` (written by extract.py; this adds keys, it changes none)
usf = os.path.join(OUT, 'us.json'); u = json.load(open(usf, encoding='utf-8'))
u['family'].update(us_shares([x[0] for x in u['family']['any']])); u['files'].update(files.get('us', {}))
open(usf, 'w', encoding='utf-8').write(json.dumps(u, ensure_ascii=False, separators=(',', ':')) + '\n')
print('us shares', {k: (len(v), [x[0] for x in v[:6]]) for k, v in u['family'].items()})

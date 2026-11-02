# The city's name lists (brief 089, Pulse): real given- and family-name frequency tables by country, cut to the decades of birth the
# people generator draws from, written as data to contracts/people/names/<country>.json. The downloads are untrusted data: they sit in a
# folder of their own (out/scratch/pulse/names-dl/<country>/, fetched by hand from the URLs in contracts/people/names/sources.json) and are
# only read here, as tables. No model reads or picks a name.
# Run (xlrd and openpyxl in a venv outside the downloads):  <venv>/bin/python -I tools/people/names/extract.py <downloads dir> <out dir>
# Same files, same bytes. Each list keeps its order (the source's rank, most common first) and its count; the top 3 stay in, marked by
# their place, because the generator skips them by rule (people.rules.json names.lists.skipTop), not this script.
import csv, gzip, hashlib, io, json, os, re, sys, unicodedata, zipfile

SRC, OUT = sys.argv[1], sys.argv[2]
DECADES = [1940, 1950, 1960, 1970, 1980, 1990, 2000]  # born 1940-2009: people 17 to 86 against 2026 (rules names.lists.ageReference)
GIVEN_N, FAMILY_N = 150, 800  # kept per decade and sex, and per family list: enough for a district, small enough to read through
NAME = re.compile(r"^[^\W\d_](?:[^\W\d_]|['\-]){1,15}$")  # one word of letters (an apostrophe or hyphen inside), as the rules' pattern


def sha(path):
    h = hashlib.sha256()
    with open(path, 'rb') as f:
        for b in iter(lambda: f.read(1 << 20), b''): h.update(b)
    return h.hexdigest()


O_NAMES = {'BRIEN', 'CONNOR', 'CONNELL', 'DONNELL', 'NEIL', 'NEILL', 'REILLY', 'HARA', 'KEEFE', 'LEARY', 'MALLEY', 'SULLIVAN', 'ROURKE', 'DWYER', 'TOOLE', 'GRADY', 'SHEA', 'BRYANT', 'BANNON', 'HALLORAN', 'HAGAN', 'CALLAGHAN', 'MEARA', 'GORMAN', 'DOHERTY', 'FARRELL', 'BOYLE', 'NEAL', 'KELLY', 'CONNELLY'}


def cased(s, us=False):
    """An upper-case table name in its usual case: JEAN-PIERRE -> Jean-Pierre, WIŚNIEWSKI -> Wiśniewski, MCDONALD -> McDonald (US census)."""
    s = s.strip()
    if not s: return s
    if s != s.upper(): return s  # already in case (Norway, Sweden, the US given names)
    out = re.sub(r"[^\W\d_]+", lambda m: m.group(0)[0] + m.group(0)[1:].lower(), s)
    if us:
        if s.startswith('MC') and len(s) > 3: out = 'Mc' + s[2].upper() + s[3:].lower()
        if s.startswith('O') and s[1:] in O_NAMES: out = "O'" + s[1] + s[2:].lower()
    return out


def keep(rows, n):
    """[(name, count)] most common first, one entry per name (the first, highest), names that are one word, at most n."""
    seen, out = set(), []
    for name, count in rows:
        if not NAME.match(name) or name in seen or count <= 0: continue
        seen.add(name); out.append([unicodedata.normalize('NFC', name), int(round(count))])
        if len(out) >= n: break
    return out


def by_decade(table):
    """{(decade, sex): {name: count}} -> {decade: {f: [...], m: [...]}}"""
    return {str(d): {s: keep(sorted(table.get((d, s), {}).items(), key=lambda x: (-x[1], x[0])), GIVEN_N) for s in ('f', 'm')} for d in DECADES}


def add(table, year, sex, name, count):
    d = (year // 10) * 10
    if d in DECADES: table.setdefault((d, sex), {}); table[(d, sex)][name] = table[(d, sex)].get(name, 0) + count


files = {}


def use(country, rel):
    p = os.path.join(SRC, country, rel); files.setdefault(country, {})[rel] = sha(p); return p


def jsonstat(path, dim):
    d = json.load(open(path, encoding='utf-8'))
    cat = d['dimension'][dim]['category']
    codes = sorted(cat['index'], key=lambda k: cat['index'][k])
    return d, codes, cat['label']


def us():
    t = {}
    with open(use('us', 'hadley-baby-names.csv'), encoding='utf-8') as f:
        for r in csv.DictReader(f): add(t, int(r['year']), 'm' if r['sex'] == 'boy' else 'f', r['name'], float(r['percent']) * 1e6)
    fam = []
    with open(use('us', 'census/Names_2010Census.csv'), encoding='utf-8') as f:
        for r in csv.DictReader(f):
            if r['rank'] != '0': fam.append((cased(r['name'], us=True), int(r['count'])))
    return {'given': by_decade(t), 'family': {'any': keep(fam, FAMILY_N)}}


def fr():
    t = {}
    with open(use('fr', 'x/prenoms-2025-nat.csv'), encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter=';'):
            if r['prenom'].startswith('_') or not r['periode'].isdigit(): continue
            add(t, int(r['periode']), 'm' if r['sexe'] == '1' else 'f', cased(r['prenom']), int(r['valeur']))
    fam = []
    with open(use('fr', 'x/noms2008nat_txt.txt'), encoding='latin-1') as f:
        rd = csv.reader(f, delimiter='\t'); head = next(rd)
        cols = [i for i, h in enumerate(head) if h in ('_1941_1950', '_1951_1960', '_1961_1970', '_1971_1980', '_1981_1990', '_1991_2000')]
        for r in rd:
            if r[0].startswith('AUTRES'): continue
            fam.append((cased(r[0]), sum(int(r[i]) for i in cols)))
    fam.sort(key=lambda x: (-x[1], x[0]))
    return {'given': by_decade(t), 'family': {'any': keep(fam, FAMILY_N)}}


def es():
    import xlrd
    b = xlrd.open_workbook(use('es', 'nombres_por_fecha.xls'))
    t = {}
    for sheet, sex in (('ESPAÑA_hombres', 'm'), ('ESPAÑA_mujeres', 'f')):
        s = b.sheet_by_name(sheet)
        for c in range(s.ncols):
            m = re.search(r'(\d{4}) A (\d{4})', str(s.cell_value(3, c)))
            if not m: continue
            d = int(m.group(1))
            if d not in DECADES: continue
            for r in range(5, s.nrows):
                n, k = s.cell_value(r, c), s.cell_value(r, c + 1)
                if n and isinstance(k, float): t.setdefault((d, sex), {})[cased(n)] = k
    a = xlrd.open_workbook(use('es', 'apellidos_frecuencia.xls')).sheet_by_index(0)
    fam = [(cased(a.cell_value(r, 1)), a.cell_value(r, 2)) for r in range(5, a.nrows) if isinstance(a.cell_value(r, 2), float)]
    return {'given': by_decade(t), 'family': {'any': keep(fam, FAMILY_N)}}


def no():
    d, codes, label = jsonstat(use('no', 'ssb-10467-born-by-name-1940-2008.json'), 'Fornavn')
    years = [int(y) for y in sorted(d['dimension']['Tid']['category']['index'], key=lambda k: d['dimension']['Tid']['category']['index'][k])]
    t = {}
    for i, code in enumerate(codes):  # 1 = girls' names, 2 = boys' names
        for j, y in enumerate(years):
            v = d['value'][i * len(years) + j]
            if v: add(t, y, 'f' if code[0] == '1' else 'm', label[code], v)
    d2, c2, l2 = jsonstat(use('no', 'ssb-12891-last-names-2025.json'), 'Etternavn')
    fam = sorted(((l2[c], v) for c, v in zip(c2, d2['value']) if v), key=lambda x: (-x[1], x[0]))
    return {'given': by_decade(t), 'family': {'any': keep(fam, FAMILY_N)}}


def se():
    d, codes, label = jsonstat(use('se', 'scb-BE0001T100-first-by-birthyear.json'), 'Tilltalsnamn')
    tid = d['dimension']['Tid']['category']; years = [int(y) for y in sorted(tid['index'], key=lambda k: tid['index'][k])]
    t = {}
    for i, code in enumerate(codes):  # 1 = men, 2 = women
        for j, y in enumerate(years):
            v = d['value'][i * len(years) + j]
            if v: add(t, y, 'm' if code[0] == '1' else 'f', label[code], v)
    d2, c2, l2 = jsonstat(use('se', 'scb-BE0001ENamn10-last-2020.json'), 'Efternamn')
    fam = sorted(((l2[c], v) for c, v in zip(c2, d2['value']) if v), key=lambda x: (-x[1], x[0]))
    return {'given': by_decade(t), 'family': {'any': keep(fam, FAMILY_N)}}


def pl():
    import openpyxl
    def rows(rel, cols):
        ws = openpyxl.load_workbook(use('pl', rel), read_only=True).worksheets[0]
        return [(cased(r[cols[0]]), r[cols[1]]) for r in ws.iter_rows(min_row=2, values_only=True) if r[cols[0]] and isinstance(r[cols[1]], (int, float))]
    living = {'m': rows('imiona-meskie-pesel-2023-01-31.xlsx', (0, 2)), 'f': rows('imiona-zenskie-pesel-2023-01-31.xlsx', (0, 2))}
    kids = {'m': {}, 'f': {}}
    with open(use('pl', 'imiona-dzieci-2000-2019.csv'), encoding='utf-8') as f:
        for r in csv.DictReader(f):
            if 2000 <= int(r['Rok']) <= 2009: s = 'm' if r['Płeć'] == 'M' else 'f'; n = cased(r['Imię']); kids[s][n] = kids[s].get(n, 0) + int(r['Liczba'])
    # the living register has no year of birth: every decade before 2000 draws from it (said in sources.json); the 2000s from the names given in 2000-2009
    given = {str(d): {s: keep(sorted(kids[s].items(), key=lambda x: (-x[1], x[0])) if d == 2000 else living[s], GIVEN_N) for s in ('f', 'm')} for d in DECADES}
    fam = {'m': keep(rows('nazwiska-meskie-2026-01-20.xlsx', (0, 1)), FAMILY_N), 'f': keep(rows('nazwiska-zenskie-2026-01-20.xlsx', (0, 1)), FAMILY_N)}
    return {'given': given, 'family': fam}


os.makedirs(OUT, exist_ok=True)
for cc, fn in (('us', us), ('fr', fr), ('es', es), ('no', no), ('se', se), ('pl', pl)):
    data = fn()
    body = {'schema': 'suborned.names/1', 'country': cc, 'decades': DECADES, 'files': files[cc], **data}
    with open(os.path.join(OUT, f'{cc}.json'), 'w', encoding='utf-8') as f:
        f.write(json.dumps(body, ensure_ascii=False, separators=(',', ':')) + '\n')
    g = sum(len(v[s]) for v in data['given'].values() for s in v); fm = sum(len(v) for v in data['family'].values())
    print(f'{cc}: given {g} entries over {len(DECADES)} decades, family {fm}')

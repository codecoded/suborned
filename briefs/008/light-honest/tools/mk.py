import json, sys, copy
BASE='/Users/sky/Projects/suborned/out/glint-honest/cand-00-current.json'
def make(name, **kw):
    """kw keys: dotted paths into light.json (sun.intensity), or pal_<key>=hex for palette entries. Returns path."""
    d = json.load(open(BASE))
    for k, v in kw.items():
        if k.startswith('pal_'):
            d['palette']['light.' + k[4:]]['hex'] = v
        else:
            o = d; parts = k.split('__')
            for p in parts[:-1]: o = o[p]
            o[parts[-1]] = v
    path = f'/Users/sky/Projects/suborned/out/glint-honest/cands/{name}.json'
    json.dump(d, open(path, 'w'), indent=1)
    return path

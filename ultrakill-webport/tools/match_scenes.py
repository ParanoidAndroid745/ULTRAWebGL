# -*- coding: utf-8 -*-
"""
Empareja escenas ORIGINALES del bundle con los .unity exportados (permytados).
Compara por TOKENS (palabras) porque en el YAML el valor va pegado al campo.
Cachea los conjuntos de tokens para poder iterar sin recomprimir.
"""
import os, re, sys, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from unityfs import Bundle

TOKEN = re.compile(rb'[A-Za-z][A-Za-z0-9\-\'\.]{3,}')
STOP = set('''the and for with from that this into level name data assets scenes unity
gameobject monobehaviour transform assetscene prefab script guid fileid mref
http https www com html json xml txt png tga wav mp3 false true null none null
build player shared resources resource bundle bundlemanifest assetbundle
'''.split())


def tokens(data):
    out = set()
    for m in TOKEN.finditer(data):
        t = m.group().decode('ascii')
        if t.lower() in STOP:
            continue
        out.add(t)
    return out


def cached(path, key, fn):
    cache = path + '.tokens.json'
    data = {}
    if os.path.exists(cache):
        data = json.load(open(cache))
    if key in data:
        return set(data[key])
    v = fn()
    data[key] = sorted(v)
    json.dump(data, open(cache, 'w'))
    return v


def main():
    bundle_path, scenes_dir = sys.argv[1], sys.argv[2]
    cache = os.path.join(scenes_dir, '_tokens_cache.json')
    b = Bundle(bundle_path)
    scene_nodes = [n for n in b.nodes
                   if n['path'].startswith('BuildPlayer-')
                   and not n['path'].endswith('.sharedAssets')
                   and not n['path'].endswith('.resource')]
    orig = {}
    for n in scene_nodes:
        name = n['path'][len('BuildPlayer-'):]
        key = 'B:' + name
        orig[name] = cached(cache, key, lambda n=n: tokens(b.read_node(n)))
        print('%-14s %6d tokens' % (name, len(orig[name])), flush=True)

    exported = {}
    for f in sorted(os.listdir(scenes_dir)):
        if f.endswith('.unity'):
            key = 'E:' + f[:-6]
            exported[f[:-6]] = cached(cache, key,
                                     lambda f=f: tokens(open(os.path.join(scenes_dir, f), 'rb').read()))
    print('\n=== EMPAREJAMIENTO (tokens compartidos) ===')
    res = {}
    for oname, ostr in orig.items():
        scores = sorted(((len(ostr & estr), ename) for ename, estr in exported.items()), reverse=True)
        (i1, n1), (i2, n2) = scores[0], scores[1]
        res[oname] = {'exportado': n1, 'tokens': i1, 'segundo': n2, 'tokens2': i2}
        print('%-14s -> %-16s tokens=%-5d   (2o: %-16s %d)' % (oname, n1, i1, n2, i2))
    json.dump(res, open(os.path.join(scenes_dir, '_scene_map.json'), 'w'), indent=1, ensure_ascii=False)


if __name__ == '__main__':
    main()

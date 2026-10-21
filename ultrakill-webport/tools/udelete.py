# -*- coding: utf-8 -*-
"""
Borra GameObjects (y su subarbol) del YAML de Unity de forma quirurgica.

  python udelete.py <fichero.yaml> <nombreGO> [<nombreGO> ...] [--dry]

- Elimina los bloques (GameObject, RectTransform, CanvasRenderer, MonoBehaviour,
  Text, Image...) cuyo fileID pertenece al subarbol borrado.
- Quita de las listas m_Children los RectTransform borrados.
- Avisa de CUALQUIER referencia que quede colgando (para poder arreglarla).
"""
import re, sys, io, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from uyaml import split_blocks, hierarchy, children_of, field

BLOCK_RX = re.compile(r'^--- !u!(\d+) &(\d+)( stripped)?\s*$')


def subtree(h, go):
    """GameObject ids del subarbol (incluido go)."""
    out = []
    stack = [go]
    while stack:
        cur = stack.pop()
        if cur in out:
            continue
        out.append(cur)
        stack.extend(children_of(h, cur))
    return out


def main():
    path = sys.argv[1]
    names = sys.argv[2:]
    dry = '--dry' in names
    names = [n for n in names if n != '--dry']

    text = io.open(path, 'r', encoding='utf-8', errors='replace', newline='').read()
    lines = text.split('\n')
    blocks = split_blocks(text)
    # preamble
    first = blocks[0]['start']
    preamble = lines[:first]

    h = hierarchy(path)
    names_map = h['go_name']
    byid = h['by_id']

    # 1) identificar GameObjects a borrar
    from uyaml import path_of
    targets = []
    for n in names:
        if '/' in n:
            found = [go for go, nm in names_map.items() if path_of(h, go).endswith(n)]
        else:
            found = [go for go, nm in names_map.items() if nm == n]
        if not found:
            print('AVISO: no existe el GameObject "%s" en %s' % (n, os.path.basename(path)))
        targets.extend(found)
    if not targets:
        return

    removed_go = set(targets)
    removed_ids = set()
    for go in targets:
        for x in subtree(h, go):
            removed_ids.add(x)
            # y todos los fileID de los componentes de esos GameObjects
            for ln in byid[x]['lines']:
                m = re.match(r'^\s*- component: \{fileID: (\d+)\}', ln)
                if m:
                    removed_ids.add(int(m.group(1)))
    # RectTransforms de los GO borrados
    removed_rt = set()
    for go in removed_go:
        rt = h['rt_of_go'].get(go)
        if rt:
            removed_rt.add(rt)

    print('== %s ==' % os.path.basename(path))
    print('GameObjects a borrar: %s' % ', '.join('%d(%s)' % (g, names_map.get(g)) for g in targets))
    print('fileIDs afectados: %d' % len(removed_ids))

    if dry:
        print('DRY RUN: no se escribe')
        return

    # 2) reconstruir sin los bloques borrados
    newblocks = [b for b in blocks if b['id'] not in removed_ids]
    out = list(preamble)
    for b in newblocks:
        out.extend(b['lines'])
    newtext = '\n'.join(out)

    # 3) quitar m_Children colgantes
    lines2 = newtext.split('\n')
    for i, ln in enumerate(lines2):
        m = re.match(r'^(\s*)- \{fileID: (\d+)\}$', ln)
        if m and int(m.group(2)) in removed_rt:
            lines2[i] = None
    newtext = '\n'.join([x for x in lines2 if x is not None])

    # 4) referencias colgantes
    left = {}
    for b in split_blocks(newtext):
        for ln in b['lines']:
            for m in re.finditer(r'\{fileID: (\d+)\}', ln):
                fid = int(m.group(1))
                if fid in removed_ids and fid not in removed_rt:
                    left.setdefault(fid, []).append((b['class'], b['id'], ln.strip()))
    if left:
        print('!! REFERENCIAS COLGANTES:')
        for fid, lst in sorted(left.items()):
            print('  fileID %d:' % fid)
            for cls, bid, ln in lst[:6]:
                print('     clase %d bloque %d : %s' % (cls, bid, ln[:120]))
    else:
        print('sin referencias colgantes')

    io.open(path, 'w', encoding='utf-8', newline='').write(newtext)
    print('escrito (%d bytes, antes %d)' % (len(newtext.encode('utf-8')), len(text.encode('utf-8'))))


if __name__ == '__main__':
    main()

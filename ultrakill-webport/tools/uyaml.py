# -*- coding: utf-8 -*-
"""
Utilidades para editar el YAML de Unity (prefab / escena) de forma quirurgica:
 - lista la jerarquia (GameObject -> padre/hijo) a partir de los RectTransform
 - desactiva GameObjects por nombre (m_IsActive: 0)
 - desactiva el GameObject que contiene un Text con un texto dado
 - lista los textos (m_Text) de un archivo
"""
import re, sys, io

BLOCK = re.compile(r'^--- !u!(\d+) &(\d+)( stripped)?\s*$')


def split_blocks(text):
    """Devuelve [(classid, fileid, line_start, lines)] en orden."""
    lines = text.split('\n')
    out = []
    cur = None
    for i, ln in enumerate(lines):
        m = BLOCK.match(ln)
        if m:
            if cur:
                out.append(cur)
            cur = {'class': int(m.group(1)), 'id': int(m.group(2)), 'start': i, 'lines': [ln]}
        elif cur is not None:
            cur['lines'].append(ln)
    if cur:
        out.append(cur)
    return out


def field(block, name):
    """Primer valor escalar del campo 'name' en el bloque."""
    pat = re.compile(r'^\s*' + re.escape(name) + r':\s*(.*)$')
    for ln in block['lines']:
        m = pat.match(ln)
        if m:
            return m.group(1).strip()
    return None


def comp_ids(block):
    out = []
    for ln in block['lines']:
        m = re.match(r'^\s*- component: \{fileID: (\d+)\}', ln)
        if m:
            out.append(int(m.group(1)))
    return out


def child_ids(block):
    out = []
    for ln in block['lines']:
        m = re.match(r'^\s*- \{fileID: (\d+)\}', ln)
        if m:
            out.append(int(m.group(1)))
    return out


def father_id(block):
    m = re.search(r'^\s*m_Father: \{fileID: (\d+)\}', '\n'.join(block['lines']), re.M)
    return int(m.group(1)) if m else 0


def gameobject_id(block):
    m = re.search(r'^\s*m_GameObject: \{fileID: (\d+)\}', '\n'.join(block['lines']), re.M)
    return int(m.group(1)) if m else 0


def load(path):
    with io.open(path, 'r', encoding='utf-8', errors='replace') as f:
        return f.read()


def hierarchy(path):
    text = load(path)
    blocks = split_blocks(text)
    by_id = {b['id']: b for b in blocks}
    # GameObject -> name
    go_name = {}
    go_active = {}
    for b in blocks:
        if b['class'] == 1:
            n = field(b, 'm_Name')
            go_name[b['id']] = n
            go_active[b['id']] = field(b, 'm_IsActive')
    # RectTransform -> (go, father, children)
    rt_go = {}
    rt_father = {}
    rt_children = {}
    for b in blocks:
        if b['class'] == 224:
            go = gameobject_id(b)
            rt_go[b['id']] = go
            rt_father[b['id']] = father_id(b)
            rt_children[b['id']] = child_ids(b)
    # hijo RectTransform -> GameObject
    rt_of_go = {}
    for rt, go in rt_go.items():
        if go:
            rt_of_go[go] = rt
    # padre de un GameObject
    parent_of_go = {}
    for rt, go in rt_go.items():
        f = rt_father.get(rt, 0)
        if f in rt_go:
            parent_of_go[go] = rt_go[f]
    return dict(blocks=blocks, by_id=by_id, go_name=go_name, go_active=go_active,
                rt_of_go=rt_of_go, parent_of_go=parent_of_go, rt_children=rt_children,
                rt_go=rt_go, rt_father=rt_father, text=text)


def path_of(h, go):
    """Ruta de nombres desde la raiz hasta el GameObject."""
    names = []
    cur = go
    seen = set()
    while cur and cur not in seen:
        seen.add(cur)
        names.append(h['go_name'].get(cur, '?%d' % cur))
        cur = h['parent_of_go'].get(cur, 0)
    return '/'.join(reversed(names))


def children_of(h, go):
    rt = h['rt_of_go'].get(go)
    if not rt:
        return []
    return [h['rt_go'].get(c, 0) for c in h['rt_children'].get(rt, []) if h['rt_go'].get(c)]


def texts(path):
    """[(fileid, texto)] de todos los componentes Text (clase 114 con m_Text)."""
    text = load(path)
    out = []
    for b in split_blocks(text):
        if b['class'] == 114:
            t = field(b, 'm_Text')
            if t is not None:
                out.append((b['id'], t))
    return out


def set_active(text, go_ids_active_off):
    """Pone m_IsActive: 0 en los GameObjects con esos fileID. Devuelve (texto, n)."""
    out = []
    for b in split_blocks(text):
        if b['class'] == 1 and b['id'] in go_ids_active_off:
            changed = False
            for i, ln in enumerate(b['lines']):
                if ln.strip().startswith('m_IsActive:'):
                    b['lines'][i] = '  m_IsActive: 0'
                    changed = True
            if changed:
                out.append((b['id'], b['lines']))
    # reconstruir el texto
    res = []
    res_txt = []
    # Volvemos a ensamblar por bloques
    lines = text.split('\n')
    idx = {}
    for b in split_blocks(text):
        idx[b['start']] = b
    newlines = []
    i = 0
    n = i
    blist = split_blocks(text)
    for b in blist:
        newlines.extend(b['lines'])
    return '\n'.join(newlines), len(out)


if __name__ == '__main__':
    cmd = sys.argv[1]
    path = sys.argv[2]
    if cmd == 'tree':
        h = hierarchy(path)
        target = sys.argv[3]
        for go, name in h['go_name'].items():
            if name == target:
                print('GameObject %d  "%s"  activo=%s' % (go, name, h['go_active'].get(go)))
                print('  ruta: %s' % path_of(h, go))
                print('  padre: %d (%s)' % (h['parent_of_go'].get(go, 0),
                                            h['go_name'].get(h['parent_of_go'].get(go, 0), '?')))
                print('  hijos:')
                for c in children_of(h, go):
                    print('    - %d "%s" activo=%s' % (c, h['go_name'].get(c), h['go_active'].get(c)))
    elif cmd == 'texts':
        for fid, t in texts(path):
            print('%d  %s' % (fid, t))

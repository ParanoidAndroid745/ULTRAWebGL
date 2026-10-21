# -*- coding: utf-8 -*-
"""Comprueba que el bundle act-1 construido tiene EXACTAMENTE los mismos
nombres de escena que el bundle original del juego, y que el contenido de
cada escena es el mismo (títulos, nextAct, targetLevelName).

Uso: python verify_bundle.py <bundle_construido> <bundle_original>
"""
import os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from unityfs import Bundle

TITLES = {
    'Level 1-1': 'HEART OF THE SUNRISE',
    'Level 1-2': 'THE BURNING WORLD',
    'Level 1-3': 'HALLS OF SACRED REMAINS',
    'Level 1-4': 'CLAIR DE LUNE',
    'Level 2-1': 'BRIDGEBURNER',
    'Level 2-2': 'DEATH AT 20,000 VOLTS',
    'Level 2-3': 'SHEER HEART ATTACK',
    'Level 2-4': 'COURT OF THE CORPSE KING',
    'Level 3-1': 'BELLY OF THE BEAST',
    'Level 3-2': 'IN THE FLESH',
    'Level P-1': 'SOUL SURVIVOR',
    'Level 0-1': 'INTO THE FIRE',
    'Level 0-2': 'THE MEATGRINDER',
    'Level 0-3': 'DOUBLE DOWN',
    'Level 0-4': 'A ONE-MACHINE ARMY',
    'Level 0-5': 'CERBERUS',
    'Level 0-S': 'TESTAMENT I',
    'Level 1-S': 'TESTAMENT I',
}


def scenes(path):
    b = Bundle(path)
    out = {}
    for n in b.nodes:
        p = n['path']
        if not p.startswith('BuildPlayer-'):
            continue
        if p.endswith('.sharedAssets') or p.endswith('.resource'):
            continue
        name = p[len('BuildPlayer-'):]
        data = b.read_node(n)
        out[name] = data
    return out


def sig(data):
    """(nombre del nivel, act, target) leidos de la region del StockMapInfo.

    En el binario serializado de Unity no hay nombres de campo, pero los
    VALORES si: la region que sigue a "LAYER /// NUMBER" contiene el
    levelName, el nextAct y el targetLevelName, en ese orden.
    """
    txt = data.decode('latin-1')
    m = re.search(r'LAYER /// NUMBER(.{0,260})', txt, re.S)
    reg = m.group(1) if m else ''
    strs = re.findall(r'[\x20-\x7e]{4,}', reg)
    # fuera las rutas de assets (vienen de los shared assets del bundle)
    strs = [s for s in strs if 'archive:/' not in s and 'Assets/Scenes' not in s
            and '.unity' not in s and '.sharedAssets' not in s and '.resource' not in s]
    level = ''
    act = ''
    target = ''
    for s in strs:
        if not level and s not in ('LEVEL NAME',) and not s.startswith('act-'):
            level = s
        elif s.startswith('act-'):
            act = s
        elif not target and (s.startswith('Level ') or s.startswith('Intermission')):
            target = s
    return level, act, target


def main():
    built, orig = sys.argv[1], sys.argv[2]
    bs = scenes(built)
    os_ = scenes(orig)
    print('bundle construido : %d escenas' % len(bs))
    print('bundle original   : %d escenas' % len(os_))
    print()
    print('%-16s %-22s %-22s %s' % ('escena', 'construido', 'original', 'ok'))
    allok = True
    for name in sorted(set(bs) | set(os_)):
        b = bs.get(name)
        o = os_.get(name)
        sb = sig(b) if b else None
        so = sig(o) if o else None
        ok = (b is not None and o is not None and sb == so)
        if not ok:
            allok = False
        print('%-16s %-22s %-22s %s' % (name, sb, so, 'OK' if ok else 'DIFIERE/FALTA'))
    print()
    print('RESULTADO:', 'IDENTICO AL ORIGINAL' if allok else 'HAY DIFERENCIAS')
    return 0 if allok else 1


if __name__ == '__main__':
    sys.exit(main())

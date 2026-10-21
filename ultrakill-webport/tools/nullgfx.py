# -*- coding: utf-8 -*-
"""Pone a null las 4 referencias del componente GraphicsOptions (pixelization,
textureWarping, vertexWarping, colorCompression) tras borrar sus GameObjects."""
import re, sys, io, os

FIELDS = ['pixelization', 'textureWarping', 'vertexWarping', 'colorCompression']


def fix(path):
    txt = io.open(path, 'r', encoding='utf-8', errors='replace', newline='').read()
    count = [0]

    def mk(f):
        def rep(m):
            if m.group(2) != '0':
                count[0] += 1
            return '%s%s: {fileID: 0}' % (m.group(1), f)
        return rep

    for f in FIELDS:
        pat = re.compile(r'^(\s*)' + f + r': \{fileID: (\d+)\}\s*$', re.M)
        txt = pat.sub(mk(f), txt)
    if count[0]:
        io.open(path, 'w', encoding='utf-8', newline='').write(txt)
    print('%s: %d referencias anuladas' % (os.path.basename(path), count[0]))


if __name__ == '__main__':
    for p in sys.argv[1:]:
        fix(p)

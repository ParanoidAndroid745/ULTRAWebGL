# -*- coding: utf-8 -*-
"""
Parser de bundles UnityFS (Unity 2019, LZ4HC) para extraer assets originales.

Uso:
  python unityfs.py <bundle> list
  python unityfs.py <bundle> grep <node-substr> <needle> [...]   # busca cadenas en los nodos
  python unityfs.py <bundle> extract <node-substr> <outdir>
"""
import struct, sys, os

# ---------------------------------------------------------------- LZ4 block
def lz4_decompress(src, uncompressed_size):
    """Decodificador LZ4 block (formato LZ4 / LZ4HC)."""
    dst = bytearray(uncompressed_size)
    sp = 0
    dp = 0
    n = len(src)
    while sp < n:
        token = src[sp]; sp += 1
        lit = token >> 4
        if lit == 15:
            while True:
                b = src[sp]; sp += 1
                lit += b
                if b != 255: break
        dst[dp:dp+lit] = src[sp:sp+lit]
        sp += lit; dp += lit
        if sp >= n: break
        offset = src[sp] | (src[sp+1] << 8); sp += 2
        match = token & 0x0F
        if match == 15:
            while True:
                b = src[sp]; sp += 1
                match += b
                if b != 255: break
        match += 4
        m = dp - offset
        if offset >= 8:
            # copia rapida por bloques de 8
            for i in range(match):
                dst[dp+i] = dst[m+i]
        else:
            for i in range(match):
                dst[dp+i] = dst[m+i]
        dp += match
    return bytes(dst)


def lzma_decompress(src, uncompressed_size):
    import lzma
    # Unity LZMA: 5 bytes de props + datos (formato "alone" con tamaño desconocido)
    props = src[:5]
    lc = props[0] % 9
    lp = (props[0] // 9) % 5
    pb = props[0] // 45
    dict_size = struct.unpack('<I', props[1:5])[0]
    filt = [{"id": lzma.FILTER_LZMA1, "lc": lc, "lp": lp, "pb": pb, "dict_size": dict_size}]
    d = lzma.LZMADecompressor(format=lzma.FORMAT_RAW, filters=filt)
    out = d.decompress(src[5:], uncompressed_size)
    return out


# ---------------------------------------------------------------- bundle
class Bundle:
    def __init__(self, path):
        self.path = path
        self.f = open(path, 'rb')
        self._read_header()
        self._read_dir()

    def _read_header(self):
        f = self.f
        magic = f.read(8)
        assert magic[:7] == b'UnityFS', magic
        self.version = struct.unpack('>i', f.read(4))[0]
        # En estos bundles (Unity 2019, version 7) las dos cadenas van terminadas
        # en NUL, no con prefijo de longitud.
        self.uw = self._rstr(); self.minrev = self._rstr()
        self.size = struct.unpack('>q', f.read(8))[0]
        self.c_blocks_info = struct.unpack('>I', f.read(4))[0]
        self.u_blocks_info = struct.unpack('>I', f.read(4))[0]
        self.flags = struct.unpack('>I', f.read(4))[0]
        self.compression = self.flags & 0x3F
        self.blocks_info_at_end = bool(self.flags & 0x80)
        self.need_padding = (self.flags & 0x3F) == 0x40

    def _rstr(self):
        out = bytearray()
        while True:
            b = self.f.read(1)
            if not b or b == b'\x00':
                break
            out += b
        return out.decode('utf8', 'replace')

    def _read_dir(self):
        f = self.f
        if self.blocks_info_at_end:
            f.seek(0, 2)
            self.dir_offset = f.tell() - self.c_blocks_info
        else:
            # El directorio va alineado a 16 bytes (flags 0x40 = OldWebPluginCompatibility)
            f.seek((f.tell() + 15) & ~15)
            self.dir_offset = f.tell()
        f.seek(self.dir_offset)
        raw = f.read(self.c_blocks_info)
        if self.compression in (0x12, 0x02, 0x03, 0x13, 0x11):
            data = lz4_decompress(raw, self.u_blocks_info)
        elif self.compression in (0x21, 0x01, 0x23):
            data = lzma_decompress(raw, self.u_blocks_info)
        else:
            raise SystemExit('compression no soportada: %r' % self.compression)
        p = 0
        p += 16  # uncompressed data hash
        block_count = struct.unpack('>i', data[p:p+4])[0]; p += 4
        self.blocks = []
        for _ in range(block_count):
            usize, csize, bflags = struct.unpack('>IIH', data[p:p+10]); p += 10
            self.blocks.append((usize, csize, bflags))
        self.blocks_info_count = struct.unpack('>i', data[p:p+4])[0]; p += 4
        self.nodes = []
        for _ in range(self.blocks_info_count):
            # UnityFS v7: uint64 offset | uint64 size | uint32 flags | path terminado en NUL
            offset, size, nflags = struct.unpack('>qqI', data[p:p+20]); p += 20
            end = data.index(b'\x00', p)
            path = data[p:end].decode('utf8', 'replace')
            p = end + 1
            self.nodes.append({'path': path, 'offset': offset, 'size': size, 'flags': nflags})
        # tabla de bloques -> rango de offsets descomprimidos
        self.block_offsets = []
        acc = 0
        for usize, csize, bflags in self.blocks:
            self.block_offsets.append((acc, acc + usize))
            acc += usize
        self.data_offset = self.dir_offset + self.c_blocks_info

    # --- descompresion
    def _decompress_block(self, idx):
        usize, csize, bflags = self.blocks[idx]
        self.f.seek(self.data_offset + sum(c for _, c, _ in self.blocks[:idx]))
        raw = self.f.read(csize)
        comp = bflags & 0x3F
        if comp == 0:
            return raw
        if comp in (0x12, 0x02, 0x03, 0x13, 0x11):
            return lz4_decompress(raw, usize)
        if comp in (0x21, 0x01, 0x23):
            return lzma_decompress(raw, usize)
        raise SystemExit('comp de bloque no soportada %r' % comp)

    def read_node(self, node):
        """Devuelve los bytes del nodo (descomprimiendo solo los bloques necesarios)."""
        out = bytearray()
        start, end = node['offset'], node['offset'] + node['size']
        for i, (b0, b1) in enumerate(self.block_offsets):
            if b1 <= start or b0 >= end:
                continue
            data = self._decompress_block(i)
            s = max(start - b0, 0)
            e = min(end - b0, len(data))
            out += data[s:e]
        return bytes(out)


def main():
    cmd = sys.argv[1]
    if cmd == 'list':
        b = Bundle(sys.argv[2])
        print('version=%d unity=%s rev=%s flags=%#x comp=%#x blocks=%d nodes=%d' %
              (b.version, b.uw, b.minrev, b.flags, b.compression, len(b.blocks), len(b.nodes)))
        for n in b.nodes:
            print('%10d  %s' % (n['size'], n['path']))
    elif cmd == 'grep':
        b = Bundle(sys.argv[2])
        sub = sys.argv[3]
        needles = [x.encode('utf8') for x in sys.argv[4:]]
        for n in b.nodes:
            if sub.lower() not in n['path'].lower():
                continue
            data = b.read_node(n)
            print('=== %s (%d bytes) ===' % (n['path'], len(data)))
            for nd in needles:
                idx = 0
                hits = []
                while True:
                    k = data.find(nd, idx)
                    if k < 0: break
                    hits.append(k); idx = k + 1
                    if len(hits) > 20: break
                print('   %-22s -> %d hits %s' % (nd.decode(), len(hits), hits[:10]))
                for h in hits[:6]:
                    lo = max(0, h - 60); hi = min(len(data), h + 80)
                    seg = data[lo:hi]
                    txt = ''.join(chr(c) if 32 <= c < 127 else '.' for c in seg)
                    print('        ...%s...' % txt)
    elif cmd == 'extract':
        b = Bundle(sys.argv[2])
        sub = sys.argv[3]
        outdir = sys.argv[4]
        os.makedirs(outdir, exist_ok=True)
        for n in b.nodes:
            if sub.lower() in n['path'].lower():
                data = b.read_node(n)
                out = os.path.join(outdir, os.path.basename(n['path']).replace('/', '_'))
                open(out, 'wb').write(data)
                print('escrito %s (%d bytes)' % (out, len(data)))
    else:
        raise SystemExit('comando desconocido')


if __name__ == '__main__':
    main()

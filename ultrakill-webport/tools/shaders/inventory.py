import os, re, sys, json, collections
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\export\ExportedProject\Assets"
OUT = r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\shader_inventory"
os.makedirs(OUT, exist_ok=True)

# ---- shaders ----
shaders = {}
for dirpath, _, files in os.walk(ROOT):
    for f in files:
        if not f.endswith(".shader"):
            continue
        p = os.path.join(dirpath, f)
        txt = open(p, encoding="utf-8", errors="replace").read()
        m = re.search(r'^\s*Shader\s+"([^"]+)"', txt, re.M)
        name = m.group(1) if m else "?"
        mp = p + ".meta"
        guid = None
        if os.path.exists(mp):
            g = re.search(r"guid:\s*([0-9a-f]{32})", open(mp, encoding="utf-8", errors="replace").read())
            guid = g.group(1) if g else None
        stub = ("DummyShaderTextExporter" in txt) or ("_ResY" not in txt)
        shaders[guid] = dict(path=os.path.relpath(p, ROOT), name=name, guid=guid,
                             stub=stub, size=len(txt), has_resY="_ResY" in txt)

# ---- materials ----
mat_use = collections.Counter()
mat_names = collections.defaultdict(list)
mats = 0
for dirpath, _, files in os.walk(ROOT):
    for f in files:
        if not f.endswith(".mat"):
            continue
        p = os.path.join(dirpath, f)
        txt = open(p, encoding="utf-8", errors="replace").read()
        m = re.search(r"m_Shader:\s*\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-f]{32})", txt)
        nm = re.search(r"^\s*m_Name:\s*(.+)$", txt, re.M)
        mats += 1
        if m:
            mat_use[m.group(2)] += 1
            mat_names[m.group(2)].append((nm.group(1).strip() if nm else "?", os.path.relpath(p, ROOT)))
        else:
            mat_use["SIN_SHADER"] += 1
            mat_names["SIN_SHADER"].append((nm.group(1).strip() if nm else "?", os.path.relpath(p, ROOT)))

stub_guids = [g for g, s in shaders.items() if s["stub"]]
print(f"shaders: {len(shaders)}  stubs: {len(stub_guids)}  materials: {mats}")
used = [(g, mat_use.get(g, 0)) for g in stub_guids if mat_use.get(g, 0)]
print(f"stubs usados por algun material: {len(used)}  materiales afectados: {sum(n for _, n in used)}")

rows = []
for g in stub_guids:
    s = shaders[g]
    rows.append(dict(name=s["name"], guid=g, path=s["path"], size=s["size"],
                     mats=mat_use.get(g, 0), matlist=mat_names.get(g, [])[:8]))
rows.sort(key=lambda r: (-r["mats"], r["name"]))

with open(os.path.join(OUT, "stubs.json"), "w", encoding="utf-8") as f:
    json.dump(rows, f, ensure_ascii=False, indent=1)

print("\n%-52s %4s %6s  %s" % ("shader", "mats", "bytes", "ruta"))
for r in rows:
    print("%-52s %4d %6d  %s" % (r["name"][:52], r["mats"], r["size"], r["path"]))

unused = [r for r in rows if r["mats"] == 0]
print(f"\nstubs sin materiales: {len(unused)}")

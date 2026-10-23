import os, re, sys, json, collections
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\export\ExportedProject\Assets"
INV = r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\shader_inventory"

rows = json.load(open(os.path.join(INV, "stubs.json"), encoding="utf-8"))
targets = [r for r in rows if r["mats"] > 0]
by_name = collections.defaultdict(list)
for r in targets:
    by_name[r["name"]].append(r)

# index materials by shader guid
mats_by_guid = collections.defaultdict(list)
for dirpath, _, files in os.walk(ROOT):
    for f in files:
        if not f.endswith(".mat"):
            continue
        p = os.path.join(dirpath, f)
        txt = open(p, encoding="utf-8", errors="replace").read()
        m = re.search(r"m_Shader:\s*\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-f]{32})", txt)
        if not m:
            continue
        nm = re.search(r"^\s*m_Name:\s*(.+)$", txt, re.M)
        props = re.findall(r"^\s{2}-?\s*(\w+):", txt[ txt.find("m_SavedProperties"): ] if "m_SavedProperties" in txt else "", re.M)
        # properties within m_TexEnvs / m_Floats / m_Colors
        tex = re.findall(r"^\s{4}(\w+):\s*$", txt, re.M)
        floats = re.findall(r"^\s{4}(\w+):\s*-?[\d.]+", txt, re.M)
        cols = re.findall(r"^\s{4}(\w+):\s*\{r:", txt, re.M)
        mats_by_guid[m.group(2)].append(dict(name=nm.group(1).strip() if nm else "?",
                                             path=os.path.relpath(p, ROOT),
                                             tex=sorted(set(tex)), floats=sorted(set(floats)), colors=sorted(set(cols))))

for name in sorted(by_name, key=lambda n: -sum(r["mats"] for r in by_name[n])):
    entries = by_name[name]
    total = sum(r["mats"] for r in entries)
    print("=" * 76)
    print(f"{name}  ({total} materiales en {len(entries)} ficheros)")
    seen = set()
    for e in entries:
        for mt in mats_by_guid.get(e["guid"], []):
            key = mt["name"]
            if key in seen:
                continue
            seen.add(key)
            print(f"   - {mt['name']:38} tex={mt['tex']} floats={mt['floats'][:6]} colors={mt['colors']}")

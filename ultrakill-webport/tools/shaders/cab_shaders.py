import UnityPy, sys, os, json
sys.stdout.reconfigure(encoding="utf-8")

BUNDLES = [
    r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\original-bundles\StreamingAssets\common",
    r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\original-bundles\StreamingAssets\acts\act-0",
    r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\original-bundles\StreamingAssets\acts\act-1",
    r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\original-bundles\StreamingAssets\acts\prime",
]

WANT = ("water", "railgun", "heart", "blood", "invert", "post", "emissive", "volumetric",
        "probuilder", "standard vertex", "unlit")

found = {}
for b in BUNDLES:
    env = UnityPy.load(b)
    n = 0
    for o in env.objects:
        if o.type.name != "Shader":
            continue
        d = o.read()
        try:
            name = d.m_ParsedForm.m_Name
        except Exception:
            name = None
        n += 1
        if name and name not in found:
            found[name] = (os.path.basename(b), o.path_id, d)
    print(os.path.basename(b), "shaders:", n)

print("\ntotal unique names:", len(found))
for name in sorted(found):
    if any(w in name.lower() for w in WANT):
        print("  ", name)

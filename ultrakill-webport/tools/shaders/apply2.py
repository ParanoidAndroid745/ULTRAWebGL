import os, re, sys
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\export\ExportedProject\Assets"
NEW = r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\tools\shaders\new"
SKIP = {"Hidden/ProBuilder/HideVertices"}  # ya arreglado a mano en la sesion anterior

new_by_name = {}
for f in os.listdir(NEW):
    if not f.endswith(".shader"):
        continue
    txt = open(os.path.join(NEW, f), encoding="utf-8").read()
    m = re.search(r'^\s*Shader\s+"([^"]+)"', txt, re.M)
    new_by_name[m.group(1)] = txt

done = []
for dp, _, fs in os.walk(ROOT):
    for f in fs:
        if not f.endswith(".shader"):
            continue
        p = os.path.join(dp, f)
        txt = open(p, encoding="utf-8", errors="replace").read()
        m = re.search(r'^\s*Shader\s+"([^"]+)"', txt, re.M)
        if not m or m.group(1) not in new_by_name or m.group(1) in SKIP:
            continue
        open(p, "w", encoding="utf-8", newline="\n").write(new_by_name[m.group(1)])
        done.append((m.group(1), os.path.relpath(p, ROOT)))

print("escritos:", len(done))
for n, r in sorted(done):
    print(f"  {n:38} {r}")

# comprobacion: todos los frag tienen return
bad = []
for n, r in done:
    t = new_by_name[n]
    frag = t.split("float4 frag", 1)[-1]
    if "return" not in frag:
        bad.append(n)
print("frag sin return:", bad)

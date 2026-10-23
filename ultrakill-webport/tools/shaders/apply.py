import os, re, sys, json
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\export\ExportedProject\Assets"
NEW = r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\tools\shaders\new"
INV = r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\shader_inventory"

new_by_name = {}
for f in os.listdir(NEW):
    if not f.endswith(".shader"):
        continue
    txt = open(os.path.join(NEW, f), encoding="utf-8").read()
    m = re.search(r'^\s*Shader\s+"([^"]+)"', txt, re.M)
    new_by_name[m.group(1)] = txt

report, failed = [], []
for name, rel, guid in json.load(open(os.path.join(INV, "replaced.json"), encoding="utf-8")):
    p = os.path.join(ROOT, rel)
    mp = p + ".meta"
    if not os.path.exists(p):
        failed.append((rel, "no existe"))
        continue
    cur = re.search(r"guid:\s*([0-9a-f]{32})", open(mp, encoding="utf-8").read()).group(1)
    if cur != guid:
        failed.append((rel, f"guid {cur} != {guid}"))
        continue
    open(p, "w", encoding="utf-8", newline="\n").write(new_by_name[name])
    report.append((name, rel, guid))

print(f"reescritos: {len(report)}   fallos: {len(failed)}")
for f in failed:
    print("  FALLO", f)
for name, rel, guid in sorted(report)[:6]:
    print(f"  {name:38} {rel}")

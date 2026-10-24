import os, re, sys
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\export\ExportedProject\Assets"

# Luces de los focos de Minos: las de rango enorme (500) son los "searchlights"
# que iluminan media arena y desplazan a las luces propias del nivel en el
# vertex lighting (limite de 4 luces por vertice). Se apagan; las de rango 30
# (aura del jefe) se dejan.
targets = []
for dp, _, fs in os.walk(ROOT):
    for f in fs:
        if not f.endswith(".prefab"):
            continue
        if "MinosBoss" in f or "MinosBackground" in f:
            targets.append(os.path.join(dp, f))

changed = []
for p in targets:
    txt = open(p, encoding="utf-8", errors="replace").read()
    docs = re.split(r"(?=\n--- )", txt)
    out = []
    n = 0
    for d in docs:
        if d.lstrip().startswith("--- !u!108") or d.startswith("--- !u!108"):
            m = re.search(r"m_Range:\s*([\d.]+)", d)
            if m and float(m.group(1)) >= 100.0:
                new = re.sub(r"(\n  m_Enabled:\s*)1", r"\g<1>0", d, count=1)
                if new != d:
                    n += 1
                d = new
        out.append(d)
    if n:
        open(p, "w", encoding="utf-8", newline="\n").write("".join(out))
        changed.append((os.path.relpath(p, ROOT), n))

print("prefabs tocados:", len(changed))
for c in changed:
    print("  ", c)

import os, re, sys
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\export\ExportedProject\Assets"
WANT = re.compile(r"^psx/unlit/additive/nocull-nofog")

changed = []
for dp, _, fs in os.walk(ROOT):
    for f in fs:
        if not f.endswith(".shader"):
            continue
        p = os.path.join(dp, f)
        t = open(p, encoding="utf-8", errors="replace").read()
        m = re.search(r'^\s*Shader\s+"([^"]+)"', t, re.M)
        if not m or not WANT.match(m.group(1)):
            continue
        orig = t
        # Blend aditivo -> transparencia normal
        t = t.replace("Blend One One", "Blend SrcAlpha OneMinusSrcAlpha")
        # Opacidad por alfa en vez de multiplicar el color
        t = t.replace("half4 color = c * _OpacScale;",
                      "half4 color = c;\n\t\t\t\t\tcolor.a = saturate(c.a * _OpacScale * 2.0);")
        if t != orig:
            open(p, "w", encoding="utf-8", newline="\n").write(t)
            changed.append((m.group(1), os.path.relpath(p, ROOT)))

print("shaders cambiados:", len(changed))
for c in sorted(changed):
    print("  ", c)

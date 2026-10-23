import UnityPy, sys, os, collections, json
sys.stdout.reconfigure(encoding="utf-8")

TARGETS = ["psx/water", "psx/railgun", "psx/heartbeat", "ULTRAKILL/bloodstain",
           "psx/vertexlit-noambient-emissive", "ProBuilder/Standard Vertex Color",
           "Hidden/ProBuilder/HideVertices", "ULTRAKILL/Invert", "ULTRAKILL/Main_PostProcess",
           "Unlit/Volumetric", "Hidden/ProBuilder/EdgePicker", "Hidden/ProBuilder/FacePicker",
           "Hidden/ProBuilder/VertexPicker", "ProBuilder/UnlitVertexColor"]

for path in sys.argv[1:]:
    if not os.path.exists(path):
        print("FALTA", path)
        continue
    env = UnityPy.load(path)
    shaders = {}
    for o in env.objects:
        if o.type.name != "Shader":
            continue
        d = o.read()
        try:
            name = d.m_ParsedForm.m_Name
        except Exception:
            continue
        blob = len(getattr(d, "compressedBlob", b"") or b"")
        decomp = getattr(d, "decompressedSize", 0)
        shaders[name] = (blob, decomp, o.path_id)

    mats = collections.Counter()
    for o in env.objects:
        if o.type.name != "Material":
            continue
        d = o.read()
        pptr = d.m_Shader
        sid = pptr.m_PathID if pptr else 0
        for nm, (b, dc, pid) in shaders.items():
            if pid == sid:
                mats[nm] += 1
                break

    print("=" * 78)
    print(os.path.basename(path), f"{os.path.getsize(path)/1e6:.1f} MB", "| shaders:", len(shaders))
    for t in TARGETS:
        if t in shaders:
            b, dc, pid = shaders[t]
            print(f"  {t:38} blob={b:6} decomp={dc or 0:7} mats={mats.get(t,0)}")
        else:
            print(f"  {t:38} NO PRESENTE")


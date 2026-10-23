import UnityPy, sys, os, json, re
sys.stdout.reconfigure(encoding="utf-8")

BUNDLES = [r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\original-bundles\StreamingAssets\common",
           r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\original-bundles\StreamingAssets\acts\act-0",
           r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\original-bundles\StreamingAssets\acts\act-1",
           r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\original-bundles\StreamingAssets\acts\prime"]

WANT = ["psx/water", "psx/railgun", "psx/heartbeat", "ULTRAKILL/bloodstain", "ULTRAKILL/Invert",
        "ULTRAKILL/Main_PostProcess", "ULTRAKILL/Additive", "Unlit/Volumetric", "psx/vertexlit/emissive",
        "ProBuilder/Standard Vertex Color", "Hidden/ProBuilder/HideVertices", "ULTRAKILL/postblood",
        "ULTRAKILL/Skybox", "ddShaders/ddInvertMapped", "psx/vertexlit/cyber", "psx/vertexlit/pulsating"]

BLEND = {0: "Zero", 1: "One", 2: "DstColor", 3: "SrcColor", 4: "OneMinusDstColor", 5: "SrcAlpha",
         6: "OneMinusSrcColor", 7: "DstAlpha", 8: "OneMinusDstAlpha", 9: "SrcAlphaSaturate", 10: "OneMinusSrcAlpha"}
CULL = {0: "Off", 1: "Front", 2: "Back"}
CMP = {0: "Disabled", 1: "Never", 2: "Less", 3: "Equal", 4: "LessEqual", 5: "Greater", 6: "NotEqual", 7: "GreaterEqual", 8: "Always"}

out = {}
profiles = {}
for b in BUNDLES:
    env = UnityPy.load(b)
    for o in env.objects:
        if o.type.name != "Shader":
            continue
        d = o.read()
        try:
            pf = d.m_ParsedForm
            name = pf.m_Name
        except Exception:
            continue
        if name not in WANT or name in out:
            continue
        props = []
        for pr in pf.m_PropInfo.m_Props:
            props.append(dict(name=pr.m_Name, type=pr.m_Type, flags=pr.m_Flags,
                              desc=pr.m_Description,
                              default=[getattr(pr, f"m_DefValue_{i}_") for i in range(4)]))
        subs = []
        for ss in pf.m_SubShaders[:3]:
            passes = []
            for ps in ss.m_Passes[:2]:
                st = ps.m_State
                passes.append(dict(
                    blendOp=getattr(st, "m_BlendOp", None),
                    src=BLEND.get(getattr(st, "m_SrcBlend", None), getattr(st, "m_SrcBlend", None)),
                    dst=BLEND.get(getattr(st, "m_DstBlend", None), getattr(st, "m_DstBlend", None)),
                    zwrite=getattr(st, "m_ZWrite", None),
                    ztest=CMP.get(getattr(st, "m_ZTest", None), getattr(st, "m_ZTest", None)),
                    cull=CULL.get(getattr(st, "m_CullMode", None), getattr(st, "m_CullMode", None)),
                    lighting=getattr(st, "m_Lighting", None),
                    tags=(ps.m_Tags.tags if hasattr(ps, "m_Tags") and ps.m_Tags else None),
                ))
            subs.append(dict(tags=(ss.m_Tags.tags if ss.m_Tags else None), passes=passes))
        out[name] = dict(props=props, subshaders=subs, fallback=pf.m_FallbackName)

os.makedirs(r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\shader_inventory", exist_ok=True)
with open(r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\shader_inventory\cab_shaders.json", "w", encoding="utf-8") as f:
    json.dump(out, f, ensure_ascii=False, indent=1)

for name, info in out.items():
    print("=" * 70)
    print(name, "| fallback:", info["fallback"])
    print("  props:", ", ".join(f'{p["name"]}({p["type"]})' for p in info["props"]) or "(ninguna)")
    for i, ss in enumerate(info["subshaders"]):
        print(f"  sub{i} tags:", ss["tags"])
        for p in ss["passes"]:
            print("     pass:", {k: v for k, v in p.items() if k != "tags"})



# ULTRAWebGL

This repository is an unofficial, community-driven reverse engineering project aimed at porting **ULTRAKILL** to WebGL for educational, research, and platform-interoperability purposes. 

---

## Legal Disclaimer & Copyright Notice

* **Ownership:** This project is **strictly unofficial**. We (the repository maintainers and contributors) do not own, claim ownership of, or hold any commercial rights to **ULTRAKILL**.
* **Intellectual Property:** All copyrights, trademarks, assets (including but not limited to 3D models, textures, audio, music, dialogue, characters, logos, and original codebase) belong entirely to **Arsi "Hakita" Patala**, **New Blood Interactive**, and their respective owners.

## Takedown Policy (DMCA & Rightsholders)

We deeply respect the work of *Hakita* and the *New Blood Interactive* team. 

If you are a rightsholder, representative, or member of **New Blood Interactive** and you wish for this repository to be removed, **please let us know**. 

* **Immediate Compliance:** This repository will be permanently taken down or modified immediately upon request by New Blood Interactive or any authorized individual holding legal rights to the *ULTRAKILL* intellectual property. 
* To request a takedown, you can open an Issue in this repository or contact us directly via e-mail, paranoidandroid745@proton.me.

---

## License

No open-source license is granted for the original game logic or decompiled components contained herein. All original game code remains under the exclusive copyright of its original creators. Any custom boilerplate or build scripts created solely by this project's contributors are provided as-is without warranty.

---

Reconstrucci├│n del juego **ULTRAKILL** (Unity 2019.4.16f1) para que funcione en
el navegador, a partir del build **pre-GREED** (Patch 6 Hotfix #2, 19/04/2021)
exportado con AssetRipper.

> Uso personal y de estudio. El repo **no** incluye los assets del juego
> (son ~3 GB y se regeneran): ver [REGENERAR-ASSETS.md](REGENERAR-ASSETS.md).

## Qu├® hay aqu├¡

```
.opencode/                 skill del proyecto + comandos /build /shots /idx
ultrakill-webport/
  HANDOFF.md               DIARIO DEL PROYECTO (l├®elo primero)
  PLAN.md, PORT-NOTES.md
  export/ExportedProject/  proyecto de Unity
    Assets/Scripts/Assembly-CSharp/   los 576 scripts (con nuestros parches)
    Assets/Shader/                    los 107 shaders reales
    Assets/Editor/WebGLTemplate.html  la p├ígina web del port (JS)
    Assets/Editor/WebGLBuilder.cs     constructor del player
    Assets/Editor/WebGLBundleBuilder.cs constructor de los bundles
  tools/
    assetindex/asset-index.ps1        ├¡ndice de assets (GUIDs, materiales, escenas)
    visual/ukshot.py                  arn├®s visual: capturas + diff de p├¡xeles
  serve-webgl.ps1, serve-lan.ps1      servidores locales
```

## Estado

**v20.5** ÔÇö PRELUDE (0-1ÔÇª0-5, 0-S) + LIMBO/LUST/GLUTTONY (1-1ÔÇª3-2) + PRIME (P-1).
Iluminaci├│n original (vertex lighting PSX), men├║ de niveles (F3) para saltar a
cualquier nivel, ajustes de gr├íficos funcionales, pausa usable y atajos del
navegador bloqueados en pantalla completa.

## Lo que este repo NO incluye

Los assets binarios del juego y los builds WebGL (3,5 GB + 500 MB). Se
regeneran con AssetRipper y los scripts de build ÔåÆ [REGENERAR-ASSETS.md](REGENERAR-ASSETS.md).

## Herramientas

```powershell
# ├¡ndice de assets (qu├® materiales usan este shader, qu├® nivel real hay en cada
# escena, qui├®n referencia un GUID...)
$ai = ".\ultrakill-webport\tools\assetindex\asset-index.ps1"
& powershell -NoProfile -ExecutionPolicy Bypass -File $ai scenes
& powershell -NoProfile -ExecutionPolicy Bypass -File $ai shader-users "psx/unlit/ambient"

# capturas del juego y diff de p├¡xeles (verifica que un cambio altera la imagen)
$py = "$env:LOCALAPPDATA\Programs\Python\Python312\python.exe"
& $py .\ultrakill-webport\tools\visual\ukshot.py --url "http://localhost:8000/" `
      --out "$env:TEMP\shots" --actions "wait:50;key:Escape;wait:3;shot:pausa"
& $py .\ultrakill-webport\tools\visual\ukshot.py --diff a.png b.png
```

La **skill `ultrakill-port`** (en `.opencode/skills/`) recoge las trampas
conocidas de Unity/WebGL: los uniforms de `Properties` que no existen en la
etapa de vertex en GLES, los bloques `Properties` mal formados que compilan en
silencio, las escenas permutadas por AssetRipper, etc.

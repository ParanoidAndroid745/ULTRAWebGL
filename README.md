# Port WebGL de ULTRAKILL

Reconstrucción del juego **ULTRAKILL** (Unity 2019.4.16f1) para que funcione en
el navegador, a partir del build **pre-GREED** (Patch 6 Hotfix #2, 19/04/2021)
exportado con AssetRipper.

> Uso personal y de estudio. El repo **no** incluye los assets del juego
> (son ~3 GB y se regeneran): ver [REGENERAR-ASSETS.md](REGENERAR-ASSETS.md).

## Qué hay aquí

```
.opencode/                 skill del proyecto + comandos /build /shots /idx
ultrakill-webport/
  HANDOFF.md               DIARIO DEL PROYECTO (léelo primero)
  PLAN.md, PORT-NOTES.md
  export/ExportedProject/  proyecto de Unity
    Assets/Scripts/Assembly-CSharp/   los 576 scripts (con nuestros parches)
    Assets/Shader/                    los 107 shaders reales
    Assets/Editor/WebGLTemplate.html  la página web del port (JS)
    Assets/Editor/WebGLBuilder.cs     constructor del player
    Assets/Editor/WebGLBundleBuilder.cs constructor de los bundles
  tools/
    assetindex/asset-index.ps1        índice de assets (GUIDs, materiales, escenas)
    visual/ukshot.py                  arnés visual: capturas + diff de píxeles
  serve-webgl.ps1, serve-lan.ps1      servidores locales
```

## Estado

**v20.5** — PRELUDE (0-1…0-5, 0-S) + LIMBO/LUST/GLUTTONY (1-1…3-2) + PRIME (P-1).
Iluminación original (vertex lighting PSX), menú de niveles (F3) para saltar a
cualquier nivel, ajustes de gráficos funcionales, pausa usable y atajos del
navegador bloqueados en pantalla completa.

## Lo que este repo NO incluye

Los assets binarios del juego y los builds WebGL (3,5 GB + 500 MB). Se
regeneran con AssetRipper y los scripts de build → [REGENERAR-ASSETS.md](REGENERAR-ASSETS.md).

## Herramientas

```powershell
# índice de assets (qué materiales usan este shader, qué nivel real hay en cada
# escena, quién referencia un GUID...)
$ai = ".\ultrakill-webport\tools\assetindex\asset-index.ps1"
& powershell -NoProfile -ExecutionPolicy Bypass -File $ai scenes
& powershell -NoProfile -ExecutionPolicy Bypass -File $ai shader-users "psx/unlit/ambient"

# capturas del juego y diff de píxeles (verifica que un cambio altera la imagen)
$py = "$env:LOCALAPPDATA\Programs\Python\Python312\python.exe"
& $py .\ultrakill-webport\tools\visual\ukshot.py --url "http://localhost:8000/" `
      --out "$env:TEMP\shots" --actions "wait:50;key:Escape;wait:3;shot:pausa"
& $py .\ultrakill-webport\tools\visual\ukshot.py --diff a.png b.png
```

La **skill `ultrakill-port`** (en `.opencode/skills/`) recoge las trampas
conocidas de Unity/WebGL: los uniforms de `Properties` que no existen en la
etapa de vertex en GLES, los bloques `Properties` mal formados que compilan en
silencio, las escenas permutadas por AssetRipper, etc.

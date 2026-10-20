# ULTRAKILL — port WebGL

Reconstrucción de **ULTRAKILL** (Unity 2019.4.16f1) para que funcione en el
navegador, a partir del build **pre-GREED** (Patch 6 Hotfix #2, 19/04/2021)
exportado con AssetRipper y recompilado con el módulo WebGL de Unity.

Uso personal y de estudio. El proyecto original, sus assets y su código
pertenecen a Arsi "Hakita" Patala / New Blood Interactive (ver el disclaimer del
README del repositorio).

## Estado

**v20.5** — jugable de principio a fin:

- **PRELUDE** (acto 0): 0-1 INTO THE FIRE, 0-2 THE MEATGRINDER, 0-3 DOUBLE DOWN,
  0-4 A ONE-MACHINE ARMY, 0-5 CERBERUS y el secreto 0-S.
- **LIMBO / LUST / GLUTTONY** (acto 1): 1-1 … 1-4, 1-S, 2-1 … 2-4, 2-S, 3-1, 3-2.
- **PRIME**: P-1 SOUL SURVIVOR.
- Iluminación original (vertex lighting PSX, niebla, mapeo afín).
- Menú de niveles (F3) para saltar a cualquier nivel sin avanzar la campaña.
- Ajustes de gráficos funcionales (pixelización, compresión de color, presets
  PC/PSX).
- Menú de pausa funcional, ratón capturado y recuperable, atajos del navegador
  bloqueados en pantalla completa.

## Jugar

```powershell
.\serve-webgl.ps1          # http://localhost:8000/
.\serve-lan.ps1 -Port 8010 # http://<ip-del-equipo>:8010/  (otro dispositivo)
```

El paquete distribuible (`dist/ULTRAKILL-WebGL-v20.5.zip`, 262 MB) es
**autocontenido**: incluye el player, los tres bundles WebGL, la página y un
lanzador (`JUGAR.bat`). Se descomprime y se juega; no hace falta recompilar
nada ni tener los assets originales.

## Estructura

```
ultrakill-webport/
├── LEEME.md              este fichero
├── NOTAS.md              notas técnicas: pipeline, trampas de Unity/WebGL, cambios
├── serve-webgl.ps1       servidor local (HttpListener)
├── serve-lan.ps1         servidor de red (TcpListener)
├── export/
│   └── ExportedProject/  proyecto de Unity 2019.4.16f1
│       ├── Assets/Scripts/Assembly-CSharp/   scripts (con los parches del port)
│       ├── Assets/Shader/                    los 107 shaders reales
│       ├── Assets/Editor/WebGLTemplate.html  la página web del port
│       ├── Assets/Editor/WebGLBuilder.cs      constructor del player
│       └── Assets/Editor/WebGLBundleBuilder.cs constructor de los bundles
├── tools/
│   ├── assetindex/       índice de assets (GUIDs, materiales, escenas, uniforms)
│   ├── visual/ukshot.py  pruebas visuales: capturas y diff de píxeles
│   └── build/            utilidades de build
└── logs/                 logs de build (no se versionan)
```

## Reconstruir los assets

Los assets binarios del juego (~3 GB) **no están en el repositorio**: se
regeneran con AssetRipper desde la copia del juego. El procedimiento completo
está en [`REGENERAR-ASSETS.md`](../REGENERAR-ASSETS.md) (depot de Steam,
manifest, AssetRipper y scripts de build).

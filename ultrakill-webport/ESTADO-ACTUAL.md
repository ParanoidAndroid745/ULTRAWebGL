# ESTADO ACTUAL — port WebGL de ULTRAKILL

> Esto se escribe para que el trabajo no dependa de la memoria de la sesión:
> si opencode compacta el contexto, si la sesión se cae, o si abres otra, el
> estado está aquí. Última actualización: 2026-10-21 05:55 (v20.7 publicada).

## ESTADO: v20.7 PUBLICADA (pre-release) — las 3 tareas están hechas

- Build: `ultrakill-webport/export/ExportedProject/Builds/WebGL` (465,8 MB).
- ZIP: `ultrakill-webport/dist/ULTRAKILL-WebGL-v20.7.zip` (243,2 MB, verificado).
- GitHub: commit `7c91fbcc2` en `main`; release **v20.7** marcada como
  **pre-release** con el ZIP adjunto.
  https://github.com/ParanoidAndroid745/ULTRAWebGL/releases/tag/v20.7

## v20.8 (WIP, SIN publicar) — el texture warping fuera

- Commits locales: `0ad206b54` (parche de shaders) y `a2d41f460` (revision de
  los subagentes). **NO se ha pusheado nada** (el usuario aun no lo aprueba).
- Build: OK, 0 "Shader error in" y 0 "error CS".
  ZIP: `dist/ULTRAKILL-WebGL-v20.8.zip` (243,2 MB, verificado).
- Que era el warping: el **mapeo afin de textura**. El vertex premultiplicaba la
  UV por `S = distance + (w*UNITY_LIGHTMODEL_AMBIENT.a*8)/distance/2` y el fragment
  dividia por `IN.normal.r` (la distancia interpolada): esa division ES la
  deformacion (texturas estiradas hacia el punto de fuga). El global
  `_TextureWarping` no lo leia NINGUN shader real (solo estaba en el `Properties`
  de los dummy), por eso ponerlo a 0 no hacia nada.
- Fix: los 107 shaders reales leen `_TextureWarping` (uniform suelto, como
  `_ResY`) y con valor 0 la UV no se premultiplica y el fragment no divide =>
  **mapeo perspectivamente correcto**. Los 1726 materiales con claves de warp
  quedan a neutro (`_TextureWarping=0`, `_VertexWarping=9999`), que es lo que dice
  la documentacion del juego.
- TOWER encontro que la seccion `--PSX SETTINGS--` del menu de pausa seguia VIVA en
  los 22 niveles (v20.7 solo la quito del prefab Canvas y de Tutorial/Main Menu):
  desactivados los 5 objetos de cada escena (110 en total).
- `?v=208` en `MapLoader` (los shaders cambian dentro de los bundles, que se
  cachean 1 hora).
- Verificado visualmente: la senal "KEEP OUT" ya sale como rectangulo limpio (antes
  se deformaba) y el menu GRAPHICS de un nivel arranca en PERFORMANCE SETTINGS.

### La cadena de revision (protocolo del usuario)
ULTRA (parche) -> STRAWBERRY (`opencode/glm-5.3-flash-free#max`) -> TOWER
(`opencode/deepseek-v4.1-flash-free#max`) -> ULTRA.
- STRAWBERRY: el parche de shaders estaba bien, pero el barrido de materiales de
  ULTRA se habia quedado corto (535 materiales con sufijo `_N`, 310 del shader
  principal del mundo `psx_vertexlit_vertexlit`).
- TOWER: la seccion PSX viva en los niveles y el `?v=207` cacheado.
- Informes: `%TEMP%\opencode\STRAWBERRY-informe.md` y `TOWER-informe.md`.

### Decisiones del usuario tras probar la v20.8
- **Aceptada** la v20.8 (warping fuera). Se pushea a la repo y se publica la
  pre-release.
- **El hueco en blanco que dejaban los ajustes PSX en el menu de pausa de los
  niveles se queda asi por ahora.** En una build futura la idea es que esos
  ajustes **se vean pero sean inaccesibles**, tapados por un overlay "WIP"
  (asi el menu no parece roto). En la v20.8 los objetos estan DESACTIVADOS
  (no borrados) en las 22 escenas de nivel, asi que reactivarlos seria solo
  cambiar `m_IsActive: 0` por `1` y anadir el overlay encima.

## Tarea 1 — Escenas de act-1 (HECHA y VERIFICADA)

**Diagnóstico**: AssetRipper exportó las escenas con los nombres permutados.
La verdad está en el **bundle original** (`original-bundles/StreamingAssets/`).
Se escribió un parser de UnityFS v7 (LZ4HC; cabecera
`uint64 offset | uint64 size | uint32 flags | path terminada en NUL`) en
`%TEMP%\opencode\unityfs.py`.

Mapeo definitivo (tokens + tuplas levelName/nextAct/targetLevelName):

| canónico | exportado | |
|---|---|---|
| Level 1-4 | Level 3-S.unity | renombrado |
| Level 2-S | Intermission1.unity | renombrado |
| Level 3-2 | Level 2-S.unity | renombrado |
| Intermission1 | Level 3-2.unity | renombrado |

- Los `targetLevelName` ya eran canónicos → no hubo que tocar referencias.
- "Level 3-S" no existe en ningún bundle (0 hits en los 39 nodos): era un
  nombre inventado. Se corrigió la única referencia corrupta
  (`Prime FinalRoom Variant.prefab`: Level 3-S → Level 2-4) y la entrada
  fantasma del menú F3.
- `WebGLBundleBuilder.BuildAll`: act-0 = Tutorial + Level 0-*;
  act-1 = Level 1-* + 2-* + 3-* + Intermission1 (13, igual que el original);
  prime = Level P-*.
- `MapLoader.WebGLBundleQuery` → "?v=207".

**Verificación**: el bundle act-1 construido tiene los mismos 13 nombres y
contenidos que el original (`verify_bundle.py`), y el 1-4 carga con su
escenario (captura `shots_v207/level14.png`).

## Tarea 2 — Corte PSX (HECHA y VERIFICADA)

- `GraphicsOptions.cs`: `ApplyNeutral()` fuerza `_ResY=0` (sin rejilla),
  `_ColorPrecision=2048` (sin cuantizar), warping 0; **ignora PlayerPrefs**;
  `PSXPreset()` ya no aplica el look PSX; todo null-safe.
- **UI borrada** del YAML de Unity con `%TEMP%\opencode\udelete.py` (borra el
  subárbol y avisa de referencias colgantes: 0 en los 3 ficheros):
  `Assets/prefabs/Canvas.prefab`, `Assets/Scenes/Tutorial.unity` (además los
  botones "PSX" y "PSX Example"), `Assets/Scenes/Main Menu.unity` (2 copias).
  Las 4 referencias del componente `GraphicsOptions` se puso a `{fileID: 0}`.
- Los 107 shaders reales usan `_ResY`; los 61 sin `_ResY` son exactamente los
  *dummy* → el corte es completo.
- **Verificación**: aplicar el preset PC no cambia ni un píxel (**0,00 %**);
  en la build vieja el cambio PSX→PC era del 14,42 %. El panel GRAPHICS ya
  empieza en "--PERFORMANCE SETTINGS--" (captura `shots_v207/graf4.png`).

## Tarea 3 — Atajos de Chrome / Ctrl+D (HECHA y VERIFICADA)

Diagnóstico: **Chrome solo concede `navigator.keyboard.lock()` en pantalla
completa iniciada por JavaScript** (docs de Chrome) y lo suelta en silencio
al pulsar Escape o al perder el foco (sin avisar → el estado quedaba
obsoleto y el reintento se detenía para siempre).

Parches en `Assets/Editor/WebGLTemplate.html`:
1. Pantalla completa automática con el primer gesto real del usuario.
2. `capturePointer()`: si se pierde el fullscreen en partida, se vuelve a pedir.
3. El reintento no depende de `lockActive` (si no se confirmó hace < 1,5 s,
   se vuelve a pedir).
4. `blur` / `visibilitychange` → se olvida el estado del bloqueo.
5. `toggle()` y Shift+Esc → el usuario deja de querer fullscreen.
6. El menú F3, el toast y el estado del teclado se **mueven dentro** de
   `#unityContainer` al entrar en fullscreen (en fullscreen el navegador solo
   pinta ese elemento; y el loader de Unity vacía el contenedor al arrancar,
   así que hay que moverlos en runtime, no en el HTML).

**Verificación** (`%TEMP%\opencode\kblock3.py`, Chromium headed): tras el clic
→ fullscreen ✓, lock concedido ✓, **Ctrl+D llega a la página (= el atajo
está anulado)** ✓; tras Escape → el fullscreen sigue ✓, el lock sigue ✓,
Ctrl+D sigue anulado ✓; el panel F3 está dentro del fullscreen ✓; 0 errores.

## Pendiente (no tocado)

- Los **61 shaders dummy** (latido, railgun, sangre, UI) → aplazado por el usuario.
- El **post-proceso real** (viñeta, gradiente, scanlines) → pendiente.
- Los 61 stubs se pueden traer del repo `genizy/web-port` (~4 GB).

## Cómo construir y verificar

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\2019.4.16f1\Editor\Unity.exe"
$proj  = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\export\ExportedProject"
& $unity -batchmode -quit -projectPath $proj -logFile bundles.log -buildTarget WebGL -executeMethod WebGLBundleBuilder.BuildAll
Copy-Item "$proj\Builds\WebGLBundles\acts\*" "$proj\Assets\StreamingAssets\acts\" -Force
& $unity -batchmode -quit -projectPath $proj -logFile player.log -buildTarget WebGL -executeMethod WebGLBuilder.Build
# validar por el log: "Shader error in" = 0 y "error CS" = 0
```

Lanzador: `%TEMP%\opencode\build207.ps1`. Empaquetado:
`%TEMP%\opencode\package_v207.ps1`.

Arnés visual: `ultrakill-webport/tools/visual/ukshot.py`
(`--url http://localhost:8000/?v=207 --out ... --actions "wait:45;click:640,400;wait:6;key:F3;wait:4;shot:x"`).
Ojo: el primer clic salta a pantalla completa y **cambia el tamaño del canvas
(1280×720 → 1280×800)**, así que las coordenadas de la UI cambian: hay que
hacer un clic de Invernalido antes de medir.

Otros scripts de esta sesión (en `%TEMP%\opencode`): `unityfs.py` (parser
UnityFS), `match_scenes2.py` + `verify_map.py` (emparejar escenas),
`verify_bundle.py` (comparar el bundle construido con el original),
`udelete.py` + `uyaml.py` + `nullgfx.py` (edición del YAML de Unity),
`kblock2.py` / `kblock3.py` (test del bloqueo de teclado).

## Entorno

- Unity 2019.4.16f1 en `C:\Program Files\Unity\Hub\Editor\2019.4.16f1` (licencia
  Personal activa). Smart App Control **desactivado** (si no, IL2CPP no arranca).
- Python 3.12 en `%LOCALAPPDATA%\Programs\Python\Python312\python.exe` (playwright,
  pillow, Chromium ya instalados). `git` en `C:\Program Files\Git\cmd\git.exe`,
  `gh` en `C:\Program Files\GitHub CLI\gh.exe` (**no están en el PATH**).
- **Los servidores 8000/8010 se mueren si los lanzan procesos hijos del shell**:
  hay que lanzarlos con `background: true` del tool `shell`.
- Si la máquina se suspende, los builds se quedan congelados: relanzarlos.
- Índice de assets: `ultrakill-webport/tools/assetindex/asset-index.ps1`
  (índice cacheado en `tools/assetindex/index/`; `build` lo reconstruye en ~5 min).

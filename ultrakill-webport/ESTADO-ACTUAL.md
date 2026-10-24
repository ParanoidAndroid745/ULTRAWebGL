# ESTADO ACTUAL — port WebGL de ULTRAKILL

> Esto se escribe para que el trabajo no dependa de la memoria de la sesión:
> si opencode compacta el contexto, si la sesión se cae, o si abres otra, el
> estado está aquí. Última actualización: 2026-10-22 (v21.1.4 pre-release, todo
> en `main`; `risky` eliminada).

## v21.1.4 (pre-release, `main`) — rayos de Minos al 0.1

Unico cambio respecto a v21.1.3: `WebGLRenderFix.Init()` fija
`_OpacScale = 0.1` (antes 0.4) → los haces de Minos Prime (2-4) salen un **90%
mas transparentes**. Solo se toco C#, asi que **solo se recompilo el player**
(los bundles de v21.1.3 son validos y se copiaron tal cual).

- Rama `risky` eliminada (local y remota): todo el trabajo vive en `main`.
- Build del player: 0 `Shader error in`, 0 `error CS`, `BUILD RESULT: Succeeded`.

## v21.1.3 (pre-release; su tag sigue apuntando a su commit)

**Cambios respecto a v21.1.2:**
- **Colas de render de los unlit**: 28 shaders transparent + 6 additive a `Queue=Transparent` y 12 alphatest a `AlphaTest/TransparentCutout` (colas del juego original, verificadas en el CAB). Las alas de Gabriel (3-2) dejan de ser invisibles: con blend alfa en la cola Geometry las tapaba la geometria opaca posterior.
- **Rayos de Minos Prime (2-4)**: se añade `_OpacScale` como *uniform suelto* (los properties numéricos no existen en la etapa de vertex) a los 5 shaders unlit additive de los haces, y `WebGLRenderFix.Init()` hace `Shader.SetGlobalFloat("_OpacScale", 0.4f)` = 60% menos luz. Los rayos salen más transparentes.
- **Fondo negro en Prelude, Limbo y Gluttony**: se quita la skybox por defecto de Unity (fileID 10304) de las escenas sin cielo propio; Lust y Cyber Grind conservan su skybox.
- `MapLoader.WebGLBundleQuery`: `?v=213`.
- Servidores con `no-store` para `.js/.wasm/.data/.unityweb/.json`.
- Revisión en cadena: STRAWBERRY + TOWER (APTO).
- Build: bundles 0 `Shader error in` / 0 `error CS`; player `BUILD RESULT: Succeeded | errors=0 | warnings=27 | sizeMB=466,0`.

## Rama `risky` (cola de render de los unlit: las alas de Gabriel)

## Rama `risky` (SIN publicar) - cola de render de los unlit: las alas de Gabriel

**Sintoma**: las alas de Gabriel salian **invisibles**.

**Diagnostico** (con datos): el material real de las alas en 3-2 es
`Material/GabrielWings_1.mat` -> shader `psx/unlit/transparent/nocull_v2`, que
tiene `Blend SrcAlpha OneMinusSrcAlpha` y `ZWrite Off` pero estaba en la cola
**Geometry** (opaca). En Unity, un material con blend alfa en una cola < 2500 se
dibuja en el **pase opaco**, no escribe profundidad y lo tapa la geometria opaca
que se dibuja despues (la pared del fondo de la arena). El cuerpo de Gabriel es
opaco y escribe profundidad, por eso se veia. Descartado: la textura
(`T_Wings.png` es RGBA con alpha real), las UVs (0.017-1.94), los vertex colors
(255,255,255,255) y la niebla (el vertexlit usa el mismo codigo y se ve bien).

**Arreglo** (las colas son las del juego original, verificadas en el CAB):
- 28 shaders unlit **transparent** -> `Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }`
- 6 shaders unlit **additive** -> mismo Tags
- 12 shaders **alphatest** -> `Tags{ "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" }`
- El original lleva `IgnoreProjector` solo en los alphatest; en transparent y
  additive es inerte aqui (0 proyectores activos) y se mantiene por coherencia
  con el resto del port.
- Se quita el `ZWrite Off` duplicado de las variantes zwrite (inerte).
- `MapLoader.WebGLBundleQuery` -> `?v=212` (los bundles cambian).
- Sin tocar CG, materiales ni escenas. 171 materiales usan la familia; 10 tienen
  `m_CustomRenderQueue: 5000` (ExplosionShockwave, preexistente).

**Revision**: STRAWBERRY (cobertura, trampas de nombre, materiales mas
delicados) y TOWER (verifico las Tags contra el CAB del juego original,
corrigio la familia alphatest a AlphaTest/TransparentCutout, subio el `?v=212`).

**Pendiente de prueba visual** (en orden): 1) alas de Gabriel (normal/enrage y
Juggle), 2) MinosPrimeBody, 3) BloodDrop/BloodDrop_Trail, 4) MinosPrimeVeins y
MinosWire, 5) BonusGhost/VirtueSphere, 6) HealingEffect/StainedGlass, 7)
aditivos (Rimlight, LaserGrid, NailgunGlow, MinosSearchLights).

**Rayos de Minos Prime (2-4)**: los haces (materiales `MinosSearchLights`,
shaders unlit additive, textura `shockwavetex.png` de RGB casi blanco con alpha
plano que el blend aditivo ignora) salian como conos blancos solidos. Se anade
`_OpacScale` a esos 5 shaders (como uniform **suelto**: los properties
numericos no existen en la etapa de vertex, por eso da "undeclared identifier"
y hay que fijarlo desde codigo como `_ResY` o `_ColorPrecision`) y
`WebGLRenderFix.Init()` hace `Shader.SetGlobalFloat("_OpacScale", 0.4f)`
= 60% menos luz. El resto de la familia additive no cambia (solo declaran el
uniform los shaders de los haces).

**Sin rebuild de bundles no se ve nada**: los shaders viajan en los bundles.

## Fondo negro en Prelude, Limbo y Gluttony (cambiado, SIN publicar)

**Sintoma**: al salirse del nivel se veia la **skybox por defecto de Unity**
(el cielo azul procedural). Las escenas sin cielo propio referencian el builtin
`Default-Skybox` (fileID 10304, guid `0000000000000000f000000000000000`) en su
`RenderSettings.skybox`, asi que nunca es null y la camara principal acababa
con `ClearFlags.Skybox` (el "fix" de cielos de la v21.1 lo dejo asi).

**Arreglo**: `WebGLRenderFix.ApplySkybox()` quita esa skybox y pone el fondo en
negro (`backgroundColor = Color.black`) en los niveles de **Prelude** (0-1..0-S),
**Limbo** (1-1..1-S) y **Gluttony** (3-1, 3-2). Lust (2-1..2-4) y Cyber Grind
(Endless) tienen skybox propia y no se tocan. Ninguna shader del proyecto
muestrea la skybox de la escena (el cielo de Limbo usa su propio `_Cube`), asi
que quitarla no rompe nada.

**Fuera de la lista** (tambien usan la skybox por defecto, pendientes de
decidir): 2-S, Intermission1, Tutorial, Level P-1, Intro, Main Menu y
DebugRoom.

## v21.1.2 (pre-release) - arreglo real de los unlit negros, sin fullbright

**Causa**: las 25 escenas traen ambiente plano negro (`m_AmbientMode: 3`,
`m_AmbientSkyColor: 0,0,0`) y los 66 shaders unlit multiplicaban el color de
vertice por `UNITY_LIGHTMODEL_AMBIENT`. Los vertexlit usan
`ShadeVertexLightsFull`, que tambien parte del ambiente: por eso la prueba
v21.1.1 (ambiente blanco en runtime) arreglaba los enemigos pero dejaba el
mundo en fullbright.

**Arreglo**: fuera el factor de ambiente **solo** de la linea de color de los
66 shaders unlit (`psx_unlit_*` con sufijos `_0.._3` y
`ultrakill-unlit*`/`psx-unlit-*` de `Assets/shaders`):
`o.color = v.color*UNITY_LIGHTMODEL_AMBIENT` -> `o.color = v.color`.
La familia `-ambient` se unifica con la unlit normal (antes, con ambiente
negro, daba un gris plano 0.5). No se toca la linea del warping
(`UNITY_LIGHTMODEL_AMBIENT.a`), igual que en v21.1. `WebGLRenderFix` vuelve a
no tocar el ambiente y `MapLoader.WebGLBundleQuery` sube a `?v=211`.

**Servidores**: `serve-webgl.ps1`, `serve-lan.ps1` y el `servidor.ps1` del
paquete mandan `no-store` tambien para `.js/.wasm/.data/.unityweb/.json`, para
que un ZIP nuevo no arranque con el player viejo cacheado.

**Build**: bundles (act-0/act-1/prime) con 0 `Shader error in` y 0 `error CS`.
Player `BUILD RESULT: Succeeded | errors=0 | warnings=27 | sizeMB=466,0`, 0
errores. Los bundles dentro del player coinciden (SHA256) con los recien
construidos.

**Revision**: STRAWBERRY (cobertura exacta de los 66, familia `-ambient`,
alpha) y TOWER (vertexlit/reflectivos intactos, revert completo; corrigio
finales de linea y el cache de los servidores). **Pendiente visual del
usuario**: 1-2 (Streetcleaner), alas de Gabriel, lasers/Bonus, grietas y agua.

**Fuera de alcance**: 74 materiales con shader dummy (Water 3/4/5, railgun,
puertas...). (La cola de los `psx_unlit_transparent_*` se arreglo despues, en la
rama `risky`.)

## v21.1.1 (pre-release) - prueba: ambiente blanco en runtime

**Hipotesis**: los enemigos/props negros (p. ej. el Streetcleaner de 1-2) usan
materiales unlit (`psx_unlit_*`, `ultrakill-unlit*`) que multiplican el color
por `UNITY_LIGHTMODEL_AMBIENT`. Las 25 escenas traen ambiente plano negro
(`m_AmbientMode: 3`, `m_AmbientSkyColor: 0,0,0`), asi que ese factor es 0 y el
objeto sale completamente negro.

**Cambio**: `WebGLRenderFix.ApplyAmbient()` fuerza `AmbientMode.Flat`,
`ambientLight = Color.white` y `ambientIntensity = 1` en `Init()` y despues de
cada carga de escena (0.1/0.75/2 s). Solo se toco C#: **no** hay cambios en
shaders, materiales ni bundles; los bundles son los de v21.1.

**Riesgo esperado**: los shaders vertexlit usan `ShadeVertexLightsFull`, que
tambien parte del ambiente, asi que el mundo puede verse mas claro o lavado.
Es una prueba: si el resultado no convence, se revierte el archivo y se
reconstruye el player.

**Build**: `BUILD RESULT: Succeeded | errors=0 | warnings=27 | sizeMB=466,0`,
0 `Shader error in` y 0 `error CS` en el log del player.

**Pendiente**: validacion visual del usuario en 1-2 (Streetcleaners) y del
aspecto general del mundo.

## v21 (pre-release) — agua/cristal/sangre/grietas: shaders transparentes reparados

**Síntoma**: el agua de 1-2 (y cristales, sangre, grietas) salía rosa y negra.

**Causa**: en los shaders exportados el header del `Pass` estaba mal formado:

```
Pass Blend SrcAlpha OneMinusSrcAlpha
ZWrite Off
ZWrite Off
{
```

Unity **no da error**: compila el shader a NADA (el asset de 132 bytes que hay
en los bundles) y el material se dibuja con el shader de error. Afectaba a 10
shaders (`psx_vertexlit_alphatest`, `psx_vertexlit_transparent_transparent`,
`psx_vertexlit_transparent_zwrite` + `_0/_1/_2`, `ultrakill-vertexlit-alphatest`
y los `ultrakill-*-transparent-*`) y a los materiales que los usan (`Water`,
`Water 1`, `Blood`, `forgiveme`, `Glass`, `GlassFake`, `crack`, `crack 1`,
`StainedGlass1A 10-13`, `ExplosionShockwave`, ...).

**Arreglo** (en el proyecto Unity, sin parchear el build a mano):

1. `Pass {` + `Blend SrcAlpha OneMinusSrcAlpha` + un solo `ZWrite Off`
   + `Lighting On`.
2. `Tags { "Queue"="Transparent" "RenderType"="Transparent" }` en los 8
   transparentes: estaban en cola *Geometry* con `ZWrite Off`, así que el suelo
   opaco se dibujaba después y tapaba el agua.
3. `MapLoader.WebGLBundleQuery` → `?v=209` (cache-busting).
4. Rebuild: `WebGLBundleBuilder.BuildAll` y después `WebGLBuilder.Build`.

**Verificación sobre el bundle construido**:

- 0 `Shader error in` y 0 `error CS` en los dos logs.
- `Water` (Level 1-3) → shader **8.972 bytes** (antes 132) y con `_MainTex`
  (el material de 576 B la había perdido).
- Ningún shader de 132 B en el bundle; el más pequeño mide 3.608 B.
- `BUILD RESULT: Succeeded | errors=0 | warnings=27 | sizeMB=464,2`.

**Entorno**: Unity 2019.4.16f1 instalado en `C:\Users\Damian\Unity\2019.4.16f1`
(licencia Personal activa). Los scripts `serve-webgl.ps1` / `serve-lan.ps1` ya
no llevan la ruta de la máquina anterior: usan `$PSScriptRoot`.

**Pendiente**: probar en navegador (nivel 1-2) y afinar `ZWrite`/alpha si el
agua no queda como debe.

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
- Para los stubs **no hay fuente pública conocida**: los repos de terceros que
  hemos comprobado (y el `genizy`, que resultó ser un ZIP de juegos web ajenos)
  solo traen stubs, y el build no conserva los snippets. O se escribe el shader a
  mano, o se reexporta un build antiguo donde AssetRipper sí pudiera decompilar.

## Rama `shadertest` — shaders funcionales en lugar de los stubs

**Qué se ha hecho** (fases 1-4; el playtest lo hace el usuario):

- Inventario: **61 stubs** de AssetRipper (`DummyShaderTextExporter`), de los
  cuales **40 los usan 84 materiales**; los otros 21 no los usa nadie.
- Para cada stub usado se sacó del **CAB original** (`m_ParsedForm`) la lista de
  propiedades, la cola y el estado de render (blend, ZWrite, Cull, ZTest). No se
  ha adivinado nada: p. ej. `psx/water` es `SrcAlpha OneMinusSrcAlpha`, `ZWrite
  On`, `Cull Off`, cola `Transparent`; `ULTRAKILL/bloodstain` es
  `SrcAlpha SrcColor`, `ZWrite Off`, cola `Transparent-1`.
- **13 shaders reescritos** (35 ficheros `.shader`, uno por cada GUID duplicado)
  conservando su `.meta`, así que los materiales siguen apuntando al mismo
  asset: `psx/water`, `psx/railgun`, `psx/heartbeat`,
  `psx/vertexlit-noambient-emissive`, `ULTRAKILL/bloodstain`, `ULTRAKILL/Invert`,
  `ULTRAKILL/Main_PostProcess`, `ProBuilder/Standard Vertex Color`,
  `ProBuilder/UnlitVertexColor`, los tres `Hidden/ProBuilder/*Picker` y
  `Unlit/Volumetric`.
- `Hidden/ProBuilder/HideVertices` **no se ha tocado**: ya estaba arreglado
  (`ColorMask 0`) en la sesión anterior; es el shader de los `InvisibleFace`.
- Herramientas nuevas en `tools/shaders/`: `inventory.py`, `cab_extract.py`,
  `generate.py` (fuente de los shaders), `apply2.py` (los copia conservando
  GUID) y `verify_bundle.py` (los comprueba dentro del bundle construido).

**Verificación**:

- Bundles: **0 `Shader error in`, 0 `error CS`**. Compilan con blob real (water
  5,9 KB, railgun 5,8 KB, heartbeat 4,6 KB, bloodstain 5,7 KB); antes eran stubs
  que solo muestreaban la textura.
- Materiales que los usan dentro del bundle: water 3 (act-1), railgun 3-4 (los
  tres acts), heartbeat 2 (act-1), bloodstain 1 por acto, ProBuilder 1 por acto,
  Invert 1 por acto, postprocess 2 por acto.
- Player: `BUILD RESULT: Succeeded | errors=0 | warnings=31 | sizeMB=491,8`, 0
  `Shader error in`.
- `MapLoader.WebGLBundleQuery` → `?v=215`.

**Iteración 2 (v215)**:

- **Sangre**: los decals salían como rectángulos negros. El CAB pide
  `SrcAlpha SrcColor` y el material trae `_Color` rojo: donde la textura tiene
  alfa 0 (`Bloodsplatter1.png` es rojo puro con alfa variable) el blend
  multiplicaba el fondo por (0.82, 0, 0), dejándolo casi negro. Además el CAB
  trae `Lighting Off` y estaba iluminada por vértices, así que solo se veía de
  cerca.
- **Carne** (`psx/heartbeat`): primer intento de animación (vertex warping por
  normales + distorsión de textura).

**Iteración 3 (v216, tras el segundo playtest)**:

- **Sangre**: el blend alfa la dejaba opaca y saturada. Vuelve al blend del CAB
  (`SrcAlpha SrcColor`) pero **premultiplicando el tinte**: el fragment devuelve
  `lerp(1, tex*_Color, alfa)` con alfa 0, así el fondo se multiplica por 1 donde
  no hay sangre (nada de rectángulos negros) y se tiñe de rojo donde la hay, sin
  taparlo. `Offset -1,-1` para el z-fighting del decal.
- **Corazón**: el vertex warping por normales **abría la malla** (vértices
  partidos) y el shader quedaba fullbright. Ahora va iluminado
  (`ShadeVertexLightsFull`) y la animación es una escala mínima (0,6 %) más una
  distorsión de UV de 0,0015.
- **Paredes de carne de Gluttony**: no usan `psx/heartbeat` sino
  `psx/vertexlit/radialdistortion` (`Flesh*`, `FleshPulsating*`), que en el port
  era un vertexlit plano sin animación. Se le añade la distorsión radial que
  respira (UV) y una ondulación de vértices continua de 0,01 (misma posición →
  mismo desplazamiento, no se abren costuras).

**Iteración 4 (v217, tras el tercer playtest)**:

- **Focos de Minos**: `MinosBoss.prefab` (y sus variantes) lleva 4 luces de
  rango 500 por instancia. No hay sombras, pero **el vertex lighting solo usa 4
  luces por vértice**: al acercarse Minos, sus focos desplazaban a las luces del
  nivel y la zona cambiaba de brillo. Se apagan esas luces (`m_Enabled: 0`); las
  de rango 30 (aura del jefe) se quedan. El haz en sí es `Blend One One`
  (aditivo) sobre sus propios píxeles: no ilumina otros objetos.
- **Carne**: se quita el movimiento de textura (la distorsión radial se veía
  como "agua"); queda solo la ondulación de vértices.

**Iteración 5 (v218)**: el aura de Minos (2 luces de rango 30 e intensidad 25
por instancia) seguía iluminando el terreno, así que se apagan también. Ahora
`MinosBoss.prefab` y sus variantes tienen **0 luces activas**; lo único que
puede tocar el terreno es el haz aditivo (`_OpacScale` global = 0,1), que solo
suma en sus propios píxeles.

**Límites conocidos** (funcional, no fiel):

- El agua es la textura con scroll y alpha; **sin refracción**.
- La animación de la carne es una aproximación procedural, no el ruido del
  original.
- `Main_PostProcess` es un pass-through; el port tiene el post-proceso
  desactivado, así que no se ve.
- Los 17 materiales de `psx/vertexlit-noambient-emissive` (puertas, drones,
  Minos de `Assets/materials` y `models/`) no aparecen en ningún bundle ni en el
  player: son duplicados sin usar. Las puertas reales usan
  `psx/vertexlit/emissive`.

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

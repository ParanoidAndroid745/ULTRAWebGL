# HANDOFF — ULTRAKILL WebGL port

Estado al 2026-10-20 ~20:30. Leer esto antes de continuar.

## 📦 Repo: https://github.com/ParanoidAndroid745/ULTRAWebGL (privada)

7.465 ficheros, 35 MB. Versiona **nuestro trabajo**: los scripts parcheados, los
107 shaders reales, los materiales, la página web, las herramientas y la doc.
**NO** versiona los assets binarios del juego (~3 GB) ni los builds → se
regeneran con AssetRipper (ver `REGENERAR-ASSETS.md` en la raíz del repo).

- `git push` con la credencial de `gh` (HTTPS, keyring de Windows).
- El README conserva el descargo legal original y añade debajo la sección del
  proyecto.
- `git` y `gh` instalados vía winget; si el PATH no los tiene, están en
  `C:\Program Files\Git\cmd\git.exe` y `C:\Program Files\GitHub CLI\gh.exe`.

## ✅ VERIFICACIÓN DE v20.5 HECHA POR MÍ (no depende del usuario)

Con el arnés visual de Playwright (`ultrakill-webport/tools/visual/ukshot.py`):

| Prueba | Resultado |
|---|---|
| El juego arranca y renderiza | ✓ captura del Tutorial con el vertex lighting correcto, 0 errores de consola |
| **Menú de pausa con Escape** | ✓ se abre y el cursor pasa a `default` (visible) |
| **Pulsar un botón de la pausa (OPTIONS)** | ✓ abre el panel de OPTIONS — **82,75 % de píxeles cambian**. El bug "no puedo seleccionar nada" **está corregido** |
| **Preset PSX vs PC** | ✓ **14,42 % de píxeles distintos** → los ajustes de gráficos **sí cambian la imagen** |
| Control (PSX vs PSX) | **0,00 % — imágenes idénticas** → la cámara está quieta, la diferencia es atribuible al ajuste |
| Pointer lock | ✓ concedido (`LOCKED`) con eventos *trusted* |
| Consola | 0 errores en todas las pruebas |

**Limitación conocida**: `SendMessage` con valor numérico falla (Unity lo pasa como
string) → usar los presets sin argumento (`send:Canvas,PCPreset`).

## 🧰 Herramientas del proyecto (creadas 2026-10-20 18:00-19:20)

| Qué | Dónde | Para qué |
|---|---|---|
| **Arnés visual (Playwright)** | `ultrakill-webport/tools/visual/ukshot.py` | Capturas del juego (página completa), teclado/ratón *trusted* (pointer lock real), diff de píxeles. Python 3.12 + playwright + pillow ya instalados. |
| **Skill de proyecto** | `.opencode/skills/ultrakill-port/` (SKILL.md + references/ + scripts/) | Memoria dura: trampas de shaders/Unity en WebGL, pipeline, reglas. Se carga sola con `skill` (id `ultrakill-port`). |
| **Índice de assets** | `.opencode/skills/ultrakill-port/scripts/asset-index.ps1` (copia en `ultrakill-webport/tools/assetindex/`) | GUIDs ↔ rutas, materiales→shader,shader→materiales, escenas→nivel real, uniforms→shaders. Índice cacheado en `ultrakill-webport/tools/assetindex/index/` (5 MB, 26.552 GUIDs, 60.357 referencias inversas, ~5 min de build). |
| **Comandos** | `.opencode/commands/{build,shots,idx}.md` | `/build` (rebuild + verificación por log), `/shots` (capturas + diff), `/idx` (consultar el índice). |
| **Arnés CDP** | `%TEMP%\opencode\cdp_lib.ps1` | Respaldo del anterior (`Shot` vía `Page.captureScreenshot`). |
| Sondas | `%TEMP%\opencode\probe.js` (píxeles), `%TEMP%\opencode\ShaderUniformProbe.cs` (compilación en Unity) | Métricas y sondas de shaders. |
| ffmpeg | `%LOCALAPPDATA%\ms-playwright\ffmpeg-1013\ffmpeg-win64.exe` (viene con Playwright) | Extraer fotogramas de MP4. |

**Validación cruzada del índice** (coincide con mediciones independientes):
85 materiales usan `psx/vertexlit/vertexlit` · 36 usan `psx/unlit/ambient` ·
84 materiales con shader dummy · 61 de 168 shaders dummy · 11 nombres duplicados.

## ✅ v20.5 — ESTADO ACTUAL (2026-10-20 17:45). Ajustes de gráficos + atajos de teclado.

| Qué | Estado |
|---|---|
| Player | **v20.5, 17:40** — `Succeeded | errors=0 | warnings=27 | 487,5 MB`, **0 errores de shader**, 0 errores CS |
| Bundles | act-0 118,5 MB · act-1 113,8 MB · prime 34,3 MB (17:31-17:37, 0 errores de shader) |
| Página | hash `D069569CE0FF` (con el fix de teclado) |
| ZIP | `dist/ULTRAKILL-WebGL-v20.5.zip` (262,1 MB, 28 entradas, verificado) |
| Enlaces | `http://localhost:8000/?v=205` · `http://192.168.0.17:8010/` |
| Fallback | `dist/ULTRAKILL-WebGL-v20.3.zip` (sin los ajustes de gráficos) |

### 🔬 HALLAZGO TÉCNICO IMPORTANTE: en WebGL los uniforms numéricos de `Properties` no existen
Symptoma: `Shader error in 'psx/unlit/ambient': undeclared identifier '_ResY' at line 47 (on gles)`.
**Solo aparece al compilar para WebGL** (el mismo shader compila sin errores en el editor).
Causa: el conversor HLSL→GLSL de Unity (**HLSLcc**) **no declara en la etapa de vertex los
uniforms numéricos del cbuffer `UnityPerMaterial`** (los que salen de un bloque `Properties`).
Los *samplers* (texturas) sí se declaran, y por eso `_MainTex` nunca ha dado problemas.

**Workaround (el que está aplicado):** declarar el uniform **suelto**, fuera de `Properties`:
```hlsl
uniform float _ResY;          // en vez de _ResY("Resolution", Float) = 720
uniform float _ColorPrecision;
```
El C# sigue usando `Shader.SetGlobalFloat("_ResY", v)` (los globales de Unity son uniforms
sueltos, así que el valor llega igual ✓). Ojo: `_ColorPrecision` pasó de `SetGlobalInt` a
`SetGlobalFloat` (un uniform `float` no lee bien un int).

**Cómo se averiguó (para no repetirlo):** sonda de editor `ShaderUniformProbe.cs`
(7 variantes mínimas compiladas con `ShaderUtil.GetShaderMessages`) + el shader real copiado.
Las 7 compilan en el editor → el fallo es **exclusivo de la plataforma GLES**.
Scripts de la sonda: `%TEMP%\opencode\ShaderUniformProbe.cs` (NO está en el proyecto:
se borró para que no entrase en el build).

### ⚠️ Trampa: no te fíes del log viejo
Durante varias iteraciones estuve leyendo errores de un log **obsoleto** (el nombre del log
se reutiliza y el mtime no se comprobó). **Siempre: mira el mtime del log y la hora actual.**
Y ojo: **matar Unity a mitad de build deja el `Library` inconsistente** (los scripts de
diagnóstico de `Assets\Editor` dejaron de compilar: `TeleportPlayer`/`PlayerPosInfo` no se
encontraban). Se arregla borrando esos scripts de un solo uso (ya borrados:
`WebGLFloorProbe.cs`, `WebGLSceneDump.cs`, `LightingProbe.cs`, `PortDiag2.cs`).

### 🧪 Verificar que los ajustes hacen algo (sin screenshots)
El `GraphicsOptions` está en el GameObject **"Canvas"**:
```js
unityInstance.SendMessage('Canvas', 'Pixelization', 0);   // sin pixelizar (preset PC)
unityInstance.SendMessage('Canvas', 'Pixelization', 3);   // 360 -> rejilla 640x360 (PSX)
unityInstance.SendMessage('Canvas', 'PSXPreset');
unityInstance.SendMessage('Canvas', 'PCPreset');
```
Con la sonda de píxeles se mide antes/después. **Ojo**: la cámara del intro del Tutorial
se mueve, así que hay que comparar **rápido** (A/B/A) o esperar a que termine la intro;
si no, la diferencia se pierde en el ruido de la cámara.

## 🔧 v20.4 / v20.5 — "los ajustes de gráficos no hacen nada" + "los atajos de Chrome se cuelan"

### ⚠️ Sobre el análisis externo que se nos pasó
Vino un análisis con 5 "causas raíz" muy bien documentado. **Al verificarlo, 7 de sus
afirmaciones eran incorrectas** (la tabla completa está en este mismo fichero, abajo).
Lo que SÍ era cierto: la causa raíz real del bug de gráficos. Se resume:

| Afirmación del análisis | Veredicto |
|---|---|
| "los 61 ficheros de `Assets\shaders\**` no los referencia nadie" | **FALSO**: esa carpeta tiene 44 ficheros, de los que **718 materiales** los referencian (335 en uso). El 61 era el nº de shaders *dummy* de todo el proyecto |
| "los 1.897 materiales con shader real apuntan a los 107 de `Assets\Shader\**`" | **FALSO**: 1.210 apuntan a `Assets\Shader\**` y **687 a `Assets\shaders\**` |
| "no hay ningún listener que llame a esos métodos" | **FALSO**: hay 6 *persistent calls* en `Canvas.prefab` y 4 en `Tutorial.unity` |
| "`Shader.Find("psx/...")` reasigna shaders en runtime" | **FALSO**: **cero** `Shader.Find` en todo el proyecto |
| "los nombres duplicados rompen el render (magenta)" | **FALSO**: los materiales enlazan por GUID y no hay `Shader.Find` → impacto nulo |
| "ApplyCameras no desactiva el handler" | **FALSO**: sí lo desactiva (`WebGLRenderFix.cs` L387-415) |
| "los presets solo cambian la etiqueta" | **CIERTO**, y además `Initialize()` sí aplicaba los valores al arrancar (lo omitted el análisis) |

### ✅ Causa raíz REAL (medida, no estimada)
- **Ningún shader de los 168 leía los uniforms de los ajustes**: `_ResY` → **0**,
  `_ColorPrecision` → **0**, `_TextureWarping` → 1 (solo declarado, sin usar),
  `_VertexWarping` → 1 (idem). El pixelado estaba **hardcodeado** en 107 shaders
  (`floor(640 * vertex.x) / 640`) y lo cambiaba el agente generador a ojo (160/120
  en 14 shaders → rejilla **inconsistente** entre objetos).
- Los presets `PSXPreset()`/`PCPreset()` **no aplicaban** los valores (asignar
  `Dropdown.value` no dispara `onValueChanged`).
- En WebGL el post-proceso está **anulado** (`WebGLRenderFix.ApplyCameras` desactiva
  el handler y la "Virtual Camera") y su shader es un **dummy** → sin post-proceso
  no hay pixelización, warping, compresión de color, gradiente ni viñeta.

### 🔧 Arreglos aplicados
1. **v20.4 — Atajos del navegador (Ctrl+D, Ctrl+T, Ctrl+W)**
   - `navigator.keyboard.lock()` **solo se concede con un gesto del usuario**
     (transient activation) y antes solo se pedía dentro de `if (isFs())` →
     **fuera de pantalla completa el teclado nunca se bloqueaba**.
   - Ahora se pide en **cada gesto real** (`keydown`, `mousedown`, `pointerdown`,
     `touchstart`), sin la condición de fullscreen, y con reintento cada 600 ms
     mientras haya un gesto en los últimos 5 s.
   - `preventDefault()` solo en teclas **sin** Ctrl/Meta/Alt (no puede anular los
     atajos de nivel de navegador, pero sí evita el scroll y el F3 del navegador).
   - `ControlsOptions.OnGUI()`: con Ctrl/Alt/Meta **no se reasigna ninguna tecla**
     (antes un Ctrl+D durante un rebinding abría los marcadores de Chrome *y*
     guardaba la `D` como tecla del juego).
2. **v20.4 — Presets que no aplicaban**: `PCPreset()`/`PSXPreset()` ahora llaman a
   los 4 métodos de aplicación y a `PlayerPrefs.Save()`.
3. **v20.4 — PlayerPrefs en WebGL**: `PlayerPrefs.Save()` tras cada ajuste (en WebGL
   no se llama a `OnApplicationQuit`, así que no se guardaba solo).
4. **v20.4 — Rejilla unificada**: los 14 shaders que tenían `160/120` pasan a
   `640/360` (el valor dominante) → el mundo deja de mezclar dos rejillas.
5. **v20.5 — Ajustes cableados a los shaders**: los **107** shaders reales declaran
   `_ResY` y `_ColorPrecision` en `Properties` (para que `Shader.SetGlobalFloat`
   los aplique por encima del valor del material, que es como funciona el juego
   original) y los **usan**: `_ResY` dirige la rejilla del vertex snapping
   (`_ResY = 0` → **sin rejilla → el juego deja de verse "PSX"**) y `_ColorPrecision`
   cuantiza los niveles de color en el fragment.

### 🧪 Cómo verificar que los ajustes hacen algo (sin screenshots)
El `GraphicsOptions` vive en el GameObject **"Canvas"**, así que se puede cambiar
desde la página:
```js
unityInstance.SendMessage('Canvas', 'Pixelization', 0);   // sin pixelizar
unityInstance.SendMessage('Canvas', 'PSXPreset');
unityInstance.SendMessage('Canvas', 'PCPreset');
```
Con la sonda de píxeles (`window.__probe`, ver más abajo) se compara el antes y el
después: la rejilla de 360 px vs 0 px cambia el `avg` y el nº de colores distintos.

### 🔬 Sonda de píxeles (infraestructura de verificación)
Este entorno **no permite capturas** (la ventana del escritorio está oculta) ni
*pointer lock* real. Se instaló una sonda que engancha `requestAnimationFrame` y
lee el framebuffer con `gl.readPixels` **dentro** del rAF (Unity crea el contexto
con `preserveDrawingBuffer:false`, así que fuera del rAF el buffer está vacío):
```js
// pegar en la consola del navegador; luego leer window.__probe
{ frames, reads, avg:[r,g,b], nonBlackPct, blackPct, distinct, history:[] }
```
Interpretación: `nonBlackPct` ~95-98 % = el mundo se está dibujando; ~50 % o menos
= pantalla negra o casi. `distinct` = nº de colores distintos (un mundo plano da
muy pocos). **Baseline v20.3**: avg (50,29,17), nonBlack 96,8 %, distinct 343.

### ⏳ Lo que queda pendiente
- **Post-proceso real** (el为何 de la "calidad mala"): falta el shader
  `ULTRAKILL/Main_PostProcess` original (consume `_ResY`, `_TextureWarping`,
  `_VertexWarping`, `_ColorPrecision` + gradiente y viñeta) **y** reactivar la
  cadena de RenderTexture en WebGL (Main Camera → RT → quad "Standard" (Main VSRM)
  → pantalla; HUD Camera → RT → quad "StandardHud" (HUD VSRM)). **Pendiente de
  hacer**: es la parte arriesgada (la cadena con RT ya falló una vez en WebGL) y
  aquí no se puede ver el resultado.
- **Texture warping y vertex warping** siguen sin efecto: solo los consumía el
  post-proceso. `_ResY` (pixelización) y `_ColorPrecision` (compresión) ya funcionan.
- 61 de 168 shaders siguen dummy (efectos: latido, railgun, agua, UI, sangre).
- 11 nombres de shader duplicados (43 ficheros): ruido de AssetRipper, impacto nulo
  (los materiales enlazan por GUID), pero el build puede emitir avisos.

## ✅ v20.3 — ESTADO ACTUAL (2026-10-20 15:05). Relevo de agente.

**Es exactamente lo que está sirviéndose ahora mismo**, verificado por hash:

| Qué | Estado |
|---|---|
| Player (wasm + data) | **v20.2, 04:03-04:40** — con los shaders arreglados |
| Bundles act-0 / act-1 / prime | **04:35-04:38** — con los shaders arreglados |
| Página (`index.html`) | **07:16** — la del agente anterior, con un arreglo extra |
| Bundle query | `?v=202` (`MapLoader.WebGLBundleQuery`) |
| ZIP | `dist/ULTRAKILL-WebGL-v20.3.zip` (262,1 MB, 28 entradas, verificado) |
| Enlaces | `http://localhost:8000/?v=202` · `http://192.168.0.17:8010/` |

**Arreglo que aportó el agente anterior (07:16) y que está incorporado:**
```js
// OJO: tiene que estar en window, porque el boton lo llama desde su
// atributo onclick (fuera del IIFE). Con "var" aqui el ReferenceError
// hacia que el menu de niveles no hiciera nada.
window.UKDebug = { ... };
```
→ El **botón "NIVELES (F3)"** de la página ahora funciona (antes daba
`ReferenceError`; la **tecla** F3 sí funcionaba, por eso el bug se colaba).

**Servidores**: estaban **caídos** (se fueron con la sesión anterior). Relanzados
a las 15:04. Ojo con el quoting: `Start-Process powershell -ArgumentList ... '-Root', $root`
**parte la ruta en el espacio** de "Proyecto predeterminado" → el servidor no
arranca. Usar lanzadores `.ps1` intermedios (`%TEMP%\opencode\srv8000.ps1`).

**ZIP obsoletos**: movidos a `dist\obsoletos\` (v19.10, v19.12, v20.0, v20.1,
v20.2). El v20.1 del agente anterior tenía la página buena pero **el player y
los bundles viejos**.

### ⚠️ Pendiente de confirmar por el usuario (no verificable aquí)
El navegador automatizado de este entorno no concede *pointer lock* ni
pantalla completa reales, no entrega eventos de teclado (`browser.press` no
llega a la página) y no permite capturas (la ventana del escritorio está
oculta). Hay que confirmar en un navegador real:
1. Enemigos y orbe ya NO negros (shaders arreglados).
2. Menú de pausa seleccionable.
3. Ctrl+D / Ctrl+T bloqueados en pantalla completa.
4. Cámara 360° en 0-2…0-5.

## ✅ v20.2 — CERRADA (2026-10-20 ~04:45). Los 5 bugs de v20.1 corregidos.

- **Player**: `BUILD RESULT: Succeeded | errors=0 | warnings=27 | sizeMB=487,5`
  → **0 errores de shader** y **0 errores CS** (comprobado en los logs de los
  dos builds: `%TEMP%\opencode\build202\bundles.log` y `player.log`).
- **Bundles** (WebGL, con los shaders ya corregidos):
  `acts/act-0` 118,5 MB · `acts/act-1` 113,8 MB (Limbo+Lust+Gluttony) · `acts/prime` 34,3 MB.
- **Paquete**: `dist/ULTRAKILL-WebGL-v20.2.zip` (262,1 MB, 28 entradas),
  verificado extrayéndolo en limpio: los hashes de `index.html`, `act-0`,
  `act-1` y `prime` coinciden con el build.
- **Enlaces**: `http://localhost:8000/?v=202` (este PC) ·
  `http://192.168.0.17:8010/` (otro dispositivo). Los dos sirven la página con
  hash `E951016076C2` (la nueva) y el act-0 de 118,5 MB.
- **Verificado en navegador**: la página arranca, el juego carga sin errores de
  consola, el menú de niveles (F3) abre con 21 botones en 4 grupos y al elegir
  uno llama a `SendMessage('MapLoader','DebugLoadLevel','Level 1-2')` con el
  toast "Cargando Level 1-2...".

### ⚠️ Lo que NO se ha podido verificar aquí
El navegador automatizado de este entorno no concede *pointer lock* ni
pantalla completa reales, no entrega eventos de teclado (`browser.press` no
llega a la página) y no permite capturas (la ventana del escritorio está
oculta). Por eso **la pausa, el giro 360° y el aspecto visual de los enemigos
los tiene que confirmar el usuario en su navegador real**.

## 🔧 v20.2 — REVISIÓN DE v20.1: 5 bugs corregidos (2026-10-20 ~03:30-04:00)

Revisión hecha sobre la build v20.1 del agente anterior (player + bundles nuevos).
Los 5 fallos que reportó el usuario **no estaban en el juego**: estaban en el
propio port. Causas y arreglo:

### 1. 🐛 ENEMIGOS NEGROS + ORBE AZUL ROTO — TRES FALLOS ENCADENADOS EN LOS SHADERS
Eran tres bugs distintos del script que genera los shaders. Todos rompían el
aspecto **sin dar error de compilación**, por eso el build "era verde":

**(a) El bloque `Properties` salía mal formado.** El script generador
(`%TEMP%\opencode\gen_variants*.ps1`) hacía `Substring(0, $i + 10)` sobre
`"Properties{"`, que tiene **11** caracteres → se comía la llave:
```shaderlab
Properties_AmbientColor("Ambient Color", Color) = (0.5,0.5,0.5,1)
    {
    _MainTex("Base (RGB)", 2D) = "white" {}
}
```
ShaderLab **no da error**: ignora la directiva desconocida, el shader compila
**sin propiedades** (sin `_MainTex`) → la textura no se hace bind → **el objeto
sale NEGRO**. Afectaba a **394 materiales**.
→ *Arreglo*: off-by-one corregido en los 3 scripts (`$i + 10` → `$i + 11`) y
normalización de los 107 shaders reales al formato canónico de ShaderLab
(`Properties {` en su propia línea y **una property por línea**; el parser de
ShaderLab es sensible a esto). Script: `%TEMP%\opencode\norm_properties.ps1`.

**(b) Properties que solo se usan en el VERTEX shader.** `_AmbientColor` se usa
en la línea 47 (vertex) y no en el fragment. **El backend GLSL de Unity (WebGL)
no declara esas uniforms** → error real:
`Shader error in 'psx/unlit/ambient': undeclared identifier '_AmbientColor' at line 47 (on gles)`.
→ Ningún material serializa `_AmbientColor` (0 de 28.584), así que su valor es
siempre el del shader (0.5,0.5,0.5,1) → se sustituye por un literal
`half4(0.5,0.5,0.5,1)`: idéntico visualmente y compila siempre.
Afectaba a 7 shaders. Barrido completo en `%TEMP%\opencode\scan2.ps1`:
solo `_AmbientColor` (las `_Cutoff` de 48 shaders están *declaradas pero sin
usar*, y Unity las strippea sin problema).

**(c) En los shaders rotos, Unity ni siquiera daba error** → el síntoma era
"objeto negro" y no "shader roto". Por eso hay que mirar los logs del build
(`Select-String "Shader error in"`) además del resultado.

**Explicación de "solo funcionaba la iluminación en el Tutorial":** el Tutorial
usa `psx/vertexlit/vertexlit` (sin properties extra → nunca se rompió). Los
enemigos usaban `psx/unlit/ambient` (con `_AmbientColor` → roto) y el orbe
`psx/transparent/unlit-noambient-nocull-fresnel` (sin `_MainTex` → roto).

### 2. 🐛 MENÚ DE PAUSA NO SE PODÍA USAR — el port re-bloqueaba el ratón
**Causa raíz:** parche propio del port en `CameraController.cs` (bloque `#if UNITY_WEBGL`):
```csharp
if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) {
    Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
```
Al pinchar un botón de la pausa, `OptionsManager.Pause()` había puesto
`Cursor.lockState = None` + `Cursor.visible = true` → esta línea volvía a bloquear
el ratón **y a ocultarlo** → el navegador congela el puntero → el EventSystem recibe
siempre la misma posición → el menú deja de responder ("no puedo seleccionar nada").
- **Arreglo:** la línea ahora solo se ejecuta si NO hay un menú abierto
  (`IsMenuOpen()` → `OptionsManager.paused || OptionsManager.mainMenu`).

### 3. 🐛 ATAJOS DE CHROME (Ctrl+D, Ctrl+T, Ctrl+W) — se perdía el bloqueo de teclado
**Causa raíz:** dos cosas.
- `requestFs()` hacía **dos** peticiones de fullscreen (la nativa + `unityInstance.SetFullscreen(1)`).
  La segunda hacía que el navegador *saliera y volviera a entrar* de fullscreen, y al
  salir Chrome **libera el Keyboard Lock** → los atajos volvían a funcionar.
- `onFullscreenChange()` mataba el bloqueo **para siempre**: comprobaba `wasLocked`
  **después** de poner `lockActive = false`, así que la condición nunca era cierta →
  nunca se re-entraba ni se re-bloqueaba.
- **Arreglo:** (a) `Element.prototype.requestFullscreen` ignora la petición si ya se
  está en fullscreen o hay una en curso; (b) al salir de fullscreen solo se abandona
  si la salida fue intencionada (`!desired || Escape pulsado largo`); (c) reintento
  periódico del bloqueo de teclado cada 1,2 s mientras se esté en fullscreen.

### 4. 🐛 MENÚ DE NIVELES (F3) no se veía en pantalla completa
El panel estaba dentro de `.webgl-content` (que está escalado con `transform`), así
que en fullscreen quedaba fuera de la vista.
- **Arreglo:** el panel es ahora hijo directo de `<body>` con `position: fixed`;
  además muestra un aviso (`toast`) al pedir un nivel, cierra solo al elegir, y
  con el menú abierto la página **no** captura el puntero (si no, los botones no
  respondían). Se llama a `MapLoader.DebugLoadLevel` (método nuevo, elige el bundle
  del acto según el nombre del nivel) en lugar de `SendMessage('Panel', ...)`, que
 Iamaba a ~196 objetos llamados "Panel".

### 5. 🐛 CACHÉ DE LOS BUNDLES
`MapLoader.WebGLBundleQuery` era `?v=20` → el navegador seguía sirviendo los bundles
viejos (el servidor manda `max-age=3600` para los que no son .html). Subido a `?v=202`.

### Limitación que sigue (documentada, no es un bug)
- El post-proceso (`ULTRAKILL/Main_PostProcess`) sigue siendo un shader dummy y el
  port **desactiva** la cadena de RenderTexture en WebGL (`WebGLRenderFix.ApplyCameras`).
  Por eso el aspecto es más plano que el del juego original (sin viñeta/gradiente).
  Arreglarlo exige el shader real (no está en el repo público de decompilación usado).
- 61 de 168 shaders siguen dummy (efectos secundarios: heartbeat, railgun, water,
  UI Extensions, bloodstain...). Afectan a efectos, no al aspecto del nivel.
- `FirstPersonInputModule` **se deja** con el parche `#if UNITY_WEBGL` (no toca
  `Cursor.lockState`): es lo que evita que el input de la UI destruya el bloqueo del
  ratón. Es seguro **gracias al arreglo nº 2**.

## ESTADO ACTUAL (2026-10-20) — v20.0 en curso: ILUMINACIÓN + ACTO 1 + MENÚ DEBUG

### 🎯 v20.0 — La iluminación (el "fullbright"): causa raíz y arreglo
- **Causa raíz: los 168 shaders del export son DUMMY.** AssetRipper no pudo
  exportar los shaders custom de ULTRAKILL (van compilados dentro de los
  bundles) y dejó un fichero "DummyShaderTextExporter" que **solo muestrea la
  textura**: sin `ShadeVertexLightsFull`, sin vertex snapping, sin mapeo afín,
  sin niebla → **el mundo se dibuja sin luz = "fullbright"**.
  - El shader principal es `psx/vertexlit/vertexlit` (lo usan `Wall.mat`,
    `Metal Floor 1.mat`, ...). El del post-proceso es `psx/vertexlit/cyber`.
  - Descartados: el ambiente es correcto (`Flat` negro, `m_AmbientMode: 3` =
    Flat) y el skybox no es la causa (el editor reporta
    `skybox=Default-Skybox, ambientMode=Flat, ambientLight=negro`).
- **Arreglo:** los shaders reales están en el repo público de decompilación
  **https://github.com/gabrieldj81/Ultrakill-Prelude-Decompilation/** (tiene
  `Assets/PSX/Shaders/*.shader` en ShaderLab real). Se copian **encima de los
  dummies conservando los `.meta` (GUID intactos)** → los materiales siguen
  apuntando al mismo asset y ahora tienen el código real.
  - Script: `%TEMP%\opencode\install_shaders*.ps1` (mapea por el nombre
    declarado `Shader "..."`; normaliza `psx/unlit/transparent/x` ↔
    `psx/transparent/x`).
  - Resultado: **44 de 168 shaders reales instalados** (los que importan:
    `psx/vertexlit/vertexlit`, `psx/vertexlit/cyber`, `psx/unlit/*`,
    `psx/transparent/*`, Outline, ProBuilder, p3d). Los 124 restantes son
    efectos secundarios (heartbeat, railgun, water, UI Extensions) que siguen
    como dummy.
  - Ese port **confirma de forma independiente** los dos diagnósticos: su
    PATCH1.2 dice "Fixes issue where camera in Level (0 2) all the way to
    (1 S) has a god damn stroke — by **disabling a script called 'First Person
    Input Module'**" y su PATCH1.4 "**Adds back the shaders of ultrakill**...
    the decompiled shaders were broken and needed to be replaced with an online
    equivalent".
  - Su fix de `FirstPersonInputModule` (forzar `Cursor.lockState = Locked` tras
    procesar el ratón) **rompe la tienda** (su PATCH1.3). El nuestro es mejor:
    el parche a nivel de página (v19.12) conserva el bloqueo y solo lo suelta
    si el juego muestra un menú (`Cursor.visible = true`).
- **Pendiente:** rebuild de los bundles (act-0 con los shaders nuevos) y del
  player, verificación visual y empaquetado.

### ✅ v20.0 — CERRADO (2026-10-20 ~02:40)
- **Player v20.0**: `BUILD RESULT: Succeeded | errors=0 | warnings=27 | sizeMB=488,0`
  (incluye los shaders reales y `MapLoader.WebGLBundleQuery = "?v=20"` para que
  el navegador no sirva los bundles viejos).
- **Bundles reconstruidos** (WebGL, con los shaders reales dentro):
  `acts/act-0` 118,7 MB · `acts/act-1` 113,9 MB (Limbo+Lust+Gluttony) ·
  `acts/prime` 34,3 MB (P-1). Copiados a `Assets/StreamingAssets/acts` y a
  `Builds/WebGL/StreamingAssets/acts`.
- **Iluminación verificada visualmente** (captura del Tutorial con Chrome real
  vía CDP): ahora se ve el aspecto real del juego (vertex lighting, color
  cálido, niebla, contraste) frente al gris plano anterior.
- **Menú de niveles** en la página: botón "NIVELES (F3)" / tecla F3, salta a
  cualquier nivel con `SendMessage('Panel','RanklessNextLevel', escena)`.
- **Paquete:** `dist/ULTRAKILL-WebGL-v20.0.zip` (262,4 MB, 28 entradas, 16 s),
  verificado extrayéndolo en limpio (contiene Build, StreamingAssets con los
  tres bundles, index.html con el menú, LEEME, JUGAR.bat, servidor.ps1).
- Se borró la copia de trabajo del proyecto (`export/ExportedProject_act1`).

### 🔎 Pendiente / limitaciones conocidas de v20.0 → **resuelto en v20.1**
_(v20.0这些问题: iluminación solo en el Tutorial y de mala calidad, enemigos
negros, pausa sin poder pinchar, debug menu que no hace nada, atajos de Chrome
vuelven, cheat menu abierto al entrar. Causas y arreglo en la sección v20.1
de más abajo.)_


### 🗺️ v20.0 — act-1 (Limbo + Lust + Gluttony) + prime
- **Verificado con los datos (manifest originales):** `act-0` = 0-1..0-5,0-S
  (Prelude) · `act-1` = 1-1..1-4,1-S + 2-1..2-4,2-S + 3-1,3-2
  (Limbo+Lust+Gluttony) · `prime` = P-1. **No existen act-2+** en esta versión
  (no hay capas Greed/Wrath/Heresy).
- **Las escenas de AssetRipper están PERMUTADAS** (como act-0). Método fiable:
  el título de misión aparece en el YAML. Validado contra act-0 (mapeo correcto)
  y aplicado a act-1:
  | destino | origen (fichero exportado) | título |
  |---|---|---|
  | Level 1-1 | Level 2-3 | HEART OF THE SUNRISE |
  | Level 1-2 | Level 1-1 | THE BURNING WORLD |
  | Level 1-3 | Level 1-2 | HALLS OF SACRED REMAINS |
  | Level 2-1 | Level 1-4 | BRIDGEBURNER |
  | Level 2-2 | Level 2-1 | DEATH AT 20,000 VOLTS |
  | Level 2-3 | Level 1-3 | SHEER HEART ATTACK |
  | Level 2-4 | Level 2-2 | COURT OF THE CORPSE KING |
  | Level 3-1 | Level 1-S | BELLY OF THE BEAST |
  | Level 3-2 | Level 2-4 | IN THE FLESH |
  | Level 1-S | Level 3-1 | (secreto) |
  | Level 2-S | Level 2-S | (secreto) |
  | Level 3-S | Level 3-2 | (secreto) |
  (script: `%TEMP%\opencode\rename_act1b.ps1`, dos fases)
- `WebGLBundleBuilder.BuildAct1()` añadido (construye `acts/act-1` y
  `acts/prime`); `BuildAll()` ya incluía los tres actos.
- **Limitación conocida:** `FinalRank.RanklessNextLevel` carga el bundle del
  acto **de la escena actual** (`StockMapInfo.nextAct`), así que para saltar a
  un nivel de otro acto hay que estar en un nivel de ese acto.

### 🛠️ v20.0 — Menú de niveles (debug) en la página
- Botón **"NIVELES (F3)"** en la barra de la página (o tecla **F3**): lista de
  todos los niveles (Tutorial, 0-1..0-S, 1-1..3-S, P-1) que llama a
  `unityInstance.SendMessage('Panel','RanklessNextLevel','Level X-Y')`.
- No requiere recompilar Unity (es JS de la página).

### Utilidades de esta sesión (en `%TEMP%\opencode\`)
- `install_shaders*.ps1` — shaders reales sobre los dummies.
- `rename_act1b.ps1` — renombrado de las escenas de act-1.
- `check_scene_integrity.ps1` — % de referencias rotas por escena (act-1/2/3
  intactas: 0,1 %).
- `proof_names.ps1` — manifest de los bundles + cadena `nextAct`.
- `LightingProbe.cs` (en `Assets/Editor/`) — renders comparativos en el editor
  (Main directo vs post-proceso vs variantes de ambiente) + brillo medio.

### Estado anterior (v19.12) — cerrado y verificado por el usuario

### ✅ v19.12 — CIERRE DE VERSIÓN (candidata final)
- **Composición de la versión:** binario **v19** (player + bundle act-0, sin
  cambios) + **página v19.12** (todo el arreglo es de la página, sin recompilar).
- **Qué incluye la página v19.12:**
  1. Parche de `document.exitPointerLock()`: el ratón solo se suelta de verdad
     cuando el juego abre un menú (cursor visible); en partida el bloqueo se
     conserva (neutraliza `FirstPersonInputModule`).
  2. Petición de bloqueo en cada tecla (**incluida Esc**) y cada clic.
  3. Reintento periódico (500 ms, espera creciente hasta 2 s) mientras el juego
     esté en partida sin bloqueo → recuperación automática.
  4. Arreglo del bucle de re-entrada a pantalla completa (`wasLocked`).
  5. Servidores con `Cache-Control: no-store` para `.html` (nunca una página vieja).
  6. Pie de página con el estado real: "Ratón bloqueado" / "Ratón sin bloqueo".
- **Estado de la verificación:** ✅ **CONFIRMADO POR EL USUARIO en su navegador**
  (recarga + 0-2..0-5: la cámara gira 360° sin saltos; el menú de pausa sigue
  usándose con el ratón). Unit test del parche OK (3/3). El test end-to-end
  automático no se pudo completar porque, con el usuario usando el PC, Chrome de
  pruebas no consigue el foco (`WrongDocumentError`).
- **Paquete final:** `dist/ULTRAKILL-WebGL-v19.12.zip` (136,9 MB, 24 entradas,
  12 s) con la página v19.12 verificada por hash. Se borró el ZIP v19.11
  (contenía la página vieja, sin el arreglo de `FirstPersonInputModule`).
  La carpeta `dist/ULTRAKILL-WebGL/` es el mismo contenido, sin comprimir.
  (Queda un `ULTRAKILL-WebGL-v19.10.zip` ancient: se puede borrar, es
  inservible.)
- **Descargas:** `http://localhost:8000/?v=19.12` (este PC) y
  `http://192.168.0.17:8010/` (otro dispositivo, misma Wi-Fi; el firewall ya
  tiene reglas para PowerShell en perfil público).

### 🎯 Causa raíz definitiva (v19.12) — por qué 0-2..0-5 rompen la cámara y 0-1/0-S no
- **El componente `FirstPersonInputModule` (módulo de input de la UI) es la causa.**
  Está presente en: **0-2, 0-3, 0-4, 0-5** (y 1-x, 2-x, 3-x) y **NO** en
  Tutorial, 0-1, 0-S ni Main Menu → **coincide exactamente con los niveles que
  al usuario le fallan y los que le funcionan** (lo que olió su amigo).
- Qué hace (vanilla, no es un bug del port):
  ```csharp
  CursorLockMode lockState = Cursor.lockState;
  Cursor.lockState = CursorLockMode.None;      // <-- suelta el ratón
  ... (procesa el evento de ratón / hover de la UI) ...
  Cursor.lockState = lockState;                // <-- lo vuelve a poner en Locked
  ```
  Se ejecuta en **CADA evento de ratón** (`GetMousePointerEventData`) y en cada
  `ProcessMove`/`ProcessDrag`.
- Por qué rompe el navegador: `Cursor.lockState = None` → Unity llama a
  `_emscripten_exit_pointerlock()` → **`document.exitPointerLock()`** (confirmado
  en `WebGL.wasm.framework.unityweb`) → **el navegador suelta el bloqueo** y,
  además, Unity hace `JSEvents.removeDeferredCalls(JSEvents.requestPointerLock)`
  → **cancela su propia petición de bloqueo**. Luego Unity "repone" el estado
  interno a `Locked` (pero para Unity nunca cambió → **no vuelve a pedirlo**).
  Resultado: el bloqueo del ratón **se pierde para siempre** → Unity usa el ratón
  **absoluto** → la cámara **no gira 360°** y da saltos. Y como Chrome no
  concede el bloqueo de nuevo sin un gesto del usuario, solo se recupera al
  hacer clic.
- La pista del usuario era correcta en lo esencial (los niveles buenos son
  exactamente los que NO tienen ese componente), aunque la causa no era el fix.

### v19.12 (solo página, sin recompilar Unity) — arreglo
- En `Assets/Editor/WebGLTemplate.html` (copiada a `Builds/WebGL/index.html`):
  se **intercepta `document.exitPointerLock()`** y se **retrasa 250 ms**:
  - Si el juego sigue en **partida** (cursor del canvas oculto = `cursor:none`)
    → **no se suelta el ratón** (el bloqueo se conserva).
  - Si el juego ha abierto un **menú** (cursor visible = `cursor:default`) →
    la liberación real se aplica y el menú se puede usar con el ratón.
  - Si el juego vuelve a pedir el bloqueo (patrón "suelta y vuelve a pedir" de la
    UI) → se cancela la liberación pendiente (parche de
    `HTMLCanvasElement.prototype.requestPointerLock`).
  - Todos los "suelta el ratón" del juego (pausa, shop, salir de misión,
    `IntroText`, `UnlockMouse`) van **acompañados de `Cursor.visible = true`**,
    así que la regla "cursor visible ⇒ soltar" los cubre todos; el único que
    suelta **sin** mostrar el cursor es `FirstPersonInputModule` → es
    precisamente el que se neutraliza.
- **Unit test del parche (determinista, con Chrome real por CDP):**
  en partida → 0 liberaciones reales ✓ | en menú → 1 liberación ✓ |
  patrón UI "suelta y vuelve a pedir" → 0 liberaciones ✓.
- Además (v19.11, ya en la página): se pide el bloqueo en **cada tecla
  (incluida Escape) y clic**, con **reintento periódico** (500 ms, espera
  creciente hasta 2 s) mientras el juego esté en partida sin bloqueo, para
  recuperarlo tras un Esc o al cambiar de nivel (Chrome bloquea el re-bloqueo
  ~1,3 s tras un Esc: `SecurityError: Pointer lock cannot be acquired
  immediately after the user has exited the lock`).
- **Pendiente:** si al reiniciar el juego en 0-2..0-5 el ratón se bloquea al
  primer clic/tecla y la cámara gira 360° → el arreglo está completo. Si aun
  así falla, el siguiente paso es el **fix en C#** (parchear
  `FirstPersonInputModule` con `#if UNITY_WEBGL` para no tocar
  `Cursor.lockState`) → requiere recompilar el player (~6 min) y rehacer el ZIP.

### Utilidades de diagnóstico creadas (en `%TEMP%\opencode\`)
- `cdp_lib.ps1` — cliente CDP (WebSocket) en PowerShell 5.1: `Start-CdpBrowser`,
  `Eval`, `Key`, `Click`, `Shot`, `Focus-ChromeWindow`.
- `chrome_probe.ps1` — guion de bloqueo de ratón (W → Esc → espera → Esc → clic).
- `test_transicion.ps1` — carga un nivel real desde el juego:
  `unityInstance.SendMessage('Panel','RanklessNextLevel','Level 0-2')`
  (el `FinalRank` del Tutorial, con `StockMapInfo.nextAct = act-0`).
- `test_patch.ps1` — unit test del parche de `exitPointerLock`.
- `frames.ps1` / `frames2.ps1` — extraen fotogramas de un MP4 con Chrome
  (no hay ffmpeg en el PC).
- `scene_probe.ps1`, `scene_chain.ps1`, `scene_objs.ps1`, `find_objs.ps1` —
  analizadores del YAML de las escenas.
- **Limitación del entorno:** con el usuario usando el PC, Chrome no puede
  robar el foco → el navegador rechaza el bloqueo con `WrongDocumentError`
  (no es un fallo del port). Con el PC libre funciona bien.

### v19 (binario, BUILD OK: `errors=0`, 339 MB)
- **La pista de "el fix solo está en Tutorial y 0-1" era falsa.** Se comprobó a
  fondo: `CameraController.cs` no tiene lógica por escena; el bloque
  `#if UNITY_WEBGL` es global; `WebGLRenderFix` se aplica a todas las escenas; y en
  las 6 escenas del Acto 0 el `CameraController` está en el mismo objeto
  (`Main Camera`, hijo de `Player`), habilitado y con valores serializados idénticos.
  Jerarquía y `m_IsActive` de ancestros idénticos. `OptionsMenuToManager` (el que
  fija `mouseSensitivity`) está en todas las escenas. No hay `IntroTextController`,
  `UnlockMouse` ni `TitleScreen` en el Acto 0 (el Esc no se ignora por `inIntro`).
  `PlayerActivator`: 0-1 tiene 4, 0-2..0-5 y 0-S tienen 1 (no explica la diferencia).
- **Causa real (medida en Chrome 155 real vía CDP):** el juego pide el *pointer lock*
  al entrar en el nivel, pero **Chrome solo lo concede dentro de un gesto del
  usuario** y además **rechaza la petición durante ~1,3 s después de que el usuario
  abandone el bloqueo con la tecla Esc**:
  `SecurityError: Pointer lock cannot be acquired immediately after the user has exited the lock`.
  Con el Esc, Chrome suelta el bloqueo (y Unity sigue creyendo que lo tiene:
  `Cursor.lockState` interno = `Locked`, así que Unity **nunca vuelve a pedirlo**) →
  la cámara pasa a ratón **absoluto** → "salta al mover el ratón".
  Peor: cada pulsación nueva de Esc reinicia el "cooldown" de Chrome → la cámara
  **nunca** se recuperaba (el usuario pulsaba Esc porque la cámara fallaba).
- **Herramienta de diagnóstico creada:** `C:\Users\JJ\AppData\Local\Temp\opencode\`
  - `cdp_lib.ps1` — cliente CDP (WebSocket) en PowerShell 5.1.
  - `chrome_probe.ps1` — lanza **Chrome real** (perfil temporal, puerto 9222),
    instrumenta `requestPointerLock`/`pointerlockchange`/`pointerlockerror` y
    ejecuta el guion: W → Esc → espera → Esc → espera → clic.
  - Trucos: `Page.bringToFront` + `Emulation.setFocusEmulationEnabled` + traer la
    ventana al frente con `SetForegroundWindow` (si el documento no está "fully
    active", Chrome responde `WrongDocumentError`).
  - Ojo: el navegador automatizado (Electron de OpenCode) **no** concede pointer
    lock ni fullscreen real; para eso sirve el Chrome real por CDP.

### v19.11 (solo página, sin recompilar Unity) — arreglo del ratón
- En `Assets/Editor/WebGLTemplate.html` (copiada a `Builds/WebGL/index.html` y al
  paquete):
  - `requestPointerLock()` con bandera `lockPending`: se pide en **cada gesto**
    (tecla o clic) mientras el juego tenga el cursor oculto (`cursor:none` = en
    partida; en menús/pausa es `default` y no se pide, para no bloquear los menús).
  - **Reintento periódico** (cada 500 ms, con espera creciente hasta 2 s) mientras
    el juego esté en partida y falte el bloqueo → **la cámara se recupera sola**
    tras el cooldown de Chrome, sin que el usuario tenga que hacer nada.
  - `pointerlockchange`/`pointerlockerror` marcan `lockPending` → se reintenta.
  - **La tecla Escape NO pide bloqueo** (el juego está pausing/reanudando en ese
    instante y Unity suelta el ratón después; pedirlo ahí solo disparaba el
    cooldown de Chrome).
  - **Bug corregido:** en `onFullscreenChange` se llamaba `unlockKeyboard()` (que
    pone `lockActive=false`) **antes** de comprobar `if (lockActive || heldLong)`,
    así que la condición nunca se cumplía y **cualquier salida de fullscreen
    disparaba un re-entrado automático**. Ahora se guarda `wasLocked` antes.
  - El pie de la página muestra el estado: "Ratón bloqueado" / "Ratón sin bloqueo:
    pulsa una tecla" / "Teclado bloqueado".
  - **Los servidores** (`serve-webgl.ps1` y el `servidor.ps1` del paquete) mandan
    `Cache-Control: no-store` para `.html` → el navegador nunca sirve una copia
    antigua de la página.
- **Verificado en Chrome real (CDP):** con la primera tecla → `LOCKED`; tras el Esc
  → el bloqueo se recupera **solo** en ~1,5 s; con un clic → `LOCKED` inmediato.
- **Sin cambios en el binario**: mismo player v19 y mismo bundle act-0
  (hash `08F24123...`).
- **Paquete:** `dist/ULTRAKILL-WebGL-v19.11.zip` (136,9 MB, 24 entradas, 10,5 s).
  `dist/ULTRAKILL-WebGL/` = contenido del ZIP (idéntico, verificado por hash).
- **PENDIENTE (usuario):** probar 0-2, 0-3, 0-4, 0-5 y 0-S de principio a fin en
  Chrome y confirmar que la cámara no da saltos; comprobar que al pausar con Esc
  el ratón se recupera solo.

### v19 (binario, BUILD OK: `errors=0`, 339 MB)
- **Escenas del Acto 0 renombradas según su contenido real** (AssetRipper las exportó
  permutadas): `Level 0-1`=INTO THE FIRE, `0-2`=THE MEATGRINDER, `0-3`=DOUBLE DOWN,
  `0-4`=A ONE-MACHINE ARMY, `0-5`=CERBERUS, `0-S`=secreto. Cadena verificada:
  0-1→0-2, 0-2→0-3 (y 0-S), 0-3→0-4, 0-4→0-5, 0-5→1-1 (act-1 no está construido:
  cae al menú principal por el callback de error de `FinalRank.LoadNextLevel`).
- **Bundle act-0 reconstruido a la fuerza** (`ForceRebuildAssetBundle`): el build
  incremental daba un no-op con el bundle viejo. Tamaño 124.159.743 bytes
  (21:31). Copiado a `Assets/StreamingAssets/acts/act-0` y al player.
- `MapLoader.WebGLBundleQuery = "?v=19"` para evitar caché del bundle.
- `WebGLRenderFix.FixTutorialState` limpia `OptionsManager.inIntro` (el salto de
  intro llamaba a `Over()` antes de que `IntroTextController` pusiera
  `inIntro=false`, y `OptionsManager.Update` ignoraba Escape mientras `inIntro`).
- Build servida: `http://localhost:8000/?v=19` (player v19, Prelude completo).

### v19.5 (solo página, sin recompilar el player) — pantalla completa
- **Problema del usuario:** al jugar en fullscreen, Escape (o cualquier tecla) salía
  de pantalla completa; se quiere que Escape pause sin salir.
- **Solución en la página** (`Assets/Editor/WebGLTemplate.html`, copiada a
  `Builds/WebGL/index.html`):
  - Botón **PANTALLA COMPLETA** (además del icono del template) que llama a
    `unityInstance.SetFullscreen(1)`.
  - **Keyboard Lock API** (`navigator.keyboard.lock(['Escape'])`) al entrar en
    fullscreen: en Chrome/Edge Escape se entrega al juego y **no sale** de
    pantalla completa. Se reintenta en el primer clic si el lock no se activó.
  - **Re-entrada automática** en `fullscreenchange` si el navegador sale igualmente
    (Firefox no soporta Keyboard Lock): reintenta fullscreen; si el navegador exige
    gesto, el botón parpadea para volver a entrar con un clic.
  - **Shift+Esc** sale de pantalla completa a propósito (para no quedar atrapado).
  - `WebGLBuilder.cs` copia `Assets/Editor/WebGLTemplate.html` a
    `Builds/WebGL/index.html` tras cada build correcto (persistente).
- **URL para el usuario:** `http://localhost:8000/?v=19.5` (el binario Unity es el
  mismo de v19; solo cambia la página).

### v19.6 (solo página) — pointer lock / cámara
- **Síntoma del usuario:** la cámara se movía raro, chocaba con los límites de la
  ventana y no se podía girar 360°.
- **Causa:** el juego pide el pointer lock **una sola vez** al entrar en el nivel
  (`CameraController.Start`). Si el navegador lo deniega (petición sin gesto de
  usuario), Unity deja `Cursor.lockState = Locked` a nivel interno y **no vuelve a
  pedirlo** (el chequeo `Cursor.lockState != Locked` de `CameraController.Update`
  nunca se cumple). Sin pointer lock, Unity WebGL calcula el ratón con coordenadas
  **absolutas dentro del canvas** (`Browser.calculateMouseEvent` → rama sin lock):
  la cámara se detiene en los bordes. Con lock, `Browser.mouseX/Y` acumula
  `movementX/Y` sin límite (giro 360°).
- **Fix en `WebGLTemplate.html`:** en cada `mousedown` sobre el canvas y en cada
  tecla (excepto Escape), si `getComputedStyle(canvas).cursor === 'none'` (Unity
  pone `cursor:none` solo cuando estás jugando; en menús/pausa es `default`) y no
  hay pointer lock, se llama `canvas.requestPointerLock()` dentro del gesto real.
  Así el ratón se captura en el primer clic/tecla de juego y se re-captura tras
  pausa/respawn.
- **Extra:** si el navegador saca de fullscreen al pausar (Firefox), el siguiente
  clic/tecla dentro del juego vuelve a entrar en fullscreen (`fsFallback`).
- No requiere recompilar Unity (mismo binario v19). **URL: `?v=19.6`**.

### v19.7-v19.10 (solo página) — Escape no sale de fullscreen + bloquear atajos del navegador
- **Síntoma del usuario:** al pulsar Escape se salía de pantalla completa; además los
  atajos del navegador (Ctrl+D marcadores, Ctrl+T/Ctrl+W pestañas, etc.) hacían perder
  el control del juego. Objetivo: todo el teclado debe ir a ULTRAKILL.
- **Chrome/Edge:** `navigator.keyboard.lock()` **sin argumentos** captura **todas** las
  teclas (no solo Escape): Escape ya no sale y los atajos del navegador se entregan al
  juego. Se reintenta al entrar, a los 0/0.2/0.6/1.5/3 s y en cada tecla/clic mientras
  el lock no esté activo (`lockActive`). El estado se muestra en el pie:
  "Teclado bloqueado" / "Pulsa una tecla para bloquear los atajos".
- **Firefox 151+ / Safari 26.4+:** se parchea `Element.prototype.requestFullscreen`
  para añadir `{ keyboardLock: "browser" }` (también a la petición que hace Unity).
  Con eso un toque de Escape **no** sale (hace falta mantenerlo ~1.5 s) y el navegador
  permite anular atajos reservados con `preventDefault()`. Si el navegador no soporta
  la opción (`NotSupportedError`), se desactiva y se reintenta **sin** ella; en Chrome
  ni se envía porque usa `navigator.keyboard.lock()`.
- **`preventDefault()` en todo keydown mientras hay fullscreen** (Esc, Ctrl+D/T/W,
  Alt+←, F5, Tab...). No impide que Unity reciba el evento; solo anula la acción del
  navegador. F11 y Ctrl+Alt+Del siguen siendo del sistema (imposible bloquearlos).
- **Salida intencionada:** mantener Escape ~2 s (mecanismo del navegador) o
  **Shift+Esc** (lo maneja la página) o el botón PANTALLA COMPLETA (fuera del
  fullscreen no se ve). Se detecta pulsación larga (`escapeHeldSince` /
  `lastEscapeHeldMs`) para no re-entrar cuando la salida fue a propósito.
- **Aviso:** pulsar F11 **no** activa el bloqueo (es fullscreen del navegador, no del
  elemento); hay que usar el botón PANTALLA COMPLETA. El pie lo indica.
- Firefox <151: imposible bloquear Escape/atajos desde la web; el pie muestra
  "Firefox antiguo... Usa Chrome/Edge". No requiere recompilar Unity.
- **URL para el usuario: `http://localhost:8000/?v=19.10`** (mismo binario v19).

### Paquete ZIP para probar en otro dispositivo (2026-10-19 ~23:05)
- **`ultrakill-webport\dist\ULTRAKILL-WebGL-v19.10.zip`** (136,9 MB): build jugable completo
  (mismo player v19 + bundle act-0) **sin modificar**, más:
  - `JUGAR.bat`: doble clic → abre `servidor.ps1`.
  - `servidor.ps1`: portátil, sirve **su propia carpeta** (`$PSScriptRoot`), busca puerto libre
    desde 8000 (hasta 8019), abre el navegador y mantiene la ventana para parar con Ctrl+C/cerrar.
    Usa streaming (no carga archivos enteros en RAM).
  - `LEEME.txt`: instrucciones (no abrir por `file://`, fullscreen, atajos, fallback Python).
- **Verificado:** ZIP extraído a una carpeta limpia y servido con `servidor.ps1` en el puerto
  8124: `index.html` (200, contiene `UKFullscreen`), `Build/WebGL.json` (200),
  `StreamingAssets/AssetBundles.manifest` (200), 404 correcto. Hashes de `index.html` y
  `StreamingAssets/acts/act-0` idénticos al build original.
- El otro dispositivo debe ser Windows con PowerShell (10/11) o usar Python
  (`python -m http.server 8000`) si PowerShell está bloqueado. Requiere navegador moderno
  (Chrome/Edge recomendados; Firefox 151+).
- No requiere recompilar Unity ni tocar los archivos del build.

### Pendiente de verificar por el usuario (navegador normal) — v19.11
1. **Cámara en 0-2, 0-3, 0-4, 0-5 y 0-S**: la cámara NO debe dar saltos al mover el
   ratón. Si aun así falla: pulsar una tecla o hacer clic la recupera al instante y
   sola en ~1,5 s. El pie dice "Ratón bloqueado" / "Ratón sin bloqueo: pulsa una tecla".
2. PRELUDE jugable de inicio a fin: 0-1 → 0-2 → 0-3 → 0-4 → 0-5 (y secreto 0-S).
3. Pausa con Escape en el Tutorial y en PRELUDE: **un toque debe pausar sin salir**
   de fullscreen (Chrome/Edge y Firefox 151+). Al reanudar, el ratón se recupera
   solo en ~1,5 s (Chrome bloquea el re-bloqueo 1,3 s después de un Esc).
4. Atajos del navegador en fullscreen: Ctrl+D, Ctrl+T, Ctrl+W, F5 no deben hacer nada.
5. Salida intencionada de fullscreen: mantener Escape ~2 s o Shift+Esc.
6. Anotar el navegador y lo que diga el pie ("Teclado bloqueado", "Ratón bloqueado").
   Recordatorio: Chrome muestra un aviso ("mantén pulsado Esc") en cada pulsación
   de Esc dentro de fullscreen; es del navegador, no un error del port.


## ESTADO ACTUAL (2026-10-19 ~19:45) — depurando el respawn del Tutorial

### Diagnóstico del bucle “reaparezco arriba” (dump de la escena en el editor)
`Assets/Editor/WebGLSceneDump.cs` (nuevo) volcó la escena Tutorial con Unity en batch:
- **No hay `PlayerPosInfo`, ni `CheckPoint`, ni `OutOfBoundsTargetSetter`** en el Tutorial.
- El jugador está en `(-0.3, 105.8, -66.3)` y cuelga de `FirstRoom/Player`.
- Único `DeathZone`: `OutOfBounds/Cube` en `(0,-300,0)`, `affected=EnemiesOnly`,
  `notInstakill=false` → **no afecta al jugador**.
- `OutOfBounds` en `(0,-200,0)`, caja 2000×50×2000, `targets=All`: si el jugador baja de
  y≈-175, `OnTriggerEnter` pone la velocidad a cero y hace
  `player.position = StatsManager.spawnPos`.
- `StatsManager.Start` fija `spawnPos = posición inicial del player` (**105.8**) y, al no
  haber `PlayerPosInfo`, aplica `velocity = down * 100` (caída de intro). El Tutorial se
  carga como “custom” (`isCustomLoaded=true`), así que el bloque de `PlayerPosInfo` se
  saltaría igualmente.
- `StatsManager.Update`: si `hp<=0` y se pulsa **R o Fire1 (clic)**, llama a `Restart()`;
  sin checkpoints, `Restart()` hace `SceneManager.LoadScene` de la propia escena → el
  jugador vuelve a caer desde 105.8. Esto explica el bucle (caer → reaparecer arriba) y
  que cada clic lo agrave (Fire1 al intentar recuperar el pointer lock).

### Resultado v12 (probada en navegador automatizado)
- `sceneLoaded 'Tutorial'` **una sola vez** (no hay recargas de escena).
- `StatsManager.Start: pos=(-0.3, 105.8, -66.3) custom=False` → el Tutorial se carga
  **directo del build** (no del bundle), así que `isCustomLoaded` es false.
- El jugador se recoloca al suelo… pero **vuelve a aparecer a y≈69.4 cada ~0.5 s**
  (el guardián lo captura en bucle). No es `OutOfBounds` (está a y=-200 y el jugador
  nunca baja tanto). Algo devuelve al jugador a ~69.4 una y otra vez.
- Hipótesis vivas: Animator/animación escribiendo la posición, plataforma móvil
  (“elevador”) llevando al jugador, o un trigger de intro que re-hace la caída.

### CAUSA RAÍZ ENCONTRADA (v13 probada): trigger `TeleportPlayer` en el suelo del inicio
Los logs de la v13 lo demuestran:
- El teletransporte **sí se aplica** (`readback`/`rbpos` = destino; `parent=FirstRoom`,
  `grav=True`, `kin=False`, `anim=root=False`; un solo `id`).
- **1 frame después**, el jugador está en 69.6 = destino (-0.4) **+70** y cae con
  gravedad normal. El `UP-JUMP` lo registra (`de (-0.3,-0.4,-66.3) a (-0.3,69.6,-66.3)`).

El responsable es un trigger del **escape del editor** (no aparece en el listado de
raíces): `FirstRoom/Room/Pit (3)/Cube (1)` (layer 16, BoxCollider trigger
10×20×10, centro `(0, 7.7, -66.72)`, es decir x∈[-5,5], y∈[-2.3,17.7],
z∈[-71.7,-61.7]) con script **`TeleportPlayer`**:
`affectPosition=True, notRelative=False, relativePosition=(0, 70, 0)` →
`OnTriggerEnter` hace `position += (0,70,0)`.

Ese volumen **contiene el punto de aterrizaje** `(-0.3, -0.4, -66.3)` que usa
nuestro arreglo → cada recolocación sumaba +70 al jugador → bucle
(recolocar → +70 → caer → guardián y>50 → recolocar…). El “elevador de inicio”
del usuario era justamente este empuje.

En el Tutorial hay 3 `TeleportPlayer` (los otros dos en los “pit” del pasillo final,
y=-111, +70). Estado de colliders: ver `scene-dump2.txt`. Volcado con el editor:
`WebGLFloorProbe` (rayos verticales desde y=30) → `floor-probe.txt`.

### CAUSA RAÍZ CONFIRMADA: la intro original desactiva el pit

Tercer volcado del editor (`scene-dump3.txt`) — `IntroTextController`:

```
### FirstRoom/Player/Canvas/Intro active=False page2Screen=.../Intro/Page 2
  deactivateOnIntroEnd (1):
    FirstRoom/Room/Pit (3)/Cube (1) active=True
### FirstRoom/Player/Canvas/Intro 2 active=False page2Screen=.../Intro 2/Page 2
  deactivateOnIntroEnd (1):
    FirstRoom/Room/Pit (3)/Cube (1) active=True
```

En el juego original la secuencia es: la intro congela al jugador a y=105.8 →
al terminar la intro (`deactivateOnIntroEnd`) **se apaga `Pit (3)/Cube (1)`** →
recién entonces el jugador cae desde 105.8 y aterriza sin rebote.
Como nuestro port salta la intro, el pit queda encendido y su
`TeleportPlayer` (+70) atrapa cada recolocación → bucle infinito.

### Arreglo v14 (probado) y v15 (en compilación)

`WebGLRenderFix.ComputeFloorTarget` replica lo que hace la intro original:
1. Calcula el punto natural del suelo (raycast en la posición del jugador).
2. Si la cápsula del jugador ahí toca un collider con `TeleportPlayer`,
   **desactiva ese trigger** y registra un log. Desactivar el componente (no el
   GameObject) es robusto ante reactivaciones por `ObjectActivator`.
3. Si algo siguiera bloqueando el punto, prueba desplazamientos
   `(0,0), (0,-4), (0,4), (0,-8), (0,8), (0,-12), (0,12), (±4,±6)` y usa el
   primero con suelo y sin `TeleportPlayer` en la cápsula.
4. Ese destino se usa en: recolocación inicial, guardián y>50, red y<-50 y
   `StatsManager.spawnPos`.
5. Se mantienen el guardián y>50 (rescate) y la red y<-50.

**Resultado v14 en navegador (BUILD OK, errors=0):**
- El bucle desapareció al inicio: jugador estable en `(-0.3, -0.5, -74.3)`,
  sin UP-JUMP, `spawnPos` en el suelo.
- El movimiento con teclado funciona (las pulsaciones de W movieron al jugador).
- Se confirmó que `grav=False` estando quieto en el suelo es NORMAL:
  `NewMovement.Move()` apaga la gravedad cuando `gc.onGround` y sin input.
- **PERO**: al caminar hacia la zona del pit, el +70 volvió. Dato empírico:
  **`OnTriggerEnter` se sigue recibiendo aunque el MonoBehaviour esté
  desactivado** → hay que apagar el **collider**.

**Cambios v15:**
- `DisableTeleportTriggersAt` apaga el **collider** además del componente.
- `OverlapsTeleportTrigger` ignora colliders desactivados.
- `EnforceStartTriggers()` cada 0.5 s en el Tutorial: apaga cualquier
  `TeleportPlayer` en la zona del inicio (|x|≤25, -30≤y≤30, -100≤z≤-30);
  cubre reactivaciones y clones. Los pits del final (y=-111, `FinalPit`) no se tocan.
- `TeleportPlayer.PerformTheTeleport` ahora registra ruta/posición/id de la
  instancia que dispare (para identificar cualquier culpable restante).

**Resultado v15 en navegador (BUILD OK):**
- `collider del pit desactivado: FirstRoom/Room/Pit (3)/Cube (1) id=185946`.
- El jugador aterriza en el punto previsto `(-0.30, -0.40, -66.30)` (ya no en
  el desplazamiento -8) y `spawnPos` queda ahí.
- Prueba de caminata: de z=-66.3 a z=+38.5 (pasillo del slide, dash y sala de
  salud) **sin un solo +70, sin `TeleportPlayer` disparado y sin UP-JUMP**.
- El juego se congela si la pestaña queda en segundo plano (Chrome congela
  rAF); hay que enfocar la pestaña para que corra.
- El test F9 a y=-400 recargó la página (posición artificial extrema) → v16
  lo cambia a y=-60.

**Cambios v16 (compilando):**
- Red y<-50: **no interviene si z>100** (zona del `FinalPit`), para no romper
  la secuencia de final del Tutorial (el pozo baja a y=-111 y el juego lo usa
  durante ~5 s antes de `SendInfo`).
- F9 ahora tira al jugador a y=-60 (prueba limpia de la red de seguridad).

**Resultado v16 en navegador (BUILD OK, verificado):**
- Arranque impecable: `collider del pit desactivado`, teleport a
  `(-0.30, -0.40, -66.30)`, `spawnPos` ahí, ticks estables sin UP-JUMP.
- Caminata completa: con pulsaciones de W el jugador recorrió el Tutorial
  entero (slide, dash, sala de salud), llegó al pozo final, la secuencia
  `FinalPit` funcionó (la guarda z>100 no interfiere) y el nivel terminó
  cargando **`Level 0-1` desde el bundle** ✓.
- En `Level 0-1` el fix no interfiere (el jugador cae en la intro del nivel,
  muere y el juego reinicia el nivel normalmente).
- Pitidos de "build listo" dados SIN ventana emergente.
- Pendiente: verificación del usuario en Firefox/Chrome normal (pointer lock
  y jugabilidad). El navegador automatizado NO puede adquirir pointer lock
  (`SecurityError`), como estaba previsto.

**Confirmado por el usuario:** "por fin, tenemos el tutorial jugable".

**Limpieza (v17, build final limpio):**
- Quitados TODOS los diagnósticos: logs `[WebGLFix]`, traza de frames, ticks,
  tecla F9 y el bloqueo por `?debug=1`.
  Archivos: `WebGLRenderFix.cs` (reescrito limpio), `TeleportPlayer.cs`
  (log temporal fuera), `OutOfBounds.cs` y `StatsManager.cs` (logs fuera).
- Se mantiene toda la lógica: bypass de cámaras WebGL, spawn al suelo con
  raycast, apagado del collider del pit del inicio + refuerzo cada 0.5 s,
  guardián y>50, red y<-50 (con guarda z>100 para el FinalPit), `spawnPos`.
- URL limpia para el usuario: `http://localhost:8000/?v=17` (sin debug).

**Verificación visual v17 (captura del navegador):**
- El juego renderiza perfecto: mundo 3D, HUD (100 de vida), tarjeta de nivel
  PRELUDE y menú de pausa del juego.
- CERO logs `[WebGLFix]` (limpieza correcta). El juego incluso avanzó solo a un
  nivel de PRELUDE (el guardado persiste en IndexedDB) y se ve correctamente.
- El menú de pausa se abre en el navegador automatizado porque NO se puede
  capturar el puntero (`SecurityError` en modo CDP); en un navegador normal con
  clic del usuario no ocurre.
- Quedaba un diagnóstico antiguo (`WebGLDiagnostics.cs`) volcando renderers en
  cada carga → eliminado (archivo + .meta). De ahí la recompilación v18.
- El log `Hmm` de `StatsManager.StartTimer` es código ORIGINAL del juego
  (decompilado), se deja.

**v18:** build limpio final (sin WebGLFix ni WebGLDiag).

**Verificación v18 en navegador:** consola sin ningún log de diagnóstico y sin
errores; el juego arranca y corre. URL final para el usuario:
`http://localhost:8000/?v=18`.

**Estado final del port (para retomar en otra sesión):**
- Tutorial jugable de principio a fin (confirmado por el usuario).
- Final del Tutorial → `FinalPit` → carga `Level 0-1` (PRELUDE) desde el bundle.
- El guardado del juego persiste en IndexedDB entre sesiones.
- Pendiente/opcional: que el usuario confirme también algún nivel del PRELUDE
  de principio a fin y el pointer lock en Firefox/Chrome normales (el
  navegador automatizado no puede probarlo).

Los otros dos `TeleportPlayer` (pasillo final, y=-111) son `FinalPit` (mecanismo
de fin de nivel) y se dejan intactos.

### Build v13 (probada, sin notificador)
`WebGLRenderFix.cs` con diagnóstico profundo:
1. Tick cada 1 s con: pos, vel, useGravity, isKinematic, parent@pos, localPos, hp,
   dead, anim (root motion / sin controller), spawnPos e **instanceID**.
2. En cada teletransporte: `velAntes`, `grav`, `kin`, `readback` y `rbpos`; más una
   **traza de 12 frames** con la posición para ver si el teletransporte “agarra”.
3. UP-JUMP corregido (compara tras el teletransporte).
4. `OutOfBounds` **desactivado** en el Tutorial (la red y<-50 ya cubre).
5. Se mantienen: guardián 0.5 s, spawnPos al suelo, F9 de prueba con `?debug=1`.
6. `StatsManager` y `OutOfBounds` siguen con logs `[WebGLFix]`.

### Probar v13
`http://localhost:8000/?v=13&debug=1` (Ctrl+F5). Con los logs hay que distinguir:
- `vel≈(0,-100,0)` al capturar → alguien re-aplica la caída de intro.
- `vel≈0` + `grav=false` → el jugador está **congelado flotando** (intro `inMenu`).
- la traza post-teletransporte muestra si algo reescribe la posición cada frame.

## Historial (v9–v11, ya superado)

### Ya integrado
- Bundle WebGL del Acto 0: `Assets/StreamingAssets/acts/act-0` (118.4 MB) con
  Tutorial, Level 0-1…0-5, Level 0-S, Intermission1.
- `MapLoader` en WebGL carga el bundle con `UnityWebRequestAssetBundle`.
- `IntroText` en WebGL salta toda la intro/calibración.
- Build actual: `BUILD RESULT: Succeeded | errors=0 | warnings=27`, salida 183 MB.
- Servidor: `http://localhost:8000/` (usar `?v=N` + Ctrl+F5).

### Causa real de la “cámara negra” (diagnóstico runtime)
Diagnóstico `WebGLDiagnostics.cs` en la consola del navegador:
- Cámara/jugador en `(-0.3, 107.2, -66.3)`, euler `(0,0,0)`.
- **Todo el nivel está entre y≈0.2 y y≈30.5**. El jugador estaba a **y≈105.8**,
  ~75 unidades por encima del techo: mirando al vacío. Lo único visible era el
  brazo/viewmodel (layer 13) pegado a la cámara.
- El salto de intro omite el teletransporte al punto de inicio; el jugador se
  queda en la posición del prefab en la escena (y≈105.8).
- Además, el composite de post-proceso por RenderTexture (quads `Main VSRM` /
  `HUD VSRM`) no es fiable en WebGL y la cámara HUD podía tapar el mundo.

### Fix en curso (build `?v=9`)
`Assets/Scripts/Assembly-CSharp/WebGLRenderFix.cs`:
1. En Tutorial, si el jugador está a y>50, raycast desde y=4.5 hacia abajo y lo
   coloca en el suelo (offset del collider + 0.05).
2. En WebGL se salta el post-proceso: `Main Camera` dibuja el mundo directo a
   pantalla (SolidColor, depth -1), `HUD Camera` dibuja el brazo encima con
   `ClearFlags.Depth`, `Virtual Camera`/`Shop Camera` desactivadas y los
   `ULTRAKILL_PostProcessHandler` desactivados.
3. También se corrigió `Assets/Shader/ULTRAKILL_Main_PostProcess.shader` a
   alpha blending (por si se reactiva el composite).

### Feedback del usuario (v9, Firefox)
- El mundo ya se ve y se puede jugar: “parecemos estar cerca, está todo bien”.
- **Pendiente 1 (elevador):** al caer del elevador del inicio, el juego devuelve al
  jugador arriba (spawn original y≈105.8). Nuestro fix solo corría al cargar la
  escena. Ya añadido en `WebGLRenderFix.Update`: si el jugador está a y>50,
  se reaplica el teletransporte al suelo (se incluirá en el próximo build).
  **Causa real:** `StatsManager.Start()` guarda `spawnPos` con la posición
  inicial del player (y=105.8) **antes** de moverlo; el `DeathZone`/muerte usa
  ese `spawnPos`, así que respawnea arriba. Además, el `DeathZone` es instakill
  (`notInstakill=false`), por lo que su `respawnTarget` no se usa. Fix: en
  `WebGLRenderFix.SetSpawnPos` se asigna `StatsManager.spawnPos` al suelo; también
  se desactiva `OutOfBoundsTargetSetter` y se redirigen los `DeathZone` con
  target alto.
- **Pendiente 2 (iluminación):** todo está fullbright porque los shaders
  exportados por AssetRipper son dummy/unlit. El usuario dijo de dejarlo para
  luego.

### Siguiente paso
1. Build `?v=9` ya probada en el navegador automatizado:
   - El mundo **ya se renderiza** (habitación/pasillo rojo con paneles grises).
   - Jugador en `(-0.3, -0.4, -66.3)`, cámara en `(-0.3, 1.0, -66.3)`.
   - El raycast encontró suelo en `y=-2.0` (la sala de inicio está en `y=-2`).
   - `Main Camera` dibuja directo, `HUD Camera` con `Depth`, `Virtual/Shop` y
     `ULTRAKILL_PostProcessHandler` desactivados.
   - Las flechas para mirar funcionan (probado con evento de teclado sintético).
2. Falta que el usuario pruebe en su Firefox: movimiento WASD, ratón/pointer lock,
   y si el jugador queda atascado contra la reja roja del inicio.
3. Si la reja bloquea, identificar el collider (raycast desde la cámara) y abrir la
   puerta o mover el spawn unos metros.
4. El brazo/viewmodel puede seguir raro (posición/escala); se ajusta después.
5. Quitar `WebGLDiagnostics.cs` cuando terminemos de depurar.

### Scripts de diagnóstico/port
- `WebGLDiagnostics.cs`: logs `[WebGLDiag]` de cámaras, capas, renderers y jugador.
- `WebGLRenderFix.cs`: spawn + bypass de post-proceso WebGL.
- `WebGLBuilder.cs`, `WebGLBundleBuilder.cs`, `PortDiag2.cs`.

## Objetivo
Portar el build pre-GREED (Patch 6 Hotfix #2, 2021-04-19) a WebGL jugable, uso personal/estudio.

## Build objetivo (verificado)
- app `1229490`, depot `1229491`, manifest `4296180771784366658`
- Unity **2019.4.16f1**, Mono, 1.22 GB, 282 archivos
- Fuente: `C:\Program Files (x86)\Steam\steamapps\content\app_1229490\depot_1229491`

## Herramientas instaladas
- Unity Hub 3.22 + editor `2019.4.16f1 (e05b6e02d63e)` en `C:\Program Files\Unity\Hub\Editor\2019.4.16f1`
- Modulo **WebGL** instalado (`Editor\Data\PlaybackEngines\WebGLSupport` existe)
- Licencia Personal activa: `C:\ProgramData\Unity\Unity_lic.ulf`
- AssetRipper 2.0.0: `ultrakill-webport\tools\AssetRipper\AssetRipper.GUI.Free.exe` (API 127.0.0.1:57916)
- ILSpy 11.1 (winget)

## Export ya hecho
`ultrakill-webport\export\ExportedProject` (56.661 archivos, 3.57 GB)
- 25 escenas, 576 scripts, 1.820 prefabs, 2.572 materiales, 168 shaders dummy
- `ProjectVersion.txt` = 2019.4.16f1

## Estado actual (actualizado)
- **Importacion TERMINADA**: `Exiting batchmode successfully now!` return code 0.
- `Library`: 16.854 archivos, 1.638 MB. **0 errores `error CS`**.
- `CrashedAssetImports.txt` vacio.
- EditorBuildSettings ya trae: Intro, Main Menu, Endless, DebugRoom, Tutorial.
- **Build WebGL de prueba en curso** (background):
  `Unity.exe -batchmode -quit -projectPath <proj> -logFile ultrakill-webport\build-webgl.log -buildTarget WebGL -executeMethod WebGLBuilder.Build`
- Script de build: `ExportedProject\Assets\Editor\WebGLBuilder.cs` (Intro + Main Menu, salida `Builds\WebGL`, compresion desactivada, 512 MB).
- Log del build: `ultrakill-webport\build-webgl.log`.

## Monitor de progreso (import, ya terminado)
```powershell
cd "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport"
powershell -ExecutionPolicy Bypass -File .\watch-import.ps1
```

## Siguiente paso
1. Revisar `build-webgl.log` y `Builds\WebGL` (errores de IL2CPP/shaders/plugins nativos).
2. Servir la build en local (`python -m http.server` o similar) y probar en navegador.
3. Parches de port (orden previsto):
   - `Assets\Scripts\Assembly-CSharp\MapLoader.cs`: `AssetBundle.LoadFromFile` / `File.Exists` no existen en WebGL → cargar desde `StreamingAssets` via UnityWebRequest o mover bundles a Resources/Addressables.
   - `FirstPersonInputModule.cs`: pointer lock en WebGL.
   - Steamworks (`steam_api64.dll`) y Discord (`discord_game_sdk.dll`): excluir/desactivar en WebGL.
   - Guardado (PlayerPrefs/File IO) y rutas.
5. Primer build WebGL y prueba en navegador; iterar errores runtime.

## Notas
- Los shaders exportados son placeholders `//DummyShaderTextExporter`; habra que reemplazarlos (URP/Built-in propios o decompilar con herramienta premium).
- Los bundles originales de StreamingAssets son para Windows/D3D11: NO reutilizables en WebGL.
- `MapLoader.EnsureCommonIsLoaded` usa `LoadFromFile`; `common` = 527 MB, `act-0` = 92 MB, `act-1` = 306 MB, `prime` = 64 MB.
- El instalador de Unity se colgo en `dism ... NetFx3`; se mato DISM y siguio. No repetir ese paso.
- La shell en background puede reportar "completed" antes de que Unity termine; verificar con procesos.

## Smart App Control (RESUELTO)
Estaba bloqueando IL2CPP porque `Unity.IL2CPP.Building.dll` no esta firmado.
El usuario lo desactivo (`VerifiedAndReputablePolicyState = 0`) y el DLL ya carga.
No tocar de nuevo.

## Build WebGL: EXITO
`BUILD RESULT: Succeeded | errors=0 | warnings=24 | sizeMB=1153,6` (2026-10-19 16:42).
Salida en `export\ExportedProject\Builds\WebGL` (1007 MB, incluye StreamingAssets originales).
Se sirve con `serve-webgl.ps1` en `http://localhost:8000/`.

### Primer test en navegador
- El motor arranca: Unity 2019.4.16f1, WebGL 2.0, input system OK, sin errores fatales.
- Pantalla negra: la escena `Intro` usa `VideoPlayer` (`IntroViolenceScreen.cs`), que no
  funciona en WebGL y deja el fade esperando para siempre.
- 404 de `sharedassets0.resource` (esta dentro de WebGL.data.unityweb; probablemente inofensivo).
- `Challenge - Title` tiene un script missing preexistente.

### Progreso real
- Con `Main Menu` como escena unica, el menu **renderiza completo**: logo ULTRAKILL, PLAY,
  OPTIONS, QUIT, botones de Discord/Twitter/YouTube y la pantalla de OPTIONS con sliders.
- El crash de `dlopen` esta resuelto (guardas `#if UNITY_WEBGL` en `SteamController` y
  `DiscordController`).
- Al pulsar PLAY falla porque `MapLoader` carga los AssetBundles originales de Windows
  (`StreamingAssets/common`, `acts/act-0`), incompatibles con WebGL.
- `ProgressChecker.Awake()` hace `SceneManager.LoadScene("Tutorial")` al arrancar si no hay
  save; por eso salia el error en bucle.
- Fix actual: `WebGLBuilder.cs` ahora incluye `Main Menu` + `Tutorial`. Rebuild en marcha.
- `MapLoader.CheckForAct` devuelve `true` en WebGL. Falta adaptar `LoadAct`,
  `EnsureCommonIsLoaded` (AssetBundle.LoadFromFile no existe en WebGL) y `FinalRank`.
- La automatizacion usa una instancia de Firefox **oculta y separada**; el usuario debe abrir
  su propio navegador en `http://localhost:8000/` para probar.

## Siguiente: AssetBundles WebGL
Los bundles originales (`StreamingAssets/common`, `acts/*`) son de Windows y WebGL no los puede
leer. Plan:
1. `Assets/Editor/WebGLBundleBuilder.cs` ya creado. Genera `Builds/WebGLBundles/acts/act-0`
   desde las escenas exportadas con `BuildPipeline.BuildAssetBundles(..., BuildTarget.WebGL)`.
2. Copiar los bundles WebGL a `Assets/StreamingAssets/acts/` (sustituyendo los de Windows).
3. Parchear `MapLoader`:
   - `EnsureCommonIsLoaded`: en WebGL no-op (o cargar un `common` WebGL si hace falta).
   - `LoadAct`: usar `UnityWebRequestAssetBundle.GetAssetBundle(url)` en corutina
     (`AssetBundle.LoadFromFile` no existe en WebGL).
   - `SmartLoadBundle`: idem.
   - `CheckForAct`: ya devuelve `true` en WebGL.
4. `FinalRank` ya recibe el bundle cargado; deberia funcionar si el bundle es WebGL.
5. Rebuild del player con solo `Main Menu` (las escenas de actos van dentro de los bundles).

Comando para generar el bundle del Acto 0 (tras cerrar el build del player):
```
Unity.exe -batchmode -quit -projectPath <ExportedProject> -logFile <log> -buildTarget WebGL -executeMethod WebGLBundleBuilder.BuildAct0
```

## Progreso del build
- Intento 1: fallo por `UnityEngine.SpatialTracking.dll` (ARModule). DLLs XR movidos a `removed-plugins\`.
- Intento 2: fallo en postproceso WebGL por `webGLTemplate` vacio. Corregido:
  `webGLTemplate: APPLICATION:Default`, `WebGL: 0` en QualitySettings, `preloadedAssets` limpio.
- Intento 3: fallo en IL2CPP por Smart App Control (bloqueo actual).
- Escenas y assets si compilan; se generaron ~145 MB antes del fallo.

## Archivos clave
- `ultrakill-webport\PLAN.md`
- `ultrakill-webport\PORT-NOTES.md`
- `ultrakill-webport\import.log`
- `ultrakill-webport\export\ExportedProject`
- `ultrakill-webport\export.log`

---

## v20.1 (2026-10-20) - arreglo de los regresos de v20.0

### Lo que reportaba el usuario
"solo funciona la iluminacion en el tutorial, con muy mala calidad" - "los
enemigos se ven completamente negros" - "falta parte de la textura del orbe
azul" - "no puedo seleccionar cosas en el menu de pausa" - "el debug menu no
funciona" - "vuelven los atajos de Chrome (Ctrl+D)" - "inicia con el cheat menu
activado".

### Causas reales (confirmadas con datos)
- Iluminacion mala/negra: los dummies de AssetRipper declaraban NOMBRES DE
  SHADER GENERICOS DUPLICADOS (varios assets con Shader "psx/vertexlit"), asi
  que el registro de shaders de Unity se mezclaba y cada material recibia el
  shader equivocado. Ademas el shader real hace vertex snapping a 160x120
  (bloques de 12x9 px a 1080p = el aspecto sucio).
- Pausa sin poder pinchar: FirstPersonInputModule (solo en 0-2..0-5) pone
  Cursor.lockState = None en cada evento de raton; en el navegador eso suelta
  el bloqueo y Unity cree que sigue bloqueado, asi que no lo reintenta.
- Debug menu: FinalRank.RanklessNextLevel carga el bundle del acto DE LA
  ESCENA ACTUAL, asi que al saltar a otro acto no encuentra la escena.
- Atajos de Chrome: el bloqueo de teclado solo existe en pantalla completa, y
  el boton pedia el fullscreen a traves de Unity (difiere la peticion y pierde
  el gesto del usuario) -> el fullscreen no entraba.
- Cheat menu al entrar: CheatsController fuerza CheatsInfoPanelOpen = 1.

### Cambios de v20.1
1. Shaders: 93 assets reescritos con CODIGO REAL y NOMBRE UNICO por asset
   (generador %TEMP%\opencode\gen_variants3.ps1: psx/vertexlit y psx/unlit
   reales + variantes nocull/nofog/ambient/alphatest/additive/transparent/
   zwrite/emissive). Rejilla de pixel 160x120 -> 640x360 (4x mas fina).
   Materiales con shader aun dummy: 46 de 1037 (solo efectos).
2. FirstPersonInputModule.cs: en WebGL no toca Cursor.lockState.
3. MapLoader.DebugLoadLevel(string): carga cualquier nivel eligiendo el bundle
   del acto (act-0 / act-1 / prime) -> el menu de niveles funciona.
4. CheatsController: en WebGL el panel de cheats no se abre al entrar.
5. Pagina: el boton de fullscreen lo pide directamente sobre el contenedor
   (dentro del clic, con gesto de usuario) y luego avisa a Unity -> entra el
   fullscreen y con el el bloqueo de teclado. El menu de niveles usa
   MapLoader.DebugLoadLevel.
### Builds de v20.1 (2026-10-20 ~03:20)
- Player v20.1: BUILD RESULT Succeeded, errors=0, sizeMB=477,1 (incluye los 93
  shaders nuevos, FirstPersonInputModule, MapLoader.DebugLoadLevel y el fix del
  panel de cheats).
- Bundles reconstruidos con los shaders nuevos:
  acts/act-0 113,9 MB | acts/act-1 111,4 MB | acts/prime 32,7 MB.
- Paquete: dist/ULTRAKILL-WebGL-v20.1.zip (253,6 MB, 28 entradas).
- Verificado con Chrome real (CDP): el Tutorial ya se ve con el aspecto
  original (vertex lighting) y con la rejilla fina 640x360 (se acabo el
  aspecto sucio). El rectangulo magenta es el ReadyFlash del propio juego.
- Las copias de trabajo del proyecto (export/ExportedProject_act1*) se han
  borrado; los bundles ya estan copiados al proyecto y al build servido.
### v20.1b (2026-10-20 ~07:20) - por que "no me deja entrar"
- Los dos servidores de desarrollo estaban CAIDOS (los mato una limpieza de
  procesos). Relanzados: http://localhost:8000 (serve-webgl.ps1) y
  http://192.168.0.17:8010 (serve-lan.ps1). Ojo: si se muere el servidor, el
  navegador da ERR_CONNECTION_REFUSED y parece que el juego esta roto.
- El menu de niveles NO funcionaba porque UKDebug estaba declarado con "var"
  DENTRO del IIFE (no era global) y el boton lo llama desde su atributo
  onclick -> ReferenceError. Corregido a window.UKDebug. Verificado:
  el panel se abre con 25 niveles.
- Verificado el salto a un nivel de verdad (Chrome real por CDP):
  [DebugLoadLevel] Level 0-1 -> bundle act-0
  [WebGL] Loading bundle http://localhost:8000/StreamingAssets/acts/act-0?v=202
  y la captura muestra el nivel 0-1 CON la iluminacion vertex-lit original.
- Ojo: FinalRank.RanklessNextLevel no sirve desde el Tutorial
  ("SendMessage: object Panel not found!"); por eso el metodo nuevo
  MapLoader.DebugLoadLevel (que busca el bundle del acto por el nombre).
- Ademas, alguien (el usuario) reconstruyo los bundles a las 04:35; los tres
  act-0/act-1/prime del build servido estan sincronizados con el paquete.
### Paquete de traspaso a otro dispositivo (2026-10-20 ~07:40)
- C:\Users\JJ\Documents\ULTRAKILL-traspaso-v20.1.zip  (proyecto completo, ~5,3 GB
  sin comprimir; el ZIP final depende de la compresion)
- Contenido:
    LEEME-COMO-SEGUIR.txt   <- instrucciones (que hay, como abrir Unity
                               2019.4.16f1, como reconstruir player y
                               bundles, como servir, que funciona y que no)
    ultrakill-webport/      <- el proyecto (sin Library/Temp/Logs: Unity
                               reimporta en 15-20 min la primera vez),
                               original-bundles/, tools/, HANDOFF.md
    dist/ULTRAKILL-WebGL-v20.1.zip  <- el juego listo para jugar
- Se comprimio con WinRAR en modo rapido (-m1 -tzip) para no perder mucho
  tiempo: la mayor parte (texturas, wasm, bundles) ya va comprimida, asi que
  el ratio es pobre (~80-85%).
- Para reducirlo mas: excluir original-bundles/ (944 MB, son los bundles
  ORIGINALES de Windows: referencia, no hacen falta para reconstruir) y/o
  export/ExportedProject/Library/ (3,4 GB, cache regenerable).
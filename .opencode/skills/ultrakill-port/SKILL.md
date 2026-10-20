---
name: ULTRAKILL WebGL Port
description: Conocimiento duro del port WebGL de ULTRAKILL (Unity 2019.4.16f1) - pipeline de build, trampas de shaders/Unity en WebGL, y como verificar cambios sin depender de capturas del usuario. Usar SIEMPRE que se toque este proyecto (Assets, shaders, bundles, build WebGL).
---

# Port WebGL de ULTRAKILL

Port de ULTRAKILL (Unity 2019.4.16f1, WebGL) a partir del build pre-GREED
(Patch 6 Hotfix #2, 19/04/2021) reconstruido con AssetRipper.

- **Proyecto Unity**: `ultrakill-webport/export/ExportedProject` (56k assets, 3,5 GB)
- **Diario del proyecto**: `ultrakill-webport/HANDOFF.md` — **léelo primero**, tiene el
  estado exacto de cada versión, los bugs ya resueltos y lo que queda.
- **Servidores**: `http://localhost:8000/` (local) y `http://192.168.0.17:8010/` (red).
  Se lanzan con `ultrakill-webport/serve-webgl.ps1` y `serve-lan.ps1`.
  **Los servidores en background se mueren**: lánzalos con `background: true` en el
  tool `shell`, o con `Start-Process` desde un `.ps1` intermedio (una ruta con
  espacios en `-ArgumentList` se parte y el servidor no arranca).

## Trampas (esto es lo que cuesta horas si no se sabe)

1. **WebGL/GLES: los uniforms numéricos de `Properties` NO existen en la etapa de
   vertex.** El conversor HLSL→GLSL de Unity (HLSLcc) no los declara →
   `Shader error in 'X': undeclared identifier '_ResY' at line 47 (on gles)`.
   **Solo aparece compilando para WebGL**; en el editor el mismo shader compila
   bien. Solución: declarar el uniform **suelto** (`uniform float _ResY;`) fuera de
   `Properties`, y que el C# lo fije con `Shader.SetGlobalFloat` (los globales de
   Unity son uniforms sueltos, llegan igual). Ojo: si el uniform es `float`, el C#
   debe usar `SetGlobalFloat` (**no** `SetGlobalInt`).

2. **Un bloque `Properties` mal formado en ShaderLab NO da error de compilación.**
   Unity lo ignora en silencio, el shader compila **sin propiedades** (sin
   `_MainTex`) → la textura no se hace bind → **el objeto sale completamente
   negro**. Formato estricto: el header `Properties {` en su propia línea y **una
   property por línea**. Un `Properties_Nombre(...)` pegado es el síntoma típico.

3. **61 de los 168 shaders son "dummy"** (marcador `DummyShaderTextExporter`):
   solo muestrean la textura. Los objetos que los usan se ven mal o negros.
   `asset-index.ps1 dummy-materials` lista los 84 materiales afectados.

4. **AssetRipper exportó las escenas PERMUTADAS** (el fichero `Level 0-1.unity`
   contiene el contenido de otro nivel). **No confíes en el nombre del fichero**:
   usa `asset-index.ps1 scenes` (muestra el título real de cada escena) o el
   `levelName` de la escena.

5. **Hay dos carpetas de shaders** y no son equivalentes:
   `Assets/Shader/**` (107, los que usan los materiales) y `Assets/shaders/**`
   (44, con nombres tipo `_v4`; 718 materiales SÍ los referencian).
   `Assets/resources/shaders/**` = los 17 restantes, dummy.

6. **La UI de Unity necesita el ratón ABSOLUTO.** Si mantienes el puntero
   bloqueado (pointer lock) mientras hay un menú abierto, el EventSystem recibe
   siempre la misma posición y **no se puede pinchar nada**. El juego suelta el
   ratón al abrir menús (`OptionsManager` pone `Cursor.lockState=None` +
   `Cursor.visible=true`); el port tiene que respetarlo.

7. **No mates Unity a mitad de build**: deja el `Library` inconsistente y los
   scripts de `Assets/Editor` dejan de compilar (`TeleportPlayer`/`PlayerPosInfo`
   "not found"). Si pasa, borra los scripts de diagnóstico de un solo uso.

8. **MIRA SIEMPRE EL MTIME DEL LOG** antes de fiarte de sus errores: el log se
   reutiliza entre builds y varias veces leí errores de un build anterior.

9. **PlayerPrefs no se guarda solo en WebGL** (no hay `OnApplicationQuit`):
   llama a `PlayerPrefs.Save()` tras cada ajuste.

10. **Caché del navegador**: los bundles se sirven con `max-age=3600`, así que
    hay que versionar la URL (`MapLoader.WebGLBundleQuery = "?v=NNN"`) y el HTML
    se sirve con `no-store`.

## Verificar cambios (no dependas del usuario)

**1) Arnés visual (Playwright) — LA MÁS VALIOSA.** `ultrakill-webport/tools/visual/ukshot.py`
(Python 3.12 en `%LOCALAPPDATA%\Programs\Python\Python312\python.exe`; `playwright` y
`pillow` ya instalados; Chromium de Playwright ya descargado).

```powershell
$py  = "$env:LOCALAPPDATA\Programs\Python\Python312\python.exe"
$sc  = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\tools\visual\ukshot.py"
& $py $sc --url "http://localhost:8000/?v=205" --out "$env:TEMP\opencode\shots" `
      --actions "wait:50;key:Escape;wait:3;click:640,448;wait:3;shot:pausa;state"
& $py $sc --diff "$env:TEMP\opencode\shots\a.png" "$env:TEMP\opencode\shots\b.png"
```

- Acciones (separadas por `;`): `wait:N`, `key:ESC`, `click:X,Y`, `scroll:N@X,Y`,
  `shot:NOMBRE`, `state`, `send:GameObject,Metodo,Valor`.
- **Los eventos son *trusted*** → el navegador concede el **pointer lock** como
  lo haría el usuario, y las teclas (Escape, F3) llegan al juego.
- `--diff A B` → **% de píxeles distintos**: la forma objetiva de demostrar que
  un cambio altera la imagen. **Usa un control A/B/A** (mismo ajuste dos veces →
  debe dar 0 % de diferencia; si no, la cámara se ha movido y el test no vale).
- **Lee los PNG con la herramienta `read`**: las capturas incluyen el juego Y la
  página (con el pie de la barra de botones).
- `SendMessage` con valor numérico **falla** (Unity lo pasa como string): usa
  los presets sin argumento (`send:Canvas,PCPreset` / `send:Canvas,PSXPreset`).

**2) Arnés CDP** (el del proyecto anterior, `%TEMP%\opencode\cdp_lib.ps1`): `Shot`
también funciona con la ventana oculta, pero Playwright es más fiable y da
página completa. Úsalo solo si Playwright falla.

Las otras dos sondas están en `%TEMP%\opencode\`:
- `probe.js` → métricas de píxeles del canvas (avg, % no negro, nº de colores).
  **Ojo**: la cámara del intro se mueve, así que compara A/B/A rápido o espera a
  que termine la intro; si no, la diferencia se pierde en el ruido.
- `ShaderUniformProbe.cs` → compila variantes de shader en el editor y reporta
  errores. Para responder "¿declara esto bien?" sin adivinar.

**Índice de assets** (no_more grep a ciegas por 56k assets):
`scripts/asset-index.ps1` (en esta skill; también en `ultrakill-webport/tools/assetindex/`).
Índice cacheado en `ultrakill-webport/tools/assetindex/index/` (reconstruir: `build`, ~5 min).

```powershell
$ai = "C:\Users\JJ\Documents\Proyecto predeterminado\.opencode\skills\ultrakill-port\scripts\asset-index.ps1"
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ai scenes          # escena -> nivel real
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ai shader-users "psx/unlit/ambient"
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ai users <guid>     # quien referencia un asset
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ai globals _ResY   # quien declara/usa un uniform
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ai shaders         # nombres duplicados
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ai path <ruta>     # ruta -> GUID
```

## Reglas del proyecto

- **Validar cada cambio mirando el LOG DEL BUILD**, no solo el código de salida:
  `Select-String "Shader error in"` y `Select-String "error CS"`. Los 5 bugs
  anteriores pasaban el build en verde.
- **No tocar el menú F3 ni la carga de bundles**: es lo único verificado que
  funciona (ver HANDOFF.md).
- **No editar a mano el `index.html` del build**: el origen es
  `Assets/Editor/WebGLTemplate.html` y el builder lo copia al build.
- **Documentar cada cambio en `HANDOFF.md`** con su criterio de aceptación.
- Al tocar shaders, **reconstruir los bundles** también (el nivel vive en el
  bundle, no en el player): `WebGLBundleBuilder.BuildAll` + `WebGLBuilder.Build`.
- Si el usuario pide una versión nueva: numerar, empaquetar en
  `ultrakill-webport/dist/`, verificar el ZIP extrayéndolo en limpio y comparar
  hashes, y avisar de los enlaces.

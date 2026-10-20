# Notas técnicas

Documento de referencia del port: qué se hizo, cómo se compila y qué trampas
tiene Unity 2019.4 al compilar para WebGL.

## Objetivo

Portar el build **pre-GREED** (Patch 6 Hotfix #2, 19/04/2021) a WebGL. Ese
build es el último antes de GREED: app `1229490`, depot `1229491`, manifest
`4296180771784366658`, Unity **2019.4.16f1**, scripting **Mono** (no IL2CPP en
el juego original), 1,22 GB.

## Export

`AssetRipper` 2.0 convierte el build en un proyecto de Unity:

- 25 escenas, 576 scripts, 1.820 prefabs, 2.572 materiales, 168 shaders.
- Los bundles del juego son de **StandaloneWindows64**: no se pueden cargar en
  WebGL, así que hay que **reconstruirlos** con `BuildPipeline.BuildAssetBundles`
  para el target WebGL (`Assets/Editor/WebGLBundleBuilder.cs`).
- **Las escenas salen permutadas**: el fichero `Level 0-1.unity` contiene otro
  nivel. El título real de cada escena está en su campo `levelName`
  (`tools/assetindex/index/scenes.tsv` lo deja en una tabla).
- **Los shaders no se pueden exportar**: AssetRipper deja un *stub* que solo
  muestrea la textura. Los reales se toman del repositorio público de
  decompilación `gabrieldj81/Ultrakill-Prelude-Decompilation` y se instalan
  **sobrescribiendo el `.stub` y conservando su `.meta` (GUID)**, para que los
  materiales sigan apuntando al mismo asset.

## Cambios del port

| Fichero | Cambio | Por qué |
|---|---|---|
| `MapLoader.cs` | `AssetBundle.LoadFromFile` → `UnityWebRequestAssetBundle`; `CheckForAct` devuelve `true` en WebGL; `DebugLoadLevel(string)` nuevo | `LoadFromFile` no existe en WebGL; el menú de niveles necesita cargar el bundle del acto que toque, no el de la escena actual |
| `FirstPersonInputModule.cs` | En WebGL no toca `Cursor.lockState` | `Cursor.lockState = None` hace que el navegador suelte el *pointer lock* y Unity nunca lo vuelve a pedir → la cámara se queda en ratón absoluto y no gira 360° |
| `CameraController.cs` | El re-bloqueo del ratón con clic solo se aplica si **no** hay un menú abierto (`IsMenuOpen()`) | Con el menú de pausa abierto, re-bloquear congela el puntero y la UI deja de responder |
| `GraphicsOptions.cs` | `PCPreset()`/`PSXPreset()` aplican los valores; `PlayerPrefs.Save()` tras cada ajuste | Asignar `Dropdown.value` no dispara `onValueChanged`; en WebGL no hay `OnApplicationQuit` y los ajustes no se guardaban |
| `ControlsOptions.cs` | Con Ctrl/Alt/Meta no se reasigna ninguna tecla | Con el teclado sin bloquear, un Ctrl+D abre los marcadores del navegador **y** se guarda como tecla del juego |
| `WebGLRenderFix.cs` | Reposiciona al jugador al inicio del Tutorial, apaga el trigger `TeleportPlayer` del pozo, desactiva la cadena de post-proceso | El salto de intro dejaba al jugador en el aire y el trigger del pozo lo teletransportaba +70 en Y en bucle |
| `Assets/Editor/WebGLTemplate.html` | Página propia: pantalla completa con bloqueo de teclado, *pointer lock*, menú de niveles (F3) | Unity no gestiona el teclado del navegador; hace falta capturarlo a nivel de página |

## Trampas de Unity/WebGL

1. **En WebGL (GLES) los uniforms numéricos de `Properties` no existen en la
   etapa de vertex.** HLSLcc no los declara →
   `Shader error in 'X': undeclared identifier '_ResY' at line 47 (on gles)`.
   En el editor el mismo shader compila bien: **solo se ve compilando para
   WebGL**. Solución: declarar el uniform suelto (`uniform float _ResY;`) fuera
   de `Properties` y fijarlo con `Shader.SetGlobalFloat` (los globales de Unity
   son uniforms sueltos). Si el uniform es `float`, usar `SetGlobalFloat`
   (no `SetGlobalInt`).

2. **Un bloque `Properties` mal formado en ShaderLab no da error.** Unity lo
   ignora y el shader compila **sin propiedades** (sin `_MainTex`) → la textura
   no se hace bind → **el objeto sale completamente negro**. El formato es
   estricto: header `Properties {` en su propia línea y **una property por
   línea**. Un `Properties_Nombre(...)` pegado al header es el síntoma típico.

3. **61 de los 168 shaders son *stubs* de AssetRipper** (marcador
   `DummyShaderTextExporter`): solo muestrean la textura. Los objetos que los
   usan se ven mal o negros.

4. **Las escenas están permutadas** (ver Export).

5. **La UI de Unity necesita el ratón absoluto.** Con el puntero bloqueado
   (*pointer lock*) el EventSystem recibe siempre la misma posición y no se
   puede pinchar nada. El juego suelta el ratón al abrir menús
   (`OptionsManager` pone `Cursor.lockState = None` + `Cursor.visible = true`).

6. **Matar Unity a mitad de build deja el `Library` inconsistente**: los
   scripts de `Assets/Editor` dejan de compilar ("type not found") y el build
   siguiente falla con errores en cascada.

7. **Los logs de build se reutilizan**: hay que comprobar el `LastWriteTime` del
   log antes de fiarse de sus errores.

8. **`PlayerPrefs` no se guarda solo en WebGL** (no hay `OnApplicationQuit`):
   hay que llamar a `PlayerPrefs.Save()`.

9. **Caché del navegador**: los bundles se sirven con `max-age=3600`, así que
   hay que versionar la URL (`MapLoader.WebGLBundleQuery = "?v=NNN"`) y el HTML
   con `no-store`.

10. **El *pointer lock* de Chrome tiene un enfriamiento de ~1,3 s**: tras
    pulsar Escape el navegador rechaza la recaptura durante un momento.

## Build

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\2019.4.16f1\Editor\Unity.exe"
$proj  = "export\ExportedProject"

# bundles (act-0, act-1, prime)
& $unity -batchmode -quit -projectPath $proj -logFile bundles.log `
        -buildTarget WebGL -executeMethod WebGLBundleBuilder.BuildAll
Copy-Item "$proj\Builds\WebGLBundles\acts\*" "$proj\Assets\StreamingAssets\acts\" -Force

# player
& $unity -batchmode -quit -projectPath $proj -logFile player.log `
        -buildTarget WebGL -executeMethod WebGLBuilder.Build
```

**Validar siempre por el log**, no por el código de salida:

```powershell
Select-String -Path $log -Pattern "Shader error in"   # debe ser 0
Select-String -Path $log -Pattern "error CS"           # debe ser 0
Select-String -Path $log -Pattern "BUILD RESULT"
```

Un build sano: `BUILD RESULT: Succeeded | errors=0 | warnings=27 | sizeMB=487,5`.

Los assets de un nivel viven en el **bundle**, no en el player: si se tocan
shaders hay que rehacer **ambos**.

## Herramientas

| Qué | Dónde | Para qué |
|---|---|---|
| Índice de assets | `tools/assetindex/asset-index.ps1` | GUIDs ↔ rutas, materiales→shader, shader→materiales, escena→nivel real, uniform→shaders. Índice cacheado (reconstruir: `build`) |
| Pruebas visuales | `tools/visual/ukshot.py` | Capturas con un navegador headless, teclado/ratón reales y **diff de píxeles** entre dos capturas |
| Progreso de build | `tools/build/ver-progreso.ps1` | Panel del build en curso |

## Historial

| Versión | Qué cambió |
|---|---|
| v19.12 | Menú de pausa y cámara 360° en 0-2…0-5 (bug del `FirstPersonInputModule`) |
| v20.0 | Shaders reales instalados; acto 1 (Limbo/Lust/Gluttony) y Prime integrados; menú de niveles (F3) |
| v20.2 | Corrección de los shaders con el bloque `Properties` mal formado (objetos negros), bloqueo de teclado del navegador, presets de gráficos que sí aplican, rejilla de *vertex snapping* unificada |
| v20.5 | Ajustes de gráficos cableados a los shaders (`_ResY`, `_ColorPrecision`), pausa y cámara verificadas |

# Informe TOWER — revisión final del trabajo ULTRA + STRAWBERRY (ULTRAKILL WebGL v20.8 wip)

- **Fecha:** 21/10/2026
- **Base revisada:** commit `0ad206b54` ("v20.8 wip") + árbol de trabajo (cambios de STRAWBERRY y correcciones de TOWER).
- **Alcance:** parche de warping PSX (107 shaders reales + 1726 materiales con las claves de warping), ajustes PSX en menús, resto de efectos PSX, y coherencia con los cambios v20.7 (renombres de escenas y reparto de bundles).
- **Método:** análisis 100 % estático (scripts Python + greps sobre el árbol de trabajo y sobre blobs de git). **No se ha ejecutado Unity ni ningún build** (regla del encargo).
- **Cambios hechos por TOWER:** 22 escenas `.unity` + `MapLoader.cs`. No se ha tocado ningún `.mat` ni `.shader`, ni se ha hecho commit/push.

## Veredicto: **CORREGIDO**

Con las dos correcciones de la sección 2, el estado del árbol es coherente con el objetivo "siempre aspecto PC, sin look PSX". No queda ningún camino funcional conocido por el que warping, pixelización o compresión de color puedan activarse desde la UI. La única validación que por regla no he ejecutado es el build real de Unity WebGL (`Shader error in` en el log); queda apuntada en la sección 5.

## 1) Fallos encontrados

### F1 — (funcional) La sección `--PSX SETTINGS--` seguía activa en el menú de pausa de 22 escenas

v20.7 eliminó los controles PSX de `Assets/prefabs/Canvas.prefab`, `Tutorial.unity` y `Main Menu.unity`, pero los niveles tienen copias **horneadas** (no instancias de prefab) de todo el menú de opciones dentro del `Player`, y ahí la sección PSX seguía presente y **activa**:

- 1 texto de cabecera `"--PSX SETTINGS--\n"` (GameObject llamado `Text (5)` en estas escenas),
- y los 4 controles: `Pixelization`, `Texture Warping`, `Vertex Warping`, `Color Compression`.

Escenas afectadas (22): `DebugRoom`, `Endless`, `Intermission1`, `Level 0-1`…`0-5`, `0-S`, `1-1`…`1-4`, `1-S`, `2-1`…`2-4`, `2-S`, `3-1`, `3-2`, `P-1`.

Por qué importa: la sección cuelga de `OptionsMenu → Contents` (el mismo menú de pausa del juego) y sus controles son alcanzables. `GraphicsOptions.Initialize()` solo aplica el preset neutro en `Awake`/`Start`; un cambio manual del jugador llama directamente a `Pixelization()` / `TextureWarping()` / etc. y escribe los globales `_ResY` / `_TextureWarping` en el acto, así que el look PSX podía volver durante la partida (pixelización de vértices y warping), además de dejar valores no neutros en `PlayerPrefs`.

### F2 — (riesgo de release) El query de cache de los bundles seguía en `?v=207`

`MapLoader.cs` tenía:

```csharp
// v205: bundles reconstruidos con los shaders graphics-cableados (_ResY).
private const string WebGLBundleQuery = "?v=207";
```

v20.8 cambia los shaders que van **dentro** de los bundles de actos (`acts/act-0`, `acts/act-1`, `acts/prime`). `SmartLoadBundle` añade `WebGLBundleQuery` a la URL, y el servidor de pruebas sirve con `max-age=3600`: un navegador que ya hubiera cargado los bundles v20.7 podía seguir usando shaders viejos (con warping) sin recargarlos. El comentario además estaba desfasado (decía v205 con valor 207).

## 2) Correcciones aplicadas

### C1 — Ocultar (no borrar) la sección PSX en las 22 escenas

En cada una de las 22 escenas he puesto `m_IsActive: 0` en los 5 GameObjects de la sección:

- el GameObject del texto `--PSX SETTINGS--`,
- `Pixelization`, `Texture Warping`, `Vertex Warping`, `Color Compression`.

Total: **22 escenas × 5 objetos = 110 GameObjects desactivados**.

- **Por qué así:** es el cambio mínimo y sin riesgo de YAML. No se borran bloques ni se tocan `fileID`s, así que las referencias serializadas de `GraphicsOptions` (campos `pixelization`, `textureWarping`, `vertexWarping`, `colorCompression`) y del layout siguen siendo válidas; Unity ignora los GameObjects inactivos para render, input y layout, con lo que no se ven ni son clicables (y dejan de ocupar espacio en `Contents`).
- **Auditoría:** comprobado en las 22 escenas que los 110 objetos estaban en `m_IsActive: 1` y ahora están en `0`; que todos cuelgan de `Contents` dentro de `OptionsMenu` (cadena `Contents → Scroll Rect → Video Options → OptionsMenu → Canvas → Player`), y que no hay ningún otro objeto con esos nombres. `Level P-1` tiene un segundo `Text (5)` ajeno (`--MINOR ASSISTS--`): el script identifica la cabecera por el contenido `--PSX SETTINGS--`, no por el nombre, y ese otro objeto sigue activo.
- **Diff exacto:** 22 ficheros, 110 inserciones / 110 borrados, y **0 líneas cambiadas que no sean `m_IsActive`**. Los `.unity` son LF en HEAD y siguen LF (verificado byte a byte en los 22).
- No hay ningún `SetActive(true)` en todo `Assets/Scripts` que pueda reactivar estos objetos, y ningún script aparte de `GraphicsOptions` referencia esos controles.

### C2 — Subir el query de cache a `?v=208`

`MapLoader.cs`:

```csharp
// v208: bundles reconstruidos con el parcheo PSX (afin) y los shaders graphics-cableados (_ResY).
private const string WebGLBundleQuery = "?v=208";
```

- **Por qué:** fuerza la recarga de los tres bundles de actos cuando ULTRA los reconstruya con los shaders v20.8. Diff: 2 líneas; el fichero era LF en HEAD y sigue LF.
- **Aviso para el release:** este query solo cubre los bundles de actos. El build principal (`index.html`, `Build/*.data`, `*.wasm`, `*.framework.js`) también debe servirse con cache-busting/versión nueva en el despliegue (eso ya se hacía con `shots_v207`, `package_v207.ps1`, etc.; está fuera del código).

## 3) Verificación del parche PSX (estado final del árbol)

### 3.1 Shaders — 107 reales, correctos en revisión estática

- Hay **107 shaders reales** (los que tienen `#pragma vertex` + `#pragma fragment`) con el parche: cada uno tiene **exactamente una** declaración `uniform float _TextureWarping;`, **ningún** shader real declara `_TextureWarping` (ni `_ResY`/`_ColorPrecision`) en `Properties`, y cada uno tiene **una** multiplicación de UV en el vertex protegida por `if (_TextureWarping > 0.001)` y el uso en fragment protegido por ternario `(_TextureWarping > 0.001 ? IN.uv_MainTex / IN.normal.r : IN.uv_MainTex)`.
- Los 4 `psx_vertexlit_emissive*` usan el ternario dos veces en el mismo `frag` (textura base y máscara emisiva): ambas protegidas. Sigue habiendo una sola declaración por shader.
- Los 107 tienen **un solo `CGPROGRAM`** y forma correcta (`#include "UnityCG.cginc"`, `vert`/`frag`), tanto los de mayúsculas (`Assets/Shader/`) como los de `Assets/shaders/main|special/`. No hay símbolos duplicados ni fragmentos huérfanos.
- El patrón de **uniform suelto** es intencionado y correcto para este proyecto: en el backend GLES de Unity 2019.4 los uniforms que solo se usan en la etapa de vertex pueden no declararse (`undeclared identifier`), por eso `_ResY`/`_TextureWarping`/`_ColorPrecision` se declaran a mano y `_ResY` se usa **también** en el fragment (comentario incluido en los shaders).
- Con `_TextureWarping = 0` el vértice no multiplica la UV y el fragment no divide por `IN.normal.r`: no hay warping afín. Con el valor a 0 el shader hace el camino "recto".
- Los 107 `.shader` aparecen modificados en el árbol de trabajo **solo por normalización de fin de línea**: comparado con HEAD normalizando CRLF→LF, el contenido es idéntico (0 diferencias de contenido en los 107). No hay cambios de código de TOWER en shaders.
- Limitación: no he compilado. La sintaxis es válida a ojo en HLSL/GLSL ES (ramas con condición uniforme, ternarios, `floor`, sin bucles raros); la confirmación real es el log del build (sección 5).

### 3.2 Materiales — los 1726 con claves de warping quedan inertes

- 2572 `.mat` en total; **1726** contienen `_TextureWarping` y `_VertexWarping`. Tras la corrección de STRAWBERRY: **1701 neutros** (`0 / 9999`) y **25 no neutros** (`1 / 160`), que son inofensivos:
  - 16 con `m_Shader` guid `0000000000000000f000000000000000` (referencia rota / builtin, no hay shader que lea las claves),
  - 9 sobre los 3 dummies `psx_railgun_{0,1,2}.shader` (shaders planos de `tex2D`, sin warp; además solo declaran `_VertexWarpScale`).
- Con esto, ningún material sobre shader real puede pisar los globales con un valor de warping no neutro (en Unity la propiedad serializada del material tiene prioridad sobre `Shader.SetGlobalFloat`), que era el fallo que corrigió STRAWBERRY en 535 ficheros.
- Nota: el único "shader" que conserva `_TextureWarping` en `Properties` es el dummy `Assets/shaders/main/ultrakill-vertexlit-emissive.shader` (surface shader de exportación que **no usa** la clave; sus 17 materiales están neutros o sin la clave). No es uno de los 107 reales y no pinta nada con warping.

### 3.3 Resto de efectos PSX — neutralizados

- **Vertex snapping / pixelización:** gobernado por `_ResY`. Con `_ResY = 0` (`ApplyNeutral`) el `if (_ResY > 0.5)` no entra y el shader reconstruye la posición de clip original (`vertex.xyz *= snapToPixel.w`), sin rejilla. No hay ningún shader real que use `_VertexWarping` para snapping (se comprueba: 0 usos en código de los 107).
- **Compresión de color:** siempre se ejecuta `floor(color.rgb * prec) / prec` pero con `_ColorPrecision = 2048` → `prec = 2047` (11 bits por canal), imperceptible; y si el global aún no se hubiera escrito, el fallback del propio shader también da `prec = 2047` (`resY = 2048`). No hay `dither`, `trunc`, `round` ni `sin`: barrido sobre los 107 sin resultados.
- **Radial:** el shader `psx/vertexlit/radialdistortion_v2` no contiene ninguna matemática radial; su código es el mismo warp afín con el guard. Sin efecto.
- **Cubemap/skybox:** el `o.reflect *= o.normal` de los reflexivos es una multiplicación por un escalar (el factor afín), no cambia la dirección del cubemap; con el guard a 0 no se aplica.
- **`_VertexWarping` global:** `GraphicsOptions` lo deja en 9999 ("off" del juego) y ningún shader real lo lee.

### 3.4 Escritores de los globales — correcto

- Barrido de todo `Assets/Scripts/**/*.cs`: los únicos `Shader.SetGlobalFloat` de `_ResY`, `_TextureWarping`, `_VertexWarping`, `_ColorPrecision` están en `GraphicsOptions.cs`. (`OptionsMenuToManager` toca `_UseEdgeDetection`, `ULTRAKILL_PostProcessHandler` `_SandTex`, `Water` `_PlayerIsUnderwater`; nada que ver.)
- `ApplyNeutral()` = Pixelization 0 / TextureWarping 0 / VertexWarping 0 (`_VertexWarping = 9999`) / ColorCompression 0 (`_ColorPrecision = 2048`). `Initialize()` ignora a propósito los `PlayerPrefs` (`Pix`, `TexWar`, `VerWar`, `ColCom`) y `PSXPreset()` redirige a `ApplyNeutral()`, así que un `Pix = 3` guardado de una partida vieja no vuelve a activar nada.
- `DownscaleChangeSprite.CheckScale()` solo cambia sprites de UI según `"Pix"`; no afecta al render 3D.

### 3.5 Cambios v20.7 (escenas / bundles) — coherentes

- Reparto de bundles: `act-0` = `Tutorial` + `Level 0-*`; `act-1` = `Level 1-*` + `Level 2-*` + `Level 3-*` + `Intermission1`; `prime` = `Level P-*`. `Intermission1` no está en `act-0`; no queda ninguna referencia a `Level 3-S`; los `targetLevelName` no vacíos apuntan a escenas existentes (`Level 0-2`, `0-S`, `1-1`… etcétera).
- Build principal (`EditorBuildSettings`): `Intro`, `Main Menu`, `Endless`, `DebugRoom`, `Tutorial`. `Endless` y `DebugRoom` llevan ahora la corrección C1; `Tutorial` y `Main Menu` ya venían sin sección PSX de v20.7; `Intro` no tiene sección PSX.

## 4) Dudas y cosas que he dejado sin tocar

1. **CR extra en la línea de `_ResY` (107 shaders).** Los 107 terminan la línea `uniform float _ResY;` con `\r\r\n` (un `\r` de más, probablemente de la inserción automática del parche). Es espacio en blanco para HLSL/GLSL y no debería romper nada, por eso **no lo he tocado** (habría que editar 107 ficheros). Si el build saca un warning/error raro en esa línea, quitar el `\r` duplicado es trivial.
2. **La UI PSX sigue físicamente en las 22 escenas**, solo inactiva. Es el equivalente mínimo y seguro a borrarla (borrar los bloques YAML sería un diff enorme y con riesgo de referencias). Hoy no hay ningún `SetActive(true)` que pueda revivirla (0 resultados en `Assets/Scripts`), pero si en el futuro alguien añade uno, volvería.
3. **Prefabs huérfanos con sección PSX.** Además de las escenas, siguen con copia de la sección (y de la cabecera) `prefabs/player/Player.prefab`, `prefabs/levels/POOPO SECRET.prefab`, los 5 `prefabs/levels/special rooms/FirstRoom*.prefab` y los 9 `GameObject/FirstRoom*.prefab`. **No los he tocado porque no los usa nada**: he buscado sus GUID en todas las escenas, prefabs, `.asset` y `.cs` y no aparecen; no están en `EditorBuildSettings` ni en las listas de escenas de `WebGLBundleBuilder`, así que no entran en el build. Si ULTRA cambiara eso, habría que limpiarlos.
4. **Dummy `shaders/main/ultrakill-vertexlit-emissive.shader`:** declara `_TextureWarping` con default 1 y `_VertexWarping` con default 160 en `Properties`, pero su `surf` no las usa y sus materiales están neutros. Inerte hoy; lo dejo por no ampliar el diff en un fichero que no forma parte de los 107.
5. **Comentario de `GraphicsOptions.cs` desfasado.** Dice que los GameObjects de los controles "se han BORRADO del prefab Canvas y de las escenas (Tutorial y Main Menu)"; ahora, además, están **desactivados** en los niveles. No lo he cambiado (es un comentario), pero conviene ajustarlo cuando se toque el fichero.
6. **`ESTADO-ACTUAL.md` sigue diciendo `?v=207`** y las capturas `shots_v207`; es documentación de la iteración anterior, no la he tocado. Tras C2, ese dato queda desactualizado.
7. **Sin compilar.** No hay forma de verificar en estático la GLES real (es el backend que históricamente dio el problema de uniforms). Hace falta el log de ULTRA.
8. **El guard de `_TextureWarping` es binario** (`> 0.001`), no gradual: al reactivarlo da warping pleno, no escala con el valor del slider. Con la sección oculta y el global a 0 da igual; si algún día se reexpone el slider, habría que decidir si se quiere gradual.

## 5) Qué falta por hacer (para ULTRA)

1. **Reconstruir los bundles de actos** (`ULTRAKILL Port/Bundles/Build All` → `acts/act-0`, `acts/act-1`, `acts/prime`), para que incluyan las 22 escenas corregidas y los shaders v20.8; el query `?v=208` hará que el navegador no reutilice los viejos.
2. **Reconstruir el player completo** (`WebGLBuilder.Build`): `Endless` y `DebugRoom` están en `EditorBuildSettings` y llevan el cambio C1.
3. **Revisar el log del build** con `Select-String "Shader error in"` (y de paso `already exists`): es la única validación GLES real. Si aparece algo en la línea de `_ResY`, aplicar la duda nº 1.
4. **Servir con cache-busting nuevo** (`?v=208` o ruta nueva) y comprobar en red que `acts/act-*?v=208` devuelve el bundle nuevo, no un 304 del viejo.
5. **Smoke test de UI:** abrir un nivel desde bundle (p. ej. 1-4), `ESC → Options → Video`: no debe aparecer `--PSX SETTINGS--` ni pixelización/warping; comprobar también que la lista de opciones no deja un hueco raro (los inactivos no ocupan layout).

---

### Resumen de cambios de TOWER (para el commit de ULTRA)

| Fichero(s) | Cambio | Líneas |
|---|---|---|
| `Assets/Scenes/*.unity` (22: DebugRoom, Endless, Intermission1, 0-1…0-5, 0-S, 1-1…1-4, 1-S, 2-1…2-4, 2-S, 3-1, 3-2, P-1) | `m_IsActive: 1 → 0` en cabecera PSX + 4 controles | 110 |
| `Assets/Scripts/Assembly-CSharp/MapLoader.cs` | `?v=207 → ?v=208` y comentario `v205 → v208` | 2 |

Total: 23 ficheros, 112 líneas. Sin cambios en `.mat`/`.shader` (los 535 `.mat` y la normalización EOL de los 107 `.shader` son trabajo previo de STRAWBERRY/ULTRA, intacto). Sin commit.

# Informe STRAWBERRY — revisión de ULTRA (v20.8 wip, commit `0ad206b54`)

Fecha: 21/10/2026 · Alcance: parche de warping PSX de ULTRA (107 shaders reales + 1149 materiales)
y estado general del "aspecto PSX". No se han lanzado builds de Unity (regla). Análisis 100 % estático
(scripts Python + greps sobre blobs de git + árbol de trabajo).

## 1) Fallos encontrados

### F1 — 535 materiales de shaders reales seguían con warping activo (fallo funcional)
El barrido de materiales de ULTRA cambió 1149 `.mat` (TW 1→0, VW 160→9999), pero se le escaparon
**535 materiales vinculados a shaders reales de los 107 parcheados**, casi todos los duplicados
`_N` de AssetRipper (`Altar_0.mat`, `Arm White_1.mat`, `Nailgun_2.mat`, …):

| Combinación ( `_TextureWarping`, `_VertexWarping` ) | Materiales sobre shader REAL |
|---|---|
| `1 / 160` | 529 |
| `0 / 160` | 3 |
| `0.3 / 160` | 3 |

Reparto por shader afectado (los 529 del primer grupo):
`psx_vertexlit_vertexlit{_0,_1,_2}` (130+122+58 = **310 materiales de geometría de nivel**, el shader
principal del mundo), `psx_unlit_ambient_{0,1,2}` (36×3), `psx_unlit_unlit_*` (24), `ultrakill-vertexlit`
y `ultrakill-vertexlit-radialdistortion` (24+24 vía sus idiomas `ultrakill-*`), transparencias, fresnels,
alphatest, etc. Ubicación: 534 en `Assets/Material/`, 1 en `Assets/models/nailgun/Nailgun_1.mat`.

Por qué importa: los shaders reales NO declaran `_TextureWarping` en `Properties` (verificado en los
107: 0 propiedades warp), o sea que el uniform llega por la ruta global/material. En el pipeline legacy
de Unity, si el material tiene un valor serializado con ese nombre, el material manda sobre
`Shader.SetGlobalFloat` (que es solo el *fallback* para propiedades que el material no define). Con
`_TextureWarping: 1` en el `.mat`, esos objetos seguirían con el mapeo afín aunque `GraphicsOptions`
pusiera el global a 0. Es exactamente el objetivo de esta iteración fallado: **el look PSX no habría
desaparecido en los materiales del nivel**.

Incluso si el runtime no enlazara el valor serializado (semántica que no se puede cerrar del todo sin
compilar), el estado quedaba **incoherente con el propio parche de ULTRA** (su objetivo declarado:
"poner a neutro los valores de warping de 1149 materiales"). La corrección es segura en ambos mundos.

Detalle del objetivo chasco de ULTRA: escribió "1140 materiales con `_TextureWarping: 1` y 1149 con
`_VertexWarping: 160`"; el recuento real era mayor (1726 `.mat` con la clave; 1689 no neutros antes de
mi corrección): su barrido no cubrió los sufijos `_0/_1/_2`.

### (Nada más funcional) — el resto del parche de ULTRA sale limpio

## 2) Qué he corregido y por qué

**Corrección única: neutralizar los 535 materiales restantes** con el mismo cambio exacto que aplicó
ULTRA al resto (`- _TextureWarping: <x>` → `0`, `- _VertexWarping: <y>` → `9999`):

- Diff: 535 ficheros, 1067 líneas cambiadas (529 con las dos líneas, 3 solo VW, 3 con TW 0.3→0 + VW).
- Formato byte-idéntico al de ULTRA (4 espacios + guion YAML), **LF preservado** en los editados
  (verificado: 49 LF / 0 CRLF originales, se mantienen; sin BOM añadido; sin CRLF nuevo).
- Con la corrección, la re-comprobación total del proyecto da **0 materiales con clave warp no neutra
  sobre shader real**. Quedan 25 no-neutros **intencionadamente**: 9 sobre shaders dummy (el dummy no
  lee esas claves, inerte) y 16 con referencias de shader rotas (guid inexistente o `00000000…`,
  no pueden enlazar nada). No los he tocado: no afectan al render y meterlos habría crecido el diff
  sin beneficio.

Verificación realizada tras el cambio:
- Escaneo completo de los 2572 `.mat`: 0 claves no neutras sobre shaders reales (antes 535).
- `git diff` por fichero: exactamente las 2 líneas (comparado con el patrón de ULTRA: idéntico).
- Ningún `.prefab`/`.unity`/`.asset` contiene claves de warping instanciadas (cero) → la cobertura
  `.mat` es completa.
- No se ha hecho commit ni push (árbol de trabajo; 535 ficheros modificados respecto a HEAD).

## 3) Verificación del parche de shaders de ULTRA (sale limpio; nada que corregir)

1. **Diagnóstico confirmado**: 111 fuentes del mapeo afín — `tex2D(_MainTex, IN.uv_MainTex / IN.normal.r)`
   107 + `tex2D(_EmissiveTex, IN.uv_MainTex / IN.normal.r)` 4 — el original Hamburguesa `texCUBE`
   nunca dividió (comprobado en el blob anterior), el vertex premultiplica y el fragment divide por la
   distancia interpolada: es warping de PSX. NADA del parche toca mal esa cadena.
2. **Cobertura exacta**: 107/107 shaders reales parcheados, 0 dummies tocados, 0 shaders reales
   olvidados, 0 mención a `_TextureWarping` fuera de lo esperado en los 107 (1 `uniform` + 1 vertex-if
   + 1 ternario por muestreo; 326 menciones en total = 107+107+111+1 Properties de un dummy).
   Todos los premultiply están guardados (`if (_TextureWarping > 0.001) { … }`) ×107, todos los divides
   están guardados (ternario ×111, incluidos los 4 emissive).
3. **IN.normal** no se usa para nada más fuera de la división (0 ocurrencias extra: el reflejo del
   cubemap va por `IN.reflect` en el interpolador COLOR2, niebla por `IN.colorFog` → el cambio de
   lectura no rompe nada más).
4. **Vertex**: `o.normal = distance + …` sigue escribiéndose incondicional (necesario para la interpolación
   de la escala y para el `o.reflect *= o.normal` de los cubemaps): **no es fallo** — el ternario decide en
   el fragment si se usa. Ninguna no-guardada.
5. **Sin paths restantes PSX**: `floor()` en los 107 = solo rejilla de vertex snapping (guardada con
   `if (_ResY > 0.5)` ×107) + cuantización de color (1 bloque por fichero, neutrales `_ResY=0` /
   `_ColorPrecision=2048` → `prec=2047`, error ≤1/2047 = imperceptible). 0 dither, 0 `trunc`, 0 `round`,
   0 otro floor. Los "radialdistortion"/"cyber"/"pulsating" **no contiene** distorsión radial ni
   efectos geométricos PSX: es el mismo bloque estándar (nombre heredado).
6. **Compilación GLES/HLSL**: 
   - 1 solo `CGPROGRAM` por fichero; `uniform float _TextureWarping;` justo tras `#include UnityCG.cginc`,
     antes de `vert` y de `frag` en los 107 (verificado por orden en la fuente).
   - Las 2 formas de codificación (0-indent ×74 y 5-tab ×29 + variante emissive ×4) usan la misma forma.
   - Ternario con ramas idénticas (ambas leen `IN.uv_MainTex`, mismo `float2`) → sin problema de tipos
     HLSLcc. La misma forma (`cond ? … : …`, `if (cond) { … }`) ya compila en GLES tras v20.2–v20.7 con
     `_ResY`/`_ColorPrecision` (mismo patrón, mismo backend).
   - Balance de `{}` verificado contra el blob previo: 0 errores.
7. **Materiales**: nada más de lo explicado (F1).
8. **Dummies (61)**: intocados ✓ (código sin render, inerte).

## 4) C#

- `GraphicsOptions.cs` ✓: `ApplyNeutral()` desde `Awake`+`Start`+`Initialize`; `TextureWarping(0)` →
  `SetGlobalFloat("_TextureWarping", 0)` ✓ (0×0.003=0), `VertexWarping(0)` → 9999 ✓, `PSXPreset` ==
  neutral ✓ (_yaw de PlayerPrefs ignorado, null-safe UI borrada).
- `WebGLRenderFix.cs` ✓: no toca globals de warping; apaga la cadena de post-proceso (irrelevante para
  el mapeo afín, pero es lo que v20.7 ya hizo).
- **Nadie más** escribe/lee `_TextureWarping`/`_VertexWarping`/`_ResY`/`_ColorPrecision`: el grep
  total de `Scripts/` solo muestra `GraphicsOptions` (dueño único) y llamadas ajenas que son del
  juego original (`EndlessGrid._PCGamerMode=0`, `Railcannon._EmissiveIntensity`).
- Escenas/prefabs: solo quedan 2 callbacks wired a `PCPreset` (→ neutral, correcto) en `Tutorial.unity`;
  no hay callbacks a `TextureWarping/VertexWarping/Pixelization/ColorCompression`.
- Sin psicología `GraphicsOptions` en la escena: las globals parten de default GL = 0 para
  `_TextureWarping` y `_ResY` (flat), `_ColorPrecision=0` se re-deriva a `prec=2047` (no-op):
  **los defaults del shader son neutro igualmente**. Robusto.

## 5) Dudosos, dejado como estaba (documentado para ULTRA)

1. **Cubemap: `o.reflect *= o.normal;` (vertex, incondicional)** en los shaders `psx_reflective_*` /
   `psx-unlit-{add,mult,…}`: premultiplica el vector de reflexión por la escala afín y el fragment
   muestrea el cubemap sin dividir (`texCUBE(_Cube, IN.reflect)`). Es el comportamiento original del
   juego (comprobado en `0ad206b54^`), no leía ningún global y NO es el efecto visual de "rayas
   radiales" del mapeo afín de UV. Apagarlo (gated con `_TextureWarping`) sería tomar una decisión de
   diseño más allá de corregir un fallo: no lo he tocado. Recomendación si las reflexiones se ven
   "onduladas" tras el build: mismo patrón del ternario para `o.reflect`/`texCUBE`, pero decidido por
   ULTRA al final.
2. **EOL de los 107 shaders**: ULTRA reescribió los 107 de LF (en repo) a **EOL mixto** (108 líneas
   CRLF + 2 LF sueltas por fichero de media). Los 1149 `.mat` sí conservaron LF. Unity no le importa y
   no lo re-normalizo (sería otro diff gigante); solo apuntado en el informe. Al editar los ficheros yo
   he preservado el EOL actual.
3. **Dummies con `_TextureWarping`/`_VertexWarping` en Properties** y los 9 mats dummy con `1/160`:
   código dummy, inerte. Sin tocar. Ídem 16 mats con guids de shader inexistentes (`00000000…` u
   otros): no pueden enlazar nada.
4. **`_VertexWarping` ya no es funcional**: ningún shader real lo lee (el único que lo declara es el
   dummy `ultrakill-vertexlit-emissive`, línea de Properties). El `9999` es cosmético, coherente con
   ULTRA.

## 6) Pendiente de ULTRA (siguiente etapa del ciclo)

1. Recompilar **ambos**: `WebGLBundleBuilder.BuildAll` + `WebGLBuilder.Build` (los shaders viven en los
   bundles). Validar por log: `Select-String "Shader error in"` = 0 y `error CS` = 0.
2. Verificación visual A/B/A con `tools/visual/ukshot.py` en un nivel con vertexlit (`psx_vertexlit_vertexlit`
   cubrió 529 mats: cualquier pared de Limbo/Lust/Gluttony) y en el Intro del Tutorial; el look PCX del
   warping ha de haber desaparecido. Si algo sigue deformado en el visor, el sospechoso nº 1 es el
   binding global-vs-material descrito en F1 (ya apalizado a 0 en ambos lados).

## Veredicto: **CORREGIDO**

El parche de shaders de ULTRA es correcto y completo en los 107; el fallo real era el barrido parcial de
materiales (535 restantes, tocables y visibles en la geometría del nivel: quedaba warping activo). Corrección
aplicada al árbol de trabajo, diff mínimo (2 líneas por fichero), sin commits, sin tocar dummies ni
arquitectura. Restos PSX restantes: 0 (solo queda el matiz del cubemap escrito en §5.1).

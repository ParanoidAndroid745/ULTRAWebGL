---
description: Consulta el índice de assets del proyecto Unity (GUIDs, materiales, shaders, escenas, uniforms)
---

Consulta el índice de assets del proyecto Unity del port ULTRAKILL. **No hagas grep a ciegas por 56.000 assets**: usa el índice.

Herramienta: `%TEMP%\opencode\...` → en realidad
`.opencode/skills/ultrakill-port/scripts/asset-index.ps1`
(índice cacheado en `ultrakill-webport/tools/assetindex/index/`; `build` lo reconstruye en ~5 min).

Comandos (sustituye `CMD` por el que toque la pregunta):

!`powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Users\JJ\Documents\Proyecto predeterminado\.opencode\skills\ultrakill-port\scripts\asset-index.ps1" stats`

| Pregunta | Comando |
|---|---|
| ¿qué nivel real contiene cada escena? | `scenes` |
| ¿qué materiales usan este shader? | `shader-users "psx/unlit/ambient"` |
| ¿qué assets referencian este GUID/fichero? | `users <guid>` / `path <ruta>` |
| ¿qué shaders declaran o usan este uniform? | `globals _ResY` |
| ¿qué shaders tienen nombre duplicado? | `shaders` |
| ¿qué materiales usan un shader dummy? | `dummy-materials` |
| GUID de una ruta | `path <ruta>` |

Pregunta concreta: $ARGUMENTS

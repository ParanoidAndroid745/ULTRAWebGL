---
description: Reconstruye los bundles y el player WebGL del port ULTRAKILL y verifica el log del build
---

Reconstruye el port WebGL de ULTRAKILL y **verifica el resultado por el log**, no por el código de salida.

1. Lee primero la skill `ultrakill-port` (trampas de shaders/Unity en WebGL) y `ultrakill-webport/HANDOFF.md` (estado actual).
2. Rebuild completo (los assets de un nivel viven en el **bundle**, no en el player: si tocas shaders, hay que rehacer ambos):
   - bundles: `%TEMP%\opencode\build206.ps1` → `WebGLBundleBuilder.BuildAll`
   - player: `WebGLBuilder.Build`
   Lánzalo en background y espera a que termine (6-8 min bundles + ~6 min player).
3. **Comprueba el log mirando su mtime** (el nombre del log se reutiliza entre builds; leer un log viejo fue el error que me costó dos iteraciones):
   - `Select-String "Shader error in"` → debe ser 0
   - `Select-String "error CS"` → debe ser 0
   - `BUILD RESULT: Succeeded | errors=0`
   Si hay errores de shader, **arréglalos antes de seguir**: las dos causas habituales están en la skill (bloque `Properties` mal formado en ShaderLab, y uniforms numéricos de `Properties` que no existen en la etapa de vertex en GLES).
4. Copia los bundles a `Assets/StreamingAssets/acts/` si el builder no lo hizo.
5. Actualiza `ultrakill-webport/HANDOFF.md` con el resultado y sus criterios de aceptación.

Argumento opcional (número de versión para el empaquetado): $ARGUMENTS

---
description: Captura el juego en marcha con Playwright y lo analiza leyendo los PNG + diff de píxeles
---

Captura el port WebGL de ULTRAKILL **y míralo de verdad** (tool `read` sobre el PNG). No deduzcas el resultado de los logs: míralo.

1. Comprueba que el servidor responde: `http://localhost:8000/` (si no, lánzalo con `serve-webgl.ps1` en background).
2. Usa el arnés visual (Playwright, eventos *trusted* → el navegador concede el pointer lock como el usuario):

```powershell
$py = "$env:LOCALAPPDATA\Programs\Python\Python312\python.exe"
$sc = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\tools\visual\ukshot.py"
& $py $sc --url "http://localhost:8000/?v=205" --out "$env:TEMP\opencode\shots" --actions "wait:50;key:Escape;wait:3;click:640,448;wait:3;shot:pausa;state"
```

Acciones: `wait:N`, `key:ESC`, `click:X,Y`, `scroll:N@X,Y`, `shot:NOMBRE`, `state`, `send:Obj,Metodo,Valor`.

3. **Lee cada PNG con la herramienta `read`** y analiza lo que se ve (mundo negro, enemigos sin textura, iluminación, HUD, menús).
4. Para demostrar que un cambio altera la imagen, usa el diff: `& $py $sc --diff a.png b.png` → % de píxeles distintos. **Con control A/B/A**: el mismo ajuste dos veces debe dar 0 %.
5. Ojo: la cámara del intro se mueve sola → espera a que termine (o mide A/B/A rápido). `SendMessage` con valor numérico falla (Unity lo pasa como string): usa `send:Canvas,PCPreset` / `send:Canvas,PSXPreset`.

Opcionales: 1er argumento = versión a cargar (p. ej. `205`), 2º = tecla a pulsar.

# Verificar sin depender del usuario

Este entorno **no permite capturas** con el tool `browser` (la ventana del
escritorio está oculta) y el navegador automatizado **no concede pointer lock ni
fullscreen reales**, ni entrega eventos de teclado (`browser.press` no llega a la
página). Hay sondas propias para eso.

## 1) Arnés visual con Playwright (LA MÁS VALIOSA)

`ultrakill-webport/tools/visual/ukshot.py`. Python 3.12 en
`%LOCALAPPDATA%\Programs\Python\Python312\python.exe`; `playwright` y `pillow`
instalados; Chromium de Playwright ya descargado (`%LOCALAPPDATA%\ms-playwright`).

```powershell
$py = "$env:LOCALAPPDATA\Programs\Python\Python312\python.exe"
$sc = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\tools\visual\ukshot.py"
& $py $sc --url "http://localhost:8000/?v=205" --out "$env:TEMP\opencode\shots" `
      --actions "wait:50;key:Escape;wait:3;click:640,448;wait:3;shot:pausa;state"
```

| Acción | Qué hace |
|---|---|
| `wait:N` | espera N segundos |
| `key:ESC` | pulsa una tecla (evento *trusted*: llega al juego) |
| `click:X,Y` | clic en coordenadas de página |
| `scroll:N@X,Y` | rueda del ratón (N>0 abajo) en un punto |
| `shot:NOMBRE` | captura (página completa: juego + barra de botones) |
| `state` | vuelca pointer lock, cursor del canvas, fullscreen, errores |
| `send:Obj,Metodo,Valor` | `unityInstance.SendMessage(...)` |

**Diferencia entre dos capturas** (lo que demuestra objetivamente que un cambio
alteró la imagen):

```powershell
& $py $sc --diff a.png b.png
# pixels distintos: 147671 / 1024000 = 14.421%
```

**Regla del control**: compara A/B/A. El mismo ajuste dos veces debe dar **0 %**
(si no, la cámara se ha movido y el test no vale). Ejemplo real validado:
preset PSX vs PC = **14,42 %** de píxeles distintos, control PSX vs PSX = **0,00 %**.

**Limitación**: `SendMessage` con valor numérico **falla** (Unity lo envía como
string y el método espera un int) → usa los presets sin argumento:
`send:Canvas,PCPreset`, `send:Canvas,PSXPreset`.

**Cuidado con el intro**: la cámara del Tutorial se mueve sola al arrancar;
espera a que termine (o mide A/B/A rápido) o las comparaciones no valen.

## 2) Arnés CDP (respaldo)

`%TEMP%\opencode\cdp_lib.ps1` — `Start-CdpBrowser`, `Eval`, `Key`, `Click`,
`Shot`, `Get-CdpEvents`, `Stop-CdpBrowser`, `Focus-ChromeWindow`.

`Shot` usa `Page.captureScreenshot` y funciona con la ventana oculta, pero el
WebSocket a veces se cae al cargar el juego → mata los Chrome con
`--user-data-dir chrome-cdp-profile` y relánzalo. Playwright es más fiable.

## 3) Sonda de píxeles del canvas

`%TEMP%\opencode\probe.js`. Se engancha a `requestAnimationFrame` y lee el
framebuffer con `gl.readPixels` **dentro** del rAF (Unity crea el contexto con
`preserveDrawingBuffer:false`, así que fuera del rAF el buffer está vacío).

Métricas: `avg:[r,g,b]`, `nonBlackPct`, `blackPct`, `distinct` (nº de colores).

Interpretación:
- `nonBlackPct` ~95-98 % = el mundo se dibuja. ~50 % o menos = pantalla negra.
- `distinct` alto = imagen con detalle; bajo = imagen plana.

Hoy `--diff` (Pillow) es mejor que estas métricas: compara imagenes enteras.

## 4) Sonda de compilación en Unity (sondas de shaders)

`%TEMP%\opencode\ShaderUniformProbe.cs` → copiar a `Assets/Editor/` y ejecutar:

```powershell
& $unity -batchmode -quit -projectPath $proj -logFile $log -executeMethod ShaderUniformProbe.Run
Select-String -Path $log -Pattern "\[PROBE\]"
```

Compila variantes mínimas y reporta `ShaderUtil.GetShaderMessages` de cada una.

**OJO**: el error "undeclared identifier" de los uniforms de `Properties` **solo
sale al compilar para WebGL**; en el editor (d3d11) el shader compila bien.

**Y borra la sonda del proyecto antes de hacer un build** (si no, entra en el
build y puede romper la compilación del editor assembly).

## 5) Índice de assets

`scripts/asset-index.ps1` (de esta skill). Índice cacheado en
`ultrakill-webport/tools/assetindex/index/` (~5 MB, 26.552 GUIDs, 60.357
referencias inversas). Reconstruir: `build` (~5 min).

Resuelve de segundos lo que a mano costaba minutos: qué materiales usan un
shader, qué asset referencia un GUID, qué escena contiene qué nivel real, qué
shaders declaran un uniform.

## 6) ffmpeg (viene con Playwright)

`%LOCALAPPDATA%\ms-playwright\ffmpeg-1013\ffmpeg-win64.exe` → extraer
fotogramas de un MP4 del usuario en vez de la chapuza de Chrome.


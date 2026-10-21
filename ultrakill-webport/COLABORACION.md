# Colaboración en el port WebGL de ULTRAKILL

Este documento es para **cualquiera que se sume al proyecto** (o para volver a
montar el entorno en otra máquina). Lo que hace falta para trabajar aquí.

## Qué es esto

Un port jugable de **ULTRAKILL** (Unity 2019.4.16f1) al navegador, reconstruido
con AssetRipper a partir del build pre-GREED y recompilado con el módulo WebGL de
Unity. Estado actual: **v21** (pre-release en GitHub).

- `LEEME.md` — qué se puede jugar y cómo.
- `NOTAS.md` — **los cambios del port y las trampas de Unity/WebGL**. Léelo antes de
  tocar nada.
- `ESTADO-ACTUAL.md` — el estado de trabajo vivo: qué se hizo, qué falta y por qué.
- `REGENERAR-ASSETS.md` (si existe) — cómo regenerar los assets binarios del juego.

## Requisitos

| Qué | Versión | Notas |
|---|---|---|
| Unity | **2019.4.16f1** | con licencia **Personal** activa. Es la versión con la que se compila el port; otras dan otros errores de shader. |
| Python | 3.12 | para el arnés de pruebas: `pip install playwright pillow` y `playwright install chromium`. |
| Navegador | Chrome/Edge | WebGL2. Para bloquear los atajos del navegador hace falta **pantalla completa** (Chrome solo concede el bloqueo de teclado si la página entra en fullscreen ella misma). |
| Sistema | Windows | el servidor y los scripts son `.ps1`. En Linux/macOS habría que adaptar `tools/serve-webgl.ps1`. |

⚠️ **Smart App Control** (o el antivirus que lo implemente) **bloquea IL2CPP**:
hay que desactivarlo o el build falla al final sin error claro.

## Puesta en marcha

```powershell
git clone https://github.com/ParanoidAndroid745/ULTRAWebGL.git
cd ULTRAWebGL
```

### 1. Los assets del juego

**Van en el repo** ✓: texturas, audio, mallas, animaciones, prefabs, materiales,
escenas y shaders (~57.000 ficheros). Así que un `git clone` normal vale: no hay
que pasarle nada aparte.

Lo único que **no** está en el repo son los **bundles WebGL construidos**
(`Assets/StreamingAssets/`, ~340 MB): son salidas de build, y GitHub bloquea
ficheros de más de 100 MB. Se regeneran con `tools/build-webgl.ps1` (~12 min).

### 2. Abrir el agente

Abre **opencode** en la raíz del repo. Se cargan solos:

- `.opencode/skills/ultrakill-port/` → la **skill** con las trampas de Unity/WebGL
  (léela antes de tocar shaders o escenas).
- `.opencode/commands/` → los comandos `/build`, `/shots`, `/idx`.
- `.opencode/agents/` → los agentes de la cadena de revisión (ver abajo).
- `opencode.json` → el agente por defecto y los permisos (pedir confirmación antes
  de hacer `git push` o publicar releases).

## La cadena de revisión

El proyecto usa un protocolo de revisión en cadena, con **modelos distintos** en
cada paso para que no se capelan los mismos fallos:

```
ULTRA (tú)  →  STRAWBERRY (GLM-5.3 Flash Free, max)  →  TOWER (DeepSeek V4.1 Flash Free, max)  →  ULTRA
```

- **ULTRA** hace el trabajo, compila y verifica, y entrega al usuario.
- **STRAWBERRY** analiza con dureza el trabajo de ULTRA y **corrige los fallos**
  (sin refactorizar ni añadir features). Informe en `%TEMP%\opencode\STRAWBERRY-informe.md`.
- **TOWER** revisa el conjunto y corrige lo que quede. Informe en `%TEMP%\opencode\TOWER-informe.md`.

Los revisores **no compilan** (cada build son 6-12 min y Unity no admite dos a
la vez): eso es de ULTRA.

## Construir

```powershell
# bundles (los assets de cada nivel viven aquí) + player
powershell -ExecutionPolicy Bypass -File .\ultrakill-webport\tools\build-webgl.ps1

# solo una de las dos fases
powershell -ExecutionPolicy Bypass -File .\ultrakill-webport\tools\build-webgl.ps1 -SoloBundles
powershell -ExecutionPolicy Bypass -File .\ultrakill-webport\tools\build-webgl.ps1 -SoloPlayer
```

**Validar siempre por el log**, nunca por el código de salida:

```powershell
Select-String -Path <log> -Pattern "Shader error in"   # debe ser 0
Select-String -Path <log> -Pattern "error CS"           # debe ser 0
```

Si tocas shaders o materiales hay que rehacer **bundles y player**.

## Probar

```powershell
# servidor local -> http://localhost:8000/
powershell -ExecutionPolicy Bypass -File .\ultrakill-webport\tools\serve-local.ps1
```

Arnés de pruebas visuales (Playwright: eventos de confianza, pointer lock real):

```powershell
$py = "$env:LOCALAPPDATA\Programs\Python\Python312\python.exe"
& $py .\ultrakill-webport\tools\visual\ukshot.py --url "http://localhost:8000/?v=210" `
      --out "$env:TEMP\opencode\shots" --actions "wait:45;key:Escape;wait:5;shot:pausa"
& $py .\ultrakill-webport\tools\visual\ukshot.py --diff "$env:TEMP\opencode\shots\a.png" "$env:TEMP\opencode\shots\b.png"
```

El diff de píxeles es la forma objetiva de comprobar que un cambio altera la
imagen. Ojo: en el arnés, el **primer clic salta a pantalla completa** y cambia el
tamaño del canvas (1280×720 → 1280×800), así que las coordenadas de la UI cambian:
haz un clic de inflating antes de medir.

## Publicar

- El usuario tiene que **aprobar** el build antes de publicarlo.
- Publicar = commit + `git push` + release con el ZIP:
  `powershell -ExecutionPolicy Bypass -File .\ultrakill-webport\tools\package-webgl.ps1 -Ver v21`
  y `gh release create v21 --prerelease --title "..." --notes-file notas.md <zip>`.
- **Las releases llevan el disclaimer**: el proyecto original, sus assets y su código
  pertenecen a Arsi "Hakita" Patala / New Blood Interactive.

## Herramientas del repo

| Qué | Dónde |
|---|---|
| Índice de assets (GUID ↔ rutas, materiales→shader, escena→nivel real) | `tools/assetindex/asset-index.ps1` |
| Pruebas visuales y diff de píxeles | `tools/visual/ukshot.py` |
| Test del bloqueo de teclado (Ctrl+D) | `tools/kblock.py`, `kblock2.py`, `kblock3.py` |
| Parser de bundles UnityFS (lee el bundle original del juego) | `tools/unityfs.py` |
| Editar el YAML de Unity (listar, borrar GameObjects, anular referencias) | `tools/uyaml.py`, `tools/udelete.py`, `tools/nullgfx.py` |
| Comparar el bundle construido con el original | `tools/verify_bundle.py`, `tools/match_scenes.py` |
| Build, empaquetado y servidor | `tools/build-webgl.ps1`, `tools/package-webgl.ps1`, `tools/serve-local.ps1` |
| Informes de las rondas de revisión | `tools/review/` |
| Exportar/importar sesiones de opencode | `tools/export-sesion.py` |

## Traerse el contexto de una sesión

Las sesiones con las que se hizo la v20.8 (el agente principal y las dos
rondas de revisión) están en `ultrakill-webport/sesiones/` ✓, en el formato que
devuelve `GET /api/experimental/session/<id>/export` (un JSON con `info` y
`messages`).

Para importar una en tu opencode:

```
opencode api post /api/experimental/session/import --data @sesion-ULTRA-v20.8.json
```

Y para exportar una sesión nueva desde la base de datos local:

```powershell
python .\ultrakill-webport\tools\export-sesion.py <carpeta-destino>
```


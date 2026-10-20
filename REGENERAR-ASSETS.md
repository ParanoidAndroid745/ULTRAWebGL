# Reconstruir el proyecto

El repositorio contiene **el proyecto de Unity completo**: scripts, shaders,
materiales y **los assets del juego** (2,6 GB: escenas, texturas, audio,
prefabs, animaciones). Un `git clone` + una compilación de bundles da el juego
jugable.

Únicamente quedan fuera los **bundles WebGL construidos**
(`Assets/StreamingAssets/acts/`), porque son salidas de build y los dos mayores
superan el límite de 100 MB por fichero de GitHub. Se regeneran en ~5 minutos
con el constructor de bundles (abajo).

Si prefieres empezar de cero desde tu copia del juego, o si los assets se
corrompen, aquí está el procedimiento completo.

## 1. El build del juego (depot de Steam)

El port usa el build **pre-GREED** (Patch 6 Hotfix #2, 19/04/2021):

```
app   1229490
depot 1229491
manifest 4296180771784366658
```

En Steam: `Win+R` → `steam://open/console` →

```
download_depot 1229490 1229491 4296180771784366658
```

Queda en `...\steamapps\content\app_1229490\depot_1229491`
(Unity **2019.4.16f1**, Mono, 1,22 GB).

## 2. Exportar con AssetRipper

`ultrakill-webport/tools/AssetRipper/` (AssetRipper 2.0, **no** está en el repo:
170 MB y se descarga aparte).

```
AssetRipper.GUI.Free.exe  →  exporta a  export/ExportedProject
```

Sale un proyecto de Unity con 25 escenas, 576 scripts, 1.820 prefabs,
2.572 materiales y 168 shaders.

## 3. Restaurar nuestros parches

Los cambios que **no** están en el export y hay que volver a aplicar:

| Qué | Dónde |
|---|---|
| Scripts parcheados (`MapLoader`, `CameraController`, `GraphicsOptions`, `ControlsOptions`, `WebGLRenderFix`, `FirstPersonInputModule`) | `Assets/Scripts/Assembly-CSharp/` **→ sí están en el repo** |
| Shaders reales (los 107 de `Assets/Shader/`) | **sí están en el repo** |
| La página web del port | `Assets/Editor/WebGLTemplate.html` **→ sí está en el repo** |
| El mapeo de escenas renombradas | `ultrakill-webport/tools/assetindex/index/scenes.tsv` (se regenera con `asset-index.ps1 build`) |
| Los bundles WebGL (`acts/act-0`, `act-1`, `prime`) | se reconstruyen (ver abajo) |

## 4. Reconstruir

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\2019.4.16f1\Editor\Unity.exe"
$proj  = "ultrakill-webport\export\ExportedProject"
# bundles (los unicos ficheros que no estan en el repo)
& $unity -batchmode -quit -projectPath $proj -logFile bundles.log -buildTarget WebGL -executeMethod WebGLBundleBuilder.BuildAll
Copy-Item "$proj\Builds\WebGLBundles\acts\*" "$proj\Assets\StreamingAssets\acts\" -Force
# player
& $unity -batchmode -quit -projectPath $proj -logFile player.log -buildTarget WebGL -executeMethod WebGLBuilder.Build
```

Comprueba **siempre** el log: `Select-String "Shader error in"` y `"error CS"`
deben dar 0 (ver `ultrakill-webport/NOTAS.md` para las trampas conocidas).


# Regenerar los assets (no versionados)

Este repo versiona **nuestro trabajo**: los scripts parcheados, los shaders, los
materiales, la página web, las herramientas y la documentación.

**No** versiona los assets binarios del juego (audio, texturas, mallas,
animaciones, prefabs y escenas). Son ~3 GB extraídos con AssetRipper de tu copia
del juego, se regeneran en una hora y GitHub bloquea los ficheros de más de
100 MB (los bundles WebGL pesan 118 MB).

## Por qué no están

- **Tamaño**: 3,5 GB de binarios → el repo sería lento de clonar y de subir.
- **Límite de GitHub**: los ficheros >100 MB rechazan el push (los bundles
  `act-0` y `act-1` pesan 118 y 114 MB).
- **Son un derivado**: se reconstruyen desde tu copia del juego, no son trabajo
  nuestro.

## Cómo regenerarlos

### 1. El build del juego (depot de Steam)

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

### 2. Exportar con AssetRipper

`ultrakill-webport/tools/AssetRipper/` (AssetRipper 2.0, **no** está en el repo:
170 MB y se descarga aparte).

```
AssetRipper.GUI.Free.exe  →  exporta a  export/ExportedProject
```

Sale un proyecto de Unity con 25 escenas, 576 scripts, 1.820 prefabs,
2.572 materiales y 168 shaders.

### 3. Restaurar nuestros parches

Los cambios que **no** están en el export y hay que volver a aplicar:

| Qué | Dónde |
|---|---|
| Scripts parcheados (`MapLoader`, `CameraController`, `GraphicsOptions`, `ControlsOptions`, `WebGLRenderFix`, `FirstPersonInputModule`) | `Assets/Scripts/Assembly-CSharp/` **→ sí están en el repo** |
| Shaders reales (los 107 de `Assets/Shader/`) | **sí están en el repo** |
| La página web del port | `Assets/Editor/WebGLTemplate.html` **→ sí está en el repo** |
| El mapeo de escenas renombradas | `ultrakill-webport/tools/assetindex/index/scenes.tsv` (se regenera con `asset-index.ps1 build`) |
| Los bundles WebGL (`acts/act-0`, `act-1`, `prime`) | se reconstruyen (ver abajo) |

### 4. Reconstruir

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\2019.4.16f1\Editor\Unity.exe"
$proj  = "..\ultrakill-webport\export\ExportedProject"
# bundles
& $unity -batchmode -quit -projectPath $proj -logFile bundles.log -buildTarget WebGL -executeMethod WebGLBundleBuilder.BuildAll
Copy-Item "$proj\Builds\WebGLBundles\acts\*" "$proj\Assets\StreamingAssets\acts\" -Force
# player
& $unity -batchmode -quit -projectPath $proj -logFile player.log -buildTarget WebGL -executeMethod WebGLBuilder.Build
```

Comprueba **siempre** el log: `Select-String "Shader error in"` y `"error CS"`
deben dar 0 (ver la skill `ultrakill-port` para las trampas conocidas).

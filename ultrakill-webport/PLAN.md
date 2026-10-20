# ULTRAKILL → WebGL (uso personal / estudio)

## Objetivo

Reconstruir el build **Patch 6 Hotfix #2 (2021-04-19)**, la última actualización
antes de GREED (Patch 7, 2021-06-23), como proyecto Unity y compilarlo a WebGL.

| Dato | Valor |
|---|---|
| App Steam | 1229490 |
| Depot | 1229491 |
| Manifest | 4296180771784366658 |
| Fecha | 2021-04-19 |
| Contenido | Acto I: Prelude, Limbo, Lust, Gluttony (+ 2-S, P-1, Cyber Grind) |
| Engine | Unity, build Mono (el build actual 2026 lo confirma: `ULTRAKILL_Data/Managed`) |

> El build instalado hoy (oct-2026, Unity 2022.3.29f1) **no** sirve como fuente:
> hay que bajar el depot histórico.

## Build objetivo verificado

Depot descargado en `C:\Program Files (x86)\Steam\steamapps\content\app_1229490\depot_1229491`.

- **Unity 2019.4.16f1** (changeset `e05b6e02d63e`), build **Mono** → ILSpy/AssetRipper funcionan directo.
- 1.22 GB, 282 archivos.
- Assets en bundles dentro de `ULTRAKILL_Data/StreamingAssets`:
  - `common` — 527 MB (compartido)
  - `acts/act-0` — 92 MB → **Prelude** (primer hito)
  - `acts/act-1` — 306 MB (Acto I)
  - `acts/prime` — 64 MB (P-1 / Prime)
- Plugins nativos aislables: `steam_api64.dll`, `discord_game_sdk.dll`.

## Estado del entorno (comprobado)

- ILSpy 11.1 instalado (winget).
- AssetRipper 2.0.0 portable en `tools/AssetRipper` (GUI con API HTTP en localhost).
- Unity Hub 3.22 instalado como MSIX; editor 2019.4.16f1 + WebGL en descarga.
- Export de AssetRipper en curso → `export/ExportedProject`.
- winget disponible. 307 GB libres en C:.
- Repo del port del demo (referencia): `gabrieldj81/Ultrakill-Prelude-Decompilation` — Unity 2022.3.62f1.
- Nota: la descompilación de **shaders** es función Premium de AssetRipper 2.0, así que
  el export free saldrá con shaders rotos (igual que le pasó a Cake Logic).

## Fases

### 0. Bajar el build objetivo
En Steam: `steam://open/console`

```
download_depot 1229490 1229491 4296180771784366658
```

Queda en `...\steamapps\content\app_1229490\depot_1229491`.
Verificar `ULTRAKILL.exe` → FileVersion (versión real de Unity) y que exista
`ULTRAKILL_Data\Managed` (Mono).

### 1. Herramientas
- Unity Hub + editor **exactamente** la versión del build + módulo WebGL.
- AssetRipper (exportar el build a proyecto Unity).
- ILSpy (leer/arreglar Assembly-CSharp).
- Git (control de cambios de los parches).

### 2. Reconstrucción
1. AssetRipper sobre `ULTRAKILL_Data` → exportar proyecto Unity.
2. Abrir en el editor correcto; corregir errores de compilación y referencias.
3. Guardar un commit base "tal cual sale de AssetRipper".

### 3. Port a WebGL
- Build Settings → WebGL (IL2CPP, compression, exceptions full).
- Quitar/aislar plugins nativos (steam_api64, discord_game_sdk, masked occlusion culling).
- Input: pointer lock / `FirstPersonInputModule` (referencia: parche del port del demo).
- Shaders: los descompilados no recompilan; sustituir por equivalentes.
- Guardado: `System.IO` no existe en WebGL → PlayerPrefs/IndexedDB.
- Addressables/asset bundles: reempaquetar para StreamingAssets si el build los usa.

### 4. Hitos
- [ ] Arranca a menú principal en el navegador.
- [ ] Carga y se juega Prelude.
- [ ] Acto I completo (Limbo, Lust, Gluttony, 2-S, P-1).
- [ ] Cyber Grind.

## Riesgos conocidos

- **Shaders** (el mayor): no se recupera el source; la fidelidad visual baja.
- **Memoria WebGL** (32-bit): el contenido completo puede pasarse; habrá que recortar.
- **Escenas/prefabs**: AssetRipper reconstruye mucho, pero no perfecto.
- **Plugins nativos** y **guardado**: hay que portarlos.
- Escala: Acto I son ~20 niveles; el port del demo solo cubre Prelude.

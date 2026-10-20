# Notas de port a WebGL

## Export de AssetRipper (completado)

`export/ExportedProject` — Unity **2019.4.16f1**, 56.661 archivos, 3.57 GB.

- 25 escenas (`Assets/Scenes/Level 0-1.unity` … `Level 3-S`, `Main Menu`, `Intro`, `Tutorial`, `Endless`).
- 576 scripts C# (`Assets/Scripts/Assembly-CSharp`), 1.820 prefabs, 2.572 materiales.
- 168 shaders, pero son **placeholders** del `DummyShaderTextExporter` (solo vertex/fragment básico).
  → Materiales renderizarán simple; habrá que reemplazar shaders para fidelidad.
- Build Settings original: `Intro`, `Main Menu`, `Endless`, `DebugRoom`, `Tutorial`.

## Cómo carga el juego (código decompilado)

`MapLoader.cs` carga los bundles de `StreamingAssets`:

- `EnsureCommonIsLoaded()` → `AssetBundle.LoadFromFile(StreamingAssets/common)`
- `LoadAct()` → comprueba `File.Exists(...)` y `SmartLoadBundle()`
- `SmartLoadBundle()` → `AssetBundle.LoadFromFileAsync(bundle)`

**Problema:** el juego de PC usa bundles compilados para StandaloneWindows (shaders D3D11).
No se pueden reutilizar en un build WebGL: los shaders y variantes no son compatibles.
Por eso el camino correcto es usar los assets **exportados** y compilar el proyecto a WebGL.

## Parches necesarios para WebGL

1. **MapLoader**
   - `File.Exists` no existe en WebGL → sustituir por el manifiesto/lista de acts.
   - `LoadFromFile`/`LoadFromFileAsync` → `UnityWebRequestAssetBundle.GetAssetBundle(...)`.
   - O bien: evitar bundles y cargar escenas directamente con `SceneManager.LoadScene`
     (añadir todas las escenas a Build Settings).
2. **Steamworks / Discord** (`steam_api64.dll`, `discord_game_sdk.dll`)
   - Envolver en `#if !UNITY_WEBGL`; el social/workshop no aplica.
3. **Guardado** (`System.IO`)
   - WebGL no tiene filesystem; pasar a `PlayerPrefs` (IndexedDB) o FS virtual.
4. **Input** (`FirstPersonInputModule`, pointer lock)
   - Bloquear puntero con clic; Ctrl+W cierra pestaña (remapear slide).
5. **Shaders**
   - Dummy → sustituir por equivalentes o reimplementar los que importan
     (mundo, enemigos, armas, agua, UI).

## Estrategia de hitos

1. **Compila** el proyecto en Unity 2019.4.16f1 (aunque sea con errores corregidos).
2. **Build WebGL** con `Intro` + `Main Menu`; ver que arranca en el navegador.
3. **Prelude** (`Level 0-1`…`0-S`) jugable.
4. Resto del Acto I.
5. Shaders y pulido.

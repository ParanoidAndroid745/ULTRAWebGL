# Recetas de build y empaquetado

## Unity

```
C:\Program Files\Unity\Hub\Editor\2019.4.16f1\Editor\Unity.exe
```

Licencia Personal activa en `C:\ProgramData\Unity\Unity_lic.ulf`.
Smart App Control debe estar **desactivado** (bloquea `il2cpp.exe`).

## Orden de un build completo

Los assets de un nivel (materiales, shaders, prefabs) viven en el **bundle**, no
en el player. Si tocas shaders, hay que rehacer **bundles + player**.

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\2019.4.16f1\Editor\Unity.exe"
$proj  = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\export\ExportedProject"
$out   = "$env:TEMP\opencode\build"

# 1) bundles (act-0, act-1, prime)  ~6-8 min
& $unity -batchmode -quit -projectPath $proj -logFile "$out\bundles.log" `
        -buildTarget WebGL -executeMethod WebGLBundleBuilder.BuildAll

# 2) copiar los bundles a StreamingAssets (si no lo hace el propio builder)
Copy-Item "$proj\Builds\WebGLBundles\acts\*" "$proj\Assets\StreamingAssets\acts\" -Force

# 3) player  ~6 min
& $unity -batchmode -quit -projectPath $proj -logFile "$out\player.log" `
        -buildTarget WebGL -executeMethod WebGLBuilder.Build
```

## Qué mirar en el log (SIEMPRE)

```powershell
Select-String -Path $log -Pattern "Shader error in"    # errores de shader (PLATAFORMA)
Select-String -Path $log -Pattern "error CS"            # errores de C#
Select-String -Path $log -Pattern "BUILD RESULT"        # Succeeded | errors=0 | ...
Get-Item $log | Select LastWriteTime                    # ¿el log es de ESTE build?
```

Un build sano: `BUILD RESULT: Succeeded | errors=0 | warnings=27 | sizeMB=487,5`
y **0** líneas de "Shader error in".

**Los bundles también compilan variantes de shader**, así que "Shader error in"
aparece también en el log de los bundles. Un shader roto rompe el bundle entero.

## Empaquetado

```powershell
$stage = "...\ultrakill-webport\dist\ULTRAKILL-WebGL-vNNN"
# 1. copiar Builds\WebGL\* a $stage
# 2. añadir JUGAR.bat + servidor.ps1 (copiar del paquete anterior)
# 3. escribir LEEME.txt
# 4. zip y VERIFICAR extrayéndolo en limpio y comparando hashes de
#    index.html, act-0, act-1, prime
```

Los ZIP viejos van a `dist/obsoletos/` (no se borran, pero fuera de la vista).

## Servidor

`ultrakill-webport/serve-webgl.ps1` (HttpListener, localhost) y
`serve-lan.ps1` (TcpListener, 0.0.0.0, para otros dispositivos).

**Cuidado**: `Start-Process powershell -ArgumentList ... '-Root', $ruta` parte la
ruta en el espacio de "Proyecto predeterminado" → el servidor no arranca. Usa un
`.ps1` intermedio que contenga la ruta.

Además, los procesos lanzados con `Start-Process` desde el tool `shell` mueren al
terminar el comando: lánzalos con `background: true`.

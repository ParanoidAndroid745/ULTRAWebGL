# ============================================================================
# Empaqueta el build WebGL en un ZIP autocontenido y lo verifica.
#
#   powershell -ExecutionPolicy Bypass -File .\tools\package-webgl.ps1 -Ver v20.8
#
# El ZIP incluye el player, los tres bundles, la pagina y el lanzador JUGAR.bat.
# Al final lo extrae en limpio y compara hashes con el build.
# ============================================================================
[CmdletBinding()]
param(
    [string]$Ver = "v20.8"
)

$ErrorActionPreference = "Stop"
$tools = $PSScriptRoot
# La raiz del port es la carpeta que contiene export\ExportedProject.
$root = $PSScriptRoot
while ($root -and -not (Test-Path (Join-Path $root "export\ExportedProject"))) {
    $root = Split-Path $root -Parent
}
if (-not $root) { throw "no encuentro export\ExportedProject desde $tools" }
$build = Join-Path $root "export\ExportedProject\Builds\WebGL"
$dist  = Join-Path $root "dist"
$stage = Join-Path $dist "ULTRAKILL-WebGL-$Ver"
$zip   = Join-Path $dist "ULTRAKILL-WebGL-$Ver.zip"

if (-not (Test-Path $build)) { throw "no existe el build: $build" }
New-Item -ItemType Directory -Force -Path $dist | Out-Null
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stage | Out-Null

# 1) contenido del build
Copy-Item "$build\*" -Destination $stage -Recurse -Force

# 2) lanzador: tools\launcher (versionado); si falta, se copia de la release anterior
$launcher = Join-Path $tools "launcher"
$prev = Get-ChildItem $dist -Directory -Filter "ULTRAKILL-WebGL-*" -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -ne "ULTRAKILL-WebGL-$Ver" } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
foreach ($f in @("JUGAR.bat", "servidor.ps1")) {
    $src = Join-Path $launcher $f
    if (Test-Path $src) { Copy-Item $src -Destination $stage -Force; continue }
    if ($prev -and (Test-Path (Join-Path $prev.FullName $f))) {
        Copy-Item (Join-Path $prev.FullName $f) -Destination $stage -Force
    }
}

# 3) LEEME del paquete
$leeme = @"
ULTRAKILL - WebGL ($Ver)
========================================

COMO JUGAR (Windows)
--------------------
1. Descomprime TODA la carpeta en el disco (no ejecutes nada dentro del ZIP).
2. Doble clic en JUGAR.bat.
   - Se abre una ventana del servidor y el navegador en http://localhost:8000/
     (si el puerto 8000 esta ocupado, el servidor busca el siguiente libre y
      la ventana del servidor te dice cual es).
   - La primera carga tarda (el paquete pesa ~340 MB).
3. NO cierres la ventana del servidor mientras juegas.
   Para parar: cierra esa ventana.

NO abras index.html con doble clic: el juego necesita un servidor local
(por seguridad del navegador). Con file:// no funciona.

REQUISITOS
----------
- Windows 10/11 con PowerShell (viene de serie).
- Navegador con WebGL2. Para bloquear los atajos del navegador (Ctrl+D...)
  hace falta Chrome o Edge, y en PANTALLA COMPLETA (Chrome solo concede el
  bloqueo de teclado a pantalla completa iniciada por la pagina).

Nota: el proyecto original, sus assets y su codigo pertenecen a Arsi "Hakita"
Patala / New Blood Interactive. Este port es no oficial.
"@
Set-Content -Path "$stage\LEEME.txt" -Value $leeme -Encoding utf8

# 4) zip
if (Test-Path $zip) { Remove-Item $zip -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($stage, $zip, [System.IO.Compression.CompressionLevel]::Optimal, $false)

# 5) verificacion: extraer en limpio y comparar hashes
$tmp = Join-Path $env:TEMP ("opencode\verify_" + $Ver)
if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force }
[System.IO.Compression.ZipFile]::ExtractToDirectory($zip, $tmp)
$checks = @(
    @{ n = "index.html"; a = "$build\index.html";                      b = "$tmp\index.html" },
    @{ n = "act-0";      a = "$build\StreamingAssets\acts\act-0";      b = "$tmp\StreamingAssets\acts\act-0" },
    @{ n = "act-1";      a = "$build\StreamingAssets\acts\act-1";      b = "$tmp\StreamingAssets\acts\act-1" },
    @{ n = "prime";      a = "$build\StreamingAssets\acts\prime";      b = "$tmp\StreamingAssets\acts\prime" }
)
$ok = $true
foreach ($c in $checks) {
    if (-not (Test-Path $c.b)) { Write-Host ("  FALTA {0}" -f $c.n) -ForegroundColor Red; $ok = $false; continue }
    $ha = (Get-FileHash $c.a).Hash; $hb = (Get-FileHash $c.b).Hash
    if ($ha -eq $hb) { Write-Host ("  OK {0,-11} {1}" -f $c.n, $ha.Substring(0, 12)) -ForegroundColor Green }
    else { Write-Host ("  DIFERENTE {0}" -f $c.n) -ForegroundColor Red; $ok = $false }
}
$z = Get-Item $zip
Write-Host ("ZIP: {0}  {1:N1} MB  {2} entradas" -f $z.Name, ($z.Length / 1MB), (Get-ChildItem $tmp -Recurse -File).Count) -ForegroundColor Cyan
Write-Host ("VERIFICACION: {0}" -f $(if ($ok) { "TODO OK" } else { "CON FALLOS" })) -ForegroundColor $(if ($ok) { "Green" } else { "Red" })
Remove-Item $tmp -Recurse -Force

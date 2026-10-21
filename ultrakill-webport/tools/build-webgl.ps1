# ============================================================================
# Construye el port WebGL de ULTRAKILL: bundles (act-0, act-1, prime) + player.
#
#   powershell -ExecutionPolicy Bypass -File .\tools\build-webgl.ps1
#   powershell -ExecutionPolicy Bypass -File .\tools\build-webgl.ps1 -SoloBundles
#   powershell -ExecutionPolicy Bypass -File .\tools\build-webgl.ps1 -SoloPlayer
#
# Los assets de un nivel viven en el BUNDLE, no en el player: si tocas shaders
# o materiales hay que rehacer los dos.
# ============================================================================
[CmdletBinding()]
param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\2019.4.16f1\Editor\Unity.exe",
    [switch]$SoloBundles,
    [switch]$SoloPlayer
)

$ErrorActionPreference = "Stop"
$tools = $PSScriptRoot
# La raiz del port es la carpeta que contiene export\ExportedProject.
$root = $PSScriptRoot
while ($root -and -not (Test-Path (Join-Path $root "export\ExportedProject"))) {
    $root = Split-Path $root -Parent
}
if (-not $root) { throw "no encuentro export\ExportedProject desde $tools" }
$main  = Join-Path $root "export\ExportedProject"
$out   = Join-Path $env:TEMP ("opencode\build_" + (Get-Date -Format 'yyyyMMdd_HHmmss'))
New-Item -ItemType Directory -Force -Path $out | Out-Null

function Wait-UnityFree([string]$proj) {
    for ($i = 0; $i -lt 900; $i++) {
        $busy = Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -eq "Unity.exe" -and $_.CommandLine -like "*$proj*" }
        if (-not $busy) { return $true }
        Start-Sleep -Seconds 5
    }
    return $false
}

function Run-Unity([string]$proj, [string]$method, [string]$log) {
    if (Test-Path $log) { Remove-Item $log -Force }
    $a = @('-batchmode', '-quit', '-projectPath', "`"$proj`"", '-logFile', "`"$log`"", '-buildTarget', 'WebGL', '-executeMethod', $method)
    $p = Start-Process -FilePath $Unity -ArgumentList $a -PassThru -WindowStyle Hidden
    while (-not $p.HasExited) { Start-Sleep -Seconds 10 }
    return $p.ExitCode
}

function Copy-Bundles {
    param([string]$from, [string]$to)
    New-Item -ItemType Directory -Force -Path $to | Out-Null
    Get-ChildItem $from -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^(act-0|act-1|prime)' -and $_.Name -notmatch '\.meta$' } |
        ForEach-Object {
            Copy-Item $_.FullName -Destination (Join-Path $to $_.Name) -Force
            Write-Host ("  copiado {0} ({1:N1} MB)" -f $_.Name, ($_.Length / 1MB))
        }
}

function Check-Log([string]$log, [string]$tag) {
    $sh = Select-String -Path $log -Pattern "Shader error in" -ErrorAction SilentlyContinue
    $cs = Select-String -Path $log -Pattern "error CS" -ErrorAction SilentlyContinue
    Write-Host ("  [{0}] errores de shader: {1} | errores CS: {2}" -f $tag, $sh.Count, $cs.Count)
    $sh | Select-Object -First 10 | ForEach-Object { "    " + ($_.Line.Trim() -replace ' \(on .*', '') }
    $cs | Select-Object -First 10 | ForEach-Object { "    " + $_.Line.Trim() }
}

Write-Host "=== proyecto ===" -ForegroundColor Yellow
Write-Host "  proyecto: $main"
Write-Host "  unity   : $Unity"
Write-Host "  logs    : $out"
Write-Host "=== esperando proyecto libre ===" -ForegroundColor Yellow
Wait-UnityFree $main | Out-Null

if (-not $SoloPlayer) {
    Write-Host "=== 1/2 bundles (act-0, act-1, prime) ===" -ForegroundColor Yellow
    $e = Run-Unity $main 'WebGLBundleBuilder.BuildAll' "$out\bundles.log"
    Write-Host "  exit=$e"; Check-Log "$out\bundles.log" "bundles"
    Select-String -Path "$out\bundles.log" -Pattern '\[WebGLBundles\] OK' | ForEach-Object { "  " + $_.Line.Trim() }
    Copy-Bundles "$main\Builds\WebGLBundles\acts" "$main\Assets\StreamingAssets\acts"
}

if (-not $SoloBundles) {
    Write-Host "=== 2/2 player ===" -ForegroundColor Yellow
    $e = Run-Unity $main 'WebGLBuilder.Build' "$out\player.log"
    Write-Host "  exit=$e"; Check-Log "$out\player.log" "player"
    Select-String -Path "$out\player.log" -Pattern 'BUILD RESULT|Pagina WebGL' | ForEach-Object { "  " + $_.Line.Trim() }
}

Write-Host "=== LISTO ===" -ForegroundColor Green
Get-ChildItem "$main\Assets\StreamingAssets\acts" -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notmatch 'meta' } |
    ForEach-Object { "  {0}  {1:N1} MB  {2}" -f $_.Name, ($_.Length / 1MB), $_.LastWriteTime }
Get-ChildItem "$main\Builds\WebGL" -File -ErrorAction SilentlyContinue |
    ForEach-Object { "  build: {0}  {1:N1} MB" -f $_.Name, ($_.Length / 1MB) }

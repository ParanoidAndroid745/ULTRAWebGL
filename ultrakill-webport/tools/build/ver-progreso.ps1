param(
    [int]$Intervalo = 3,
    [switch]$UnaVez
)

# ============================================================================
# Panel de progreso del build WebGL.
#
#   powershell -ExecutionPolicy Bypass -File .\tools\build\ver-progreso.ps1
#   powershell -ExecutionPolicy Bypass -File .\tools\build\ver-progreso.ps1 -UnaVez
#
# Detecta solo el proceso de Unity que este compilando para WebGL (no hace
# falta pasarle el PID) y muestra la fase, el porcentaje aproximado, el tamano
# del log y los errores.
# ============================================================================

$ErrorActionPreference = 'SilentlyContinue'
$root = Split-Path (Split-Path (Split-Path $PSCommandPath -Parent) -Parent) -Parent
$proj = Join-Path $root 'export\ExportedProject'
$log  = Join-Path $root 'logs\player.log'
$out  = Join-Path $proj 'Builds\WebGL'

$fasles = @(
    @{ n = 'Reimportando assets';  p = 0.15;  r = 'Start importing|Importing ' },
    @{ n = 'Escenas y shaders';    p = 0.55;  r = 'Building scenes|Compiling shader|Building Player' },
    @{ n = 'IL2CPP (C# -> C++)';   p = 0.85;  r = 'il2cpp|convert-to-cpp' },
    @{ n = 'Link WebAssembly';     p = 0.97;  r = 'emcc|Linking|wasm-ld' },
    @{ n = 'Escribiendo salida';   p = 1.00;  r = 'Writing output|Taking down' }
)

function Get-BuildProcess {
    Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -like '*buildTarget WebGL*' } |
        Select-Object -First 1
}

function Get-Phase([string]$logPath) {
    if (-not (Test-Path $logPath)) { return @{ n = 'Iniciando'; i = 0 } }
    $tail = Get-Content $logPath -Tail 400 -ErrorAction SilentlyContinue
    if (-not $tail) { return @{ n = 'Iniciando'; i = 0 } }
    for ($i = $fasles.Count - 1; $i -ge 0; $i--) {
        if ($tail -match $fasles[$i].r) { return @{ n = $fasles[$i].n; i = $i } }
    }
    return @{ n = 'Trabajando'; i = 0 }
}

do {
    $proc = Get-BuildProcess
    $ph   = Get-Phase $log
    $logKB = if (Test-Path $log) { [math]::Round((Get-Item $log).Length / 1KB) } else { 0 }
    $outMB = 0; $outN = 0
    if (Test-Path $out) {
        $f = Get-ChildItem $out -Recurse -File -ErrorAction SilentlyContinue
        $outN = $f.Count
        $outMB = [math]::Round((($f | Measure-Object Length -Sum).Sum) / 1MB, 1)
    }
    $errs = 0
    if (Test-Path $log) { $errs = (Select-String -Path $log -Pattern 'error CS|Shader error in' -ErrorAction SilentlyContinue).Count }

    $pct = 0
    if ($proc) { $pct = [math]::Round($fasles[$ph.i].p * 100) }

    Clear-Host
    Write-Host ''
    Write-Host '  ULTRAKILL - build WebGL' -ForegroundColor Yellow
    Write-Host '  ============================================================' -ForegroundColor DarkGray
    if ($proc) {
        Write-Host ('  Estado  : COMPILANDO   {0,3}%   (fase: {1})' -f $pct, $ph.n) -ForegroundColor Green
    } else {
        Write-Host '  Estado  : sin build en curso' -ForegroundColor DarkGray
    }
    Write-Host ('  Log     : {0} KB    Salida: {1} MB / {2} ficheros    Errores: {3}' -f $logKB, $outMB, $outN, $errs)
    if ($errs -gt 0) { Write-Host '  ATENCION: hay errores en el log (Select-String "Shader error in")' -ForegroundColor Red }
    Write-Host ''
    if (-not $UnaVez) { Start-Sleep -Seconds $Intervalo }
} while (-not $UnaVez)

if ($UnaVez) {
    Write-Host ''
    Get-Content $log -Tail 12 -ErrorAction SilentlyContinue | ForEach-Object { '   ' + $_ }
}

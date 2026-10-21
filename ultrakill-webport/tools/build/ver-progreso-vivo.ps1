param(
    [string]$BuildDir,
    [string]$Project,
    [int]$Intervalo = 1,
    [int]$BundleSeconds = 525,
    [int]$PlayerSeconds = 264,
    [switch]$UnaVez,
    [switch]$Ascii,
    [switch]$SalirAlTerminar
)

# ============================================================================
# Barra de progreso EN VIVO para el build WebGL de ULTRAKILL.
#
#   powershell -ExecutionPolicy Bypass -File .\tools\build\ver-progreso-vivo.ps1
#   powershell -ExecutionPolicy Bypass -File .\tools\build\ver-progreso-vivo.ps1 -UnaVez
#
# Detecta automaticamente la carpeta buildNNN mas reciente en
# %LOCALAPPDATA%\Temp\opencode y muestra:
#   - progreso global y por fase (bundles / player)
#   - act-0, act-1 y prime ya generados
#   - etapa actual (IL2CPP, emscripten, ...), tiempo y ETA
#   - errores de shader / CS
#
# La estimacion se basa en los tiempos de builds anteriores; Unity no expone
# un porcentaje real en batchmode.
# ============================================================================

$ErrorActionPreference = 'SilentlyContinue'
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }

if ($Ascii) {
    $script:BAR_FULL = '#'
    $script:BAR_EMPTY = '-'
} else {
    $script:BAR_FULL = [string][char]0x2588
    $script:BAR_EMPTY = [string][char]0x2591
}

$scriptRoot = Split-Path -Parent $PSCommandPath
$repoRoot = Split-Path (Split-Path $scriptRoot -Parent) -Parent
if (-not $Project) { $Project = Join-Path $repoRoot 'export\ExportedProject' }

function Get-LatestBuildDir {
    $base = Join-Path $env:LOCALAPPDATA 'Temp\opencode'
    Get-ChildItem $base -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^build\d+$' } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}

if (-not $BuildDir) { $BuildDir = Get-LatestBuildDir }
if (-not $BuildDir -or -not (Test-Path $BuildDir)) {
    Write-Host 'No encuentro ninguna carpeta buildNNN en %LOCALAPPDATA%\Temp\opencode.' -ForegroundColor Red
    exit 1
}

$buildName    = Split-Path $BuildDir -Leaf
$bundlesLog   = Join-Path $BuildDir 'bundles.log'
$playerLog    = Join-Path $BuildDir 'player.log'
$bundleOutDir = Join-Path $Project 'Builds\WebGLBundles\acts'
$playerOutDir = Join-Path $Project 'Builds\WebGL'

function Format-Duration([double]$seconds) {
    if ($seconds -lt 0) { $seconds = 0 }
    $ts = [TimeSpan]::FromSeconds($seconds)
    if ($ts.TotalHours -ge 1) {
        return ('{0:0}:{1:00}:{2:00}' -f [math]::Floor($ts.TotalHours), $ts.Minutes, $ts.Seconds)
    }
    return ('{0:00}:{1:00}' -f [math]::Floor($ts.TotalMinutes), $ts.Seconds)
}

function New-Bar([double]$pct, [int]$width = 24) {
    if ($pct -lt 0) { $pct = 0 }
    if ($pct -gt 1) { $pct = 1 }
    $n = [int][math]::Round($pct * $width)
    if ($n -lt 0) { $n = 0 }
    if ($n -gt $width) { $n = $width }
    return (($script:BAR_FULL * $n) + ($script:BAR_EMPTY * ($width - $n)))
}

function Get-FileTime([string]$path) {
    if (Test-Path $path) { return (Get-Item $path).LastWriteTime }
    return $null
}

function Get-SizeMB([string]$path) {
    if (-not (Test-Path $path)) { return 0 }
    $sum = (Get-ChildItem $path -File -Recurse -ErrorAction SilentlyContinue |
            Measure-Object Length -Sum).Sum
    if (-not $sum) { return 0 }
    return [math]::Round($sum / 1MB, 1)
}

function Get-LogErrors([string]$path) {
    $r = @{ Shader = 0; CS = 0 }
    if (-not (Test-Path $path)) { return $r }
    $r.Shader = (Select-String -Path $path -Pattern 'Shader error in' -SimpleMatch -ErrorAction SilentlyContinue | Measure-Object).Count
    $r.CS     = (Select-String -Path $path -Pattern 'error CS' -SimpleMatch -ErrorAction SilentlyContinue | Measure-Object).Count
    return $r
}

function Get-PlayerStage([string]$path, [double]$elapsed) {
    $stage = 'Compilando player'
    $floor = 0.0
    if (-not (Test-Path $path)) { return @{ Stage = 'Esperando'; Floor = 0.0 } }

    $tail = Get-Content $path -Tail 300 -ErrorAction SilentlyContinue
    $text = ($tail -join "`n")

    if ($text -match 'Invoking il2cpp') {
        $stage = 'IL2CPP (C# -> C++)'
        $floor = [math]::Max($floor, 0.42)
    }
    if ($text -match 'il2cpp\.exe exited') {
        $stage = 'IL2CPP terminado'
        $floor = [math]::Max($floor, 0.62)
    }
    if ($text -match 'emcc|emscripten|wasm-ld|Linking') {
        $stage = 'Link WebAssembly'
        $floor = [math]::Max($floor, 0.80)
    }
    if ($text -match 'Writing output|Taking down') {
        $stage = 'Escribiendo salida'
        $floor = [math]::Max($floor, 0.95)
    }
    if ($text -match 'DisplayProgressbar: Building Player' -and $floor -eq 0) {
        $stage = 'Escenas y assets'
        $floor = [math]::Max($floor, 0.18)
    }
    return @{ Stage = $stage; Floor = $floor }
}

$start = $null
if (Test-Path $bundlesLog) { $start = (Get-Item $bundlesLog).CreationTime }
if (-not $start) { $start = (Get-Item $BuildDir).CreationTime }

$lastRender = [datetime]::MinValue

do {
    $now = Get-Date

    $act0 = Get-FileTime (Join-Path $bundleOutDir 'act-0')
    $act1 = Get-FileTime (Join-Path $bundleOutDir 'act-1')
    $prime = Get-FileTime (Join-Path $bundleOutDir 'prime')
    $act0Size = if (Test-Path (Join-Path $bundleOutDir 'act-0')) { [math]::Round((Get-Item (Join-Path $bundleOutDir 'act-0')).Length / 1MB, 1) } else { 0 }
    $act1Size = if (Test-Path (Join-Path $bundleOutDir 'act-1')) { [math]::Round((Get-Item (Join-Path $bundleOutDir 'act-1')).Length / 1MB, 1) } else { 0 }
    $primeSize = if (Test-Path (Join-Path $bundleOutDir 'prime')) { [math]::Round((Get-Item (Join-Path $bundleOutDir 'prime')).Length / 1MB, 1) } else { 0 }

    $p1 = 0.0
    $phase1Done = $false
    if (Test-Path $playerLog) {
        $p1 = 1.0
        $phase1Done = $true
    } else {
        $elapsed1 = ((Get-Date) - $start).TotalSeconds
        $p1 = [math]::Min(0.54, 0.02 + (0.52 * ($elapsed1 / [math]::Max(1, $BundleSeconds * 0.57))))
        if ($act0 -and $act0 -ge $start) {
            $p1 = [math]::Max($p1, 0.55)
            $t = ((Get-Date) - $act0).TotalSeconds
            $p1 = [math]::Max($p1, [math]::Min(0.93, 0.55 + (0.38 * ($t / [math]::Max(1, $BundleSeconds * 0.38)))))
        }
        if ($act1 -and $act1 -ge $start) {
            $p1 = [math]::Max($p1, 0.93)
            $t = ((Get-Date) - $act1).TotalSeconds
            $p1 = [math]::Max($p1, [math]::Min(1.0, 0.93 + (0.07 * ($t / [math]::Max(1, $BundleSeconds * 0.07)))))
        }
        if ($prime -and $prime -ge $start) { $p1 = 1.0; $phase1Done = $true }
    }

    $p2 = 0.0
    $p2Elapsed = 0.0
    $stage = 'Esperando bundles'
    $buildDone = $false
    $buildFailed = $false
    $p2Start = $null

    if (Test-Path $playerLog) {
        $p2Start = (Get-Item $playerLog).CreationTime
        $p2Elapsed = ((Get-Date) - $p2Start).TotalSeconds
        $p2 = [math]::Min(0.96, [math]::Max(0.03, $p2Elapsed / [math]::Max(1, $PlayerSeconds)))

        $tail = Get-Content $playerLog -Tail 300 -ErrorAction SilentlyContinue
        $text = ($tail -join "`n")
        if ($text -match 'BUILD RESULT: Succeeded|Exiting batchmode successfully') {
            $p2 = 1.0
            $buildDone = $true
            $stage = 'Completada'
        } elseif ($text -match 'BUILD RESULT: Failed|Exiting batchmode with return code [^0]') {
            $p2 = 1.0
            $buildFailed = $true
            $stage = 'Fallida'
        } else {
            $info = Get-PlayerStage $playerLog $p2Elapsed
            $p2 = [math]::Max($p2, $info.Floor)
            $stage = $info.Stage
        }
    }

    $global = (0.665 * $p1) + (0.335 * $p2)
    if ($buildDone) { $global = 1.0 }
    if (-not $phase1Done -and $global -gt 0.665) { $global = 0.665 }

    $remaining = 0.0
    if (-not $buildDone) {
        if (-not $phase1Done) {
            $remaining = ((1 - $p1) * $BundleSeconds) + $PlayerSeconds
        } else {
            $remaining = [math]::Max(0, $PlayerSeconds - $p2Elapsed)
        }
    }

    $e1 = Get-LogErrors $bundlesLog
    $e2 = Get-LogErrors $playerLog
    $shaderErr = $e1.Shader + $e2.Shader
    $csErr = $e1.CS + $e2.CS
    $bundleMB = $act0Size + $act1Size + $primeSize
    $playerMB = Get-SizeMB $playerOutDir

    $elapsed = ((Get-Date) - $start).TotalSeconds
    $eta = if ($remaining -gt 0) { (Get-Date).AddSeconds($remaining).ToString('HH:mm') } else { '--:--' }

    $statusColor = if ($buildFailed) { 'Red' } elseif ($buildDone) { 'Green' } elseif ($shaderErr + $csErr -gt 0) { 'Yellow' } else { 'Cyan' }
    $statusText = if ($buildFailed) { 'BUILD FALLIDA' } elseif ($buildDone) { 'BUILD COMPLETADA' } else { 'COMPILANDO' }

    if (-not $UnaVez) { Clear-Host }

    Write-Host ''
    Write-Host ("  ULTRAKILL WebGL  -  {0}  -  {1}" -f $buildName, (Get-Date).ToString('HH:mm:ss')) -ForegroundColor Yellow
    Write-Host ('  ' + ('=' * 64)) -ForegroundColor DarkGray

    Write-Host '  Global   ' -NoNewline
    Write-Host ('[' + (New-Bar $global 24) + ']') -NoNewline -ForegroundColor $statusColor
    Write-Host (' {0,3}%   {1}   transcurrido {2}   ETA {3}' -f [math]::Round($global * 100), $statusText, (Format-Duration $elapsed), $eta) -ForegroundColor $statusColor
    Write-Host ''

    Write-Host '  Fase 1/2  Bundles  ' -NoNewline
    Write-Host ('[' + (New-Bar $p1 24) + ']') -NoNewline -ForegroundColor $(if ($phase1Done) { 'Green' } else { 'Cyan' })
    Write-Host (' {0,3}%' -f [math]::Round($p1 * 100)) -ForegroundColor $(if ($phase1Done) { 'Green' } else { 'Cyan' })
    Write-Host ('    act-0 {0,6:N1} MB {1}   act-1 {2,6:N1} MB {3}   prime {4,5:N1} MB {5}' -f `
        $act0Size, $(if ($act0 -and $act0 -ge $start) { 'OK' } else { '..' }), `
        $act1Size, $(if ($act1 -and $act1 -ge $start) { 'OK' } else { '..' }), `
        $primeSize, $(if ($prime -and $prime -ge $start) { 'OK' } else { '..' })) -ForegroundColor DarkGray
    Write-Host ''

    Write-Host '  Fase 2/2  Player   ' -NoNewline
    Write-Host ('[' + (New-Bar $p2 24) + ']') -NoNewline -ForegroundColor $(if ($buildDone) { 'Green' } elseif ($buildFailed) { 'Red' } else { 'Cyan' })
    Write-Host (' {0,3}%   {1}' -f [math]::Round($p2 * 100), $stage) -ForegroundColor $(if ($buildDone) { 'Green' } elseif ($buildFailed) { 'Red' } else { 'Cyan' })
    Write-Host ('    log player {0,7:N0} KB   salida {1,7:N1} MB   errores shader: {2}   errores CS: {3}' -f `
        $(if (Test-Path $playerLog) { (Get-Item $playerLog).Length / 1KB } else { 0 }), `
        $playerMB, $shaderErr, $csErr) -ForegroundColor $(if ($shaderErr + $csErr -gt 0) { 'Red' } else { 'DarkGray' })

    Write-Host ('  ' + ('=' * 64)) -ForegroundColor DarkGray
    if ($buildDone) {
        Write-Host '  Build terminada. Revisa que no haya errores en el log.' -ForegroundColor Green
    } elseif ($buildFailed) {
        Write-Host '  La build ha fallado. Mira el final del player.log.' -ForegroundColor Red
    } else {
        Write-Host '  Ctrl+C para salir del monitor.' -ForegroundColor DarkGray
    }

    if ($UnaVez) { break }
    if ($buildDone -and $SalirAlTerminar) { break }
    Start-Sleep -Seconds ([math]::Max(1, $Intervalo))
} while ($true)

param(
    [int]$UnityPid = 19196,
    [switch]$Once
)

# ------------------------------------------------------------------
#  Panel de progreso del build WebGL de ULTRAKILL
#  Uso normal:   powershell -ExecutionPolicy Bypass -File .\build-progress.ps1
#  Una sola foto: powershell -ExecutionPolicy Bypass -File .\build-progress.ps1 -Once
# ------------------------------------------------------------------

$root = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport"
$log  = Join-Path $root "build-webgl.log"
$out  = Join-Path $root "export\ExportedProject\Builds\WebGL"
$imp  = Join-Path $root "import.log"

# --- total de assets del import inicial (para calcular % de la fase 1) ---
$totalAssets = 0
if (Test-Path $imp) {
    $ms = Select-String -Path $imp -Pattern "Done importing asset: '([^']+)'" -AllMatches
    $hs = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($m in $ms) { foreach ($mm in $m.Matches) { [void]$hs.Add($mm.Groups[1].Value) } }
    $totalAssets = $hs.Count
}

# --- estado incremental ---
$seen        = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$pos         = 0
$partial     = ""
$errCount    = 0
$phase       = 1
$phaseStart  = Get-Date
$lastLine    = ""
$lastImport  = ""

$proc = Get-Process -Id $UnityPid -ErrorAction SilentlyContinue
if (-not $proc) {
    $proc = Get-Process Unity -ErrorAction SilentlyContinue |
            Where-Object { $_.StartTime -gt (Get-Date).AddHours(-8) } |
            Sort-Object StartTime -Descending | Select-Object -First 1
    if ($proc) { $UnityPid = $proc.Id }
}
$startTime = if ($proc) { $proc.StartTime } else { Get-Date }

function Read-NewLog {
    if (-not (Test-Path $log)) { return }
    $fs = [System.IO.File]::Open($log, 'Open', 'Read', 'ReadWrite')
    try {
        if ($fs.Length -le $script:pos) { return }
        [void]$fs.Seek($script:pos, 'Begin')
        $len = [int]($fs.Length - $script:pos)
        $buf = New-Object byte[] $len
        $n   = $fs.Read($buf, 0, $len)
        $script:pos += $n
        $text = $script:partial + [System.Text.Encoding]::UTF8.GetString($buf, 0, $n)
        $lines = $text -split "`r?`n"
        $script:partial = $lines[-1]
        $complete = if ($lines.Count -gt 1) { $lines[0..($lines.Count-2)] } else { @() }

        foreach ($line in $complete) {
            if (-not $line.Trim()) { continue }

            if ($line -match "Done importing asset: '([^']+)'") {
                [void]$script:seen.Add($matches[1])
                $script:lastImport = $matches[1]
            }
            if ($line -match 'error CS\d+|Build Failed|IL2CPP.*error|Exception:') {
                $script:errCount++
            }

            $newPhase = 0
            if ($line -match 'Building scenes|Compiling shaders|Shader compilation|Building Scene') { $newPhase = 2 }
            elseif ($line -match 'il2cpp\.exe|Building native binary|il2cpp::|IL2CPP.*->')     { $newPhase = 3 }
            elseif ($line -match '\bemcc\b|emscripten|Linking')                                  { $newPhase = 4 }
            if ($line -match 'Build Finished|BUILD RESULT')                                     { $newPhase = 5 }

            if ($newPhase -gt $script:phase) {
                $script:phase = $newPhase
                $script:phaseStart = Get-Date
            }
            $script:lastLine = $line.Trim()
        }
    } finally { $fs.Close() }
}

function Get-Bar([double]$frac, [int]$width) {
    $n = [int][math]::Round($frac * $width)
    if ($n -lt 0) { $n = 0 }
    if ($n -gt $width) { $n = $width }
    return ("#" * $n) + ("-" * ($width - $n))
}

function Show-Frame {
    Read-NewLog

    $alive = $null -ne (Get-Process -Id $UnityPid -ErrorAction SilentlyContinue)
    $done  = -not $alive
    if ($done) { $phase = 5 }

    $p1 = if ($totalAssets -gt 0) { [math]::Min(1.0, $seen.Count / $totalAssets) } else { 0.0 }
    switch ($phase) {
        1 { $frac = 0.60 * $p1 }
        2 { $frac = 0.60 + 0.12 * [math]::Min(1.0, ((Get-Date) - $phaseStart).TotalSeconds / 180) }
        3 { $frac = 0.72 + 0.20 * [math]::Min(1.0, ((Get-Date) - $phaseStart).TotalSeconds / 240) }
        4 { $frac = 0.92 + 0.06 * [math]::Min(1.0, ((Get-Date) - $phaseStart).TotalSeconds / 60) }
        default { $frac = 1.0 }
    }
    if ($done) { $frac = 1.0 }

    $elapsed = ((Get-Date) - $startTime).TotalSeconds
    $eta = ""
    if ($phase -eq 1 -and $elapsed -gt 5 -and $seen.Count -gt 50 -and $totalAssets -gt $seen.Count) {
        $rate = $seen.Count / $elapsed
        if ($rate -gt 0) {
            $s = [int](($totalAssets - $seen.Count) / $rate)
            $eta = "ETA fase 1: {0}:{1:00}" -f [int]($s / 60), ($s % 60)
        }
    }

    $outMB = 0; $outN = 0
    if (Test-Path $out) {
        $f = Get-ChildItem $out -Recurse -File -ErrorAction SilentlyContinue
        $outN  = $f.Count
        $outMB = [math]::Round((($f | Measure-Object Length -Sum).Sum) / 1MB)
    }
    $logKB = if (Test-Path $log) { [math]::Round((Get-Item $log).Length / 1KB) } else { 0 }

    Clear-Host
    Write-Host "============================================================" -ForegroundColor DarkCyan
    Write-Host "  ULTRAKILL - Build WebGL" -ForegroundColor White
    Write-Host "============================================================" -ForegroundColor DarkCyan

    $color = if ($done) { "Green" } elseif ($phase -eq 1) { "Cyan" } else { "Yellow" }
    Write-Host ("  [" + (Get-Bar $frac 46) + "]") -ForegroundColor $color -NoNewline
    Write-Host (" {0,3}%" -f [int]($frac * 100)) -ForegroundColor White
    Write-Host ("  Tiempo total: {0:hh\:mm\:ss}   {1}" -f ((Get-Date) - $startTime), $eta) -ForegroundColor DarkGray
    Write-Host "------------------------------------------------------------" -ForegroundColor DarkGray

    $steps = @(
        @{ n = 1; t = "Reimportar assets para WebGL" },
        @{ n = 2; t = "Compilar escenas y shaders" },
        @{ n = 3; t = "IL2CPP (C# -> C++)" },
        @{ n = 4; t = "Link Emscripten (.wasm)" },
        @{ n = 5; t = "Escribir salida (index.html / data)" }
    )
    foreach ($s in $steps) {
        if ($s.n -lt $phase) {
            $mark = "[x]"; $col = "Green"
        } elseif ($s.n -eq $phase) {
            $mark = "[>]"; $col = "Yellow"
        } else {
            $mark = "[ ]"; $col = "DarkGray"
        }
        $extra = ""
        if ($s.n -eq 1) { $extra = "  {0} / {1}" -f $seen.Count, $totalAssets }
        if ($s.n -eq 5 -and $outN -gt 0) { $extra = "  {0} archivos / {1} MB" -f $outN, $outMB }
        Write-Host ("  {0} {1}. {2}{3}" -f $mark, $s.n, $s.t, $extra) -ForegroundColor $col
    }

    Write-Host "------------------------------------------------------------" -ForegroundColor DarkGray

    $now = switch ($phase) {
        1 { if ($lastImport) { "Importando: " + $lastImport } else { "Preparando import de assets..." } }
        2 { "Compilando escenas y shaders..." }
        3 { "Generando C++ con IL2CPP (puede tardar varios minutos)..." }
        4 { "Linkeando WebAssembly con Emscripten..." }
        5 { if ($done) { "PROCESO TERMINADO" } else { "Escribiendo archivos de salida..." } }
    }
    Write-Host ("  Ahora: " + $now) -ForegroundColor White
    if ($lastLine -and $lastLine.Length -gt 100) { $lastLine = $lastLine.Substring(0, 100) + "..." }
    Write-Host ("  Log:   " + $lastLine) -ForegroundColor DarkGray
    Write-Host ("  build.log {0:N0} KB  |  salida {1:N0} MB  |  errores: {2}" -f $logKB, $outMB, $errCount) -ForegroundColor DarkGray
    Write-Host "============================================================" -ForegroundColor DarkCyan
    if (-not $done) { Write-Host "  Ctrl+C para salir (el build sigue en segundo plano)" -ForegroundColor DarkGray }
}

if ($Once) {
    Show-Frame
} else {
    while ($true) {
        Show-Frame
        if (-not (Get-Process -Id $UnityPid -ErrorAction SilentlyContinue)) { break }
        Start-Sleep -Seconds 2
    }
    Write-Host ""
    Write-Host "Build finalizado. Revisa el resultado en Builds\WebGL y build-webgl.log" -ForegroundColor Green
}

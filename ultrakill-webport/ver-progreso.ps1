param(
    [int]$Intervalo = 3,
    [switch]$UnaVez
)

$ErrorActionPreference = 'SilentlyContinue'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Get-LogActual {
    Get-ChildItem $root -Filter 'build-webgl-*.log' |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

function Format-Hace {
    param([datetime]$Fecha)
    $d = (Get-Date) - $Fecha
    if ($d.TotalSeconds -lt 60) { return ("hace {0:N0} s" -f $d.TotalSeconds) }
    return ("hace {0:N1} min" -f $d.TotalMinutes)
}

function Show-Estado {
    $log = Get-LogActual
    $unity = Get-Process Unity

    Write-Host "=== ULTRAKILL WebGL - progreso del build ===" -ForegroundColor Cyan
    Write-Host ("Hora: {0}" -f (Get-Date -Format 'HH:mm:ss'))
    Write-Host ""

    if ($unity) {
        $u = $unity | Select-Object -First 1
        $transcurrido = (Get-Date) - $u.StartTime
        Write-Host ("Unity: EN EJECUCION (PID {0}, {1:N1} min, CPU {2:N0} s)" -f `
            $u.Id, $transcurrido.TotalMinutes, $u.CPU) -ForegroundColor Green
    }
    else {
        Write-Host "Unity: no esta en ejecucion (build terminado o no iniciado)" -ForegroundColor Yellow
    }

    if (-not $log) {
        Write-Host "No se encontro ningun build-webgl-*.log" -ForegroundColor Red
        return
    }

    $tam = [math]::Round($log.Length / 1KB, 1)
    Write-Host ("Log: {0}  ({1} KB, actualizado {2})" -f `
        $log.Name, $tam, (Format-Hace $log.LastWriteTime))
    Write-Host ""

    $lineas = Get-Content $log.FullName
    $errores = $lineas | Select-String -Pattern 'error CS|BuildFailedException|Fatal Error|Aborting batchmode'
    $resultado = $lineas | Select-String -Pattern 'BUILD RESULT' | Select-Object -Last 1

    if ($resultado) {
        Write-Host "RESULTADO:" -ForegroundColor Cyan
        Write-Host ("  " + $resultado.Line.Trim()) -ForegroundColor Green
    }
    else {
        Write-Host "RESULTADO: aun sin linea BUILD RESULT (build en curso)" -ForegroundColor Yellow
    }

    if ($errores) {
        Write-Host ("ERRORES detectados: {0}" -f $errores.Count) -ForegroundColor Red
        $errores | Select-Object -Last 3 | ForEach-Object {
            $texto = $_.Line.Trim()
            if ($texto.Length -gt 200) { $texto = $texto.Substring(0, 200) + '...' }
            Write-Host ("  " + $texto) -ForegroundColor Red
        }
    }
    else {
        Write-Host "Errores: ninguno" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "--- Ultimas 10 lineas del log ---" -ForegroundColor DarkGray
    $lineas | Where-Object { $_.Trim() -ne '' } | Select-Object -Last 10 | ForEach-Object {
        $texto = $_
        if ($texto.Length -gt 200) { $texto = $texto.Substring(0, 200) + '...' }
        Write-Host ("  " + $texto) -ForegroundColor DarkGray
    }

    Write-Host ""
    $index = Join-Path $root 'export\ExportedProject\Builds\WebGL\index.html'
    $bundle = Join-Path $root 'export\ExportedProject\Builds\WebGL\StreamingAssets\acts\act-0'
    if (Test-Path $index) {
        $f = Get-Item $index
        Write-Host ("Player: index.html {0:N1} KB, {1} ({2})" -f `
            ($f.Length / 1KB), $f.LastWriteTime.ToString('HH:mm:ss'), (Format-Hace $f.LastWriteTime))
    }
    if (Test-Path $bundle) {
        $f = Get-Item $bundle
        Write-Host ("Bundle: act-0 {0:N1} MB, {1} ({2})" -f `
            ($f.Length / 1MB), $f.LastWriteTime.ToString('HH:mm:ss'), (Format-Hace $f.LastWriteTime))
    }
    Write-Host ""
}

if ($UnaVez) {
    Show-Estado
    return
}

Write-Host "Vista en vivo. Pulsa Ctrl+C para salir." -ForegroundColor DarkGray
Start-Sleep -Seconds 2

while ($true) {
    Clear-Host
    Show-Estado
    if (-not (Get-Process Unity)) {
        Write-Host "El build ya no esta corriendo. Puedes cerrar esta ventana." -ForegroundColor Yellow
        break
    }
    Start-Sleep -Seconds $Intervalo
}

# Muestra el progreso de la importacion de Unity en tiempo real.
# Uso:  powershell -ExecutionPolicy Bypass -File .\watch-import.ps1
$proj = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\export\ExportedProject"
$log  = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\import.log"

while ($true) {
    $u   = Get-Process Unity -ErrorAction SilentlyContinue | Select-Object -First 1
    $lib = Get-ChildItem "$proj\Library" -Recurse -File -ErrorAction SilentlyContinue
    $mb  = if ($lib) { [math]::Round((($lib | Measure-Object Length -Sum).Sum) / 1MB) } else { 0 }
    $logKB = if (Test-Path $log) { [math]::Round((Get-Item $log).Length / 1KB) } else { 0 }
    $errs = if (Test-Path $log) { (Select-String -Path $log -Pattern 'error CS\d+' -ErrorAction SilentlyContinue).Count } else { 0 }
    $state = if ($u) { "IMPORTANDO" } else { "TERMINADO" }

    "{0}  {1}  Library: {2:N0} MB / {3:N0} archivos  Log: {4:N0} KB  Errores CS: {5}" -f `
        (Get-Date -Format 'HH:mm:ss'), $state, $mb, $lib.Count, $logKB, $errs

    if (-not $u) { break }
    Start-Sleep -Seconds 10
}

"--- Ultimas lineas del log ---"
Get-Content $log -Tail 20 -ErrorAction SilentlyContinue

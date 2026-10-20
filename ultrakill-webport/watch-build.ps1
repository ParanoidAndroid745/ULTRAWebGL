# Muestra el progreso del build WebGL.
# Uso:  powershell -ExecutionPolicy Bypass -File .\watch-build.ps1
$log = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\build-webgl.log"
$out = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\export\ExportedProject\Builds\WebGL"
$unityPid = 19196

$inicio = Get-Date
while ($true) {
    $u = Get-Process -Id $unityPid -ErrorAction SilentlyContinue
    $kb = if (Test-Path $log) { [math]::Round((Get-Item $log).Length / 1KB) } else { 0 }
    $errs = if (Test-Path $log) { (Select-String -Path $log -Pattern 'error CS\d+|Build Failed|IL2CPP.*error' -ErrorAction SilentlyContinue).Count } else { 0 }
    $mb = 0; $n = 0
    if (Test-Path $out) {
        $f = Get-ChildItem $out -Recurse -File -ErrorAction SilentlyContinue
        $n = $f.Count
        $mb = [math]::Round((($f | Measure-Object Length -Sum).Sum) / 1MB)
    }
    $ultima = ""
    if (Test-Path $log) {
        $ultima = Get-Content $log -Tail 3 -ErrorAction SilentlyContinue |
                  Where-Object { $_.Trim() } | Select-Object -Last 1
        if ($ultima) { $ultima = $ultima.Trim(); if ($ultima.Length -gt 110) { $ultima = $ultima.Substring(0,110) } }
    }
    "{0}  {1}  log:{2:N0} KB  salida:{3:N0} MB/{4} arch  errs:{5}  [{6}]" -f `
        (Get-Date -Format 'HH:mm:ss'), $(if($u){"COMPILANDO"}else{"TERMINADO"}), $kb, $mb, $n, $errs, $ultima

    if (-not $u) { break }
    Start-Sleep -Seconds 10
}

"Duracion total: " + [math]::Round(((Get-Date) - $inicio).TotalMinutes, 1) + " min"
"--- Resultado ---"
Select-String -Path $log -Pattern 'BUILD RESULT|Build Finished|Build Failed|error CS' -ErrorAction SilentlyContinue |
    Select-Object -Last 20 | ForEach-Object { $_.Line.Trim() }
"--- Ultimas lineas ---"
Get-Content $log -Tail 25 -ErrorAction SilentlyContinue

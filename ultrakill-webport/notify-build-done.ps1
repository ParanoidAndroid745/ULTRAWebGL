# Aviso cuando termine el build WebGL (dos beeps + MessageBox).
# Se lanza desacoplado: funciona aunque OpenCode se cierre.
$log = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\build-webgl.log"
$out = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\export\ExportedProject\Builds\WebGL"
$res = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\BUILD-DONE.txt"

# localizar el proceso del build por su linea de comandos
$p = $null
while (-not $p) {
    $p = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue |
         Where-Object { $_.CommandLine -match 'WebGLBuilder\.Build' } |
         Select-Object -First 1
    if (-not $p) { Start-Sleep -Seconds 5 }
}
$unityPid = [int]$p.ProcessId
while (Get-Process -Id $unityPid -ErrorAction SilentlyContinue) { Start-Sleep -Seconds 10 }

$lines = Select-String -Path $log -Pattern 'BUILD RESULT|did not run properly|Exception|error CS' -ErrorAction SilentlyContinue
$resultado = ($lines | Select-Object -Last 1).Line
$mb = 0; $n = 0
if (Test-Path $out) {
    $f = Get-ChildItem $out -Recurse -File -ErrorAction SilentlyContinue
    $n = $f.Count
    $mb = [math]::Round((($f | Measure-Object Length -Sum).Sum) / 1MB)
}

@(
    "BUILD TERMINADO: " + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
    "Resultado: $resultado"
    "Salida: $mb MB / $n archivos"
    ""
    "Ultimas lineas:"
    (Get-Content $log -Tail 20 -ErrorAction SilentlyContinue)
) | Set-Content -Path $res -Encoding UTF8

[console]::beep(880, 400)
Start-Sleep -Milliseconds 200
[console]::beep(1320, 600)

Add-Type -AssemblyName System.Windows.Forms
[System.Windows.Forms.MessageBox]::Show(
    "Build WebGL terminado.`n`n$resultado`nSalida: $mb MB ($n archivos)`n`nResumen en BUILD-DONE.txt",
    "ULTRAKILL WebGL - Build terminado",
    [System.Windows.Forms.MessageBoxButtons]::OK,
    [System.Windows.Forms.MessageBoxIcon]::Information
) | Out-Null

# Aviso de escritorio cuando Unity termine de importar.
# Se lanza desacoplado de la sesion, asi que funciona aunque OpenCode se cierre.
$proj = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\export\ExportedProject"
$log  = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\import.log"
$out  = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\IMPORT-DONE.txt"
$unityPid = 6184

while (Get-Process -Id $unityPid -ErrorAction SilentlyContinue) { Start-Sleep -Seconds 15 }

$lib  = Get-ChildItem "$proj\Library" -Recurse -File -ErrorAction SilentlyContinue
$mb   = [math]::Round((($lib | Measure-Object Length -Sum).Sum) / 1MB)
$errs = (Select-String -Path $log -Pattern 'error CS\d+' -ErrorAction SilentlyContinue).Count
$fin  = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'

@(
  "IMPORTACION TERMINADA: $fin"
  "Library: $mb MB / $($lib.Count) archivos"
  "Errores CS: $errs"
  ""
  "Ultimas lineas del log:"
  (Get-Content $log -Tail 30 -ErrorAction SilentlyContinue)
) | Set-Content -Path $out -Encoding UTF8

[console]::beep(880, 400)
Start-Sleep -Milliseconds 200
[console]::beep(1320, 600)

Add-Type -AssemblyName System.Windows.Forms
[System.Windows.Forms.MessageBox]::Show(
  "La importacion de Unity termino.`n`nLibrary: $mb MB ($($lib.Count) archivos)`nErrores CS: $errs`n`nResumen en: IMPORT-DONE.txt",
  "ULTRAKILL WebGL - Import terminado",
  [System.Windows.Forms.MessageBoxButtons]::OK,
  [System.Windows.Forms.MessageBoxIcon]::Information
) | Out-Null

# ============================================================================
# Levanta el servidor local que sirve el build WebGL (http://localhost:8000/).
#
#   powershell -ExecutionPolicy Bypass -File .\tools\serve-local.ps1 -Port 8000
#
# Ojo: los servidores lanzados como procesos hijos de una terminal mueren con
# ella. Si lo lanzas desde opencode, usa el tool de shell con background: true.
# ============================================================================
[CmdletBinding()]
param(
    [int]$Port = 8000
)

$ErrorActionPreference = "Stop"
$tools = Split-Path $PSScriptRoot -Parent
$root  = Split-Path $tools -Parent        # ultrakill-webport
$web   = Join-Path $root "export\ExportedProject\Builds\WebGL"

if (-not (Test-Path $web)) { throw "no existe el build: $web" }

& (Join-Path $root "serve-webgl.ps1") -Port $Port -Root $web

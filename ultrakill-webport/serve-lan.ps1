param(
    [int]$Port = 8010,
    [string]$Root = "$PSScriptRoot\export\ExportedProject\Builds\WebGL"
)

# Servidor estatico minimo para poder jugar desde OTRO dispositivo de la misma
# red (http://<IP-de-este-PC>:8010/). No necesita permisos de administrador
# (a diferencia de HttpListener con una IP concreta) y sirve el .html siempre
# sin cache para que nunca se cargue una pagina antigua.

$ErrorActionPreference = "Stop"
$listener = New-Object System.Net.Sockets.TcpListener([System.Net.IPAddress]::Any, $Port)
$listener.Start()

$ips = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
    Where-Object { $_.IPAddress -notlike "127.*" -and $_.IPAddress -notlike "169.254*" } |
    Select-Object -ExpandProperty IPAddress

Write-Host ""
Write-Host "  ULTRAKILL - WebGL (servidor de red)" -ForegroundColor Yellow
Write-Host "  En este PC      : http://localhost:$Port/" -ForegroundColor Cyan
foreach ($i in $ips) { Write-Host "  En otro device  : http://${i}:$Port/" -ForegroundColor Cyan }
Write-Host ""
Write-Host "  Deja esta ventana abierta mientras juegas." -ForegroundColor Gray
Write-Host "  Para parar: cierra esta ventana." -ForegroundColor Gray
Write-Host ""

function Get-Mime([string]$ext) {
    switch ($ext) {
        '.html'     { 'text/html; charset=utf-8' }
        '.js'       { 'text/javascript; charset=utf-8' }
        '.json'     { 'application/json; charset=utf-8' }
        '.css'      { 'text/css; charset=utf-8' }
        '.wasm'     { 'application/wasm' }
        '.unityweb' { 'application/octet-stream' }
        '.data'     { 'application/octet-stream' }
        '.png'      { 'image/png' }
        '.jpg'      { 'image/jpeg' }
        '.ico'      { 'image/x-icon' }
        '.txt'      { 'text/plain; charset=utf-8' }
        default     { 'application/octet-stream' }
    }
}

while ($true) {
    $client = $listener.AcceptTcpClient()
    try {
        $stream = $client.GetStream()
        $stream.ReadTimeout = 15000
        $buf = New-Object byte[] 32768
        $ms = New-Object System.IO.MemoryStream
        while ($true) {
            $r = $stream.Read($buf, 0, $buf.Length)
            if ($r -le 0) { break }
            $ms.Write($buf, 0, $r)
            $txt = [Text.Encoding]::ASCII.GetString($ms.ToArray())
            if ($txt -match "`r`n`r`n") { break }
            if ($ms.Length -gt 65536) { break }
        }
        $req = [Text.Encoding]::ASCII.GetString($ms.ToArray())
        $line = ($req -split "`r`n")[0]
        $parts = $line -split ' '
        $path = if ($parts.Count -ge 2) { $parts[1] } else { '/' }
        $rel = [System.Uri]::UnescapeDataString(($path -split '\?')[0]).TrimStart('/')
        if ([string]::IsNullOrWhiteSpace($rel)) { $rel = 'index.html' }
        $file = Join-Path $Root ($rel -replace '/', '\')

        if ((Test-Path $file -PathType Leaf) -and $file.StartsWith($Root, [System.StringComparison]::OrdinalIgnoreCase)) {
            $ext = [System.IO.Path]::GetExtension($file).ToLowerInvariant()
            $bytes = [IO.File]::ReadAllBytes($file)
            $cache = if ($ext -eq '.html' -or $ext -eq '.htm') { 'no-store, no-cache, must-revalidate' } else { 'public, max-age=3600' }
            $head = "HTTP/1.1 200 OK`r`nContent-Type: $(Get-Mime $ext)`r`nContent-Length: $($bytes.Length)`r`nCache-Control: $cache`r`nConnection: close`r`n`r`n"
            $hb = [Text.Encoding]::ASCII.GetBytes($head)
            $stream.Write($hb, 0, $hb.Length)
            $stream.Write($bytes, 0, $bytes.Length)
            $stream.Flush()
        } else {
            $msg = [Text.Encoding]::UTF8.GetBytes("404: $rel")
            $head = "HTTP/1.1 404 Not Found`r`nContent-Type: text/plain; charset=utf-8`r`nContent-Length: $($msg.Length)`r`nConnection: close`r`n`r`n"
            $hb = [Text.Encoding]::ASCII.GetBytes($head)
            $stream.Write($hb, 0, $hb.Length)
            $stream.Write($msg, 0, $msg.Length)
            $stream.Flush()
        }
    } catch {
        # conexion cancelada: ignorar
    } finally {
        try { $client.Close() } catch { }
    }
}

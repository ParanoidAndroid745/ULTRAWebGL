param(
    [int]$Port = 8000
)

# Servidor estatico del paquete: sirve la carpeta donde esta este script y
# abre el navegador. No necesita permisos de administrador.

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot

# Puerto libre (si el 8000 esta ocupado, prueba los siguientes).
$libre = 0
for ($p = $Port; $p -lt ($Port + 20); $p++) {
    try {
        $l = New-Object System.Net.Sockets.TcpListener([System.Net.IPAddress]::Loopback, $p)
        $l.Start(); $l.Stop(); $libre = $p; break
    } catch { }
}
if ($libre -eq 0) {
    Write-Host "No hay ningun puerto libre entre $Port y $($Port + 19)." -ForegroundColor Red
    exit 1
}

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://localhost:$libre/")
try {
    $listener.Start()
} catch {
    Write-Host "ERROR al abrir el puerto: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "  ULTRAKILL - WebGL" -ForegroundColor Cyan
Write-Host "  http://localhost:$libre/" -ForegroundColor Green
Write-Host "  No cierres esta ventana mientras juegas. Ctrl+C para parar." -ForegroundColor DarkGray
Write-Host ""
Start-Process "http://localhost:$libre/"

while ($listener.IsListening) {
    try {
        $ctx = $listener.GetContext()
        $rel = [System.Uri]::UnescapeDataString($ctx.Request.Url.AbsolutePath).TrimStart('/')
        if ([string]::IsNullOrWhiteSpace($rel)) { $rel = 'index.html' }
        $file = Join-Path $Root $rel

        if ((Test-Path $file -PathType Leaf) -and $file.StartsWith($Root, [System.StringComparison]::OrdinalIgnoreCase)) {
            $bytes = [System.IO.File]::ReadAllBytes($file)
            $ext = [System.IO.Path]::GetExtension($file).ToLowerInvariant()
            $mime = switch ($ext) {
                '.html'    { 'text/html; charset=utf-8' }
                '.htm'     { 'text/html; charset=utf-8' }
                '.js'      { 'text/javascript; charset=utf-8' }
                '.json'    { 'application/json; charset=utf-8' }
                '.css'     { 'text/css; charset=utf-8' }
                '.wasm'    { 'application/wasm' }
                '.unityweb'{ 'application/octet-stream' }
                '.data'    { 'application/octet-stream' }
                '.png'     { 'image/png' }
                '.jpg'     { 'image/jpeg' }
                '.ico'     { 'image/x-icon' }
                default    { 'application/octet-stream' }
            }
            $ctx.Response.ContentType = $mime
            $ctx.Response.ContentLength64 = $bytes.Length
            if (@('.html', '.htm', '.js', '.wasm', '.data', '.unityweb', '.json') -contains $ext) {
                # La pagina y el player no se cachean: el paquete no versiona
                # los nombres de Build/*, asi que un .js/.wasm/.data cacheado
                # haria arrancar la version anterior aunque se descomprima
                # un ZIP nuevo (Ctrl+F5 no siempre basta).
                $ctx.Response.Headers.Add('Cache-Control', 'no-store, no-cache, must-revalidate')
                $ctx.Response.Headers.Add('Pragma', 'no-cache')
            }
            $ctx.Response.OutputStream.Write($bytes, 0, $bytes.Length)
        } else {
            $ctx.Response.StatusCode = 404
            $msg = [System.Text.Encoding]::UTF8.GetBytes("404: $rel")
            $ctx.Response.OutputStream.Write($msg, 0, $msg.Length)
        }
        $ctx.Response.Close()
    } catch {
        # ignorar errores de conexiones canceladas
    }
}

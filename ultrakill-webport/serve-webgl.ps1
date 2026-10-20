param(
    [int]$Port = 8000,
    [string]$Root = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\export\ExportedProject\Builds\WebGL",
    [string]$Bind = "localhost"
)

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://${Bind}:$Port/")
try {
    $listener.Start()
} catch {
    Write-Host "ERROR al abrir el puerto: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Prueba con otro puerto:  .\serve-webgl.ps1 -Port 8080" -ForegroundColor Yellow
    exit 1
}

Write-Host "Sirviendo $Root" -ForegroundColor Green
Write-Host "Abre: http://localhost:$Port/" -ForegroundColor Cyan
Write-Host "Ctrl+C para parar." -ForegroundColor DarkGray

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
            if ($ext -eq '.html' -or $ext -eq '.htm') {
                # La pagina lleva el arreglo del raton/fullscreen: nunca se
                # debe servir una copia cacheada antigua.
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

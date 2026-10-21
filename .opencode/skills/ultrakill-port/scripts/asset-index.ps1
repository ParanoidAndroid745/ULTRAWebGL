<#
  asset-index.ps1 (tambien en ultrakill-webport\tools\assetindex\) - Indice de assets del proyecto Unity (port ULTRAKILL WebGL).

  El proyecto son ~56.000 assets y las relaciones entre ellos son por GUID
  (UnityAssetGUID), no por nombre de fichero. Resolver "que materiales usan este
  shader" o "que escena referencia este prefab" a mano cuesta minutos.

  Indices cacheados en <tools>\assetindex\index\ :
    guids.tsv      guid -> ruta relativa del asset
    shaders.tsv    ruta .shader -> nombre declarado | dummy | properties | uniforms en codigo | uniform suelto
    materials.tsv  ruta .mat -> guid del shader | ruta del shader | dummy
    scenes.tsv     ruta .unity -> titulo real del nivel | nextAct
    refs.tsv       guid -> rutas de los assets que lo referencian

  Uso:
    asset-index.ps1 build                 (re)construye el indice
    asset-index.ps1 guid <guid>           ruta de un GUID
    asset-index.ps1 path <ruta>           GUID de una ruta
    asset-index.ps1 users <guid|ruta>     quien referencia ese asset
    asset-index.ps1 shader-users <shader> materiales que usan un shader
    asset-index.ps1 shaders               shaders con nombre duplicado / dummy
    asset-index.ps1 globals <uniform>     shaders que declaran/usan un uniform
    asset-index.ps1 scenes                mapa escena -> nivel real
    asset-index.ps1 dummy-materials       materiales cuyo shader es dummy
    asset-index.ps1 stats                 resumen del indice
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)][string]$Command = "help",
    [Parameter(Position = 1)][string]$Arg = "",
    [string]$Project = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\export\ExportedProject"
)

$ErrorActionPreference = "Stop"
$IndexDir = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\tools\assetindex\index"
$AssetsDir = Join-Path $Project "Assets"
$GuidRx = [regex]'guid: ([0-9a-fA-F]{32})'

function Say($m, $c = "Gray") { Write-Host $m -ForegroundColor $c }

function Get-IndexFiles {
    $guids    = Join-Path $IndexDir "guids.tsv"
    $shaders  = Join-Path $IndexDir "shaders.tsv"
    $materials= Join-Path $IndexDir "materials.tsv"
    $scenes   = Join-Path $IndexDir "scenes.tsv"
    $refs     = Join-Path $IndexDir "refs.tsv"
    foreach ($f in @($guids,$shaders,$materials,$scenes,$refs)) {
        if (-not (Test-Path $f)) { throw "Falta el indice ($f). Ejecuta primero: asset-index.ps1 build" }
    }
    return @{ guids=$guids; shaders=$shaders; materials=$materials; scenes=$scenes; refs=$refs }
}

# --------------------------------------------------------------------- build
function Build-Index {
    $t0 = Get-Date
    New-Item -ItemType Directory -Force -Path $IndexDir | Out-Null
    Say "== 1/5  GUIDs (meta) ==" "Cyan"
    $guidMap = @{}
    $metas = Get-ChildItem $AssetsDir -Recurse -Filter "*.meta" -File
    $i = 0
    foreach ($m in $metas) {
        $i++
        if ($i % 5000 -eq 0) { Say ("   {0}/{1} metas" -f $i,$metas.Count) }
        $asset = $m.FullName.Substring(0, $m.FullName.Length - 5)   # quita .meta
        try {
            $head = [IO.File]::ReadAllText($asset + ".meta")
        } catch { continue }
        $mm = [regex]::Match($head, 'guid: ([0-9a-fA-F]{32})')
        if ($mm.Success) {
            $rel = $asset.Substring($Project.Length + 1).Replace('\','/')
            $guidMap[$mm.Groups[1].Value.ToLower()] = $rel
        }
    }
    Say ("   {0} GUIDs" -f $guidMap.Count) "Green"
    $guidMap.GetEnumerator() | Sort-Object Value | ForEach-Object { "{0}`t{1}" -f $_.Key, $_.Value } |
        Set-Content (Join-Path $IndexDir "guids.tsv") -Encoding utf8

    Say "== 2/5  shaders ==" "Cyan"
    $shaderRows = New-Object System.Collections.ArrayList
    $shaderFiles = Get-ChildItem $AssetsDir -Recurse -Filter "*.shader" -File
    foreach ($f in $shaderFiles) {
        $t = [IO.File]::ReadAllText($f.FullName)
        $rel = $f.FullName.Substring($Project.Length + 1).Replace('\','/')
        $isDummy = $t -match "DummyShaderTextExporter"
        $decl = ""
        $mm = [regex]::Match($t, 'Shader\s+"([^"]+)"')
        if ($mm.Success) { $decl = $mm.Groups[1].Value }
        # properties declaradas
        $pm = [regex]::Match($t, '(?s)Properties\s*\{(.*?)\n[ \t]*\}')
        $props = @()
        if ($pm.Success) {
            foreach ($p in [regex]::Matches($pm.Groups[1].Value, '(?m)^[ \t]*(_[A-Za-z0-9_]+)\s*\(\s*"[^"]*"\s*,\s*([A-Za-z]+)')) {
                $props += ($p.Groups[1].Value + ":" + $p.Groups[2].Value)
            }
        }
        # uniforms usados en el codigo (fuera del bloque Properties)
        $code = if ($pm.Success) { $t.Substring($pm.Index + $pm.Length) } else { $t }
        $loose = @()
        foreach ($lm in [regex]::Matches($t, '(?m)^\s*uniform\s+(?:float|half|float4|half4)\s+(_[A-Za-z0-9_]+)\s*;')) { $loose += $lm.Groups[1].Value }
        $codeGlob = @()
        foreach ($cm in [regex]::Matches($code, '(?<![A-Za-z0-9_])(_[A-Za-z0-9_]+)(?![A-Za-z0-9_])')) {
            $n = $cm.Groups[1].Value
            if ($n -eq "_MainTex" -or $n -eq "_MainTex_ST") { continue }
            if ($props | Where-Object { $_ -like "${n}:*" }) { continue }
            if ($loose -contains $n) { continue }
            if ($codeGlob -notcontains $n) { $codeGlob += $n }
        }
        [void]$shaderRows.Add(("{0}`t{1}`t{2}`t{3}`t{4}`t{5}" -f $rel, $decl, ($(if($isDummy){"DUMMY"}else{"REAL"})), ($props -join ","), ($codeGlob -join ","), ($loose -join ",")))
    }
    $shaderRows | Set-Content (Join-Path $IndexDir "shaders.tsv") -Encoding utf8
    Say ("   {0} shaders ({1} dummy)" -f $shaderRows.Count, (@($shaderRows | Where-Object { $_ -match "`tDUMMY`t" }).Count)) "Green"

    Say "== 3/5  materiales -> shader ==" "Cyan"
    $matRows = New-Object System.Collections.ArrayList
    $mats = Get-ChildItem $AssetsDir -Recurse -Filter "*.mat" -File
    foreach ($f in $mats) {
        $t = [IO.File]::ReadAllText($f.FullName)
        $rel = $f.FullName.Substring($Project.Length + 1).Replace('\','/')
        $sm = [regex]::Match($t, 'm_Shader:\s*\{fileID:\s*4800000,\s*guid:\s*([0-9a-fA-F]{32})')
        if (-not $sm.Success) {
            $sm = [regex]::Match($t, 'm_Shader:\s*\{fileID:\s*[0-9-]+,\s*guid:\s*([0-9a-fA-F]{32})')
        }
        $sg = $sm.Groups[1].Value.ToLower()
        $sp = if ($sg -and $guidMap.ContainsKey($sg)) { $guidMap[$sg] } else { "" }
        $dummy = ""
        if ($sp -and $sp -like "*.shader") {
            $sh = $shaderRows | Where-Object { $_ -like "$sp`t*" } | Select-Object -First 1
            if ($sh) { $dummy = ($sh -split "`t")[2] }
        }
        [void]$matRows.Add(("{0}`t{1}`t{2}`t{3}" -f $rel, $sg, $sp, $dummy))
    }
    $matRows | Set-Content (Join-Path $IndexDir "materials.tsv") -Encoding utf8
    Say ("   {0} materiales" -f $matRows.Count) "Green"

    Say "== 4/5  escenas -> nivel real ==" "Cyan"
    $sceneRows = New-Object System.Collections.ArrayList
    $scenes = Get-ChildItem (Join-Path $AssetsDir "Scenes") -Filter "*.unity" -File -ErrorAction SilentlyContinue
    foreach ($f in $scenes) {
        $rel = $f.FullName.Substring($Project.Length + 1).Replace('\','/')
        $title = ""; $nextAct = ""; $levelName = ""
        try {
            $t = [IO.File]::ReadAllText($f.FullName)
            $mm = [regex]::Match($t, 'levelName:\s*(.*)')
            if ($mm.Success) { $levelName = $mm.Groups[1].Value.Trim() }
            $nm = [regex]::Match($t, '(?m)^\s*nextAct:\s*(\S+)\s*$')
            if ($nm.Success) {
                $v = $nm.Groups[1].Value.Trim()
                if ($v -notmatch '[:{}\[\]]' -and $v -ne "") { $nextAct = $v }
            }
            # titulo visible: LargeText del HUD
            $lt = [regex]::Matches($t, 'LargeText:\s*(.*)')
            if ($lt.Count -gt 0) { $title = $lt[0].Groups[1].Value.Trim() }
        } catch { }
        [void]$sceneRows.Add(("{0}`t{1}`t{2}`t{3}" -f $rel, $title, $levelName, $nextAct))
    }
    $sceneRows | Set-Content (Join-Path $IndexDir "scenes.tsv") -Encoding utf8
    Say ("   {0} escenas" -f $sceneRows.Count) "Green"

    Say "== 5/5  referencias inversas (GUID -> quien lo usa) ==" "Cyan"
    $refMap = @{}
    $scanExt = @(".unity",".prefab",".asset",".mat",".controller",".anim",".physicMaterial",".overrideController",".playable",".shadervariants")
    $files = Get-ChildItem $AssetsDir -Recurse -File | Where-Object { $scanExt -contains $_.Extension.ToLower() }
    $i = 0; $totalRefs = 0
    foreach ($f in $files) {
        $i++
        if ($i % 2000 -eq 0) { Say ("   {0}/{1} ficheros" -f $i,$files.Count) }
        try { $t = [IO.File]::ReadAllText($f.FullName) } catch { continue }
        $rel = $f.FullName.Substring($Project.Length + 1).Replace('\','/')
        $seen = @{}
        foreach ($m in $GuidRx.Matches($t)) {
            $g = $m.Groups[1].Value.ToLower()
            if ($seen.ContainsKey($g)) { continue }
            $seen[$g] = $true
            if (-not $refMap.ContainsKey($g)) { $refMap[$g] = New-Object System.Collections.ArrayList }
            [void]$refMap[$g].Add($rel)
            $totalRefs++
        }
    }
    Say ("   {0} referencias de {1} guids" -f $totalRefs, $refMap.Count) "Green"
    $outSb = New-Object System.Text.StringBuilder
    foreach ($k in ($refMap.Keys | Sort-Object)) {
        [void]$outSb.AppendLine(("{0}`t{1}" -f $k, (($refMap[$k] | Sort-Object -Unique) -join ";")))
    }
    [IO.File]::WriteAllText((Join-Path $IndexDir "refs.tsv"), $outSb.ToString(), (New-Object Text.UTF8Encoding $false))

    $sz = (Get-ChildItem $IndexDir -File | Measure-Object Length -Sum).Sum
    Say ("== indice construido en {0:N0}s  ({1:N1} MB) ==" -f ((Get-Date)-$t0).TotalSeconds, ($sz/1MB)) "Green"
}

# ------------------------------------------------------------------ queries
function Load-GuidMap {
    $p = Join-Path $IndexDir "guids.tsv"
    $m = @{}
    foreach ($line in [IO.File]::ReadLines($p)) {
        $i = $line.IndexOf("`t"); if ($i -lt 0) { continue }
        $m[$line.Substring(0,$i)] = $line.Substring($i+1)
    }
    return $m
}

switch ($Command.ToLower()) {
    "build" { Build-Index }

    "guid" {
        $m = Load-GuidMap
        $k = $Arg.ToLower()
        if ($m.ContainsKey($k)) { $m[$k] } else { Say "GUID no encontrado: $Arg" "Red" }
    }

    "path" {
        $m = Load-GuidMap
        $rel = $Arg.Replace('\','/')
        $hit = $m.GetEnumerator() | Where-Object { $_.Value -eq $rel } | Select-Object -First 1
        if ($hit) { $hit.Key } else { Say "Ruta no encontrada en el indice: $Arg" "Red" }
    }

    "users" {
        $m = Load-GuidMap
        $g = $Arg.ToLower()
        if ($m.ContainsKey($g)) { Say ("asset: " + $m[$g]) "Cyan" } else { Say "GUID desconocido: $Arg" "Red" }
        $found = $false
        foreach ($line in [IO.File]::ReadLines((Join-Path $IndexDir "refs.tsv"))) {
            $i = $line.IndexOf("`t"); if ($i -lt 0) { continue }
            if ($line.Substring(0,$i) -eq $g) {
                $found = $true
                $line.Substring($i+1) -split ";" | ForEach-Object { Say ("   " + $_) }
            }
        }
        if (-not $found) { Say "   (nadie lo referencia)" "DarkGray" }
    }

    "shader-users" {
        $q = $Arg
        # el nombre declarado del shader (p.ej. "psx/unlit/ambient") no aparece
        # en la ruta del fichero: hay que resolverlo via shaders.tsv
        $paths = New-Object System.Collections.ArrayList
        foreach ($line in [IO.File]::ReadLines((Join-Path $IndexDir "shaders.tsv"))) {
            $sp = $line -split "`t"
            if ($sp.Count -lt 6) { continue }
            if ($sp[1] -eq $q -or $sp[0] -like "*$q*") { [void]$paths.Add($sp[0]) }
        }
        $rows = @()
        foreach ($line in [IO.File]::ReadLines((Join-Path $IndexDir "materials.tsv"))) {
            $p = $line -split "`t"
            if ($p.Count -lt 4) { continue }
            if ($paths -contains $p[2] -or $p[2] -like "*$q*" -or $p[3] -like "*$q*") { $rows += ,$p }
        }
        Say ("materiales que usan '{0}': {1}" -f $q, $rows.Count) "Cyan"
        if ($paths.Count -gt 0) { Say ("  shaders: " + ($paths -join ", ")) "DarkGray" }
        $rows | Select-Object -First 40 | ForEach-Object { Say ("   {0}  -> {1} [{2}]" -f $_[0], $_[2], $_[3]) }
        if ($rows.Count -gt 40) { Say ("   ... y " + ($rows.Count-40) + " mas") "DarkGray" }
    }

    "shaders" {
        $byName = @{}
        foreach ($line in [IO.File]::ReadLines((Join-Path $IndexDir "shaders.tsv"))) {
            $p = $line -split "`t"
            if ($p.Count -lt 6) { continue }
            $n = $p[1]
            if (-not $byName.ContainsKey($n)) { $byName[$n] = @() }
            $byName[$n] += ,$p
        }
        Say "== nombres declarados DUPLICADOS ==" "Yellow"
        foreach ($k in ($byName.Keys | Sort-Object)) {
            if ($byName[$k].Count -gt 1) {
                Say ("  {0}  ({1} ficheros)" -f $k, $byName[$k].Count) "Yellow"
                $byName[$k] | ForEach-Object { Say ("      {0}  {1}" -f $_[0], $_[2]) }
            }
        }
        Say "== resumen ==" "Cyan"
        $all = @($byName.Keys).Count
        $d = @($byName.Keys | Where-Object { $byName[$_][0][2] -eq "DUMMY" }).Count
        Say ("  {0} nombres, {1} con al menos un dummy" -f $all, $d)
    }

    "globals" {
        $q = $Arg
        Say "== shaders que mencionan '$q' ==" "Cyan"
        foreach ($line in [IO.File]::ReadLines((Join-Path $IndexDir "shaders.tsv"))) {
            $p = $line -split "`t"
            if ($p.Count -lt 6) { continue }
            $inProps = ($p[3] -split ",") | Where-Object { $_ -like "${q}:*" }
            $inCode  = ($p[4] -split ",") | Where-Object { $_ -eq $q }
            $loose   = ($p[5] -split ",") | Where-Object { $_ -eq $q }
            if ($inProps -or $inCode -or $loose) {
                Say ("  {0}" -f $p[0])
                if ($inProps) { Say ("      en Properties: {0}" -f ($inProps -join ", ")) }
                if ($inCode)  { Say ("      en el codigo (sin declararProperties): SI") }
                if ($loose)   { Say ("      uniform suelto: SI") }
            }
        }
    }

    "scenes" {
        Say "== escena -> nivel real ==" "Cyan"
        foreach ($line in [IO.File]::ReadLines((Join-Path $IndexDir "scenes.tsv"))) {
            $p = $line -split "`t"
            if ($p.Count -lt 4) { continue }
            Say ("  {0,-26} titulo='{1}' levelName='{2}' nextAct='{3}'" -f $p[0], $p[1], $p[2], $p[3])
        }
    }

    "dummy-materials" {
        $rows = @()
        foreach ($line in [IO.File]::ReadLines((Join-Path $IndexDir "materials.tsv"))) {
            $p = $line -split "`t"
            if ($p.Count -ge 4 -and $p[3] -eq "DUMMY") { $rows += ,$p }
        }
        Say ("materiales con shader DUMMY: {0}" -f $rows.Count) "Yellow"
        $rows | Select-Object -First 40 | ForEach-Object { Say ("   {0} -> {1}" -f $_[0], $_[2]) }
    }

    "stats" {
        foreach ($n in @("guids","shaders","materials","scenes","refs")) {
            $f = Join-Path $IndexDir "$n.tsv"
            if (Test-Path $f) {
                $c = ([IO.File]::ReadLines($f) | Measure-Object).Count
                Say ("  {0,-10} {1,8} lineas  {2,8:N1} MB" -f $n, $c, ((Get-Item $f).Length/1MB))
            }
        }
    }

    default {
        Get-Help $PSCommandPath -Detailed
    }
}


# Volcado de los 3 TeleportPlayer del Tutorial (indice en una pasada)
$scene = "C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\export\ExportedProject\Assets\Scenes\Tutorial.unity"
$lines = [System.IO.File]::ReadAllLines($scene)

$byId = @{}
$prev = $null
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '^--- !u!(\d+) &(\d+)$') {
        if ($prev -ne $null) { $byId[$prev].End = $i - 1 }
        $cid = $Matches[2]
        $byId[$cid] = @{ Type = $Matches[1]; Start = $i; End = $lines.Length - 1 }
        $prev = $cid
    }
}

function Slice([string]$id) {
    if (-not $byId.ContainsKey($id)) { return $null }
    $b = $byId[$id]
    return $lines[$b.Start..$b.End]
}

function F([string]$id, [string]$key) {
    $b = Slice $id
    if (-not $b) { return "" }
    foreach ($l in $b) {
        if ($l -match ("^  " + [regex]::Escape($key) + ": (.*)$")) { return $Matches[1] }
    }
    return ""
}

function WorldPos([string]$id) {
    $x = 0.0; $y = 0.0; $z = 0.0
    $cur = $id
    $chain = @()
    for ($d = 0; $d -lt 40; $d++) {
        $tb = Slice $cur
        if (-not $tb) { break }
        $lp = F $cur "m_LocalPosition"
        if ($lp -match 'x: (-?[\d\.]+), y: (-?[\d\.]+), z: (-?[\d\.]+)') {
            $x += [double]$Matches[1]; $y += [double]$Matches[2]; $z += [double]$Matches[3]
        }
        $fr = F $cur "m_Father"
        if ($fr -match 'fileID: (\d+)') {
            $cur = $Matches[1]
            if ($cur -eq "0") { break }
            $gb = Slice $cur          # el bloque del GameObject padre
            if ($gb) {
                foreach ($l in $gb) { if ($l -match '^  m_Name: (.*)$') { $chain += $Matches[1]; break } }
            }
        } else { break }
    }
    return ("~({0:N2}, {1:N2}, {2:N2}) via {3}" -f $x, $y, $z, ($chain -join " <- "))
}

foreach ($goId in @('3497', '3538', '3539')) {
    "===== GO $goId : nombre = '$((  (Slice $goId) | Where-Object { $_ -match '^  m_Name: ' } | Select-Object -First 1) -replace '^  m_Name: ', '')' ====="
    "  mundo " + (WorldPos ((Slice $goId) | Where-Object { $_ -match '- component: \{fileID: (\d+)\}' } | Select-Object -First 1 | ForEach-Object { if ($_ -match '(\d+)') { $Matches[1] } }))
    $compIds = @()
    foreach ($l in (Slice $goId)) { if ($l -match '- component: \{fileID: (\d+)\}') { $compIds += $Matches[1] } }
    "  componentes: $($compIds -join ', ')"
    foreach ($c in $compIds) {
        $t = $byId[$c].Type
        $desc = switch ($t) {
            '4'   { "Transform localPos=$(F $c 'm_LocalPosition') escala=$(F $c 'm_LocalScale')" }
            '65'  { "BoxCollider trigger=$(F $c 'm_IsTrigger') enabled=$(F $c 'm_Enabled') size=$(F $c 'm_Size') center=$(F $c 'm_Center')" }
            '135' { "SphereCollider trigger=$(F $c 'm_IsTrigger') radius=$(F $c 'm_Radius') center=$(F $c 'm_Center')" }
            '136' { "CapsuleCollider trigger=$(F $c 'm_IsTrigger') radius=$(F $c 'm_Radius') height=$(F $c 'm_Height') center=$(F $c 'm_Center')" }
            '114' { "MonoBehaviour guid=$(if ((F $c 'm_Script') -match 'guid: (\w+)') { $Matches[1] })" }
            default { "tipo $t" }
        }
        "  [$t] $desc"
    }
}

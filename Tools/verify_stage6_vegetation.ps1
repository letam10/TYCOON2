param(
    [string]$Root = (Join-Path $PSScriptRoot '..')
)

$ErrorActionPreference = 'Stop'
$rootPath = [IO.Path]::GetFullPath($Root)
$checks = 0
$failures = New-Object System.Collections.Generic.List[string]

function Check([bool]$Condition, [string]$Label) {
    $script:checks++
    if (-not $Condition) {
        $script:failures.Add($Label)
    }
}

function Read-Required([string]$Path, [string]$Label) {
    Check (Test-Path -LiteralPath $Path -PathType Leaf) $Label
    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        return Get-Content -LiteralPath $Path -Raw
    }
    return $null
}

function Parse-Number([string]$Text) {
    return [double]::Parse($Text.TrimEnd('f'), [Globalization.CultureInfo]::InvariantCulture)
}

function Resolve-Constant([hashtable]$Constants, [string]$Token) {
    $key = $Token.Trim()
    if ($Constants.ContainsKey($key)) {
        return [double]$Constants[$key]
    }
    return Parse-Number $key
}

function Distance-Squared-To-SegmentXZ($Point, $Start, $End) {
    $dx = $End.X - $Start.X
    $dz = $End.Z - $Start.Z
    $lengthSquared = $dx * $dx + $dz * $dz
    if ($lengthSquared -le 0.000001) {
        $px = $Point.X - $Start.X
        $pz = $Point.Z - $Start.Z
        return $px * $px + $pz * $pz
    }
    $offsetX = $Point.X - $Start.X
    $offsetZ = $Point.Z - $Start.Z
    $projection = [Math]::Max(0.0, [Math]::Min(1.0, ($offsetX * $dx + $offsetZ * $dz) / $lengthSquared))
    $nearestX = $Start.X + $dx * $projection
    $nearestZ = $Start.Z + $dz * $projection
    $deltaX = $Point.X - $nearestX
    $deltaZ = $Point.Z - $nearestZ
    return $deltaX * $deltaX + $deltaZ * $deltaZ
}

function Is-In-Corridor($Point, [array]$Routes, [double]$Clearance) {
    foreach ($route in $Routes) {
        $radius = $route.Width / 2.0 + [Math]::Max(0.0, $Clearance)
        for ($index = 1; $index -lt $route.Nodes.Count; $index++) {
            $distance = Distance-Squared-To-SegmentXZ $Point $route.Nodes[$index - 1] $route.Nodes[$index]
            if ($distance -le $radius * $radius) {
                return $true
            }
        }
    }
    return $false
}

function Find-ReservedRoute($Point, [array]$Routes, [double]$Radius) {
    foreach ($route in $Routes) {
        if (-not $route.ReservesPlacement) {
            continue
        }
        $clearance = $route.Width / 2.0 + [Math]::Max(0.0, $Radius)
        for ($index = 1; $index -lt $route.Nodes.Count; $index++) {
            $distance = Distance-Squared-To-SegmentXZ $Point $route.Nodes[$index - 1] $route.Nodes[$index]
            if ($distance -le $clearance * $clearance) {
                return $route.Id
            }
        }
    }
    return $null
}

$generatedRoot = Join-Path $rootPath 'Assets/_Game/Art/Generated/Vegetation'
$presetPath = Join-Path $generatedRoot 'ez-tree-presets.json'
$licensePath = Join-Path $rootPath 'Docs/ThirdParty/EZ_TREE_LICENSE.md'
$catalogPath = Join-Path $rootPath 'Assets/_Game/Scripts/Runtime/WorldVegetationCatalog.cs'
$routePath = Join-Path $rootPath 'Assets/_Game/Scripts/Runtime/WorldRouteCorridor.cs'

$presetText = Read-Required $presetPath 'preset JSON exists'
$licenseText = Read-Required $licensePath 'EZ-Tree license exists'
$catalogText = Read-Required $catalogPath 'vegetation catalog exists'
$routeText = Read-Required $routePath 'route catalog source exists'

$expectedCommit = 'dcf309bd86bd521083d9c70f01f2de45fdc7c457'
$expectedHashes = @{
    'farm-tree.glb' = 'BD05BE578EB0792C8E7A17478741164967DD23D09C404302B9E276F237ECE4DC'
    'farm-tree-albedo.png' = '1ACB65C568BE7D118360D4543E48684CDF23BEAD5CB192D7DF03753AB9B054B8'
    'forest-tree.glb' = 'BD38FB63FDBB4797CA3D9701D6F0E729017A5738A32E3D3CB47ED23509A4F514'
    'forest-tree-albedo.png' = '034726990A3233F0BADE71DEE4EFDE53211B08EC9C3AE5C671AE150FC2AE8BE6'
    'coastal-tree.glb' = '2736DB84E8076D11808377A87A595B6B1EE623969E8F04FB233D2962A6F2AB02'
    'coastal-tree-albedo.png' = '77ADD85933A6E63EDACE00DE2BBCA53AB6459EE8F01C51EF789660BDEDA026EB'
}

$expectedProfiles = @(
    [pscustomobject]@{ Id = 'farm'; Seed = 20261032; Near = 28.0; Far = 86.0; Density = 0.58; Spacing = 3.5; Clearance = 4.5; LodCount = 3 },
    [pscustomobject]@{ Id = 'forest'; Seed = 20261058; Near = 34.0; Far = 118.0; Density = 0.82; Spacing = 2.75; Clearance = 5.5; LodCount = 4 },
    [pscustomobject]@{ Id = 'coastal'; Seed = 20261086; Near = 30.0; Far = 102.0; Density = 0.46; Spacing = 4.25; Clearance = 6.0; LodCount = 3 }
)

$preset = $null
if ($presetText) {
    try { $preset = $presetText | ConvertFrom-Json } catch { Check $false 'preset JSON parses' }
}
if ($preset) {
    Check ($preset.generator -eq 'ez-tree') 'preset generator is ez-tree'
    Check ($preset.upstream -eq 'https://github.com/dgreenheck/ez-tree') 'preset upstream is ez-tree'
    Check ($preset.pinnedCommit -eq $expectedCommit) 'preset pinned commit'
    Check ($preset.license -eq 'MIT') 'preset license is MIT'
    Check ([int]$preset.seed -eq 20261015) 'preset seed'
}
if ($licenseText) {
    Check ($licenseText.Contains($expectedCommit)) 'license pins expected commit'
    Check ($licenseText -match '(?i)MIT') 'license identifies MIT'
    Check ($licenseText.Contains('dgreenheck/ez-tree')) 'license identifies upstream'
}

$constants = @{}
if ($catalogText) {
    foreach ($match in [regex]::Matches($catalogText, 'const\s+(?:int|float)\s+(?<name>\w+)\s*=\s*(?<value>[-+]?\d*\.?\d+f?)\s*;')) {
        $constants[$match.Groups['name'].Value] = Parse-Number $match.Groups['value'].Value
    }
    Check ($constants.ContainsKey('Seed') -and [int]$constants['Seed'] -eq 20261015) 'catalog seed'
}

$catalogProfiles = @()
if ($catalogText) {
    $profilePattern = 'new\s+WorldVegetationProfile\(\s*"(?<id>[^"]+)"\s*,\s*Seed\s*\+\s*(?<offset>\w+)\s*,\s*(?<near>\w+)\s*,\s*(?<far>\w+)\s*,\s*(?<density>\w+)\s*,\s*(?<spacing>\w+)\s*,\s*(?<clearance>\w+)\s*,\s*(?<lod>\w+)\s*\)'
    foreach ($match in [regex]::Matches($catalogText, $profilePattern, [Text.RegularExpressions.RegexOptions]::Singleline)) {
        $catalogProfiles += [pscustomobject]@{
            Id = $match.Groups['id'].Value
            Seed = [int]$constants['Seed'] + [int](Resolve-Constant $constants $match.Groups['offset'].Value)
            Near = Resolve-Constant $constants $match.Groups['near'].Value
            Far = Resolve-Constant $constants $match.Groups['far'].Value
            Density = Resolve-Constant $constants $match.Groups['density'].Value
            Spacing = Resolve-Constant $constants $match.Groups['spacing'].Value
            Clearance = Resolve-Constant $constants $match.Groups['clearance'].Value
            LodCount = [int](Resolve-Constant $constants $match.Groups['lod'].Value)
        }
    }
}
Check ($catalogProfiles.Count -eq $expectedProfiles.Count) 'catalog profile count'
for ($index = 0; $index -lt [Math]::Min($catalogProfiles.Count, $expectedProfiles.Count); $index++) {
    $actual = $catalogProfiles[$index]
    $expected = $expectedProfiles[$index]
    foreach ($field in @('Id', 'Seed', 'Near', 'Far', 'Density', 'Spacing', 'Clearance', 'LodCount')) {
        Check ($actual.$field -eq $expected.$field) ('catalog ' + $expected.Id + ' ' + $field)
    }
}

if ($preset -and $preset.profiles) {
    Check ($preset.profiles.Count -eq $expectedProfiles.Count) 'preset profile count'
    foreach ($expected in $expectedProfiles) {
        $profile = @($preset.profiles | Where-Object { $_.id -eq $expected.Id })[0]
        Check ($null -ne $profile) ('preset profile ' + $expected.Id)
        if ($profile) {
            Check ([int]$profile.seed -eq $expected.Seed) ('preset ' + $expected.Id + ' seed')
            Check ([double]$profile.targetDensity -eq $expected.Density) ('preset ' + $expected.Id + ' density')
            Check ([double]$profile.minimumSpacing -eq $expected.Spacing) ('preset ' + $expected.Id + ' spacing')
            Check ([double]$profile.corridorClearance -eq $expected.Clearance) ('preset ' + $expected.Id + ' clearance')
            Check ($profile.lod.Count -eq $expected.LodCount) ('preset ' + $expected.Id + ' LOD count')
            if ($profile.lod.Count -ge 3) {
                Check ([double]$profile.lod[0] -eq 0) ('preset ' + $expected.Id + ' LOD zero')
                Check ([double]$profile.lod[1] -eq $expected.Near) ('preset ' + $expected.Id + ' near LOD')
                Check ([double]$profile.lod[2] -eq $expected.Far) ('preset ' + $expected.Id + ' far LOD')
                for ($lodIndex = 1; $lodIndex -lt $profile.lod.Count; $lodIndex++) {
                    Check ([double]$profile.lod[$lodIndex] -gt [double]$profile.lod[$lodIndex - 1]) ('preset ' + $expected.Id + ' ordered LOD ' + $lodIndex)
                }
            }
            Check ($profile.outputs.Count -eq 2) ('preset ' + $expected.Id + ' output count')
            foreach ($output in @($profile.outputs)) {
                Check ($expectedHashes.ContainsKey([string]$output)) ('allowlisted output ' + $output)
            }
        }
    }
}

$allowlisted = @('ez-tree-presets.json') + @($expectedHashes.Keys | Sort-Object)
if (Test-Path -LiteralPath $generatedRoot -PathType Container) {
    $actualFiles = @(Get-ChildItem -LiteralPath $generatedRoot -File | Where-Object { $_.Extension -ne '.meta' } | Select-Object -ExpandProperty Name | Sort-Object)
    Check (($actualFiles -join '|') -eq (($allowlisted | Sort-Object) -join '|')) 'generated vegetation allowlist has no extra files'
}
foreach ($entry in $expectedHashes.GetEnumerator()) {
    $outputPath = Join-Path $generatedRoot $entry.Key
    Check (Test-Path -LiteralPath $outputPath -PathType Leaf) ('output exists ' + $entry.Key)
    if (Test-Path -LiteralPath $outputPath -PathType Leaf) {
        $length = (Get-Item -LiteralPath $outputPath).Length
        Check ($length -gt 0 -and $length -le (15 * 1024)) ('output size ' + $entry.Key)
        $hashA = (Get-FileHash -LiteralPath $outputPath -Algorithm SHA256).Hash.ToUpperInvariant()
        $hashB = (Get-FileHash -LiteralPath $outputPath -Algorithm SHA256).Hash.ToUpperInvariant()
        Check ($hashA -eq $entry.Value) ('output hash ' + $entry.Key)
        Check ($hashA -eq $hashB) ('output hash stable ' + $entry.Key)
    }
}

$routes = @()
if ($routeText) {
    $number = '[-+]?\d*\.?\d+(?:[eE][-+]?\d+)?f?'
    $routePattern = 'new\s+WorldRouteCorridor\(\s*"(?<id>[^"]+)"\s*,\s*RouteKind\.\w+\s*,\s*new\[\]\s*\{(?<nodes>.*?)\}\s*,\s*(?<width>' + $number + ')\s*,\s*(?<reserve>true|false)\s*\)'
    foreach ($match in [regex]::Matches($routeText, $routePattern, [Text.RegularExpressions.RegexOptions]::Singleline)) {
        $nodes = @()
        $vectorPattern = 'new\s+Vector3\(\s*(?<x>' + $number + ')\s*,\s*(?<y>' + $number + ')\s*,\s*(?<z>' + $number + ')\s*\)'
        foreach ($vector in [regex]::Matches($match.Groups['nodes'].Value, $vectorPattern)) {
            $nodes += [pscustomobject]@{
                X = Parse-Number $vector.Groups['x'].Value
                Y = Parse-Number $vector.Groups['y'].Value
                Z = Parse-Number $vector.Groups['z'].Value
            }
        }
        $routes += [pscustomobject]@{
            Id = $match.Groups['id'].Value
            Width = Parse-Number $match.Groups['width'].Value
            ReservesPlacement = [bool]::Parse($match.Groups['reserve'].Value)
            Nodes = $nodes
        }
    }
}
Check ($routes.Count -gt 0) 'route source has default corridors'
Check ((@($routes | Select-Object -ExpandProperty Id -Unique).Count -eq $routes.Count)) 'route ids are unique'
foreach ($route in $routes) {
    Check ($route.Nodes.Count -ge 2) ('route nodes ' + $route.Id)
    Check ($route.Width -gt 0) ('route width ' + $route.Id)
}

foreach ($expected in $expectedProfiles) {
    foreach ($route in $routes) {
        $midpoint = [pscustomobject]@{
            X = ($route.Nodes[0].X + $route.Nodes[1].X) / 2.0
            Y = ($route.Nodes[0].Y + $route.Nodes[1].Y) / 2.0
            Z = ($route.Nodes[0].Z + $route.Nodes[1].Z) / 2.0
        }
        Check (Is-In-Corridor $midpoint $routes $expected.Clearance) ($expected.Id + ' rejects route ' + $route.Id)
        if ($route.ReservesPlacement) {
            Check ((Find-ReservedRoute $midpoint $routes $expected.Spacing) -eq $route.Id) ($expected.Id + ' reservation ' + $route.Id)
        }
    }
    $safePoint = [pscustomobject]@{ X = 520.0; Y = 0.0; Z = 40.0 }
    Check (-not (Is-In-Corridor $safePoint $routes $expected.Clearance)) ($expected.Id + ' safe point corridor')
    Check ($null -eq (Find-ReservedRoute $safePoint $routes $expected.Spacing)) ($expected.Id + ' safe point reservation')
}

if ($failures.Count -gt 0) {
    Write-Output ('Stage 6 vegetation audit: FAIL (' + $checks + ' checks, ' + $failures.Count + ' failures)')
    $failures | ForEach-Object { Write-Output ('- ' + $_) }
    exit 1
}
Write-Output ('Stage 6 vegetation audit: PASS (' + $checks + ' checks)')
exit 0

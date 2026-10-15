[CmdletBinding()]
param(
    [string]$ProjectRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

$pass = 0
$fail = 0
$rows = New-Object System.Collections.Generic.List[string]

function Check {
    param(
        [string]$Name,
        [bool]$Condition,
        [string]$Detail
    )

    if ($Condition) {
        $script:pass++
        [void]$script:rows.Add(('PASS {0} :: {1}' -f $Name, $Detail))
    }
    else {
        $script:fail++
        [void]$script:rows.Add(('FAIL {0} :: {1}' -f $Name, $Detail))
    }
}

function Read-Source {
    param([string]$RelativePath)
    $path = Join-Path $ProjectRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw ('Missing source: ' + $RelativePath)
    }
    return Get-Content -LiteralPath $path -Raw
}

function Test-Number {
    param([string]$Text)
    $token = $Text.Trim().TrimEnd('f', 'F')
    $value = 0.0
    $parsed = [double]::TryParse(
        $token,
        [Globalization.NumberStyles]::Float,
        [Globalization.CultureInfo]::InvariantCulture,
        [ref]$value)
    return $parsed -and -not [double]::IsNaN($value) -and -not [double]::IsInfinity($value)
}

function Test-VectorArgs {
    param([string]$Arguments)
    $parts = $Arguments.Split(',')
    if ($parts.Count -ne 3) {
        return $false
    }
    foreach ($part in $parts) {
        if (-not (Test-Number $part)) {
            return $false
        }
    }
    return $true
}

$mapSource = Read-Source 'Assets/_Game/Scripts/Runtime/CityMapLocations.cs'
$graphicSource = Read-Source 'Assets/_Game/Scripts/Runtime/CityMapGraphic.cs'
$hudSource = Read-Source 'Assets/_Game/Scripts/Runtime/GameHudMap.cs'
$indicatorSource = Read-Source 'Assets/_Game/Scripts/Runtime/MapPlayerIndicator.cs'
$labelSource = Read-Source 'Assets/_Game/Scripts/Runtime/WorldLabelAnchor.cs'
$alignmentSource = Read-Source 'Assets/_Game/Scripts/Runtime/WorldModelAlignmentAudit.cs'
$artSource = Read-Source 'Assets/_Game/Scripts/Runtime/Art.cs'
$wayfindingSource = Read-Source 'Assets/_Game/Scripts/Runtime/WorldExpansionWayfinding.cs'
$routeSource = Read-Source 'Assets/_Game/Scripts/Runtime/WorldRouteCorridor.cs'
$expansionSource = Read-Source 'Assets/_Game/Scripts/Runtime/WorldExpansionCatalog.cs'
$signalSource = Read-Source 'Assets/_Game/Scripts/Runtime/CityTrafficSignals.cs'
$signalVisualSource = Read-Source 'Assets/_Game/Scripts/Runtime/CityTrafficSignalVisual.cs'
$districtSource = Read-Source 'Assets/_Game/Scripts/Runtime/CityDistricts.cs'
$railSource = Read-Source 'Assets/_Game/Scripts/Runtime/CityRailNetwork.cs'

$expectedIds = @(
    'tycoon', 'park', 'station', 'beach', 'harbor', 'marina', 'bank',
    'restaurant', 'forest', 'forest_reserve', 'industrial_yard', 'north_village',
    'east_marina', 'south_highway'
)

$pointMatches = [regex]::Matches(
    $mapSource,
    '(?m)^\s*\("(?<id>[^"]+)",\s*"(?<label>[^"]*)",\s*(?<position>.+?)\)\s*,?\s*$')
$pointIds = @($pointMatches | ForEach-Object { $_.Groups['id'].Value })
$pointShape = $pointMatches.Count -eq $expectedIds.Count
for ($index = 0; $index -lt $expectedIds.Count; $index++) {
    if ($index -ge $pointIds.Count -or $pointIds[$index] -ne $expectedIds[$index]) {
        $pointShape = $false
    }
}
$duplicatePointIds = @($pointIds | Group-Object | Where-Object Count -gt 1)
Check 'map-point-stable-order' $pointShape ('found {0} entries; expected 14 stable ids' -f $pointMatches.Count)
Check 'map-point-id-uniqueness' ($duplicatePointIds.Count -eq 0) 'no duplicate point ids'
$pointLabelsValid = $true
$pointPositionsValid = $true
foreach ($match in $pointMatches) {
    if ([string]::IsNullOrWhiteSpace($match.Groups['label'].Value)) {
        $pointLabelsValid = $false
    }
    if ($match.Groups['position'].Value -match '(?i)NaN|Infinity') {
        $pointPositionsValid = $false
    }
}
Check 'map-point-labels' $pointLabelsValid 'every stable point has a non-empty label expression'
$referenceFinite = $districtSource -notmatch '(?i)NaN|Infinity' -and $railSource -notmatch '(?i)NaN|Infinity'
Check 'map-point-finite-positions' ($pointPositionsValid -and $referenceFinite) 'point expressions and referenced district/rail constants contain no non-finite literal'

$captionOffsetBlock = [regex]::Match(
    $mapSource,
    'public static Vector2 CaptionOffset.*?public static float CaptionWidth',
    [Text.RegularExpressions.RegexOptions]::Singleline).Value
$captionWidthBlock = [regex]::Match(
    $mapSource,
    'public static float CaptionWidth.*?public static bool Teleport',
    [Text.RegularExpressions.RegexOptions]::Singleline).Value
$captionIdsValid = $true
foreach ($id in $expectedIds) {
    if ($captionOffsetBlock -notmatch ('"' + [regex]::Escape($id) + '"')) {
        $captionIdsValid = $false
    }
}
$captionWidthValid = $captionWidthBlock -match '(?m)=>\s*(160|180|200)f' -and
    $captionWidthBlock -notmatch '(?i)NaN|Infinity|=>\s*-[0-9]'
Check 'caption-offset-coverage' $captionIdsValid 'CaptionOffset covers all stable point ids'
Check 'caption-width-footprint' $captionWidthValid 'CaptionWidth provides positive finite widths'

$labelContract = ($alignmentSource.Contains('CheckLabel') -and
    $alignmentSource.Contains('InsideFootprint') -and
    $alignmentSource.Contains('WithinDistance') -and
    $alignmentSource.Contains('FacingFinite') -and
    $alignmentSource.Contains('BillboardConfigured') -and
    $alignmentSource.Contains('HorizontalDistanceToBounds'))
$anchorContract = ($labelSource.Contains('ModelRoot') -and
    $labelSource.Contains('IsFacingOrBillboardConfigured') -and
    $artSource.Contains('AddComponent<WorldLabelAnchor>'))
$artLabelContract = ($artSource.Contains('AddComponent<WorldLabelAnchor>') -and
    $artSource.Contains('anchor.ModelRoot = parent') -and
    $artSource.Contains('AddComponent<BillboardLabel>'))
$expansionLabelCount = @([regex]::Matches($wayfindingSource, 'Art\.Label')).Count
Check 'label-footprint-facing-audit' $labelContract 'WorldModelAlignmentAudit exposes footprint, distance, forward and billboard checks'
Check 'label-anchor-renderer-contract' ($anchorContract -and $artLabelContract) 'WorldLabelAnchor and Art.Label bind model root and billboard state'
Check 'expansion-label-pair' ($expansionLabelCount -ge 2) ('expansion wayfinding emits {0} label calls' -f $expansionLabelCount)

$routeMatches = [regex]::Matches($routeSource, 'new WorldRouteCorridor\(\s*"(?<id>[^"]+)"')
$routeIds = @($routeMatches | ForEach-Object { $_.Groups['id'].Value })
$duplicateRouteIds = @($routeIds | Group-Object | Where-Object Count -gt 1)
$requiredRouteIds = @(
    'expansion-forest-access', 'expansion-industrial-approach', 'expansion-village-road',
    'expansion-marina-dock', 'expansion-south-highway'
)
$routeShape = $routeMatches.Count -eq 13 -and $duplicateRouteIds.Count -eq 0
foreach ($id in $requiredRouteIds) {
    if ($routeIds -notcontains $id) {
        $routeShape = $false
    }
}
$routeVectorMatches = [regex]::Matches($routeSource, 'new Vector3\((?<args>[^)]*)\)')
$routeVectorsFinite = @($routeVectorMatches |
    Where-Object { $_.Groups['args'].Value -match '(?i)NaN|Infinity' }).Count -eq 0
$routeApiContract = $routeSource.Contains('ValidateContinuous') -and
    $routeSource.Contains('IsReserved') -and
    $routeSource.Contains('TryGetNearestPoint')
Check 'route-catalog-coverage' $routeShape ('found {0} unique routes including all expansion corridors' -f $routeMatches.Count)
Check 'route-node-finiteness' $routeVectorsFinite ('checked {0} route Vector3 expressions for non finite tokens' -f $routeVectorMatches.Count)
Check 'route-placement-api' $routeApiContract 'continuous, reservation and nearest-point APIs are present'

$poiMatches = [regex]::Matches(
    $expansionSource,
    'new WorldExpansionPoi\(\s*"(?<id>[^"]+)"(?<body>.*?)(?=\r?\n\s*new WorldExpansionPoi|\r?\n\s*\}\);)',
    [Text.RegularExpressions.RegexOptions]::Singleline)
$poiIds = @($poiMatches | ForEach-Object { $_.Groups['id'].Value })
$expectedPoiIds = @('ForestReserve', 'IndustrialYard', 'NorthVillage', 'EastMarina', 'SouthHighway')
$poiShape = $poiMatches.Count -eq $expectedPoiIds.Count
$poiAnchorsValid = $true
foreach ($id in $expectedPoiIds) {
    $poi = @($poiMatches | Where-Object { $_.Groups['id'].Value -eq $id }) | Select-Object -First 1
    if ($null -eq $poi) {
        $poiShape = $false
        $poiAnchorsValid = $false
        continue
    }
    $vectors = [regex]::Matches($poi.Groups['body'].Value, 'new Vector3\((?<args>[^)]*)\)')
    if ($vectors.Count -lt 4) {
        $poiAnchorsValid = $false
    }
    foreach ($vector in $vectors) {
        if (-not (Test-VectorArgs $vector.Groups['args'].Value)) {
            $poiAnchorsValid = $false
        }
    }
}
$poiUnique = @($poiIds | Group-Object | Where-Object Count -gt 1).Count -eq 0
$poiApiContract = $expansionSource.Contains('RouteAnchors => routeAnchors') -and
    $expansionSource.Contains('FootprintBounds') -and
    $expansionSource.Contains('RouteAnchorMargin') -and
    $expansionSource.Contains('ExpandedWorldBounds')
$sharedAnchorLiterals = @(
    '-370, 0, 275', '465, 0, 330', '-55, 0, 320', '55, 0, 345',
    '370, 0, -135', '440, 0, -152', '470, 0, -105', '-190, 0, -300', '190, 0, -300'
)
$sharedAnchorsValid = $true
foreach ($literal in $sharedAnchorLiterals) {
    $pattern = [regex]::Escape($literal).Replace('\ ', '\s*')
    if ($expansionSource -notmatch $pattern -or $routeSource -notmatch $pattern) {
        $sharedAnchorsValid = $false
    }
}
Check 'expansion-poi-coverage' ($poiShape -and $poiUnique) ('found {0} unique expansion POIs' -f $poiMatches.Count)
Check 'expansion-route-anchors' $poiAnchorsValid 'each POI has at least two finite route anchor vectors'
Check 'expansion-footprint-contract' $poiApiContract 'POIs expose route anchors, footprint bounds and expanded-world validation'
Check 'expansion-route-anchor-sharing' $sharedAnchorsValid 'POI approach anchors share coordinates with their expansion route corridors'

$signalMatches = [regex]::Matches(
    $signalSource,
    'new\("(?<id>[^"]+)",\s*new\((?<position>[^)]*)\),\s*(?<offset>[^)]*)\)')
$signalIds = @($signalMatches | ForEach-Object { $_.Groups['id'].Value })
$expectedSignalIds = @('west_south', 'west_mid', 'west_north', 'east_south', 'east_mid', 'east_north')
$signalShape = $signalMatches.Count -eq $expectedSignalIds.Count
for ($index = 0; $index -lt $expectedSignalIds.Count; $index++) {
    if ($index -ge $signalIds.Count -or $signalIds[$index] -ne $expectedSignalIds[$index]) {
        $signalShape = $false
    }
}
$signalFinite = $true
foreach ($signal in $signalMatches) {
    if (-not (Test-VectorArgs $signal.Groups['position'].Value) -or
        -not (Test-Number $signal.Groups['offset'].Value)) {
        $signalFinite = $false
    }
}
$signalContract = $signalSource.Contains('Definitions => Defaults') -and
    $signalSource.Contains('PhaseAt') -and
    $signalVisualSource.Contains('groups.Count * 3') -and
    $signalVisualSource.Contains('CityTrafficSignalSystem.Definitions.Count') -and
    $signalVisualSource.Contains('SignalLamp_') -and
    $signalVisualSource.Contains('Red') -and
    $signalVisualSource.Contains('Amber') -and
    $signalVisualSource.Contains('Green')
Check 'traffic-signal-definitions' $signalShape ('found {0} stable signal definitions' -f $signalMatches.Count)
Check 'traffic-signal-finite-positions' $signalFinite 'signal positions and cycle offsets are finite numeric values'
Check 'traffic-signal-visual-source' $signalContract 'visual consumes the signal system and builds red/amber/green lamp groups'

$kindBlock = [regex]::Match(
    $graphicSource,
    'public static string PoiKind.*?public static string PoiGlyph',
    [Text.RegularExpressions.RegexOptions]::Singleline).Value
$glyphBlock = [regex]::Match(
    $graphicSource,
    'public static string PoiGlyph.*?public static Color PoiColor',
    [Text.RegularExpressions.RegexOptions]::Singleline).Value
$colorBlock = [regex]::Match(
    $graphicSource,
    'public static Color PoiColor.*?public void Initialize',
    [Text.RegularExpressions.RegexOptions]::Singleline).Value
$kindMap = @{}
foreach ($line in ($kindBlock -split '\r?\n')) {
    $mapping = [regex]::Match($line, '(?<keys>"[^"]+"(?:\s+or\s+"[^"]+")*)\s*=>\s*"(?<value>[^"]+)"')
    if (-not $mapping.Success) { continue }
    foreach ($key in [regex]::Matches($mapping.Groups['keys'].Value, '"(?<id>[^"]+)"')) {
        $kindMap[$key.Groups['id'].Value] = $mapping.Groups['value'].Value
    }
}
$expectedKinds = [ordered]@{
    tycoon = 'farm'; park = 'park'; station = 'rail'; beach = 'beach'; harbor = 'marina';
    marina = 'marina'; bank = 'civic'; restaurant = 'food'; forest = 'forest';
    forest_reserve = 'forest'; industrial_yard = 'industrial'; north_village = 'village';
    east_marina = 'marina'; south_highway = 'highway'
}
$kindMapValid = $true
foreach ($id in $expectedKinds.Keys) {
    if (-not $kindMap.ContainsKey($id) -or $kindMap[$id] -ne $expectedKinds[$id]) {
        $kindMapValid = $false
    }
}
$uniqueKinds = @($kindMap.Values | Sort-Object -Unique).Count
$glyphKindsValid = $true
$colorKindsValid = $true
foreach ($kind in @($expectedKinds.Values | Sort-Object -Unique)) {
    if ($glyphBlock -notmatch ('"' + [regex]::Escape($kind) + '"\s*=>\s*"[^"]+"')) {
        $glyphKindsValid = $false
    }
    if ($colorBlock -notmatch ('"' + [regex]::Escape($kind) + '"\s*=>\s*new Color32\([^)]*,\s*255\)')) {
        $colorKindsValid = $false
    }
}
$hudContract = $hudSource.Contains('CityMapLocations.Points') -and
    $hudSource.Contains('CityMapGraphic.PoiGlyph(point.id)') -and
    $hudSource.Contains('MapPin_')
$indicatorContract = $indicatorSource.Contains('map.Position(player.transform.position)')
Check 'map-icon-kind-coverage' ($kindMapValid -and $uniqueKinds -ge 10) ('mapped {0} stable ids to {1} icon kinds' -f $expectedKinds.Count, $uniqueKinds)
Check 'map-icon-glyphs' $glyphKindsValid 'every icon kind has a non-empty glyph mapping'
Check 'map-icon-colors' $colorKindsValid 'every icon kind has an opaque color mapping'
Check 'map-hud-stable-id-readers' ($hudContract -and $indicatorContract) 'HUD and player indicator consume stable ids/map positions'

$rows | Write-Output
Write-Output ('SUMMARY PASS={0} FAIL={1}' -f $pass, $fail)
if ($fail -gt 0) {
    exit 1
}
exit 0

param(
    [Parameter(Mandatory = $true)]
    [string]$RunDirectory,
    [string]$OutputPath = ''
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$runRoot = [IO.Path]::GetFullPath($RunDirectory)
$runPrefix = $runRoot + [IO.Path]::DirectorySeparatorChar
$reportPath = Join-Path $runRoot 'ordinary-play-report.json'
$playReport = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
$images = @($playReport.screenshots)
$rows = [Collections.Generic.List[object]]::new()

foreach ($name in $images) {
    $path = [IO.Path]::GetFullPath((Join-Path $runRoot $name))
    if (!$path.StartsWith($runPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Capture path must stay inside its run directory.'
    }
    $row = [ordered]@{
        file = $name
        width = 0
        height = 0
        sampleCount = 0
        distinctColors = 0
        maximumChannel = 0
        accepted = $false
        reason = 'Missing capture'
    }
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        $bitmap = [Drawing.Bitmap]::new($path)
        try {
            $row.width = $bitmap.Width
            $row.height = $bitmap.Height
            $colors = [Collections.Generic.HashSet[int]]::new()
            $maximumChannel = 0
            # Lấy mẫu phân bố trên ảnh; kiểm blank sau lượt đo để không tăng chi phí FPS.
            for ($y = 0; $y -lt 18; $y++) {
                for ($x = 0; $x -lt 32; $x++) {
                    $pixelX = [Math]::Min($bitmap.Width - 1, [int](($x + 0.5) * $bitmap.Width / 32))
                    $pixelY = [Math]::Min($bitmap.Height - 1, [int](($y + 0.5) * $bitmap.Height / 18))
                    $color = $bitmap.GetPixel($pixelX, $pixelY)
                    $colors.Add($color.ToArgb()) | Out-Null
                    $maximumChannel = [Math]::Max($maximumChannel, $color.R)
                    $maximumChannel = [Math]::Max($maximumChannel, $color.G)
                    $maximumChannel = [Math]::Max($maximumChannel, $color.B)
                    $row.sampleCount++
                }
            }
            $row.distinctColors = $colors.Count
            $row.maximumChannel = $maximumChannel
            $row.accepted = $colors.Count -gt 1 -and $maximumChannel -gt 8
            $row.reason = if ($row.accepted) { 'Nonblank capture' } else { 'Black or uniform capture' }
        }
        finally {
            $bitmap.Dispose()
        }
    }
    $rows.Add([pscustomobject]$row)
}

$failed = @($rows | Where-Object { !$_.accepted })
$result = [pscustomobject]@{
    runDirectory = $runRoot
    passed = $images.Count -gt 0 -and $failed.Count -eq 0
    imageCount = $images.Count
    failedImageCount = $failed.Count
    scope = 'Nonblank screenshot evidence only; not proof of continuous rendering or display presentation'
    images = @($rows.ToArray())
}

if ($OutputPath) {
    $target = [IO.Path]::GetFullPath($OutputPath)
    $projectRoot = Split-Path -Parent $PSScriptRoot
    $workPrefix = [IO.Path]::GetFullPath((Join-Path $projectRoot 'work')) + [IO.Path]::DirectorySeparatorChar
    if (!$target.StartsWith($workPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Capture validation output must stay inside work/.'
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    $result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $target -Encoding utf8
}

Write-Output ('Capture evidence: ' + $result.passed + ', images=' + $images.Count + ', failed=' + $failed.Count)
if (!$result.passed) {
    throw 'Missing, black, or uniform capture evidence; reject rendered-game benchmark acceptance.'
}

param(
    [Parameter(Mandatory = $true)][string]$ExpectedExe,
    [Parameter(Mandatory = $true)][string]$MeasurementScope,
    [ValidateRange(15, 3600)][int]$DurationSeconds = 180,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$workRoot = [IO.Path]::GetFullPath((Join-Path $taskRoot 'work')) + [IO.Path]::DirectorySeparatorChar
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
if (!$outputRoot.StartsWith($workRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Hardware measurement output must stay below the workspace work directory.'
}
$outputPath = Join-Path $outputRoot ('samples-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
New-Item -ItemType Directory -Path $outputPath | Out-Null
$gameProcess = $null
$expectedPath = [IO.Path]::GetFullPath($ExpectedExe)
$expectedProcessName = [IO.Path]::GetFileNameWithoutExtension($expectedPath)
$deadline = [DateTime]::UtcNow.AddSeconds(20)
while (!$gameProcess -and [DateTime]::UtcNow -lt $deadline) {
    $gameProcess = Get-Process -Name $expectedProcessName -ErrorAction SilentlyContinue | Where-Object {
        $_.Path -eq $expectedPath
    } | Select-Object -First 1
    if (!$gameProcess) { Start-Sleep -Milliseconds 200 }
}
if (!$gameProcess) { throw 'Expected Player executable is not running.' }
$cpuInfo = Get-CimInstance Win32_Processor
$logicalCount = [int]$cpuInfo.NumberOfLogicalProcessors
Start-Sleep -Seconds 15
$clock = [Diagnostics.Stopwatch]::StartNew()
$gameProcess.Refresh()
$previousCpu = $gameProcess.TotalProcessorTime.TotalMilliseconds
$previousTime = $clock.Elapsed.TotalMilliseconds
$samples = [Collections.Generic.List[object]]::new()
while ($clock.Elapsed.TotalSeconds -lt $DurationSeconds -and !$gameProcess.HasExited) {
    Start-Sleep -Milliseconds 1000
    $gameProcess.Refresh()
    if ($gameProcess.HasExited) { break }
    $sampleTime = $clock.Elapsed.TotalMilliseconds
    $sampleCpu = $gameProcess.TotalProcessorTime.TotalMilliseconds
    # Chuẩn hóa CPU của game theo toàn bộ16luồng, không coi một luồng là toàn CPU.
    $gameCpu = 100 * ($sampleCpu - $previousCpu) / (($sampleTime - $previousTime) * $logicalCount)
    $previousCpu = $sampleCpu
    $previousTime = $sampleTime
    $processor = Get-CimInstance Win32_PerfFormattedData_Counters_ProcessorInformation `
        -Filter "Name = '_Total'"
    $gpuText = & nvidia-smi --query-gpu=utilization.gpu,clocks.current.graphics,temperature.gpu,power.draw `
        --format=csv,noheader,nounits
    if ($LASTEXITCODE) { throw 'NVIDIA telemetry query failed.' }
    $gpuFields = ($gpuText | Select-Object -First 1).Split(',')
    $samples.Add([pscustomobject]@{
        utc = [DateTime]::UtcNow.ToString('o')
        seconds = $clock.Elapsed.TotalSeconds
        cpuGamePercent = $gameCpu
        cpuTotalPercent = [double]$processor.PercentProcessorTime
        cpuWindowsReportedMHz = [double]$processor.ProcessorFrequency
        cpuActualClockMHz = $null
        cpuTemperatureCelsius = $null
        gpuTotalPercent = [double]::Parse($gpuFields[0].Trim(), [cultureinfo]::InvariantCulture)
        gpuGraphicsMHz = [double]::Parse($gpuFields[1].Trim(), [cultureinfo]::InvariantCulture)
        gpuTemperatureCelsius = [double]::Parse($gpuFields[2].Trim(), [cultureinfo]::InvariantCulture)
        gpuPowerWatts = [double]::Parse($gpuFields[3].Trim(), [cultureinfo]::InvariantCulture)
    })
}
if ($samples.Count -lt 10) { throw 'Too few hardware samples for a useful report.' }
$gpuName = (& nvidia-smi --query-gpu=name --format=csv,noheader | Select-Object -First 1).Trim()
if ($LASTEXITCODE) { throw 'NVIDIA device name query failed.' }
$metrics = [ordered]@{}
foreach ($name in @('cpuGamePercent','cpuTotalPercent','cpuWindowsReportedMHz','gpuTotalPercent',
    'gpuGraphicsMHz','gpuTemperatureCelsius','gpuPowerWatts')) {
    $stats = $samples | Measure-Object -Property $name -Average -Minimum -Maximum
    $metrics[$name] = [ordered]@{ mean = $stats.Average; minimum = $stats.Minimum; maximum = $stats.Maximum }
}
$report = [ordered]@{
    scope = $MeasurementScope
    processId = $gameProcess.Id
    executable = $gameProcess.Path
    requestedSeconds = $DurationSeconds
    warmupSeconds = 15
    firstSampleUtc = $samples[0].utc
    lastSampleUtc = $samples[$samples.Count - 1].utc
    cpuName = $cpuInfo.Name.Trim()
    cpuCores = $cpuInfo.NumberOfCores
    logicalProcessors = $logicalCount
    gpuName = $gpuName
    sampleCount = $samples.Count
    measuredSeconds = $clock.Elapsed.TotalSeconds
    cpuTemperatureAvailable = $false
    cpuActualClockAvailable = $false
    cpuClockNote = 'Windows frequency field is not a directly measured boost or per-core clock'
    cpuPercentNote = 'Process CPU time normalized to16logical processors; total CPU includes other apps'
    gpuPercentNote = 'NVIDIA utilization and graphics clock are whole-device telemetry'
    presentation165HzVerified = $false
    metrics = $metrics
}
$samples | Export-Csv -LiteralPath (Join-Path $outputPath 'hardware.csv') -NoTypeInformation -Encoding utf8
$report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $outputPath 'hardware-report.json')
$outputPath
$report | ConvertTo-Json -Depth 10

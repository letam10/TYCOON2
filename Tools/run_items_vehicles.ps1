param(
    [string]$BuildPath = 'work/items-vehicles-20261009/Release/TYCOON2.exe',
    [string]$LoadPath = 'work/physical-carry-20261007/player-20261008T073854601Z/qa-save-v2.json',
    [switch]$Visible,
    [switch]$RidesOnly,
    [switch]$DiningOnly,
    [switch]$CityModels
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$executable = [System.IO.Path]::GetFullPath((Join-Path $taskRoot $BuildPath))
$source = [System.IO.Path]::GetFullPath((Join-Path $taskRoot $LoadPath))
if (!(Test-Path -LiteralPath $executable)) { throw 'Windows Player missing' }
if (!(Test-Path -LiteralPath $source)) { throw 'QA checkpoint missing' }
$sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$output = Join-Path $taskRoot ('work/items-vehicles-20261009/player-' +
    [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
New-Item -ItemType Directory -Path $output | Out-Null
$arguments = @(
    '-force-d3d11', '-screen-width', '1920', '-screen-height', '1080', '-screen-fullscreen', '0',
    '-logFile', ('"' + (Join-Path $output 'Player.log') + '"'), '--qa-items-vehicles-v8', '--require-4060',
    '--qa-output', ('"' + $output + '"'), '--qa-physical-from', ('"' + $source + '"')
)
if ($RidesOnly) { $arguments += '--qa-vehicle-rides-only' }
if ($DiningOnly) { $arguments += '--qa-dining-only' }
if ($CityModels) { $arguments += '--qa-city-models-v11' }
$style = if ($Visible) { 'Normal' } else { 'Hidden' }
$process = Start-Process -FilePath $executable -ArgumentList $arguments -PassThru -WindowStyle $style
Write-Output ('Player PID ' + $process.Id + ' output ' + $output)
$process.WaitForExit()
Write-Output ('Player exit code ' + $process.ExitCode)
if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $sourceHash) {
    throw 'Source checkpoint changed during Player acceptance'
}
Get-Content -LiteralPath (Join-Path $output 'Player.log') -Tail 12
exit $process.ExitCode

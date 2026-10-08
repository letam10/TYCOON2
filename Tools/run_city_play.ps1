param(
    [string]$BuildPath = 'work/city-expansion-20261008/Release/TYCOON2.exe',
    [switch]$Visible,
    [string]$LoadPath = '',
    [switch]$Profile,
    [switch]$Motion
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$exe = [System.IO.Path]::GetFullPath((Join-Path $taskRoot $BuildPath))
if (!(Test-Path -LiteralPath $exe -PathType Leaf)) { throw 'Windows Player missing.' }
$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
$outputRoot = Join-Path $taskRoot ('work\city-expansion-20261008\ordinary-play-' + $stamp)
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$logPath = Join-Path $outputRoot 'Player.log'
$arguments = @(
    '-force-d3d11', '-force-device-index', '0',
    '-screen-width', '1920', '-screen-height', '1080', '-screen-fullscreen', '0',
    '-logFile', ('"' + $logPath + '"'), '--qa-city-play', '--require-4060',
    '--qa-output', ('"' + $outputRoot + '"')
)
if ($LoadPath) {
    $sourcePath = [System.IO.Path]::GetFullPath($LoadPath)
    if (!(Test-Path -LiteralPath $sourcePath -PathType Leaf)) { throw 'Checkpoint missing.' }
    $arguments += @('--qa-city-from', ('"' + $sourcePath + '"'))
}
if ($Profile) { $arguments += @('--qa-city-profile', 'true') }
if ($Motion) { $arguments += @('--qa-city-motion', 'true') }
$windowStyle = if ($Visible) { 'Normal' } else { 'Hidden' }
$process = Start-Process -FilePath $exe -ArgumentList $arguments -PassThru -WindowStyle $windowStyle
. (Join-Path $PSScriptRoot 'process_record.ps1')
Write-TaskProcessRecord -Root $taskRoot -Name 'city-ordinary-play' -Record ([pscustomobject]@{
    Task = 'OrdinaryCityPlay'
    ProcessId = $process.Id
    Exe = $exe
    Arguments = $arguments
    StartedUtc = [DateTime]::UtcNow.ToString('o')
})
Write-Output ('Ordinary Player PID ' + $process.Id)
Write-Output $outputRoot
$process.WaitForExit()
Write-Output ('Player exit code ' + $process.ExitCode)
Get-Content -LiteralPath $logPath -Tail 16
exit $process.ExitCode

param(
    [string]$BuildPath = 'Builds/Windows/TYCOON2.exe',
    [switch]$Load,
    [string]$OutputPath = ''
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$playerPath = [IO.Path]::GetFullPath((Join-Path $taskRoot $BuildPath))
if (!(Test-Path -LiteralPath $playerPath -PathType Leaf)) { throw 'Windows build missing.' }
if (!$OutputPath) {
    if ($Load) { throw 'Relaunch requires the first run output directory.' }
    $OutputPath = 'work/world-map-20261014/player-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
}
$output = [IO.Path]::GetFullPath((Join-Path $taskRoot $OutputPath))
$workBoundary = [IO.Path]::GetFullPath((Join-Path $taskRoot 'work')) + '\'
if (!$output.StartsWith($workBoundary, [StringComparison]::OrdinalIgnoreCase)) { throw 'QA output must stay under work.' }
if ($Load -and !(Test-Path -LiteralPath (Join-Path $output 'feature-save-expected.json'))) { throw 'First feature run evidence missing.' }
New-Item -ItemType Directory -Path $output -Force | Out-Null
$name = if ($Load) { 'world-features-load' } else { 'world-features' }
$log = Join-Path $output ($name + '.log')
$arguments = @('-force-d3d11', '-screen-width', '1920', '-screen-height', '1080', '-screen-fullscreen', '0',
    '-logFile', ('"' + $log + '"'), ('--qa-' + $name), '--qa-m0', '--require-4060', '--qa-output', ('"' + $output + '"'))
$process = Start-Process -FilePath $playerPath -ArgumentList $arguments -PassThru -WindowStyle Hidden
. (Join-Path $PSScriptRoot 'process_record.ps1')
Write-TaskProcessRecord -Root $taskRoot -Name $name -Record ([pscustomobject]@{
    Task=$name; ProcessId=$process.Id; Exe=$playerPath; Arguments=$arguments; StartedUtc=[DateTime]::UtcNow.ToString('o')
})
Write-Output ('Feature Player PID ' + $process.Id + ' output ' + $output)
$process.WaitForExit()
Write-Output ('Player exit code ' + $process.ExitCode)
Get-Content -LiteralPath $log -Encoding UTF8 -Tail 12
exit $process.ExitCode

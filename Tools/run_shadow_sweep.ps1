param(
    [string]$BuildPath = 'work/shadow-v10-20261009/Before/TYCOON2.exe',
    [switch]$Visible
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskExe = [System.IO.Path]::GetFullPath((Join-Path $taskRoot $BuildPath))
if (!(Test-Path -LiteralPath $taskExe)) { throw 'Windows Player missing.' }
$taskOutput = Join-Path $taskRoot ('work/shadow-v10-20261009/player-' +
    [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
$taskArguments = @(
    '-force-d3d11', '-screen-width', '1920', '-screen-height', '1080', '-screen-fullscreen', '0',
    '-logFile', ('"' + (Join-Path $taskOutput 'Player.log') + '"'), '--qa-shadow-sweep', '--require-4060',
    '--qa-output', ('"' + $taskOutput + '"')
)
$taskStyle = if ($Visible) { 'Normal' } else { 'Hidden' }
$taskProcess = Start-Process -FilePath $taskExe -ArgumentList $taskArguments -PassThru -WindowStyle $taskStyle
Write-Output ('Shadow Player PID ' + $taskProcess.Id + ' output ' + $taskOutput)
$taskProcess.WaitForExit()
Write-Output ('Player exit code ' + $taskProcess.ExitCode)
Get-Content -LiteralPath (Join-Path $taskOutput 'Player.log') -Tail 12
exit $taskProcess.ExitCode

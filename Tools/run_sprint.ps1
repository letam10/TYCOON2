param([string]$BuildPath = 'Builds/Windows/TYCOON2.exe')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$playerPath = [IO.Path]::GetFullPath((Join-Path $taskRoot $BuildPath))
if (!(Test-Path -LiteralPath $playerPath -PathType Leaf)) { throw 'Windows build missing.' }
$outputPath = Join-Path $taskRoot ('work/shift-final-20261014/foundation-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$logPath = Join-Path $outputPath 'Player.log'
$arguments = @(
    '-force-d3d11', '-force-device-index', '0',
    '-screen-width', '1920', '-screen-height', '1080', '-screen-fullscreen', '0',
    '-logFile', ('"' + $logPath + '"'), '--qa-m0', '--require-4060',
    '--qa-output', ('"' + $outputPath + '"')
)
$process = Start-Process -FilePath $playerPath -ArgumentList $arguments -PassThru -WindowStyle Hidden
. (Join-Path $PSScriptRoot 'process_record.ps1')
Write-TaskProcessRecord -Root $taskRoot -Name 'sprint-foundation' -Record ([pscustomobject]@{
    Task = 'SprintFoundation'; ProcessId = $process.Id; Exe = $playerPath
    Arguments = $arguments; StartedUtc = [DateTime]::UtcNow.ToString('o')
})
Write-Output ('Sprint foundation PID ' + $process.Id + ' output ' + $outputPath)
$process.WaitForExit()
Write-Output ('Player exit code ' + $process.ExitCode)
Get-Content -LiteralPath $logPath -Tail 12
exit $process.ExitCode

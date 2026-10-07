param(
    [ValidateSet('All', 'Progression', 'Crews', 'Roles', 'Cargo', 'Events', 'Save', 'Load')]
    [string]$Case = 'All',
    [string]$BuildPath = 'work/completion-audit/Build/TYCOON2.exe',
    [string]$LoadPath = ''
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$playerPath = [IO.Path]::GetFullPath((Join-Path $taskRoot $BuildPath))
if (!(Test-Path -LiteralPath $playerPath -PathType Leaf)) {
    throw 'Build kiểm chứng chưa tồn tại.'
}
if ($Case -eq 'Load') {
    if (!$LoadPath -or !(Test-Path -LiteralPath $LoadPath -PathType Leaf)) {
        throw 'Cần chỉ định snapshot QA có thật để kiểm tra relaunch.'
    }
    $LoadPath = [IO.Path]::GetFullPath($LoadPath)
}
$runName = 'player-' + $Case.ToLower() + '-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
$outputPath = Join-Path $taskRoot ('work/completion-audit/' + $runName)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$logPath = Join-Path $outputPath 'player.log'
$mode = if ($Case -eq 'Load') { '--qa-completion-load' } else { '--qa-completion' }
$arguments = @(
    '-force-d3d11', '-force-device-index', '0',
    '-screen-width', '1920', '-screen-height', '1080', '-screen-fullscreen', '0',
    '-logFile', ('"' + $logPath + '"'), $mode, '--require-4060',
    '--qa-output', ('"' + $outputPath + '"'),
    '--qa-fixture', ('"' + (Join-Path $taskRoot 'mod/test/completion-regression.json') + '"'),
    '--qa-purchase-fixture', ('"' + (Join-Path $taskRoot 'mod/test/completion-purchases.json') + '"'),
    '--qa-livestock-fixture', ('"' + (Join-Path $taskRoot 'mod/test/completion-livestock.json') + '"'),
    '--qa-completion-case', $Case
)
if ($Case -eq 'Load') { $arguments += @('--qa-completion-from', ('"' + $LoadPath + '"')) }
$process = Start-Process -FilePath $playerPath -ArgumentList $arguments -PassThru -WindowStyle Hidden
. (Join-Path $PSScriptRoot 'process_record.ps1')
$record = [pscustomobject]@{
    Task = 'Completion-' + $Case
    ProcessId = $process.Id
    Exe = $playerPath
    Arguments = $arguments
    StartedUtc = [DateTime]::UtcNow.ToString('o')
}
Write-TaskProcessRecord -Root $taskRoot -Name ('completion-' + $Case.ToLower()) -Record $record
Write-Output ('Completion ' + $Case + ' PID ' + $process.Id + ' output ' + $outputPath)
$process.WaitForExit()
Write-Output ('Player exit code ' + $process.ExitCode)
Get-Content -LiteralPath $logPath -Tail 12
exit $process.ExitCode

param(
    [ValidateSet('Probe','Foundation','Vertical','Progression','Load','Diagnostic','Visual','Stage23','Stage45')] [string]$Task = 'Foundation',
    [string]$LoadFrom = 'Vertical',
    [string]$BuildPath = '',
    [switch]$Visible
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$exe = if($BuildPath){[System.IO.Path]::GetFullPath($BuildPath)}else{Join-Path $taskRoot 'Builds\Windows\TYCOON2.exe'}
if (!(Test-Path -LiteralPath $exe)) { throw 'Windows build missing.' }
$mode = @{ Probe='--qa-gpu-probe'; Foundation='--qa-m0'; Vertical='--qa-vertical'; Progression='--qa-progression'; Load='--qa-load'; Diagnostic='--qa-resume'; Visual='--qa-art'; Stage23='--qa-stage23'; Stage45='--qa-stage45' }[$Task]
$outputRoot = if($Task -eq 'Stage23'){Join-Path (Join-Path $taskRoot 'work\stage02-03') ('player-stage23-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))}elseif($Task -eq 'Stage45'){Join-Path (Join-Path $taskRoot 'work\stage04-05') ('player-stage45-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))}else{Join-Path $taskRoot ('QA\Evidence\' + $Task.ToLower())}
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
if ($Task -notin @('Probe','Load')) {
    $qaSavePath = [System.IO.Path]::GetFullPath((Join-Path $outputRoot 'qa-save.json'))
    $verifiedQaRoot = [System.IO.Path]::GetFullPath($outputRoot) + [System.IO.Path]::DirectorySeparatorChar
    if (!$qaSavePath.StartsWith($verifiedQaRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'QA save path escaped its output directory.'
    }
    if (Test-Path -LiteralPath $qaSavePath) {
        $ownerProcesses = Get-CimInstance Win32_Process | Where-Object {
            $_.Name -eq 'TYCOON2.exe' -and $_.CommandLine -and $_.CommandLine.Contains($outputRoot)
        }
        if ($ownerProcesses) { throw 'A QA player still owns this save.' }
        $existingQaSave = Get-Content -LiteralPath $qaSavePath -Raw | ConvertFrom-Json
        if ($existingQaSave.version -ne 1 -or !$existingQaSave.savedAt) { throw 'Unrecognized QA save; preserved.' }
        Remove-Item -LiteralPath $qaSavePath -Force
    }
}
$logPath = Join-Path $taskRoot ('work\logs\player-' + $Task.ToLower() + '.log')
$arguments = @('-force-d3d11','-force-device-index','0','-screen-width','1920','-screen-height','1080','-screen-fullscreen','0',
    '-logFile',('"' + $logPath + '"'),$mode,'--require-4060','--qa-output',('"' + $outputRoot + '"'))
if ($Task -eq 'Load') { $arguments += @('--qa-load-from', ('"' + (Join-Path $taskRoot ('QA\Evidence\' + $LoadFrom.ToLower() + '\qa-save.json')) + '"'), '--qa-mode', 'load') }
if ($Task -eq 'Diagnostic') { $arguments += @('--qa-resume-from', ('"' + (Join-Path $taskRoot ('QA\Evidence\' + $LoadFrom.ToLower() + '\qa-save.json')) + '"')) }
$windowStyle=if($Visible){'Normal'}else{'Hidden'}
$process = Start-Process -FilePath $exe -ArgumentList $arguments -PassThru -WindowStyle $windowStyle
$record = [pscustomobject]@{Task=$Task;ProcessId=$process.Id;Exe=$exe;Arguments=$arguments;StartedUtc=[DateTime]::UtcNow.ToString('o')}
$record | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $taskRoot ('work\player-' + $Task.ToLower() + '-process.json')) -Encoding utf8
$record | ConvertTo-Json -Compress -Depth 4 | Add-Content -LiteralPath (Join-Path $taskRoot 'work\process-history.jsonl') -Encoding utf8
Write-Output ('Player ' + $Task + ' PID ' + $process.Id)
$process.WaitForExit()
Write-Output ('Player exit code ' + $process.ExitCode)
if (Test-Path -LiteralPath $logPath) { Get-Content -LiteralPath $logPath -Tail 20 }
exit $process.ExitCode

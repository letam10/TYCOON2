param(
    [ValidateSet('Probe','Foundation','Vertical','Progression','Load','Diagnostic','Visual','Assets','Stage23','Stage45','Stage67','Stage14','Stage14Layout','Stage14Visual','Stage14Milestones','Town','TownLayout','TownLoad')] [string]$Task = 'Foundation',
    [string]$LoadFrom = 'Vertical',
    [string]$BuildPath = '',
    [string]$LoadPath = '',
    [switch]$FixtureOnly,
    [switch]$Visible
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$exe = if($BuildPath){[System.IO.Path]::GetFullPath($BuildPath)}else{Join-Path $taskRoot 'Builds\Windows\TYCOON2.exe'}
if (!(Test-Path -LiteralPath $exe)) { throw 'Windows build missing.' }
if($LoadPath){
    $LoadPath=[System.IO.Path]::GetFullPath($LoadPath)
    if(!(Test-Path -LiteralPath $LoadPath -PathType Leaf)){throw 'Load save missing; refusing to silently start a new game.'}
}
$mode = @{ Probe='--qa-gpu-probe'; Foundation='--qa-m0'; Vertical='--qa-vertical'; Progression='--qa-progression'; Load='--qa-load'; Diagnostic='--qa-resume'; Visual='--qa-art'; Assets='--qa-assets'; Stage23='--qa-stage23'; Stage45='--qa-stage45'; Stage67='--qa-stage67'; Stage14='--qa-stage14'; Stage14Layout='--qa-stage14-layout'; Stage14Visual='--qa-stage14-visual'; Stage14Milestones='--qa-stage14-milestones' }[$Task]
$outputRoot = if($Task -eq 'Stage23'){Join-Path (Join-Path $taskRoot 'work\stage02-03') ('player-stage23-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))}elseif($Task -eq 'Stage45'){Join-Path (Join-Path $taskRoot 'work\stage04-05') ('player-stage45-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))}elseif($Task -eq 'Stage67'){Join-Path (Join-Path $taskRoot 'work\stage06-07') ('player-stage67-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))}else{Join-Path $taskRoot ('QA\Evidence\' + $Task.ToLower())}
if($Task.StartsWith('Stage14')){
    $outputRoot=Join-Path $taskRoot ('work\stage14\player-'+$Task.ToLower()+'-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
}
if($Task -eq 'Assets'){$outputRoot=Join-Path $taskRoot ('work\art-refresh\player-assets-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))}
if($Task.StartsWith('Town')){$outputRoot=Join-Path $taskRoot ('work\town-redesign\player-'+$Task.ToLower()+'-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'));$mode=@{Town='--qa-town';TownLayout='--qa-town-layout';TownLoad='--qa-town-load'}[$Task]}
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
if($Task -in @('Stage14','Stage14Layout','Stage14Milestones')){$arguments=@('-batchmode','-nographics','-logFile',('"'+$logPath+'"'),$mode,'--qa-output',('"'+$outputRoot+'"'))}
if($Task -eq 'Stage14'){$arguments+=@('--qa-fixture',('"'+(Join-Path $taskRoot 'mod\test\stage14-balance.json')+'"'))}
if($Task -eq 'Stage14Milestones'){$arguments+=@('--qa-fixture',('"'+(Join-Path $taskRoot 'mod\test\stage14-player-near-thresholds.json')+'"'))}
if($Task -eq 'Assets'){$arguments+=@('--qa-fixture',('"'+(Join-Path $taskRoot 'mod\test\asset-preview.json')+'"'))}
if($Task -in @('Town','TownLayout')){$arguments+=@('--qa-fixture',('"'+(Join-Path $taskRoot 'mod\test\town-redesign-near-thresholds.json')+'"'))}
if($Task -eq 'Town' -and $FixtureOnly){$arguments+='--qa-town-fixture-only'}
if($LoadPath){if($Task -eq 'TownLoad'){$arguments+=@('--qa-town-load-from',('"'+$LoadPath+'"'))}elseif($Task -eq 'Assets'){$arguments+=@('--qa-preview-fixture',('"'+$LoadPath+'"'))}else{$arguments+=@('--qa-load','--qa-load-from',('"'+$LoadPath+'"'));if($Task -eq 'Stage14'){$arguments+='--qa-stage14-resume'}}}
if ($Task -eq 'Load') { $arguments += @('--qa-load-from', ('"' + (Join-Path $taskRoot ('QA\Evidence\' + $LoadFrom.ToLower() + '\qa-save.json')) + '"'), '--qa-mode', 'load') }
if ($Task -eq 'Diagnostic') { $arguments += @('--qa-resume-from', ('"' + (Join-Path $taskRoot ('QA\Evidence\' + $LoadFrom.ToLower() + '\qa-save.json')) + '"')) }
$windowStyle=if($Visible){'Normal'}else{'Hidden'}
$process = Start-Process -FilePath $exe -ArgumentList $arguments -PassThru -WindowStyle $windowStyle
$record = [pscustomobject]@{Task=$Task;ProcessId=$process.Id;Exe=$exe;Arguments=$arguments;StartedUtc=[DateTime]::UtcNow.ToString('o')}
. (Join-Path $PSScriptRoot 'process_record.ps1')
Write-TaskProcessRecord -Root $taskRoot -Record $record -Name ('player-'+$Task.ToLower())
Write-Output ('Player ' + $Task + ' PID ' + $process.Id)
$process.WaitForExit()
Write-Output ('Player exit code ' + $process.ExitCode)
if (Test-Path -LiteralPath $logPath) { Get-Content -LiteralPath $logPath -Tail 20 }
exit $process.ExitCode

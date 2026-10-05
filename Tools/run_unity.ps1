param(
    [ValidateSet('Import','Tests','Build','Scene','Polish','Stage14Tests','Stage14Layout','Stage14Play','Baseline','Stage23','Stage45','Stage67','Stage08','Stage09','Stage10','Stage11','Stage12','Stage13')] [string]$Task = 'Import',
    [string]$BuildPath = ''
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$unityExe = 'C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe'
$logDirectory = Join-Path $taskRoot 'work\logs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$baseline = Get-CimInstance Win32_Process | Select-Object ProcessId,ParentProcessId,Name,ExecutablePath
$baselinePath = Join-Path $taskRoot 'work\process-baseline.json'
if (!(Test-Path -LiteralPath $baselinePath)) {
    $baseline | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $baselinePath -Encoding utf8
}
$logPath = Join-Path $logDirectory ($Task.ToLower() + '.log')
$arguments = @('-batchmode','-nographics','-projectPath',('"' + $taskRoot + '"'),'-logFile',('"' + $logPath + '"'))
if ($Task -in @('Tests','Stage08','Stage09','Stage10','Stage11','Stage12','Stage13','Stage14Tests')) {
    $stage=if($Task -match '^Stage(\d+)'){'stage'+$Matches[1]}else{$null}
    $results=if($stage){Join-Path $taskRoot ('work\'+$stage+'\editmode-results.xml')}else{Join-Path $taskRoot 'QA\editmode-results.xml'}
    $arguments += @('-runTests','-testPlatform','EditMode','-testResults',('"' + $results + '"'))
    if($Task -eq 'Stage08'){$arguments += @('-testFilter','Tycoon.Tests.SaveV2Tests')}
    if($Task -in @('Stage09','Stage10')){$arguments += @('-testFilter','Tycoon.Tests.StageProgressionTests')}
    if($Task -in @('Stage11','Stage12','Stage13')){$arguments += @('-testFilter',('Tycoon.Tests.'+$Task+'Tests'))}
    if($Task -eq 'Stage14Tests'){$arguments += @('-testFilter','Tycoon.Tests.Stage14Tests')}
} else {
    if($Task -notin @('Baseline','Stage23','Stage45','Stage67','Stage14Layout','Stage14Play')) { $arguments += '-quit' }
    if($Task -eq 'Polish'){$arguments += @('-executeMethod','Tycoon.Editor.Stage14Tools.PrepareAnimations')}
    if($Task -in @('Stage14Layout','Stage14Play')){
        $qaOutput=Join-Path $taskRoot ('work\stage14\editor-'+$Task.ToLower()+'-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
        $qaMode=if($Task -eq 'Stage14Layout'){'--qa-stage14-layout'}else{'--qa-stage14'}
        $arguments += @('-executeMethod','Tycoon.Editor.ProjectBuilder.PlayBaseline','--qa',$qaMode,'--qa-output',('"'+$qaOutput+'"'))
    }
    if($Task -eq 'Baseline') {
        $qaOutput=Join-Path $taskRoot 'work\stage01\editor-play'
        $arguments += @('-executeMethod','Tycoon.Editor.ProjectBuilder.PlayBaseline','--qa','--qa-baseline','--qa-output',('"' + $qaOutput + '"'))
    }
    if($Task -eq 'Stage23') {
        $qaOutput=Join-Path $taskRoot 'work\stage02-03\editor-stage23'
        $arguments += @('-executeMethod','Tycoon.Editor.ProjectBuilder.PlayBaseline','--qa','--qa-stage23','--qa-output',('"'+$qaOutput+'"'))
    }
    if($Task -eq 'Stage45') {
        $qaRoot=Join-Path $taskRoot 'work\stage04-05'
        $qaOutput=Join-Path $qaRoot ('editor-stage45-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
        $arguments += @('-executeMethod','Tycoon.Editor.ProjectBuilder.PlayBaseline','--qa','--qa-stage45','--qa-output',('"'+$qaOutput+'"'))
    }
    if($Task -eq 'Stage67') {
        $qaRoot=Join-Path $taskRoot 'work\stage06-07'
        $qaOutput=Join-Path $qaRoot ('editor-stage67-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
        $arguments += @('-executeMethod','Tycoon.Editor.ProjectBuilder.PlayBaseline','--qa','--qa-stage67','--qa-output',('"'+$qaOutput+'"'))
    }
    if ($Task -eq 'Build') {
        $arguments += @('-executeMethod','Tycoon.Editor.ProjectBuilder.BuildWindows')
        if($BuildPath)
        {
            $fullBuildPath=[System.IO.Path]::GetFullPath((Join-Path $taskRoot $BuildPath))
            $workRoot=[System.IO.Path]::GetFullPath((Join-Path $taskRoot 'work'))+[System.IO.Path]::DirectorySeparatorChar
            if(!$fullBuildPath.StartsWith($workRoot,[System.StringComparison]::OrdinalIgnoreCase)){throw 'Custom build path must stay under work/.'}
            New-Item -ItemType Directory -Path (Split-Path -Parent $fullBuildPath) -Force | Out-Null
            $arguments += @('--build-output',('"'+$fullBuildPath+'"'))
        }
    }
    if ($Task -eq 'Scene') { $arguments += @('-executeMethod','Tycoon.Editor.ProjectBuilder.CreateScene') }
}
$process = Start-Process -FilePath $unityExe -ArgumentList $arguments -PassThru -WindowStyle Hidden
[pscustomobject]@{Task=$Task;ProcessId=$process.Id;Exe=$unityExe;Arguments=$arguments;StartedUtc=[DateTime]::UtcNow.ToString('o')} |
    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $taskRoot ('work\unity-' + $Task.ToLower() + '-process.json')) -Encoding utf8
[pscustomobject]@{Task=$Task;ProcessId=$process.Id;Exe=$unityExe;StartedUtc=[DateTime]::UtcNow.ToString('o')} |
    ConvertTo-Json -Compress | Add-Content -LiteralPath (Join-Path $taskRoot 'work\process-history.jsonl') -Encoding utf8
Write-Output ('Unity ' + $Task + ' PID ' + $process.Id)
$process.WaitForExit()
Write-Output ('Unity exit code ' + $process.ExitCode)
if (Test-Path -LiteralPath $logPath) {
    Get-Content -LiteralPath $logPath -Tail 25
}
exit $process.ExitCode

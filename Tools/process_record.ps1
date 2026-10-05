function Write-TaskProcessRecord {
    param([string]$Root,[object]$Record,[string]$Name)
    $Record | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Root ('work\'+$Name+'-process.json')) -Encoding utf8
    $recordMutex = [System.Threading.Mutex]::new($false,'Local\TYCOON2_ProcessHistory')
    $recordLocked=$false
    try {
        $recordLocked=$recordMutex.WaitOne(10000)
        if(!$recordLocked){throw 'Process record history is busy; individual PID record preserved.'}
        $Record | ConvertTo-Json -Compress -Depth 5 | Add-Content -LiteralPath (Join-Path $Root 'work\process-history.jsonl') -Encoding utf8
    } finally { if($recordLocked){$recordMutex.ReleaseMutex()};$recordMutex.Dispose() }
}

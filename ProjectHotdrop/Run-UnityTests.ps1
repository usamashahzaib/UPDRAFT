param(
    [ValidateSet('EditMode', 'PlayMode')][string]$Platform = 'EditMode',
    [string]$Filter = '',
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$project = $PSScriptRoot
# Pre-flight (2026-09-28 recovery): the two historical startup failures were a
# zombie Unity.Licensing.Client and a stale Temp/UnityLockfile (PM IPC stream
# connect timing out after 30s). Guard instead of guessing.
$editors = Get-Process | Where-Object {
    try { $_.Path -eq $UnityPath } catch { $false }
}
if ($editors) { throw "A Unity editor process is running (ids: $($editors.Id -join ', ')). One owner at a time; stop it first (Stop-Process -Id <id> -Force)." }
$licenseClients = Get-Process | Where-Object { $_.Name -like 'Unity.Licensing.Client*' }
if ($licenseClients) { Write-Warning "Stale Unity.Licensing.Client (ids: $($licenseClients.Id -join ', ')) - stop it (Stop-Process -Id <id> -Force) if licensing handshakes fail; Git Bash taskkill mangles /PID switches." }
if (Test-Path (Join-Path $project 'Temp\UnityLockfile')) { Write-Warning "Stale Temp/UnityLockfile - delete the Temp folder before running if the project lock is not held." }
$results = Join-Path $project "TestResults\$Platform.xml"
$log = Join-Path $project "TestResults\$Platform.log"
New-Item -ItemType Directory -Force -Path (Split-Path $results) | Out-Null
Remove-Item -LiteralPath $results -ErrorAction SilentlyContinue
# -runTests exits by itself; adding -quit makes Unity skip the test run.
# WaitForExit on the Unity process only: Start-Process -Wait also waits for
# Unity's long-lived licensing child process and never returns.
# No short timeout: a cold compile plus PlayMode run takes several minutes, and
# abandoning Unity mid-run leaves it holding the project lock.
$argsLine = "-batchmode -projectPath `"$project`" -runTests -testPlatform $Platform -testResults `"$results`" -logFile `"$log`""
if ($Filter) { $argsLine += " -testFilter $Filter" }
$p = Start-Process -FilePath $UnityPath -ArgumentList $argsLine -WindowStyle Hidden -PassThru
$p.WaitForExit()
if (-not (Test-Path $results)) {
    Select-String -Path $log -Pattern 'error CS|Exception|Aborting' | Select-Object -First 20 | ForEach-Object { $_.Line }
    throw "No test results (exit $($p.ExitCode)). See $log"
}
[xml]$x = Get-Content $results
$run = $x.'test-run'
"$Platform total=$($run.total) passed=$($run.passed) failed=$($run.failed) duration=$($run.duration)s"
$x.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
    "FAILED: $($_.fullname)"
    "  " + ($_.failure.message.'#cdata-section' -replace "`n", "`n  ")
}
# Exit non-zero on red tests so CI (and any wrapper script) can trust the exit code.
if ([int]$run.failed -gt 0) {
    Write-Host "$Platform has $($run.failed) failed test(s)."
    exit 1
}

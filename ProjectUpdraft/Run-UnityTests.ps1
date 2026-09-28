param(
    [ValidateSet('EditMode', 'PlayMode')][string]$Platform = 'EditMode',
    [string]$Filter = '',
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$project = $PSScriptRoot
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

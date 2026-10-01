$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe"
$project = "E:\Usama\Game\ProjectUpdraft"
$log = "E:\Usama\Game\ProjectUpdraft\TestResults\EditModeDirect.log"

"Running EditMode tests directly"
& $unity -batchmode -nographics -projectPath $project `
  -runTests -testPlatform EditMode `
  -logFile $log `
  -quit

$code = $LASTEXITCODE
if ($code -ne 0) {
    "Unity exited with code $code" | Write-Host -ForegroundColor Red
    Get-Content $log -Tail 40
    exit $code
}

if (Test-Path $log) {
    Get-Content $log -Tail 30
}
Write-Host "EditMode direct run exit code: $code"

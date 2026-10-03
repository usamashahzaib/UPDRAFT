$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe"
$project = "E:\Usama\Game\ProjectHotdrop"

"Launching Unity to regenerate Library/ artifacts..."
& $unity -batchmode -nographics -projectPath $project -quit

$code = $LASTEXITCODE
if ($code -eq 0) {
    Write-Host "Unity exited cleanly (exit $code). Library should be regenerated."
} else {
    Write-Host "Unity exited with code $code" -ForegroundColor Red
}

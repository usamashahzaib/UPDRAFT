$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe"
$project = "E:\Usama\Game\ProjectUpdraft"
$log = "E:\Usama\Game\ProjectUpdraft\TestResults\LinuxServerBuild.log"
$out = "E:\Usama\Game\ProjectUpdraft\Builds\Linux\UpdraftServer.x86_64"

# Pre-flight, same guards as Run-UnityTests.ps1: one Unity owner at a time,
# and a stale Temp/UnityLockfile or Library/Bee cache can serve an outdated
# compile diagnostic instead of actually recompiling.
$editors = Get-Process | Where-Object { $_.Name -eq 'Unity' }
if ($editors) { throw "A Unity editor process is running (ids: $($editors.Id -join ', ')). One owner at a time; stop it first (Stop-Process -Id <id> -Force)." }
if (Test-Path (Join-Path $project 'Temp\UnityLockfile')) { Write-Warning "Stale Temp/UnityLockfile - delete the Temp folder before running if the project lock is not held." }
if ($env:UPDRAFT_CLEAR_BEE_CACHE -eq '1') {
    $beeArtifacts = Join-Path $project 'Library\Bee\artifacts'
    if (Test-Path $beeArtifacts) {
        "Clearing Bee artifact cache: $beeArtifacts"
        Remove-Item -Recurse -Force $beeArtifacts
    }
}

"Building Linux dedicated server from $project"
& $unity -batchmode -nographics -projectPath $project `
  -executeMethod Updraft.Editor.BuildLinuxServer.Run `
  -logFile $log `
  -quit

$code = $LASTEXITCODE
if ($code -ne 0) {
    "Unity exited with code $code" | Write-Host -ForegroundColor Red
    Get-Content $log -Tail 40
    exit $code
}

if (Test-Path $out) {
    "BUILD SUCCESS: $out" | Write-Host
    Get-Item $out | Select-Object FullName, Length, LastWriteTime
} else {
    "BUILD: no binary at $out" | Write-Host -ForegroundColor Yellow
    exit 1
}

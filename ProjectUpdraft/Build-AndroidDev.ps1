param([string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$projectPath = $PSScriptRoot
if (-not (Test-Path -LiteralPath $UnityPath)) { throw "Unity editor not found at $UnityPath" }
# This nonexistent socket directory makes OpenJDK fall back to TCP pipes on
# Windows hosts where AF_UNIX connects fail. It is not a real temp directory.
$priorJavaOptions = $env:JAVA_TOOL_OPTIONS
try {
    $socketPath = Join-Path $projectPath 'Builds\JavaSocketFallback'
    $env:JAVA_TOOL_OPTIONS = "$priorJavaOptions -Djdk.net.unixdomain.tmpdir=$socketPath".Trim()
    $logPath = Join-Path $projectPath 'apk_repair.log'
    $buildStarted = Get-Date
    $process = Start-Process -FilePath $UnityPath -ArgumentList "-batchmode -nographics -projectPath `"$projectPath`" -executeMethod Updraft.Editor.BuildAndroidApk.Run -quit -logFile `"$logPath`"" -WindowStyle Hidden -PassThru -Wait
    $apk = Join-Path $projectPath 'Builds\Android\UpdraftDev.apk'
    if ($process.ExitCode -ne 0) {
        $buildLog = Get-Content -LiteralPath $logPath -Raw
        $gradleProject = Join-Path $projectPath 'Library\Bee\Android\Prj\IL2CPP\Gradle'
        $gradleSettings = Join-Path $gradleProject 'settings.gradle'
        # Only recover this identified packaging failure after fresh native
        # output. Compilation/SDK/license failures must still stop the build.
        if ($buildLog -notmatch 'Unable to establish loopback connection' -or
            -not (Test-Path -LiteralPath $gradleSettings) -or
            (Get-Item -LiteralPath $gradleSettings).LastWriteTime -lt $buildStarted) {
            throw "APK build failed (exit $($process.ExitCode)). See $logPath"
        }
        Write-Host 'Unity native build succeeded; recovering the identified Java loopback packaging failure.'
        $editorRoot = Split-Path -Parent $UnityPath
        $java = Join-Path $editorRoot 'Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\java.exe'
        $gradleJar = Join-Path $editorRoot 'Data\PlaybackEngines\AndroidPlayer\Tools\gradle\lib\gradle-launcher-9.3.1.jar'
        Push-Location $gradleProject
        try {
            & $java '-Djdk.net.unixdomain.tmpdir=E:\Usama\Game\ProjectUpdraft\Builds\JavaSocketFallback' -classpath $gradleJar org.gradle.launcher.GradleMain assembleDebug --no-daemon
            if ($LASTEXITCODE -ne 0) { throw "Direct Gradle packaging failed (exit $LASTEXITCODE)." }
            $packaged = Join-Path $gradleProject 'launcher\build\outputs\apk\debug\launcher-debug.apk'
            if (-not (Test-Path -LiteralPath $packaged)) { throw 'Gradle did not produce an APK.' }
            New-Item -ItemType Directory -Path (Split-Path -Parent $apk) -Force | Out-Null
            Copy-Item -LiteralPath $packaged -Destination $apk
        } finally { Pop-Location }
    }
    if (-not (Test-Path -LiteralPath $apk)) { throw "Unity returned without producing $apk" }
    Get-Item -LiteralPath $apk | Select-Object FullName, Length, LastWriteTime
} finally { $env:JAVA_TOOL_OPTIONS = $priorJavaOptions }

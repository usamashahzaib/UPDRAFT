param(
    [string]$FusionPackage = 'E:\Usama\Archives\Software\Game\Photon-Fusion-2.1.3-Stable-2390.unitypackage',
    [string]$KccSample = 'E:\Usama\Archives\Software\Game\fusion-simple-kcc-sample-2.1.1'
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$photonRoot = Join-Path $projectRoot 'Assets\Photon'
$kccSource = Join-Path $KccSample 'Assets\Photon\FusionAddons\SimpleKCC'
if (-not (Test-Path -LiteralPath (Join-Path $kccSource 'Fusion.Addons.SimpleKCC.dll'))) { throw 'Simple KCC addon DLL not found in supplied sample.' }
# Mechanical dependency import only: retain original vendor bytes and GUIDs.
# This fallback avoids the editor's failed package decompressor on this host.
$entries = & tar.exe -tf $FusionPackage
if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect Fusion archive.' }
foreach ($entry in $entries) {
    if ($entry -notmatch '^(\./)?([a-fA-F0-9]{32}(/(asset(\.meta)?|pathname|preview\.png))?/?|)$') {
        throw "Unexpected archive entry; refusing extraction: $entry"
    }
}
$stageRoot = Join-Path $projectRoot ('Temp\FusionImport-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stageRoot -Force | Out-Null
& tar.exe -xf $FusionPackage -C $stageRoot
if ($LASTEXITCODE -ne 0) { throw 'Fusion archive extraction failed.' }
function Copy-VendorFile([string]$source, [string]$destination) {
    if (Test-Path -LiteralPath $destination) {
        if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $destination).Hash) {
            throw "Preserving an existing differing file: $destination"
        }
        return
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination
}
$count = 0
foreach ($folder in Get-ChildItem -LiteralPath $stageRoot -Directory) {
    $pathname = Join-Path $folder.FullName 'pathname'
    if (-not (Test-Path -LiteralPath $pathname)) { throw "Missing asset pathname in $($folder.Name)" }
    $relative = (Get-Content -LiteralPath $pathname -Raw).Trim()
    $destination = [IO.Path]::GetFullPath((Join-Path $projectRoot $relative))
    if ($destination -ne $photonRoot -and $destination -ne ($photonRoot + '.meta') -and
        -not $destination.StartsWith($photonRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unexpected asset target outside Assets/Photon: $relative"
    }
    $asset = Join-Path $folder.FullName 'asset'
    if (Test-Path -LiteralPath $asset -PathType Leaf) { Copy-VendorFile $asset $destination }
    else { New-Item -ItemType Directory -Path $destination -Force | Out-Null }
    $meta = Join-Path $folder.FullName 'asset.meta'
    if (Test-Path -LiteralPath $meta) { Copy-VendorFile $meta ($destination + '.meta') }
    $count++
}
$kccDestination = Join-Path $photonRoot 'FusionAddons\SimpleKCC'
foreach ($file in Get-ChildItem -LiteralPath $kccSource -File) { Copy-VendorFile $file.FullName (Join-Path $kccDestination $file.Name) }
foreach ($folderName in @('FusionAddons', 'FusionAddons\SimpleKCC')) {
    $meta = Join-Path $KccSample ('Assets\Photon\' + $folderName + '.meta')
    if (Test-Path -LiteralPath $meta) { Copy-VendorFile $meta (Join-Path $photonRoot ($folderName + '.meta')) }
}
Write-Host "Imported $count Fusion asset records and the Simple KCC addon, with vendor GUIDs preserved. Unity refresh/compilation still required."

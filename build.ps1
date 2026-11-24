# GoldLend 构建脚本。用法见 README.md。

param(
    [string]$Configuration = "Debug",
    [string]$Sts2GameDir = $env:STS2_GAME_DIR,
    [string]$RitsuLibRoot = $env:RITSULIB_ROOT,
    [string]$RitsuLibReferenceTarget = "0.113.0",
    [string]$Dotnet = "",
    [switch]$Package
)

$ErrorActionPreference = "Stop"
if (-not $Sts2GameDir) { throw "请用 -Sts2GameDir 或环境变量 STS2_GAME_DIR 指定游戏目录。" }
if (-not $RitsuLibRoot) { throw "请用 -RitsuLibRoot 或环境变量 RITSULIB_ROOT 指定 RitsuLib 目录。" }

$proj = Join-Path $PSScriptRoot "GoldLend.csproj"
$manifestPath = Join-Path $PSScriptRoot "GoldLend.json"
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$releasePath = Join-Path $Sts2GameDir "release_info.json"
if (-not (Test-Path -LiteralPath $releasePath)) {
    throw "找不到游戏 release_info.json，无法确认版本：$releasePath"
}
$release = Get-Content -LiteralPath $releasePath -Raw | ConvertFrom-Json
$gameVersion = [version]($release.version -replace '^v', '')
$minimumGameVersion = [version]$manifest.min_game_version
$apiVersion = [version]$RitsuLibReferenceTarget
if ($gameVersion -lt $minimumGameVersion) {
    throw "本工程最低支持游戏 $minimumGameVersion，实际游戏为 $gameVersion。"
}
if ($gameVersion.Major -ne $apiVersion.Major -or $gameVersion.Minor -ne $apiVersion.Minor) {
    throw "游戏 $gameVersion 与 RitsuLib API 系列 $RitsuLibReferenceTarget 不匹配。请核对兼容分支。"
}
$ritsuManifest = Join-Path $RitsuLibRoot "mod_manifest.json"
if (-not (Test-Path -LiteralPath $ritsuManifest)) {
    throw "找不到 RitsuLib 清单：$ritsuManifest"
}
$ritsu = Get-Content -LiteralPath $ritsuManifest -Raw | ConvertFrom-Json
$minimumRitsuVersion = [version]($manifest.dependencies | Where-Object id -eq 'STS2-RitsuLib').min_version
if ([version]$ritsu.version -lt $minimumRitsuVersion) {
    throw "需要 RitsuLib $minimumRitsuVersion 或更高版本，实际为 $($ritsu.version)。"
}

# 仅有运行时的 dotnet 无法构建项目。
function Test-SdkDotnet([string]$exe) {
    if (-not $exe) { return $false }
    if (-not (Test-Path $exe)) { return $false }
    $sdks = & $exe --list-sdks 2>$null | Where-Object { $_ -match "^\d+\.\d+\.\d+" }
    return [bool]$sdks
}

$candidates = @()
if ($Dotnet) { $candidates += $Dotnet }
$onPath = Get-Command dotnet -ErrorAction SilentlyContinue
if ($onPath) { $candidates += $onPath.Source }
$candidates += (Join-Path (Split-Path $PSScriptRoot -Parent) "_tools\dotnet\dotnet.exe")

$resolved = ""
foreach ($c in $candidates) {
    if (Test-SdkDotnet $c) { $resolved = $c; break }
}
if (-not $resolved) {
    throw "找不到带 SDK 的 dotnet。请安装 .NET 9 SDK，或用 -Dotnet 指定可执行文件。"
}
$Dotnet = $resolved

$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:DOTNET_NOLOGO = "1"
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = "true"
$localTools = Join-Path (Split-Path $PSScriptRoot -Parent) "_tools"
if (Test-Path -LiteralPath (Join-Path $localTools 'nuget')) {
    $env:NUGET_PACKAGES = Join-Path $localTools 'nuget'
    $env:DOTNET_CLI_HOME = Join-Path $localTools 'clihome'
}

Write-Host "dotnet       : $Dotnet"
Write-Host "project      : $proj"
Write-Host "game dir     : $Sts2GameDir"
Write-Host "game version : $gameVersion"
Write-Host "RitsuLib     : $RitsuLibRoot (compat $RitsuLibReferenceTarget)"
Write-Host "configuration: $Configuration"

$buildArgs = @(
    $proj,
    "-c", $Configuration,
    "-p:Sts2GameDir=$Sts2GameDir",
    "-p:RitsuLibRoot=$RitsuLibRoot",
    "-p:RitsuLibReferenceTarget=$RitsuLibReferenceTarget"
)
& $Dotnet build @buildArgs

if ($LASTEXITCODE -ne 0) { throw "构建失败，退出码 $LASTEXITCODE" }

$out = Join-Path $PSScriptRoot ".godot\mono\temp\bin\$Configuration"
Write-Host ""
Write-Host "产物目录: $out"
Get-ChildItem $out -Filter "GoldLend.*" | Select-Object Name, Length | Format-Table -AutoSize
Write-Host "发布到 Mod 目录时只需要 GoldLend.dll（GoldLend.json 为清单，GoldLend.pdb 可选）。"

if ($Package) {
    $distRoot = Join-Path (Split-Path $PSScriptRoot -Parent) 'dist'
    $packageName = "GoldLend-$($manifest.version)"
    $packageDir = Join-Path $distRoot $packageName
    $packageFiles = @('GoldLend.dll', 'GoldLend.pdb', 'GoldLend.json')
    if (Test-Path -LiteralPath $packageDir) {
        $unexpected = @(Get-ChildItem -LiteralPath $packageDir -Force | Where-Object { $_.PSIsContainer -or $_.Name -notin $packageFiles })
        if ($unexpected.Count -gt 0) {
            throw "打包目录包含非本脚本管理的文件，停止覆盖：$packageDir"
        }
    }
    New-Item -ItemType Directory -Path $packageDir -Force | Out-Null
    foreach ($name in $packageFiles) {
        Copy-Item -LiteralPath (Join-Path $out $name) -Destination (Join-Path $packageDir $name) -Force
    }
    $zipPath = Join-Path $distRoot "$packageName.zip"
    $filesToZip = @($packageFiles | ForEach-Object { Join-Path $packageDir $_ })
    Compress-Archive -LiteralPath $filesToZip -DestinationPath $zipPath -Force
    Write-Host "发布包目录: $packageDir"
    Write-Host "发布包 ZIP : $zipPath"
}

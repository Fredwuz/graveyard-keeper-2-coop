<#
.SYNOPSIS
    Decompiles the game's code (Assembly-CSharp.dll) into artifacts\decomp\<game version>, to read
    how the game does something before the mod patches it.

.DESCRIPTION
    Installs ILSpy's command line (ilspycmd) into .tools the first time. Git ignores both folders.
    The decompiled code is the game developers' code: never commit it or share it. Run the script
    again after a game update; method and field names can change.

.EXAMPLE
    .\scripts\Decompile-Game.ps1
.EXAMPLE
    .\scripts\Decompile-Game.ps1 -Force
#>
[CmdletBinding()]
param(
    [string]$GamePath = (dotnet msbuild "$PSScriptRoot\..\src\GK2Coop\GK2Coop.csproj" -getProperty:GamePath),
    # Decompile again although the folder for this game version is there.
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$managed = @('GraveyardKeeper2_Data', 'GraveyardKeeper2Demo_Data') | ForEach-Object { Join-Path $GamePath "$_\Managed" } |
    Where-Object { Test-Path -LiteralPath (Join-Path $_ 'Assembly-CSharp.dll') } | Select-Object -First 1
if (-not $managed) { throw "No Assembly-CSharp.dll found in $GamePath" }
$dll = Join-Path $managed 'Assembly-CSharp.dll'

# The game's version (1.011 and so on) as Unity stores it in globalgamemanagers; else the DLL's date.
$version = (Get-Item -LiteralPath $dll).LastWriteTime.ToString('yyyy-MM-dd')
$settings = Join-Path (Split-Path $managed) 'globalgamemanagers'
if (Test-Path -LiteralPath $settings) {
    $match = [regex]::Match([Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($settings)), '\x00\x00\x00(\d+\.\d{2,3})(?![\w.])')
    if ($match.Success) { $version = $match.Groups[1].Value }
}
$output = Join-Path $projectRoot "artifacts\decomp\$version"
if ((Test-Path -LiteralPath $output) -and -not $Force) {
    Write-Host "Already decompiled: $output (-Force decompiles again)."
    return
}

$tools = Join-Path $projectRoot '.tools'
if (-not (Get-ChildItem -LiteralPath $tools -Filter 'ilspycmd.*' -ErrorAction SilentlyContinue)) {
    dotnet tool install ilspycmd --tool-path $tools
    if ($LASTEXITCODE -ne 0) { throw 'Could not install ilspycmd (needs the .NET SDK and an internet connection).' }
}
# The SDK makes ilspycmd.exe or ilspycmd.cmd, depending on its version.
$ilspy = (Get-ChildItem -LiteralPath $tools -Filter 'ilspycmd.*' | Select-Object -First 1).FullName

if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Recurse -Force }
New-Item -ItemType Directory -Force -Path $output | Out-Null
& $ilspy -p -o $output $dll
if ($LASTEXITCODE -ne 0) { throw 'Decompiling failed.' }
Write-Host "Decompiled game $version to $output ($((Get-ChildItem -LiteralPath $output -Recurse -Filter *.cs).Count) files)."

[CmdletBinding()]
param(
    [string]$HostPath = (dotnet msbuild "$PSScriptRoot\..\src\GK2Coop\GK2Coop.csproj" -getProperty:GamePath),
    [string]$ClientPath = "$(Split-Path $PSScriptRoot)\testbed\GK2Coop-FullClient"
)

# Clones the installed demo so two instances can run side by side on one machine.
#
# The game refuses to start twice from the same directory — the second process exits
# immediately — but two copies in different directories run happily together. That turns a
# two-machine test into a single-machine one for everything that does not need a real second
# GPU or a real network.
#
# The clone is a plain file copy: no Steam involvement, so it runs without Steam auth and logs
# a harmless SteamAPI_Init failure. It is roughly 1.4 GB.

$ErrorActionPreference = 'Stop'
# A relative path (testbed\GK2Coop-FullHost) counts from the current folder.
$ClientPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ClientPath)

$gameExeName = if (Test-Path -LiteralPath (Join-Path $HostPath 'GraveyardKeeper2.exe')) { 'GraveyardKeeper2.exe' } else { 'GraveyardKeeper2Demo.exe' }
if (-not (Test-Path -LiteralPath (Join-Path $HostPath $gameExeName))) {
    throw "Source install not found: $HostPath"
}
if (Get-Process -Name ([IO.Path]::GetFileNameWithoutExtension($gameExeName)) -ErrorAction SilentlyContinue) {
    throw 'Close every Graveyard Keeper 2 instance first.'
}
if (Test-Path -LiteralPath $ClientPath) { throw "Refusing to mirror into an existing folder: $ClientPath" }

$drive = Get-PSDrive -Name (Split-Path -Qualifier $ClientPath).TrimEnd(':') -ErrorAction SilentlyContinue
if ($drive -and $drive.Free -lt 3GB) {
    throw ("Less than 3 GB free on drive " + $drive.Name + ": the clone needs about 1.4 GB.")
}

Write-Host "Cloning $HostPath -> $ClientPath ..."
robocopy $HostPath $ClientPath /MIR /MT:16 /NFL /NDL /NJH /NP /R:1 /W:1 | Out-Null
if ($LASTEXITCODE -ge 8) {
    throw "robocopy failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path -LiteralPath (Join-Path $ClientPath $gameExeName))) {
    throw 'Clone did not produce a runnable install.'
}
# The copy starts as itself, not relaunched through Steam (which would start the real install).
Set-Content -LiteralPath (Join-Path $ClientPath 'steam_appid.txt') -Value '4358690' -NoNewline

Write-Host "Local co-op client ready at $ClientPath"
Write-Host 'Run scripts\Run-LocalCoopTest.ps1 to drive a two-instance session.'
# robocopy's 1 means "files copied"; callers must not read it as a failure.
exit 0

# Baut das Programm und daraus installer\output\TaskbarGruppen-Setup-<Version>.exe.
# Inno Setup kommt als NuGet-Paket und wird nicht im System installiert.
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

& .\build.ps1
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet restore installer\tools.csproj --nologo -v quiet
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$packages = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE '.nuget\packages' }
$compiler = Join-Path $packages 'tools.innosetup\6.7.3\tools\ISCC.exe'
& $compiler /Qp installer\TaskbarGruppen.iss
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Get-ChildItem installer\output\*.exe | ForEach-Object { "Fertig: $($_.FullName) ($([int]($_.Length / 1KB)) KB)" }

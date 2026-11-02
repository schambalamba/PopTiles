# Baut das Tool nach .\dist. Laufende Kacheln oder Fenster vorher schliessen.
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
dotnet build TaskbarGruppen.csproj -c Release -o dist --nologo -v minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Fertig: $PSScriptRoot\dist\PopTiles.exe"

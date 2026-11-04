param([string]$SonarUrl = "http://localhost:9000", [string]$Token = $env:SONAR_TOKEN)
$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($Token)) { throw "Defina SONAR_TOKEN con un token de SonarQube." }
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
dotnet tool restore
dotnet sonarscanner begin /k:"EmprendeMarket" /d:sonar.host.url="$SonarUrl" /d:sonar.token="$Token"
dotnet build
dotnet sonarscanner end /d:sonar.token="$Token"

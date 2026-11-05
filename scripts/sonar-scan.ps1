param([string]$SonarUrl = "http://localhost:9000", [string]$Token = $env:SONAR_TOKEN)
$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($Token)) { throw "Defina SONAR_TOKEN con un token de SonarQube." }
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
function Save-SonarMeasures([string]$Name) {
    $encodedToken = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${Token}:"))
    $headers = @{ Authorization = "Basic $encodedToken" }
    $uri = "$SonarUrl/api/measures/component?component=EmprendeMarket&metricKeys=bugs,vulnerabilities,code_smells,security_hotspots,coverage"
    Invoke-RestMethod -Uri $uri -Headers $headers -Method Get |
        ConvertTo-Json -Depth 10 |
        Set-Content (Join-Path $root "evidencias\sonar-$Name.json")
}
New-Item -ItemType Directory -Force (Join-Path $root "evidencias") | Out-Null
Save-SonarMeasures "before"
if (Test-Path (Join-Path $root ".config\dotnet-tools.json")) {
    dotnet tool restore
}
dotnet sonarscanner begin /k:"EmprendeMarket" /d:sonar.host.url="$SonarUrl" /d:sonar.token="$Token"
dotnet build
dotnet sonarscanner end /d:sonar.token="$Token"
Save-SonarMeasures "after"
Write-Host "Métricas guardadas en evidencias\sonar-before.json y evidencias\sonar-after.json"

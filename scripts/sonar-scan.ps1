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
# Guardar "antes" solo si no existe, para no sobrescribir la línea base
if (-not (Test-Path (Join-Path $root "evidencias\sonar-before.json"))) { Save-SonarMeasures "before" }
if (Test-Path (Join-Path $root ".config\dotnet-tools.json")) {
    dotnet tool restore
}
dotnet sonarscanner begin /k:"EmprendeMarket" /d:sonar.host.url="$SonarUrl" /d:sonar.token="$Token"
dotnet build --no-incremental
dotnet sonarscanner end /d:sonar.token="$Token"

# Esperar a que SonarQube termine de procesar el análisis
$task = Join-Path $root ".sonarqube\out\.sonar\report-task.txt"
$auth = @{ Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${Token}:")) }
if (Test-Path $task) {
    $ceUrl = (Get-Content $task | Where-Object { $_ -like "ceTaskUrl=*" }) -replace "^ceTaskUrl=", ""
    for ($i = 0; $i -lt 40; $i++) {
        $s = (Invoke-RestMethod -Uri $ceUrl -Headers $auth).task.status
        if ($s -in @("SUCCESS","FAILED","CANCELED")) { break }
        Start-Sleep -Seconds 3
    }
    Write-Host "Estado del procesamiento: $s"
} else {
    Start-Sleep -Seconds 45   # respaldo si no existe report-task.txt
}
Save-SonarMeasures "after"
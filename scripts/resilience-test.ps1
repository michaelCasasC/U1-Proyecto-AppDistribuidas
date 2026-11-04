param(
    [string]$Namespace = "emprendemarket",
    [string]$BaseUrl = "http://localhost:30080"
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$evidence = Join-Path $root "evidencias"
New-Item -ItemType Directory -Force $evidence | Out-Null
$log = Join-Path $evidence "resilience-$(Get-Date -Format yyyyMMdd-HHmmss).log"
function Check-Availability {
    param([int]$Seconds = 15)
    1..$Seconds | ForEach-Object {
        $time = Get-Date -Format o
        try { $health = Invoke-WebRequest "$BaseUrl/health/ready" -UseBasicParsing; "$time health=$($health.StatusCode)" | Tee-Object -FilePath $log -Append }
        catch { "$time health=ERROR $($_.Exception.Message)" | Tee-Object -FilePath $log -Append }
        Start-Sleep 1
    }
}
$pods = @(kubectl -n $Namespace get pods -l app=emprendemarket-web -o jsonpath='{.items[*].metadata.name}') -split ' '
if ($pods.Count -gt 0) { kubectl -n $Namespace delete pod $pods[0] | Tee-Object -FilePath $log -Append; Check-Availability }
$pods = @(kubectl -n $Namespace get pods -l app=emprendemarket-web -o jsonpath='{.items[*].metadata.name}') -split ' '
if ($pods.Count -ge 2) { kubectl -n $Namespace delete pod $pods[0],$pods[1] | Tee-Object -FilePath $log -Append; Check-Availability }
kubectl -n $Namespace get events --sort-by=.lastTimestamp | Tee-Object -FilePath $log -Append

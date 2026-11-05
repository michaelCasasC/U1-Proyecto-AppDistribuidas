param(
    [string]$Namespace = "emprendermarket",
    [string]$ServiceUrl = "http://emprendermarket-web",
    [int]$Seconds = 20
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$evidence = Join-Path $root "evidencias"
New-Item -ItemType Directory -Force $evidence | Out-Null
$log = Join-Path $evidence "resilience-$(Get-Date -Format yyyyMMdd-HHmmss).log"
$probe = "resilience-probe"
kubectl -n $Namespace delete pod $probe --ignore-not-found | Out-Null
kubectl -n $Namespace run $probe --image=curlimages/curl:8.10.1 --restart=Never `
    --command -- sh -c "i=0; while [ `$i -lt $Seconds ]; do date -Iseconds; curl -sS -o /dev/null -w ' status=%{http_code}\n' $ServiceUrl/health/live || echo ' status=ERROR'; i=`$((i+1)); sleep 1; done" | Out-Null
kubectl -n $Namespace wait --for=jsonpath='{.status.phase}'=Running pod/$probe --timeout=60s | Out-Null

$pods = @(kubectl -n $Namespace get pods -l app=emprendermarket-web -o jsonpath='{.items[*].metadata.name}') -split ' ' | Where-Object { $_ }
if ($pods.Count -lt 3) { throw "Se requieren al menos 3 pods web; encontrados: $($pods.Count)." }
"=== DELETE 1 POD: $($pods[0]) ===" | Tee-Object -FilePath $log
kubectl -n $Namespace delete pod $pods[0] | Tee-Object -FilePath $log -Append
Start-Sleep -Seconds $Seconds

$pods = @(kubectl -n $Namespace get pods -l app=emprendermarket-web -o jsonpath='{.items[*].metadata.name}') -split ' ' | Where-Object { $_ }
$secondBatch = $pods | Select-Object -First 2
"=== DELETE 2 PODS: $($secondBatch -join ', ') ===" | Tee-Object -FilePath $log -Append
kubectl -n $Namespace delete pod $secondBatch | Tee-Object -FilePath $log -Append
Start-Sleep -Seconds $Seconds

kubectl -n $Namespace logs $probe | Tee-Object -FilePath $log -Append
kubectl -n $Namespace get pods -l app=emprendemarket-web -o wide | Tee-Object -FilePath $log -Append
kubectl -n $Namespace get events --sort-by=.lastTimestamp | Tee-Object -FilePath $log -Append
kubectl -n $Namespace delete pod $probe --ignore-not-found | Out-Null

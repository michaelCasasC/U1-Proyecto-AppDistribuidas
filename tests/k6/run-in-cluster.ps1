param(
    [string]$Namespace = "emprendermarket",
    [int]$Vus = 10,
    [string]$Duration = "30s"
)

$ErrorActionPreference = "Stop"
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$evidence = Join-Path $root "evidencias"
$configMap = "k6-load-test"
$job = "k6-load-$([Guid]::NewGuid().ToString('N').Substring(0,8))"
$scriptPath = Join-Path $PSScriptRoot "load-test.js"
New-Item -ItemType Directory -Force $evidence | Out-Null

kubectl -n $Namespace create configmap $configMap `
    --from-file="load-test.js=$scriptPath" `
    --dry-run=client -o yaml | kubectl apply -f - | Out-Null

$manifest = @"
apiVersion: batch/v1
kind: Job
metadata:
  name: $job
  namespace: $Namespace
spec:
  backoffLimit: 0
  template:
    spec:
      restartPolicy: Never
      containers:
      - name: k6
        image: grafana/k6:latest
        env:
        - name: BASE_URL
          value: http://emprendermarket-web
        - name: VUS
          value: "$Vus"
        - name: DURATION
          value: "$Duration"
        command: ["k6"]
        args: ["run", "--summary-export", "/results/k6-summary.json", "/scripts/load-test.js"]
        volumeMounts:
        - name: script
          mountPath: /scripts
        - name: results
          mountPath: /results
      volumes:
      - name: script
        configMap:
          name: $configMap
      - name: results
        emptyDir: {}
"@
$manifest | kubectl apply -f - | Out-Null
kubectl -n $Namespace wait --for=condition=complete job/$job --timeout=900s
$pod = kubectl -n $Namespace get pods -l job-name=$job -o jsonpath="{.items[0].metadata.name}"
kubectl -n $Namespace logs $pod | Tee-Object (Join-Path $evidence "k6-$job.log")
kubectl -n $Namespace cp "$pod`:/results/k6-summary.json" (Join-Path $evidence "k6-$job-summary.json")
kubectl -n $Namespace delete job $job --ignore-not-found | Out-Null
kubectl -n $Namespace delete configmap $configMap --ignore-not-found | Out-Null

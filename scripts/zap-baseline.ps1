param([string]$BaseUrl = "http://localhost:30080")
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root "evidencias\zap"
New-Item -ItemType Directory -Force $out | Out-Null
docker run --rm -t --network host -v "${out}:/zap/wrk/:rw" ghcr.io/zaproxy/zaproxy:stable zap-baseline.py `
  -t $BaseUrl -r zap-report.html -J zap-report.json

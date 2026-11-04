param(
    [string]$Image = "michacc/emprendemarket-web:local",
    [string]$Namespace = "emprendemarket",
    [switch]$IncludeOllama,
    [switch]$IncludeHpa
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
function Require-Env([string]$Name, [string]$Default) {
    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) { return $Default }
    return $value
}
$dbPassword = Require-Env "EMPRENDEMARKET_POSTGRES_PASSWORD" "postgres"
$dbConnection = Require-Env "EMPRENDEMARKET_CONNECTION_STRING" "Host=postgres;Port=5432;Database=EmprendeMarketDB;Username=postgres;Password=$dbPassword"
$redisConnection = Require-Env "EMPRENDEMARKET_REDIS_CONNECTION" "redis:6379,abortConnect=false"

kubectl apply -f "$root\k8s\00-namespace.yaml"
kubectl -n $Namespace create secret generic emprendermarket-secrets `
    --from-literal=default-connection=$dbConnection `
    --from-literal=redis-connection=$redisConnection `
    --from-literal=postgres-password=$dbPassword `
    --dry-run=client -o yaml | kubectl apply -f -
kubectl apply -f "$root\k8s\01-configmap.yaml"
kubectl apply -f "$root\k8s\10-postgres.yaml"
kubectl apply -f "$root\k8s\11-redis.yaml"
if ($IncludeOllama) { kubectl apply -f "$root\k8s\12-ollama.yaml" }
(Get-Content "$root\k8s\20-migraciones-job.yaml" -Raw).Replace("michacc/emprendemarket-web:local", $Image) | kubectl apply -f -
kubectl -n $Namespace wait --for=condition=complete job/emprendemarket-migraciones --timeout=300s
(Get-Content "$root\k8s\30-web-deployment.yaml" -Raw).Replace("michacc/emprendemarket-web:local", $Image) | kubectl apply -f -
kubectl apply -f "$root\k8s\31-web-service.yaml"
if ($IncludeHpa) { kubectl apply -f "$root\k8s\40-hpa.yaml" }
kubectl -n $Namespace rollout status deployment/emprendemarket-web --timeout=300s
kubectl -n $Namespace get deployments,pods,services,pvc -o wide

param([string]$Namespace = "emprendemarket")
$ErrorActionPreference = "Stop"
kubectl delete namespace $Namespace --ignore-not-found

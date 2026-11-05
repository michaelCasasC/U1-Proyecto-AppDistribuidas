param([string]$Namespace = "emprendermarket")
$ErrorActionPreference = "Stop"
kubectl delete namespace $Namespace --ignore-not-found

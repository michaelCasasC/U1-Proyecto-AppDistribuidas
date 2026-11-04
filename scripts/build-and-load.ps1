param(
    [string]$Image = "michacc/emprendemarket-web:local",
    [ValidateSet("docker-desktop", "kind")]
    [string]$Cluster = "docker-desktop",
    [string]$KindCluster = "kind"
)

$ErrorActionPreference = "Stop"
docker build -t $Image .
if ($Cluster -eq "kind") {
    kind load docker-image $Image --name $KindCluster
}
Write-Host "Imagen disponible: $Image"

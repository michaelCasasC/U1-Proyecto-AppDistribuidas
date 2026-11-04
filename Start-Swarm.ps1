[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

docker stack deploy --resolve-image never -c docker-stack.yml emprendemarket
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

docker compose -f docker-compose.swarm-gateway.yml up -d
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Swarm iniciado. Abre http://localhost:8888"

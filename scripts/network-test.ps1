param(
    [string]$Namespace = "emprendermarket",
    [ValidateSet("baseline", "delay", "loss", "delay-loss")]
    [string]$Profile = "baseline",
    [string]$Delay = "100ms",
    [string]$Loss = "5%"
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$evidence = Join-Path $root "evidencias"
New-Item -ItemType Directory -Force $evidence | Out-Null
$output = Join-Path $evidence "iperf3-$Profile.txt"
$name = "iperf3-client-$([Guid]::NewGuid().ToString('N').Substring(0,8))"

if ($Profile -eq "baseline") {
    kubectl -n $Namespace run $name --rm -i --restart=Never --image=networkstatic/iperf3 `
        -- iperf3 -c iperf3-server -t 10 | Tee-Object $output
    exit $LASTEXITCODE
}

$netem = switch ($Profile) {
    "delay" { "tc qdisc add dev eth0 root netem delay $Delay" }
    "loss" { "tc qdisc add dev eth0 root netem loss $Loss" }
    "delay-loss" { "tc qdisc add dev eth0 root netem delay $Delay loss $Loss" }
}
$command = "$netem; iperf3 -c iperf3-server -t 10"
$overrides = @{
    apiVersion = "v1"
    spec = @{
        containers = @(@{
            name = $name
            image = "nicolaka/netshoot"
            securityContext = @{ capabilities = @{ add = @("NET_ADMIN") } }
            command = @("sh", "-c", $command)
        })
    }
} | ConvertTo-Json -Compress -Depth 10
try {
    kubectl -n $Namespace run $name --restart=Never --image=nicolaka/netshoot --overrides $overrides | Out-Null
    kubectl -n $Namespace wait --for=condition=Ready pod/$name --timeout=120s | Out-Null
    kubectl -n $Namespace logs -f $name | Tee-Object $output
}
finally {
    kubectl -n $Namespace delete pod $name --ignore-not-found | Out-Null
}
Write-Host "Evidencia guardada en $output"

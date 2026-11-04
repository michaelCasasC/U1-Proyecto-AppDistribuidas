param([string]$Namespace = "emprendemarket")
$ErrorActionPreference = "Stop"
kubectl -n $Namespace run iperf3-client --rm -i --restart=Never --image=networkstatic/iperf3 -- iperf3 -c iperf3-server -t 10
Write-Host "Para kind, aplicar netem en el nodo: docker exec <kind-node> tc qdisc add dev eth0 root netem delay 100ms loss 5%%"
Write-Host "Limpiar: docker exec <kind-node> tc qdisc del dev eth0 root"

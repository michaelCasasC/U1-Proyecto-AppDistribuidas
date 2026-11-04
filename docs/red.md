# Pruebas de red

Ejecutar `scripts/network-test.ps1` con el namespace desplegado para obtener la línea base de
iperf3. En kind, la latencia y pérdida se introducen en el nodo:

```powershell
docker exec <kind-node> tc qdisc add dev eth0 root netem delay 100ms loss 5%
docker exec <kind-node> tc qdisc change dev eth0 root netem delay 200ms loss 10%
docker exec <kind-node> tc qdisc del dev eth0 root
```

En Docker Desktop Kubernetes la interfaz del nodo es administrada por Docker Desktop; se
requiere un contenedor privilegiado con `nsenter`/`tc` o un pod de diagnóstico con
`NET_ADMIN`. Repetir iperf3 y k6 en cada condición, guardando la salida en `evidencias/`.

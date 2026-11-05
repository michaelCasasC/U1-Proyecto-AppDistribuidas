# Pruebas de red

Ejecutar `scripts/network-test.ps1` con el namespace desplegado. El perfil `baseline` mide
sin degradación; los perfiles `delay`, `loss` y `delay-loss` crean un pod temporal con
`NET_ADMIN` y aplican `tc netem` directamente a su interfaz, por lo que también funcionan
con Docker Desktop Kubernetes:

```powershell
.\scripts\network-test.ps1 -Profile baseline
.\scripts\network-test.ps1 -Profile delay-loss -Delay 100ms -Loss 5%
.\scripts\network-test.ps1 -Profile delay -Delay 200ms
.\scripts\network-test.ps1 -Profile loss -Loss 10%
```

Cada ejecución guarda su salida en `evidencias/iperf3-<perfil>.txt`.

Como alternativa, en kind la latencia y pérdida se pueden introducir en el nodo:

```powershell
docker exec <kind-node> tc qdisc add dev eth0 root netem delay 100ms loss 5%
docker exec <kind-node> tc qdisc change dev eth0 root netem delay 200ms loss 10%
docker exec <kind-node> tc qdisc del dev eth0 root
```

En Docker Desktop Kubernetes la interfaz del nodo es administrada por Docker Desktop; se
requiere un contenedor privilegiado con `nsenter`/`tc` o un pod de diagnóstico con
`NET_ADMIN`. Repetir iperf3 y k6 en cada condición, guardando la salida en `evidencias/`.

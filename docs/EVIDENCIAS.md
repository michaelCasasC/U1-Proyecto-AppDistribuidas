# Checklist de evidencias

Guardar capturas y salidas bajo `evidencias/` (la carpeta está ignorada por Git).

| Evidencia | Comando o acción | Archivo sugerido |
|---|---|---|
| Estructura | `tree /F /A` | `fig-01-estructura.png` |
| Recursos | `kubectl get deployments,pods,services,pvc -n emprendermarket -o wide` | `fig-02-recursos.png` |
| Pod | `kubectl describe pod -n emprendermarket <pod>` | `fig-03-pod.png` |
| Réplicas | `kubectl get deploy emprendermarket-web -n emprendermarket` | `fig-04-replicas.png` |
| SignalR | Abrir `/Tiempo-Real` en dos navegadores y registrar los pods emisores | `fig-05-signalr.png` |
| k6 dentro del clúster | `.\tests\k6\run-in-cluster.ps1 -Vus 10 -Duration 30s`; revisar el JSON generado y confirmar `http_req_failed=0` | `fig-06-k6.png` |
| Resiliencia real | `.\scripts\resilience-test.ps1 -ServiceUrl http://emprendermarket-web`; el log debe contener `DELETE 1 POD` y `DELETE 2 PODS` | `fig-07-resiliencia.png` |
| Red base | `.\scripts\network-test.ps1 -Profile baseline` | `fig-08-iperf3-base.png` |
| Red degradada | `.\scripts\network-test.ps1 -Profile delay-loss -Delay 100ms -Loss 5%` | `fig-09-iperf3-netem.png` |
| SonarQube | `docker compose -f tools/sonarqube/docker-compose.yml up -d` y `.\scripts\sonar-scan.ps1`; comparar `sonar-before.json`/`sonar-after.json` por bugs, vulnerabilidades, code smells, hotspots y coverage | `fig-10-sonar.png` |
| ZAP | `./scripts/zap-baseline.ps1` | `fig-10-zap.png` |

# Checklist de evidencias

Guardar capturas y salidas bajo `evidencias/` (la carpeta está ignorada por Git).

| Evidencia | Comando o acción | Archivo sugerido |
|---|---|---|
| Estructura | `tree /F /A` | `fig-01-estructura.png` |
| Recursos | `kubectl get deployments,pods,services,pvc -n emprendermarket -o wide` | `fig-02-recursos.png` |
| Pod | `kubectl describe pod -n emprendermarket <pod>` | `fig-03-pod.png` |
| Réplicas | `kubectl get deploy emprendermarket-web -n emprendermarket` | `fig-04-replicas.png` |
| SignalR | Abrir `/Tiempo-Real` en dos navegadores y registrar los pods emisores | `fig-05-signalr.png` |
| k6 | `k6 run tests/k6/load-test.js --summary-export evidencias/k6-summary.json` | `fig-06-k6.png` |
| Resiliencia | `./scripts/resilience-test.ps1` | `fig-07-resiliencia.png` |
| Red | `./scripts/network-test.ps1`, repetir con `tc netem` | `fig-08-iperf3.png` |
| SonarQube | `docker compose -f tools/sonarqube/docker-compose.yml up -d` y `./scripts/sonar-scan.ps1` | `fig-09-sonar.png` |
| ZAP | `./scripts/zap-baseline.ps1` | `fig-10-zap.png` |

param([string]$BaseUrl = "http://localhost:30080", [int]$Seconds = 60)
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$dir = Join-Path $root "evidencias"
New-Item -ItemType Directory -Force $dir | Out-Null
$file = Join-Path $dir "availability-$(Get-Date -Format yyyyMMdd-HHmmss).csv"
"timestamp,health_status,home_status,server" | Set-Content $file
1..$Seconds | ForEach-Object {
    $health = "ERROR"; $home = "ERROR"
    $server = "ERROR"
    try { $response = Invoke-WebRequest "$BaseUrl/health/ready" -UseBasicParsing; $health = $response.StatusCode; $server = $response.Headers["X-Instance"] } catch {}
    try { $home = (Invoke-WebRequest "$BaseUrl/" -UseBasicParsing).StatusCode } catch {}
    "$(Get-Date -Format o),$health,$home,$server" | Add-Content $file
    Start-Sleep 1
}

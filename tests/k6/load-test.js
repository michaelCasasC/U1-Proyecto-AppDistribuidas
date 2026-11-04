import http from "k6/http";
import { check, sleep } from "k6";

const baseUrl = __ENV.BASE_URL || "http://localhost:30080";
export const options = {
  stages: [
    { duration: "30s", target: 10 },
    { duration: "30s", target: 50 },
    { duration: "30s", target: 100 },
    { duration: __ENV.DURATION || "60s", target: Number(__ENV.VUS || 100) },
    { duration: "30s", target: 0 },
  ],
  thresholds: { http_req_duration: ["p(95)<800"], http_req_failed: ["rate<0.02"] },
};
export default function () {
  for (const path of ["/", "/Productos", "/Productos/Details/1"]) {
    const response = http.get(`${baseUrl}${path}`);
    check(response, { "HTTP 2xx/3xx": r => r.status >= 200 && r.status < 400 });
    sleep(1);
  }
}

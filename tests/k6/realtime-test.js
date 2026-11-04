import http from "k6/http";
import ws from "k6/ws";
import { check } from "k6";

const baseUrl = __ENV.BASE_URL || "http://localhost:30080";
export const options = { vus: Number(__ENV.VUS || 10), duration: __ENV.DURATION || "30s" };
export default function () {
  const negotiation = http.post(`${baseUrl}/hubs/notificaciones/negotiate?negotiateVersion=1`);
  check(negotiation, { "negociación SignalR": r => r.status === 200 });
  const connection = negotiation.json();
  const token = connection.connectionToken || connection.connectionId;
  const url = `${baseUrl.replace("http", "ws")}/hubs/notificaciones?id=${encodeURIComponent(token)}`;
  const response = ws.connect(url, {}, socket => {
    socket.on("open", () => socket.send('{"protocol":"json","version":1}\u001e'));
    socket.on("message", message => check(message, { "SignalR message": value => value.length > 0 }));
    socket.setTimeout(() => socket.close(), 5000);
  });
  check(response, { "WebSocket conectado": result => result && result.status === 101 });
}

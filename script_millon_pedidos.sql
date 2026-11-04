-- ============================================================
-- script_millon_pedidos.sql
-- Generación de 1.000.000 de pedidos con sus detalles
-- EmprendeMarketDB - PostgreSQL
--
-- Estrategia: usar generate_series() y random() directamente
-- en PostgreSQL (SET-BASED), lo que es mucho más rápido que
-- insertar fila por fila desde la aplicación (EF Core).
--
-- Cada pedido:
--   * Usuario al azar entre los clientes (EsCliente = true)
--   * Fecha entre hace 1 año y hoy
--   * 1 a 3 productos distintos al azar
--   * Cantidad 1 a 3, PrecioUnitario = precio del producto
-- El Total del pedido se calcula a partir de sus detalles.
--
-- Tiempo estimado: segundos (operación en el servidor de BD).
-- ============================================================

-- 0. (Opcional) Limpiar pedidos/detalles previos antes de generar
-- TRUNCATE TABLE "DetallesPedido", "Pedidos" RESTART IDENTITY CASCADE;

-- ============================================================
-- 1. PEDIDOS (1.000.000)
-- ============================================================
INSERT INTO "Pedidos" ("UsuarioId", "FechaPedido", "Total", "Activo", "FechaCreacion")
SELECT
    -- Usuario cliente al azar
    (SELECT "Id" FROM "AspNetUsers" WHERE "EsCliente" = true ORDER BY random() LIMIT 1),
    -- Fecha aleatoria en el último año
    NOW() - (random() * 365 || ' days')::interval,
    -- Total provisional (se recalcula abajo con los detalles)
    0,
    true,
    NOW()
FROM generate_series(1, 1000000) AS g;

-- ============================================================
-- 2. DETALLES DE PEDIDOS (1 a 3 por pedido => ~2 millones)
-- Se generan 3 filas por pedido y se descartan las que superen
-- un numero aleatorio de items (1..3) para cada pedido.
-- ============================================================
INSERT INTO "DetallesPedido" ("PedidoId", "ProductoId", "Cantidad", "PrecioUnitario", "Activo", "FechaCreacion")
SELECT
    "PedidoId", "ProductoId", "Cantidad", "PrecioUnitario", true, "FechaCreacion"
FROM (
    SELECT
        p."Id" AS "PedidoId",
        p."FechaCreacion",
        -- nº aleatorio de items que TENDRA este pedido (1..3), fijo por pedido
        (1 + floor(random() * 3)::int) AS items_pedido,
        gs.n AS item_n,
        -- Producto al azar (diferente por item)
        (SELECT "Id" FROM "Productos" ORDER BY random() LIMIT 1) AS "ProductoId",
        -- Cantidad 1..3
        (1 + floor(random() * 3)::int) AS "Cantidad",
        -- Precio unitario = precio actual del producto elegido
        (SELECT "Precio" FROM "Productos" pp ORDER BY random() LIMIT 1) AS "PrecioUnitario"
    FROM "Pedidos" p
    CROSS JOIN generate_series(1, 3) AS gs(n)
) sub
WHERE item_n <= items_pedido;

-- ============================================================
-- 3. RECALCULAR EL TOTAL DE CADA PEDIDO según sus detalles
-- ============================================================
UPDATE "Pedidos" p
SET "Total" = (
    SELECT COALESCE(SUM(d."Cantidad" * d."PrecioUnitario"), 0)
    FROM "DetallesPedido" d
    WHERE d."PedidoId" = p."Id"
)
WHERE p."Activo" = true;

-- ============================================================
-- Resultado esperado:
--   SELECT COUNT(*) FROM "Pedidos";          -- 1.000.000
--   SELECT COUNT(*) FROM "DetallesPedido";   -- ~2.000.000
-- ============================================================

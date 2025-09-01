-- 🧪 Test Shipment API Logic SQL 🧪
-- This will test the exact logic used in the controller

-- Test the pending orders query (what the controller should return)
SELECT 
    o."Id",
    o."WcOrderId",
    o."CustomerName",
    o."CustomerEmail",
    o."CustomerPhone",
    o."ShippingAddress",
    o."OrderTotal",
    o."Status",
    o."SyncedAt"
FROM orders_v2 o
WHERE LOWER(o."Status") NOT IN ('cancelled', 'refunded')
  AND NOT EXISTS (
      SELECT 1 FROM shipments s WHERE s.order_id = o."Id"
  )
ORDER BY o."SyncedAt" DESC;

-- Count how many orders should be returned
SELECT COUNT(*) as orders_that_need_shipping
FROM orders_v2 o
WHERE LOWER(o."Status") NOT IN ('cancelled', 'refunded')
  AND NOT EXISTS (
      SELECT 1 FROM shipments s WHERE s.order_id = o."Id"
  );

-- Show all non-cancelled, non-refunded orders
SELECT 
    "Id",
    "WcOrderId", 
    "CustomerName",
    "Status",
    "OrderTotal"
FROM orders_v2 
WHERE LOWER("Status") NOT IN ('cancelled', 'refunded')
ORDER BY "SyncedAt" DESC;



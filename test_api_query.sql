-- 🧪 Test API Query Directly 🧪
-- This will test the exact query that the controller should be running

-- Test 1: Basic query without Include
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
ORDER BY o."SyncedAt" DESC
LIMIT 5;

-- Test 2: Check if OrderItems exist
SELECT 
    o."Id",
    o."WcOrderId",
    o."CustomerName",
    COUNT(oi."Id") as item_count
FROM orders_v2 o
LEFT JOIN order_items_v2 oi ON o."Id" = oi."OrderId"
WHERE LOWER(o."Status") NOT IN ('cancelled', 'refunded')
GROUP BY o."Id", o."WcOrderId", o."CustomerName"
ORDER BY o."SyncedAt" DESC
LIMIT 5;

-- Test 3: Check if shipments table is empty
SELECT COUNT(*) as total_shipments FROM shipments;

-- Test 4: Test the full query with Include logic
SELECT 
    o."Id",
    o."WcOrderId",
    o."CustomerName",
    o."CustomerEmail",
    o."CustomerPhone",
    o."ShippingAddress",
    o."OrderTotal",
    o."Status",
    o."SyncedAt",
    CASE 
        WHEN EXISTS (SELECT 1 FROM shipments s WHERE s.order_id = o."Id") 
        THEN 'Has Shipment' 
        ELSE 'No Shipment' 
    END as shipment_status
FROM orders_v2 o
WHERE LOWER(o."Status") NOT IN ('cancelled', 'refunded')
  AND NOT EXISTS (SELECT 1 FROM shipments s WHERE s.order_id = o."Id")
ORDER BY o."SyncedAt" DESC
LIMIT 5;



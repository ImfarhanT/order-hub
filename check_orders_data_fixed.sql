-- 🔍 Check Orders Data SQL (FIXED) 🔍
-- This will show us if there are any orders and their current status

-- Check total count of orders
SELECT 'Total Orders' as info, COUNT(*) as count FROM orders_v2
UNION ALL
SELECT 'Orders with Status', COUNT(*) FROM orders_v2 WHERE "Status" IS NOT NULL
UNION ALL
SELECT 'Cancelled Orders', COUNT(*) FROM orders_v2 WHERE LOWER("Status") = 'cancelled'
UNION ALL
SELECT 'Refunded Orders', COUNT(*) FROM orders_v2 WHERE LOWER("Status") = 'refunded'
UNION ALL
SELECT 'Processing Orders', COUNT(*) FROM orders_v2 WHERE LOWER("Status") = 'processing'
UNION ALL
SELECT 'Shipped Orders', COUNT(*) FROM orders_v2 WHERE LOWER("Status") = 'shipped'
UNION ALL
SELECT 'Completed Orders', COUNT(*) FROM orders_v2 WHERE LOWER("Status") = 'completed'
UNION ALL
SELECT 'Pending Orders (need shipping)', COUNT(*) FROM orders_v2 
WHERE LOWER("Status") NOT IN ('cancelled', 'refunded');

-- Show sample orders with their status
SELECT 
    "Id",
    "WcOrderId",
    "CustomerName",
    "Status",
    "OrderTotal",
    "SyncedAt"
FROM orders_v2 
ORDER BY "SyncedAt" DESC 
LIMIT 10;

-- Check if shipments table has any data
SELECT 'Total Shipments' as info, COUNT(*) as count FROM shipments
UNION ALL
SELECT 'Shipments by Status', COUNT(*) FROM shipments GROUP BY status;

-- Check for any existing shipments
SELECT * FROM shipments LIMIT 5;



-- Check current table structure
\d shipments

-- Check foreign key constraints
SELECT 
    tc.constraint_name, 
    tc.table_name, 
    kcu.column_name, 
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name 
FROM 
    information_schema.table_constraints AS tc 
    JOIN information_schema.key_column_usage AS kcu
      ON tc.constraint_name = kcu.constraint_name
      AND tc.table_schema = kcu.table_schema
    JOIN information_schema.constraint_column_usage AS ccu
      ON ccu.constraint_name = tc.constraint_name
      AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY' 
    AND tc.table_name='shipments';

-- Check if we can insert a test record
INSERT INTO shipments (order_id, tracking_number, carrier, status) 
VALUES ('00000000-0000-0000-0000-000000000000', 'TEST123', 'TEST', 'pending')
ON CONFLICT DO NOTHING;

-- Check the test record
SELECT * FROM shipments WHERE tracking_number = 'TEST123';

-- Clean up test record
DELETE FROM shipments WHERE tracking_number = 'TEST123';



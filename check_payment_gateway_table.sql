-- Check if PaymentGatewayDetails table exists
SELECT EXISTS (
    SELECT FROM information_schema.tables 
    WHERE table_name = 'payment_gateway_details'
) as table_exists;

-- Check table structure
SELECT column_name, data_type, is_nullable
FROM information_schema.columns 
WHERE table_name = 'payment_gateway_details'
ORDER BY ordinal_position;

-- Check if table has data
SELECT COUNT(*) as record_count 
FROM payment_gateway_details;

-- Show sample data
SELECT id, gateway_code, descriptor, fees_percentage, fees_fixed, fee_type
FROM payment_gateway_details
LIMIT 5;


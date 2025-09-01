-- Migration script to update payment_gateway_details table for percentage-based fees
-- This script converts the existing fees column to support both percentage and fixed amounts

-- Step 1: Add new columns
ALTER TABLE payment_gateway_details 
ADD COLUMN IF NOT EXISTS fees_percentage DECIMAL(5,2),
ADD COLUMN IF NOT EXISTS fees_fixed DECIMAL(18,2),
ADD COLUMN IF NOT EXISTS fee_type VARCHAR(20) DEFAULT 'percentage';

-- Step 2: Migrate existing data (assuming existing fees were fixed amounts)
-- Convert existing fees to fixed amounts and set fee_type to 'fixed'
UPDATE payment_gateway_details 
SET fees_fixed = fees,
    fee_type = 'fixed'
WHERE fees IS NOT NULL AND fees > 0;

-- Step 3: Convert common payment gateway fees to percentages
-- WooCommerce Payments, Stripe, PayPal typically charge 2.9%
UPDATE payment_gateway_details 
SET fees_percentage = 2.90,
    fee_type = 'percentage',
    fees_fixed = NULL
WHERE gateway_code IN ('woocommerce_payments', 'stripe', 'paypal') 
AND fees IS NOT NULL;

-- Square typically charges 2.6%
UPDATE payment_gateway_details 
SET fees_percentage = 2.60,
    fee_type = 'percentage',
    fees_fixed = NULL
WHERE gateway_code = 'square' 
AND fees IS NOT NULL;

-- Authorize.Net typically charges 2.9%
UPDATE payment_gateway_details 
SET fees_percentage = 2.90,
    fee_type = 'percentage',
    fees_fixed = NULL
WHERE gateway_code = 'authorize_net' 
AND fees IS NOT NULL;

-- Step 4: Set default percentage for any remaining records
UPDATE payment_gateway_details 
SET fees_percentage = 2.90,
    fee_type = 'percentage'
WHERE fee_type IS NULL OR fee_type = '';

-- Step 5: Drop the old fees column (optional - uncomment if you want to remove it)
-- ALTER TABLE payment_gateway_details DROP COLUMN fees;

-- Step 6: Add constraints and indexes
ALTER TABLE payment_gateway_details 
ADD CONSTRAINT chk_fee_type CHECK (fee_type IN ('percentage', 'fixed')),
ADD CONSTRAINT chk_fees_percentage CHECK (fees_percentage IS NULL OR (fees_percentage >= 0 AND fees_percentage <= 100)),
ADD CONSTRAINT chk_fees_fixed CHECK (fees_fixed IS NULL OR fees_fixed >= 0);

-- Step 7: Create index for better performance
CREATE INDEX IF NOT EXISTS idx_payment_gateway_details_fee_type ON payment_gateway_details(fee_type);

-- Step 8: Verify the migration
SELECT 
    gateway_code,
    descriptor,
    fee_type,
    fees_percentage,
    fees_fixed,
    CASE 
        WHEN fee_type = 'percentage' THEN CONCAT(fees_percentage, '%')
        WHEN fee_type = 'fixed' THEN CONCAT('$', fees_fixed)
        ELSE 'N/A'
    END as fees_display
FROM payment_gateway_details
ORDER BY gateway_code;


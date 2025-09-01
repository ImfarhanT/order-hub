-- Create payment_gateway_details table for storing payment gateway information
-- This table stores gateway code, descriptor, and fees (percentage or fixed amount)

CREATE TABLE IF NOT EXISTS payment_gateway_details (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    gateway_code VARCHAR(100) NOT NULL,
    descriptor TEXT,
    fees_percentage DECIMAL(5,2),
    fees_fixed DECIMAL(18,2),
    fee_type VARCHAR(20) DEFAULT 'percentage' CHECK (fee_type IN ('percentage', 'fixed'))
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_payment_gateway_details_gateway_code ON payment_gateway_details(gateway_code);
CREATE INDEX IF NOT EXISTS idx_payment_gateway_details_fee_type ON payment_gateway_details(fee_type);

-- Add constraints
ALTER TABLE payment_gateway_details 
ADD CONSTRAINT chk_fees_percentage CHECK (fees_percentage IS NULL OR (fees_percentage >= 0 AND fees_percentage <= 100)),
ADD CONSTRAINT chk_fees_fixed CHECK (fees_fixed IS NULL OR fees_fixed >= 0);

-- Insert sample data for common payment gateways with percentage-based fees
INSERT INTO payment_gateway_details (gateway_code, descriptor, fees_percentage, fee_type) VALUES
    ('woocommerce_payments', 'WooCommerce Payments - Secure credit card processing', 2.90, 'percentage'),
    ('stripe', 'Stripe - Online payment processing platform', 2.90, 'percentage'),
    ('paypal', 'PayPal - Digital payment service', 2.90, 'percentage'),
    ('square', 'Square - Payment processing for businesses', 2.60, 'percentage'),
    ('authorize_net', 'Authorize.Net - Payment gateway service', 2.90, 'percentage')
ON CONFLICT (gateway_code) DO NOTHING;


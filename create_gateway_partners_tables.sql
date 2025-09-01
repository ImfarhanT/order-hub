-- Create Gateway Partners table
CREATE TABLE IF NOT EXISTS gateway_partners (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    partner_name VARCHAR(100) NOT NULL,
    partner_code VARCHAR(50) NOT NULL UNIQUE,
    description TEXT,
    revenue_share_percentage DECIMAL(5,2) NOT NULL CHECK (revenue_share_percentage >= 0 AND revenue_share_percentage <= 100),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create Gateway Partner Assignments table
CREATE TABLE IF NOT EXISTS gateway_partner_assignments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    gateway_partner_id UUID NOT NULL REFERENCES gateway_partners(id) ON DELETE CASCADE,
    payment_gateway_id UUID NOT NULL REFERENCES payment_gateway_details(id) ON DELETE CASCADE,
    assignment_percentage DECIMAL(5,2) NOT NULL CHECK (assignment_percentage >= 0 AND assignment_percentage <= 100),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    UNIQUE(gateway_partner_id, payment_gateway_id)
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_gateway_partners_partner_code ON gateway_partners(partner_code);
CREATE INDEX IF NOT EXISTS idx_gateway_partners_is_active ON gateway_partners(is_active);
CREATE INDEX IF NOT EXISTS idx_gateway_partner_assignments_partner_id ON gateway_partner_assignments(gateway_partner_id);
CREATE INDEX IF NOT EXISTS idx_gateway_partner_assignments_gateway_id ON gateway_partner_assignments(payment_gateway_id);
CREATE INDEX IF NOT EXISTS idx_gateway_partner_assignments_is_active ON gateway_partner_assignments(is_active);

-- Insert sample gateway partners
INSERT INTO gateway_partners (partner_name, partner_code, description, revenue_share_percentage, is_active) VALUES
('Stripe Partner Network', 'STRIPE_PARTNER', 'Official Stripe partner for revenue sharing', 15.00, true),
('PayPal Business Partner', 'PAYPAL_PARTNER', 'PayPal business partner program', 12.50, true),
('Square Partner Program', 'SQUARE_PARTNER', 'Square payment processing partner', 18.00, true),
('WooCommerce Payments Partner', 'WOO_PARTNER', 'WooCommerce payments partner network', 20.00, true),
('Authorize.Net Partner', 'AUTHNET_PARTNER', 'Authorize.Net partner network', 16.50, true)
ON CONFLICT (partner_code) DO NOTHING;

-- Insert sample assignments (these will be updated based on actual payment gateway IDs)
-- Note: You'll need to update the payment_gateway_id values with actual UUIDs from your payment_gateway_details table
-- INSERT INTO gateway_partner_assignments (gateway_partner_id, payment_gateway_id, assignment_percentage, is_active) VALUES
-- ('partner-uuid-1', 'gateway-uuid-1', 25.00, true),
-- ('partner-uuid-2', 'gateway-uuid-2', 30.00, true);

-- Create a function to update the updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create triggers to automatically update updated_at
CREATE TRIGGER update_gateway_partners_updated_at 
    BEFORE UPDATE ON gateway_partners 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_gateway_partner_assignments_updated_at 
    BEFORE UPDATE ON gateway_partner_assignments 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Grant permissions (adjust as needed for your setup)
-- GRANT SELECT, INSERT, UPDATE, DELETE ON gateway_partners TO your_user;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON gateway_partner_assignments TO your_user;


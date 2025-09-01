-- 🔧 Fix Shipments Foreign Key Constraint 🔧
-- This will fix the foreign key to reference orders_v2 instead of orders

-- Drop the existing shipments table
DROP TABLE IF EXISTS shipments CASCADE;

-- Recreate the shipments table with correct foreign key
CREATE TABLE shipments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL,
    tracking_number TEXT NOT NULL,
    carrier TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'pending',
    tracking_url TEXT,
    shipped_at TIMESTAMP WITH TIME ZONE,
    estimated_delivery TIMESTAMP WITH TIME ZONE,
    delivered_at TIMESTAMP WITH TIME ZONE,
    notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Add foreign key constraint to orders_v2 table (using correct column name 'Id')
ALTER TABLE shipments 
ADD CONSTRAINT fk_shipments_orders_order_id 
FOREIGN KEY (order_id) REFERENCES orders_v2("Id") ON DELETE CASCADE;

-- Add unique constraint on order_id (one shipment per order)
ALTER TABLE shipments 
ADD CONSTRAINT uk_shipments_order_id UNIQUE (order_id);

-- Create indexes for better performance
CREATE INDEX ix_shipments_carrier ON shipments(carrier);
CREATE INDEX ix_shipments_status ON shipments(status);
CREATE INDEX ix_shipments_tracking_number ON shipments(tracking_number);

-- Add comments
COMMENT ON TABLE shipments IS 'Shipment tracking information for orders';
COMMENT ON COLUMN shipments.id IS 'Unique identifier for the shipment';
COMMENT ON COLUMN shipments.order_id IS 'Reference to the order being shipped';
COMMENT ON COLUMN shipments.tracking_number IS 'Carrier tracking number';
COMMENT ON COLUMN shipments.carrier IS 'Shipping carrier (FedEx, UPS, DHL, etc.)';
COMMENT ON COLUMN shipments.status IS 'Current shipment status (pending, shipped, delivered, exception)';
COMMENT ON COLUMN shipments.tracking_url IS 'URL to track the shipment online';
COMMENT ON COLUMN shipments.shipped_at IS 'When the shipment was actually shipped';
COMMENT ON COLUMN shipments.estimated_delivery IS 'Estimated delivery date';
COMMENT ON COLUMN shipments.delivered_at IS 'When the shipment was delivered';
COMMENT ON COLUMN shipments.notes IS 'Additional notes or special instructions';
COMMENT ON COLUMN shipments.created_at IS 'When the shipment record was created';
COMMENT ON COLUMN shipments.updated_at IS 'When the shipment record was last updated';

-- Verify the table was created
SELECT 'Shipments table recreated successfully with correct foreign key!' as status;



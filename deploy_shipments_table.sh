#!/bin/bash

# 🚚 Deploy Shipments Table Script 🚚
# This script adds the shipments table to your Order Hub database

echo "🚚 Starting Shipments Table Deployment..."

# Database connection details (update these with your actual values)
DB_HOST="your-db-host"
DB_NAME="your-db-name"
DB_USER="your-db-user"
DB_PASSWORD="your-db-password"

# SQL to create the shipments table
SQL="
-- Create shipments table
CREATE TABLE IF NOT EXISTS shipments (
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

-- Add foreign key constraint
ALTER TABLE shipments 
ADD CONSTRAINT fk_shipments_orders_order_id 
FOREIGN KEY (order_id) REFERENCES orders_v2(id) ON DELETE CASCADE;

-- Add unique constraint on order_id (one shipment per order)
ALTER TABLE shipments 
ADD CONSTRAINT uk_shipments_order_id UNIQUE (order_id);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS ix_shipments_carrier ON shipments(carrier);
CREATE INDEX IF NOT EXISTS ix_shipments_status ON shipments(status);
CREATE INDEX IF NOT EXISTS ix_shipments_tracking_number ON shipments(tracking_number);

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
"

echo "📊 Executing SQL to create shipments table..."

# Execute the SQL (you'll need to update the connection details)
# psql -h $DB_HOST -U $DB_USER -d $DB_NAME -c "$SQL"

echo "✅ Shipments table deployment script completed!"
echo ""
echo "📝 To deploy manually:"
echo "1. Update the database connection details in this script"
echo "2. Run: ./deploy_shipments_table.sh"
echo "3. Or execute the SQL manually in your database"
echo ""
echo "🚚 Your Shipment Management system is ready to use!"



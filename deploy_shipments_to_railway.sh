#!/bin/bash

# 🚚 Deploy Shipments Table to Railway PostgreSQL 🚚
# This script will create the shipments table in your Railway database

echo "🚚 Starting Shipments Table Deployment to Railway..."

# Railway PostgreSQL connection details
DB_HOST="caboose.proxy.rlwy.net"
DB_PORT="30459"
DB_NAME="railway"
DB_USER="postgres"
DB_PASSWORD="WARDWCdmlGBfBjfCKoVJJWxXxmkjopUt"

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

-- Add foreign key constraint to orders_v2 table
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

-- Verify the table was created
SELECT 'Shipments table created successfully!' as status;
"

echo "📊 Connecting to Railway PostgreSQL database..."
echo "Host: $DB_HOST:$DB_PORT"
echo "Database: $DB_NAME"
echo "User: $DB_USER"

# Check if psql is available
if ! command -v psql &> /dev/null; then
    echo "❌ Error: psql command not found. Please install PostgreSQL client tools."
    echo ""
    echo "📝 Alternative: You can run the SQL manually in your Railway dashboard:"
    echo "1. Go to Railway Dashboard"
    echo "2. Click on your database"
    echo "3. Go to 'Connect' tab"
    echo "4. Click 'Open in Railway CLI' or use the connection string"
    echo "5. Run the SQL commands from create_shipments_table.sql"
    exit 1
fi

# Execute the SQL
echo "📝 Executing SQL to create shipments table..."
echo "$SQL" | PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -v ON_ERROR_STOP=1

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Shipments table deployed successfully to Railway!"
    echo ""
    echo "🚚 Your Shipment Management system is now ready to use!"
    echo "📊 You can now:"
    echo "   - View orders that need shipping"
    echo "   - Create shipments with tracking numbers"
    echo "   - Track shipment status"
    echo "   - Print order details for vendors"
else
    echo ""
    echo "❌ Error: Failed to deploy shipments table"
    echo ""
    echo "📝 Please check:"
    echo "1. Database connection details are correct"
    echo "2. Database is accessible from your network"
    echo "3. User has CREATE TABLE permissions"
    echo ""
    echo "🔄 You can also run the SQL manually in Railway dashboard"
fi



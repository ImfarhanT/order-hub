#!/bin/bash

# Deploy OrderProfits table to Railway PostgreSQL database
echo "Deploying OrderProfits table to Railway..."

# Use the provided database URL
DB_URL="postgresql://postgres:WARDWCdmlGBfBjfCKoVJJWxXxmkjopUt@caboose.proxy.rlwy.net:30459/railway"

echo "Using database URL: $DB_URL"
echo "Creating OrderProfits table..."

# Run the SQL script
psql "$DB_URL" -f create_order_profits_table.sql

if [ $? -eq 0 ]; then
    echo "✅ OrderProfits table created successfully!"
    echo "Now you can deploy the updated application."
else
    echo "❌ Error creating OrderProfits table"
    exit 1
fi

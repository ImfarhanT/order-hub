#!/bin/bash

# Database connection details
DB_URL="postgresql://postgres:WARDWCdmlGBfBjfCKoVJJWxXxmkjopUt@caboose.proxy.rlwy.net:30459/railway"

echo "🚀 Deploying shipments table to Railway database..."

# Run the SQL script
echo "📝 Creating shipments table..."
psql "$DB_URL" -f check_shipments_table.sql

echo "✅ Deployment complete!"
echo "🔍 Check the output above for any errors or success messages."


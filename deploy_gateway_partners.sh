#!/bin/bash

echo "🚀 Deploying Gateway Partners System to Railway..."
echo "=================================================="

# Check if psql is available
if ! command -v psql &> /dev/null; then
    echo "❌ Error: psql is not installed or not in PATH"
    echo "Please install PostgreSQL client tools first"
    exit 1
fi

# Check if DATABASE_URL is set
if [ -z "$DATABASE_URL" ]; then
    echo "❌ Error: DATABASE_URL environment variable is not set"
    echo "Please set it to your Railway PostgreSQL connection string"
    echo "Example: export DATABASE_URL='postgresql://user:pass@host:port/db'"
    exit 1
fi

echo "✅ Database connection configured"
echo "📊 Running Gateway Partners migration..."

# Run the SQL migration
psql "$DATABASE_URL" -f create_gateway_partners_tables.sql

if [ $? -eq 0 ]; then
    echo "✅ Gateway Partners tables created successfully!"
    echo ""
    echo "🎉 Deployment completed!"
    echo ""
    echo "📋 What was created:"
    echo "   • gateway_partners table - Store partner information and revenue share percentages"
    echo "   • gateway_partner_assignments table - Link partners to payment gateways"
    echo "   • Sample partner data (Stripe, PayPal, Square, WooCommerce, Authorize.Net)"
    echo "   • Proper indexes and constraints for performance"
    echo "   • Automatic timestamp updates"
    echo ""
    echo "🔗 Next steps:"
    echo "   1. Deploy your application to Railway: railway up"
    echo "   2. Access the new 'Gateway Partners' tab in your admin panel"
    echo "   3. Add custom partners and assign them to payment gateways"
    echo "   4. Configure revenue share percentages"
else
    echo "❌ Error: Failed to create Gateway Partners tables"
    echo "Please check your database connection and try again"
    exit 1
fi


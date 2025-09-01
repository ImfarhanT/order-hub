#!/bin/bash

echo "🚀 Payment Gateway Fee Structure Update"
echo "========================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}📋 Updating Payment Gateway Fee Structure...${NC}"
echo ""

echo -e "${YELLOW}⚠️  This will update the payment gateway details table to support:${NC}"
echo "   • Percentage-based fees (e.g., 2.9%)"
echo "   • Fixed amount fees (e.g., $0.30)"
echo "   • Fee type selection (percentage vs fixed)"
echo ""

# Check if we're in the right directory
if [ ! -f "scripts/update_payment_gateway_fees_to_percentage.sql" ]; then
    echo -e "${RED}❌ Migration script not found. Please run this from the project root.${NC}"
    exit 1
fi

echo -e "${BLUE}📝 Migration Steps:${NC}"
echo "1. ✅ Add new columns (fees_percentage, fees_fixed, fee_type)"
echo "2. ✅ Migrate existing data to new structure"
echo "3. ✅ Convert common gateways to percentage-based fees"
echo "4. ✅ Add constraints and indexes"
echo "5. ✅ Verify migration"
echo ""

echo -e "${YELLOW}⚠️  Important Notes:${NC}"
echo "   • Existing fees will be converted to fixed amounts by default"
echo "   • Common gateways (Stripe, PayPal, etc.) will be set to typical percentages"
echo "   • You can manually adjust fees after migration"
echo ""

read -p "Do you want to proceed with the migration? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}Migration cancelled.${NC}"
    exit 0
fi

echo ""
echo -e "${BLUE}🔧 Running Migration...${NC}"

# Check if psql is available
if ! command -v psql &> /dev/null; then
    echo -e "${RED}❌ PostgreSQL client (psql) not found.${NC}"
    echo "Please install PostgreSQL client or run the migration manually."
    echo ""
    echo -e "${BLUE}📝 Manual Migration:${NC}"
    echo "1. Connect to your database"
    echo "2. Run: \\i scripts/update_payment_gateway_fees_to_percentage.sql"
    exit 1
fi

# Check for database connection
if [ -z "$DATABASE_URL" ] && [ -z "$PGHOST" ]; then
    echo -e "${YELLOW}⚠️  Database connection not configured.${NC}"
    echo ""
    echo -e "${BLUE}📝 To run migration manually:${NC}"
    echo "1. Set DATABASE_URL environment variable, or"
    echo "2. Set PGHOST, PGPORT, PGDATABASE, PGUSER, PGPASSWORD, or"
    echo "3. Run the SQL script directly in your database client"
    echo ""
    echo -e "${BLUE}📄 Migration script location:${NC}"
    echo "   scripts/update_payment_gateway_fees_to_percentage.sql"
    echo ""
    echo -e "${BLUE}📄 Updated table creation script:${NC}"
    echo "   create_payment_gateway_details_table.sql"
    exit 0
fi

echo -e "${GREEN}✅ Database connection configured${NC}"
echo ""

# Run the migration
echo -e "${BLUE}🔄 Executing migration...${NC}"
if psql "$DATABASE_URL" -f scripts/update_payment_gateway_fees_to_percentage.sql; then
    echo ""
    echo -e "${GREEN}✅ Migration completed successfully!${NC}"
    echo ""
    echo -e "${BLUE}🎯 Next Steps:${NC}"
    echo "1. Deploy the updated application code"
    echo "2. Test the new payment gateway fee structure"
    echo "3. Verify that fees are now displayed as percentages"
    echo ""
    echo -e "${BLUE}🔗 Test the new functionality:${NC}"
    echo "   • Visit: /admin/paymentgatewaydetails"
    echo "   • Add/Edit gateways with percentage-based fees"
    echo "   • Toggle between percentage and fixed fee types"
else
    echo ""
    echo -e "${RED}❌ Migration failed!${NC}"
    echo "Please check the error messages above and try again."
    exit 1
fi

echo ""
echo -e "${GREEN}🎉 Payment Gateway Fee Structure Update Complete!${NC}"


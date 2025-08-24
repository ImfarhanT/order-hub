#!/bin/bash

echo "🚀 Order Hub Deployment Script"
echo "================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Check if git is initialized
if [ ! -d ".git" ]; then
    echo -e "${RED}❌ Git repository not initialized. Please run: git init${NC}"
    exit 1
fi

# Check if we have uncommitted changes
if [ -n "$(git status --porcelain)" ]; then
    echo -e "${YELLOW}⚠️  You have uncommitted changes. Committing them now...${NC}"
    git add .
    git commit -m "Deployment preparation: $(date)"
fi

echo -e "${GREEN}✅ Git repository ready${NC}"

# Generate encryption key if not exists
if [ ! -f "hub/env.production" ]; then
    echo -e "${YELLOW}⚠️  Production environment file not found. Creating one...${NC}"
    ENCRYPTION_KEY=$(openssl rand -base64 32)
    cat > hub/env.production << EOF
# Order Hub Production Environment Configuration
SITE_SECRETS_KEY=$ENCRYPTION_KEY
ADMIN_EMAIL=admin@orderhub.com
ADMIN_PASSWORD=OrderHub2024!Secure
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80
EOF
    echo -e "${GREEN}✅ Production environment file created${NC}"
fi

echo ""
echo -e "${BLUE}📋 Deployment Checklist:${NC}"
echo "1. ✅ Git repository initialized"
echo "2. ✅ Code committed"
echo "3. ✅ Production environment configured"
echo ""
echo -e "${BLUE}🔑 Your Encryption Key:${NC}"
grep "SITE_SECRETS_KEY" hub/env.production | cut -d'=' -f2
echo ""
echo -e "${BLUE}📝 Next Steps:${NC}"
echo "1. Create GitHub repository and push code:"
echo "   git remote add origin https://github.com/YOUR_USERNAME/order-hub.git"
echo "   git push -u origin main"
echo ""
echo "2. Set up Supabase database:"
echo "   - Go to https://supabase.com"
echo "   - Create new project"
echo "   - Get connection string from Settings → Database"
echo ""
echo "3. Deploy to Render:"
echo "   - Go to https://render.com"
echo "   - Connect GitHub repository"
echo "   - Set root directory to 'hub'"
echo "   - Add environment variables from env.production"
echo ""
echo -e "${GREEN}🎉 Ready for deployment!${NC}"

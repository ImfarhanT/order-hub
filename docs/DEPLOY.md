# Order Hub Deployment Guide

This guide covers deploying Order Hub to various free/low-cost hosting platforms.

## Prerequisites

- GitHub repository with Order Hub code
- PostgreSQL database (Supabase free tier recommended)
- Domain name (optional but recommended)

## Platform Options

### 1. Render (Recommended - Free Tier)

Render offers a generous free tier with automatic deployments.

#### Setup Steps

1. **Create Render Account**
   - Sign up at [render.com](https://render.com)
   - Connect your GitHub account

2. **Create New Web Service**
   - Click "New +" → "Web Service"
   - Connect your GitHub repository
   - Select the `hub` directory as root

3. **Configure Service**
   ```
   Name: order-hub
   Environment: .NET
   Build Command: dotnet publish -c Release -o out
   Start Command: dotnet out/HubApi.dll
   ```

4. **Set Environment Variables**
   ```
   SITE_SECRETS_KEY: [generate with: openssl rand -base64 32]
   ADMIN_EMAIL: admin@yourdomain.com
   ADMIN_PASSWORD: SecurePassword123!
   ConnectionStrings__Default: [your Supabase connection string]
   ASPNETCORE_ENVIRONMENT: Production
   ```

5. **Deploy**
   - Click "Create Web Service"
   - Render will automatically build and deploy
   - Your hub will be available at `https://order-hub.onrender.com`

### 2. Railway

Railway provides easy deployment with automatic scaling.

#### Setup Steps

1. **Create Railway Account**
   - Sign up at [railway.app](https://railway.app)
   - Connect your GitHub account

2. **Create New Project**
   - Click "New Project" → "Deploy from GitHub repo"
   - Select your repository

3. **Configure Service**
   - Railway will auto-detect .NET
   - Set root directory to `hub`

4. **Set Environment Variables**
   - Same variables as Render
   - Add database connection string

5. **Deploy**
   - Railway will automatically deploy
   - Get your URL from the dashboard

### 3. Fly.io

Fly.io offers global deployment with generous free tier.

#### Setup Steps

1. **Install Fly CLI**
   ```bash
   # macOS
   brew install flyctl
   
   # Windows
   powershell -Command "iwr https://fly.io/install.ps1 -useb | iex"
   
   # Linux
   curl -L https://fly.io/install.sh | sh
   ```

2. **Login to Fly**
   ```bash
   fly auth login
   ```

3. **Create App**
   ```bash
   cd hub
   fly launch
   ```

4. **Configure App**
   - Choose app name
   - Select region
   - Choose PostgreSQL (or use external)

5. **Set Secrets**
   ```bash
   fly secrets set SITE_SECRETS_KEY="your-32-byte-key"
   fly secrets set ADMIN_EMAIL="admin@yourdomain.com"
   fly secrets set ADMIN_PASSWORD="SecurePassword123!"
   ```

6. **Deploy**
   ```bash
   fly deploy
   ```

## Database Setup

### Supabase (Recommended)

1. **Create Account**
   - Sign up at [supabase.com](https://supabase.com)
   - Create new project

2. **Get Connection String**
   - Go to Settings → Database
   - Copy connection string
   - Format: `Host=db.xxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=xxx;SSL Mode=Require;Trust Server Certificate=true`

3. **Configure Environment**
   - Add connection string to your hosting platform
   - Test connection

### Alternative: Railway PostgreSQL

1. **Create Database**
   - In Railway dashboard, click "New" → "Database"
   - Choose PostgreSQL
   - Copy connection string

2. **Use Connection String**
   - Add to your web service environment variables

## Environment Variables Reference

### Required Variables

```bash
# Security
SITE_SECRETS_KEY=base64-32-byte-key-here

# Admin Access
ADMIN_EMAIL=admin@yourdomain.com
ADMIN_PASSWORD=SecurePassword123!

# Database
ConnectionStrings__Default=Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true
```

### Optional Variables

```bash
# Environment
ASPNETCORE_ENVIRONMENT=Production

# URLs
ASPNETCORE_URLS=https://+:443

# Logging
Serilog__MinimumLevel__Default=Information
Serilog__MinimumLevel__Microsoft=Warning
```

## Post-Deployment Steps

### 1. Verify Deployment

1. **Check Health**
   - Visit your hub URL
   - Should see "Welcome to Order Hub" page

2. **Test Database**
   - Login to admin at `/admin/login`
   - Create a test site

3. **Verify API**
   - Visit `/swagger` for API documentation
   - Test endpoints with Postman

### 2. Configure Domain (Optional)

1. **Add Custom Domain**
   - In your hosting platform, add custom domain
   - Update DNS records

2. **SSL Certificate**
   - Most platforms auto-provision SSL
   - Verify HTTPS works

### 3. Install WooCommerce Plugin

1. **Upload Plugin**
   ```bash
   # Copy wc-plugin folder to WordPress site
   cp -r wc-plugin /path/to/wp-content/plugins/order-hub-sync
   ```

2. **Activate Plugin**
   - Go to WordPress admin → Plugins
   - Activate "Order Hub Sync"

3. **Configure Settings**
   - Go to WooCommerce → Order Hub Sync
   - Enter Hub URL, API Key, and Secret

## Monitoring & Maintenance

### 1. Logs

- **Render**: View logs in dashboard
- **Railway**: Real-time logs in terminal
- **Fly.io**: `fly logs` command

### 2. Health Checks

- Monitor database connectivity
- Check API response times
- Verify plugin sync status

### 3. Backups

- **Database**: Supabase provides daily backups
- **Code**: GitHub repository
- **Configuration**: Document environment variables

## Troubleshooting

### Common Issues

1. **Database Connection Failed**
   - Verify connection string
   - Check SSL settings
   - Ensure database is accessible

2. **HMAC Authentication Errors**
   - Verify SITE_SECRETS_KEY is set
   - Check API key/secret in plugin
   - Ensure timestamps are synchronized

3. **Build Failures**
   - Check .NET version compatibility
   - Verify all dependencies are included
   - Review build logs

4. **Plugin Sync Issues**
   - Enable debug logging
   - Check Hub URL accessibility
   - Verify API credentials

### Support Resources

- Check application logs
- Review hosting platform status
- Test with Postman/curl
- Enable debug mode

## Security Considerations

### 1. Environment Variables

- Never commit secrets to repository
- Use hosting platform secrets management
- Rotate keys regularly

### 2. Database Security

- Use strong passwords
- Enable SSL connections
- Restrict network access

### 3. API Security

- Monitor for unusual traffic patterns
- Implement rate limiting if needed
- Regular security audits

## Cost Optimization

### Free Tier Limits

- **Render**: 750 hours/month, 512MB RAM
- **Railway**: $5/month credit, then pay-per-use
- **Fly.io**: 3 shared-cpu-1x apps, 3GB persistent volume

### Scaling Considerations

- Start with free tier
- Monitor resource usage
- Upgrade only when needed
- Consider database hosting costs

## Next Steps

After successful deployment:

1. **Test Complete Workflow**
   - Create site in hub
   - Configure plugin
   - Place test order
   - Verify sync

2. **Set Up Monitoring**
   - Configure alerts
   - Monitor performance
   - Set up logging

3. **Documentation**
   - Update team documentation
   - Create operational procedures
   - Plan maintenance schedule

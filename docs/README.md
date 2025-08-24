# Order Hub

A production-ready, free/low-cost system to aggregate orders from multiple WooCommerce stores into a central dashboard.

## Overview

Order Hub consists of two main components:

1. **Hub** - ASP.NET Core 8 Web API + Razor Pages admin UI
2. **WooCommerce Plugin** - WordPress plugin for syncing orders

## Features

- **Multi-store Order Aggregation**: Collect orders from multiple WooCommerce stores
- **Secure API**: HMAC-SHA256 authentication with nonce replay protection
- **Real-time Sync**: Automatic order and shipping updates
- **Revenue Sharing**: Configurable partner and website revenue splits
- **Admin Dashboard**: Modern Razor Pages UI with Bootstrap
- **Reporting**: Per-site and per-gateway analytics
- **CSV Export**: Download order and revenue data
- **Timezone Support**: UTC storage with Asia/Karachi display

## Architecture

### Hub (ASP.NET Core 8)

- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: Cookie-based admin auth
- **API Security**: HMAC-SHA256 signatures with encrypted secrets
- **UI**: Razor Pages with Bootstrap 5
- **Logging**: Serilog with file and console sinks

### WooCommerce Plugin (PHP)

- **Hooks**: `woocommerce_checkout_order_processed`, `woocommerce_order_status_changed`
- **Retry Logic**: Failed order queuing with automatic retries
- **Backfill Tool**: Send historical orders to hub
- **Settings Page**: Hub URL, API credentials, debug logging

## Quick Start

### 1. Deploy Hub

```bash
# Clone repository
git clone https://github.com/your-org/order-hub.git
cd order-hub/hub

# Set environment variables
export SITE_SECRETS_KEY=$(openssl rand -base64 32)
export ADMIN_EMAIL=admin@example.com
export ADMIN_PASSWORD=SecurePassword123!

# Deploy to your preferred platform
# - Render: Connect GitHub repo, set env vars
# - Railway: Connect GitHub repo, set env vars  
# - Fly.io: flyctl launch, set env vars
```

### 2. Create Database

- Create PostgreSQL database (Supabase free tier recommended)
- Update connection string in `appsettings.json`
- Run application - EF Core will create tables automatically

### 3. Install Plugin

- Upload `wc-plugin` folder to `/wp-content/plugins/order-hub-sync/`
- Activate plugin in WordPress admin
- Configure Hub URL, API Key, and Secret

### 4. Create Site

- Login to Hub admin at `/admin/login`
- Create new site in `/admin/sites`
- Copy API Key and Secret to plugin settings

## Configuration

### Environment Variables

```bash
# Required
SITE_SECRETS_KEY=base64-32-byte-key-here
ADMIN_EMAIL=admin@example.com
ADMIN_PASSWORD=SecurePassword123!

# Database
ConnectionStrings__Default=Host=...;Database=order_hub;Username=...;Password=...;SSL Mode=Require

# Optional
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443
```

### Database Schema

The system automatically creates these tables:

- `sites` - Store configurations
- `partners` - Revenue sharing partners  
- `payment_gateways` - Payment method configurations
- `orders` - Order data from WooCommerce
- `order_items` - Individual order line items
- `shipping_updates` - Shipping status updates
- `revenue_shares` - Calculated revenue splits
- `request_nonces` - Anti-replay protection

## API Endpoints

### Plugin Endpoints (HMAC Required)

```
POST /api/v1/orders/sync
POST /api/v1/shipping/update
```

### Admin Endpoints

```
GET  /api/sites
POST /api/sites
GET  /api/sites/{id}
```

### Authentication

Plugin requests require HMAC-SHA256 signatures:

```
Signature Base: site_api_key|timestamp|nonce|order_id|order_total
Signature: HMAC-SHA256(signature_base, api_secret)
```

## Security Features

- **API Secret Encryption**: AES-GCM encryption at rest
- **Nonce Replay Protection**: 15-minute TTL with unique nonces
- **Timestamp Validation**: ±10 minute skew tolerance
- **HMAC Verification**: SHA256-based request signing
- **Rate Limiting**: Per-site request throttling (configurable)

## Development

### Prerequisites

- .NET 8 SDK
- PostgreSQL 12+
- Node.js 16+ (for frontend assets)

### Local Development

```bash
# Hub
cd hub
dotnet restore
dotnet run

# Plugin
# Copy wc-plugin to WordPress site
# Activate plugin
# Configure settings
```

### Testing

```bash
cd hub
dotnet test
```

## Deployment

### Render (Recommended for Free Tier)

1. Connect GitHub repository
2. Set environment variables
3. Deploy automatically on push

### Railway

1. Connect GitHub repository  
2. Set environment variables
3. Deploy with automatic scaling

### Fly.io

```bash
flyctl launch
flyctl secrets set SITE_SECRETS_KEY=...
flyctl deploy
```

## Monitoring

### Logs

- **Application**: Serilog with structured logging
- **Database**: EF Core query logging
- **Plugin**: WordPress debug log integration

### Health Checks

- Database connectivity
- API endpoint availability
- Plugin configuration status

## Troubleshooting

### Common Issues

1. **Database Connection**: Check connection string and SSL settings
2. **HMAC Failures**: Verify API key/secret and timestamp sync
3. **Plugin Errors**: Enable debug logging, check Hub URL
4. **Performance**: Monitor database indexes and query performance

### Support

- Check logs in Hub admin at `/admin/logs`
- Enable plugin debug logging
- Verify network connectivity between plugin and hub

## Contributing

1. Fork repository
2. Create feature branch
3. Make changes with tests
4. Submit pull request

## License

MIT License - see LICENSE file for details.

## Roadmap

- [ ] Bulk order import/export
- [ ] Advanced reporting with charts
- [ ] Webhook notifications
- [ ] Multi-language support
- [ ] Mobile admin app
- [ ] API rate limiting dashboard
- [ ] Automated backups
- [ ] Performance monitoring

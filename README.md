# Order Hub

A production-ready, free/low-cost system to aggregate orders from multiple WooCommerce stores into a central dashboard.

## 🚀 Quick Start

### 1. Deploy Hub
```bash
# Clone repository
git clone https://github.com/your-org/order-hub.git
cd order-hub

# Deploy to Render (recommended)
# 1. Connect GitHub repo to Render
# 2. Set environment variables
# 3. Deploy automatically
```

### 2. Set Up Database
- Create PostgreSQL database (Supabase free tier recommended)
- Update connection string in environment variables
- Tables created automatically on first run

### 3. Install Plugin
- Upload `wc-plugin` to WordPress site
- Activate "Order Hub Sync" plugin
- Configure Hub URL and credentials

### 4. Create Site
- Login to Hub admin
- Create new site
- Copy API credentials to plugin

## 🏗️ Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   WooCommerce  │    │   Order Hub     │    │   PostgreSQL    │
│     Stores      │◄──►│   (ASP.NET 8)   │◄──►│    Database     │
│                 │    │                 │    │                 │
│ • Plugin        │    │ • API Endpoints │    │ • Orders        │
│ • Order Hooks   │    │ • Admin UI      │    │ • Sites         │
│ • HMAC Auth     │    │ • HMAC Security │    │ • Revenue Data  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## ✨ Features

- **Multi-store Aggregation**: Collect orders from multiple WooCommerce stores
- **Real-time Sync**: Automatic order and shipping updates
- **Secure API**: HMAC-SHA256 authentication with replay protection
- **Revenue Sharing**: Configurable partner and website splits
- **Admin Dashboard**: Modern Razor Pages UI with Bootstrap
- **Reporting**: Per-site and per-gateway analytics
- **CSV Export**: Download order and revenue data
- **Timezone Support**: UTC storage with configurable display

## 🛠️ Technology Stack

### Backend
- **Framework**: ASP.NET Core 8
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: Cookie-based admin auth
- **Security**: HMAC-SHA256 + AES-GCM encryption
- **Logging**: Serilog with file and console sinks

### Frontend
- **UI Framework**: Razor Pages
- **CSS Framework**: Bootstrap 5
- **JavaScript**: Vanilla JS with jQuery for admin

### Plugin
- **Platform**: WordPress/WooCommerce
- **Language**: PHP 8.1+
- **HTTP Client**: WordPress HTTP API
- **Security**: HMAC signature generation

## 📁 Repository Structure

```
order-hub/
├── hub/                    # ASP.NET Core Web API + Admin UI
│   ├── Controllers/        # API endpoints
│   ├── Models/            # Entity models
│   ├── Services/          # Business logic
│   ├── Pages/             # Razor Pages admin UI
│   ├── Data/              # DbContext and migrations
│   └── Tests/             # Unit tests
├── wc-plugin/             # WooCommerce plugin
│   ├── includes/          # Plugin classes
│   └── order-hub-sync.php # Main plugin file
├── docs/                  # Documentation
├── scripts/               # Database and deployment scripts
└── .github/               # GitHub Actions CI/CD
```

## 🔧 Configuration

### Environment Variables

```bash
# Required
SITE_SECRETS_KEY=base64-32-byte-key-here
ADMIN_EMAIL=admin@example.com
ADMIN_PASSWORD=SecurePassword123!
ConnectionStrings__Default=Host=...;Database=order_hub;...

# Optional
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443
```

### Database Schema

The system automatically creates these tables:
- `sites` - Store configurations
- `orders` - Order data from WooCommerce
- `order_items` - Individual order line items
- `revenue_shares` - Calculated revenue splits
- `request_nonces` - Anti-replay protection

## 🔐 Security Features

- **API Secret Encryption**: AES-GCM encryption at rest
- **Nonce Replay Protection**: 15-minute TTL with unique nonces
- **Timestamp Validation**: ±10 minute skew tolerance
- **HMAC Verification**: SHA256-based request signing
- **Rate Limiting**: Per-site request throttling

## 🚀 Deployment

### Free Tier Hosting

- **Render**: 750 hours/month, automatic deployments
- **Railway**: $5/month credit, then pay-per-use
- **Fly.io**: 3 shared-cpu-1x apps, global deployment

### Database Hosting

- **Supabase**: Free tier with 500MB database
- **Railway**: PostgreSQL included in project
- **External**: Any PostgreSQL 12+ provider

## 📚 Documentation

- **[README](docs/README.md)**: Comprehensive system documentation
- **[Deployment Guide](docs/DEPLOY.md)**: Step-by-step deployment instructions
- **[API Reference](docs/API.md)**: API endpoint documentation

## 🧪 Testing

```bash
# Run tests
cd hub
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 🔄 CI/CD

GitHub Actions workflow includes:
- Build and test on push/PR
- Security scanning with CodeQL
- Automated deployment to staging/production
- Plugin validation and testing

## 🤝 Contributing

1. Fork repository
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Issues**: [GitHub Issues](https://github.com/your-org/order-hub/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-org/order-hub/discussions)
- **Documentation**: [docs/](docs/) folder

## 🗺️ Roadmap

- [ ] Bulk order import/export
- [ ] Advanced reporting with charts
- [ ] Webhook notifications
- [ ] Multi-language support
- [ ] Mobile admin app
- [ ] API rate limiting dashboard
- [ ] Automated backups
- [ ] Performance monitoring

## 🙏 Acknowledgments

- Built with ASP.NET Core 8
- WooCommerce integration
- Bootstrap 5 for UI
- PostgreSQL for data storage
- GitHub Actions for CI/CD

---

**Order Hub** - Centralize your WooCommerce operations in one powerful dashboard.

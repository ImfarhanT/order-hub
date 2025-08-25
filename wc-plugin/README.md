# Order Hub Sync - WooCommerce Plugin

A WordPress/WooCommerce plugin that synchronizes orders with the Order Hub central dashboard.

## Features

- **Real-time Order Sync**: Automatically sends new orders and status updates to Order Hub
- **Backfill Tool**: Send existing orders to Order Hub for initial setup
- **Failed Order Retry**: Automatic retry system for failed API calls
- **Debug Logging**: Comprehensive logging for troubleshooting
- **Admin Interface**: Clean, tabbed settings page with connection testing
- **Secure API**: Uses API key authentication for secure communication

## Requirements

- WordPress 5.0 or higher
- WooCommerce 5.0 or higher
- PHP 7.4 or higher
- Order Hub API endpoint

## Installation

1. **Upload Plugin**: Upload the `order-hub-sync` folder to your `/wp-content/plugins/` directory
2. **Activate Plugin**: Activate the plugin through the 'Plugins' menu in WordPress
3. **Configure Settings**: Go to WooCommerce → Order Hub Sync to configure your settings

## Configuration

### 1. Hub Configuration

- **Hub URL**: The base URL of your Order Hub (e.g., `https://your-hub.railway.app`)
- **API Key**: Your site's API key from the Order Hub dashboard
- **API Secret**: Your site's API secret from the Order Hub dashboard
- **Debug Logging**: Enable to log API requests/responses to `debug.log`

### 2. Getting API Credentials

1. Go to your Order Hub dashboard
2. Navigate to Sites → Create New Site
3. Enter your site details
4. Copy the generated API Key and API Secret
5. Paste them into the plugin settings

## How It Works

### Order Synchronization

The plugin automatically sends orders to Order Hub when:

- A new order is created (`woocommerce_checkout_order_processed`)
- Order status changes (`woocommerce_order_status_changed`)
- Orders are refunded (`woocommerce_order_refunded`)
- Orders are completed (`woocommerce_order_status_completed`)

### Data Sent

Each order includes:

- **Order Details**: ID, status, totals, dates, payment method
- **Customer Information**: Name, email, phone
- **Addresses**: Billing and shipping addresses
- **Items**: Product details, quantities, prices
- **Financials**: Subtotal, tax, shipping, discounts

### Failed Order Handling

- Failed API calls are stored in a custom database table
- Automatic retry system with exponential backoff
- Maximum 3 retry attempts per order
- Manual retry available through admin interface

## Backfill Tool

The backfill tool allows you to send existing orders to Order Hub:

1. **Select Order Statuses**: Choose which order statuses to process
2. **Set Order Limit**: Maximum number of orders to process (1-1000)
3. **Start Backfill**: Process orders with progress tracking
4. **View Results**: Detailed success/failure report

## Debug Logging

When enabled, the plugin logs:

- API requests and responses
- Order processing status
- Error messages and retry attempts
- Connection test results

Logs are written to `wp-content/debug.log` (requires `WP_DEBUG_LOG` to be enabled).

## Troubleshooting

### Common Issues

1. **"Connection failed"**
   - Check your Hub URL is correct and accessible
   - Verify your API key is valid
   - Ensure your site is active in Order Hub

2. **"Authentication failed"**
   - Verify your API key matches exactly
   - Check that your site is active in Order Hub
   - Ensure no extra spaces in API key

3. **Orders not syncing**
   - Check debug logs for error messages
   - Verify WooCommerce hooks are working
   - Test connection using the admin interface

4. **Backfill not working**
   - Ensure you've selected order statuses
   - Check order limit is reasonable
   - Verify API credentials are correct

### Debug Steps

1. **Enable Debug Logging** in plugin settings
2. **Test Connection** using the admin interface
3. **Check WordPress Debug Log** for detailed error messages
4. **Verify WooCommerce** is properly configured
5. **Check Order Hub** dashboard for site status

## API Endpoints

The plugin communicates with Order Hub using these endpoints:

- **POST** `/api/v1/orders/sync` - Send order data
- **Headers**: `X-API-Key: your-api-key`
- **Content-Type**: `application/json`

## Security

- **API Key Authentication**: Secure communication using unique API keys
- **No Sensitive Data**: Only order and customer information is sent
- **HTTPS Required**: Hub URL must use HTTPS
- **Input Validation**: All data is sanitized before processing

## Support

For support and issues:

1. Check the debug logs for error messages
2. Verify your configuration settings
3. Test the connection using the admin interface
4. Check the Order Hub dashboard for site status

## Changelog

### Version 2.0.0
- Complete rewrite for Order Hub API v1
- Improved admin interface with tabs
- Enhanced error handling and retry system
- Better debug logging and troubleshooting
- Streamlined order data format

### Version 1.0.0
- Initial release
- Basic order synchronization
- Simple admin interface

## License

This plugin is licensed under the GPL v2 or later.

## Contributing

Contributions are welcome! Please ensure your code follows WordPress coding standards.

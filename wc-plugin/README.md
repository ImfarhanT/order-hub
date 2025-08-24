# Order Hub Sync - WooCommerce Plugin

This plugin synchronizes WooCommerce orders with your Order Hub for centralized management.

## Installation

1. **Upload the plugin** to your WordPress site:
   - Copy the `order-hub-sync` folder to `/wp-content/plugins/`
   - Or zip the folder and upload via WordPress admin

2. **Activate the plugin** in WordPress admin:
   - Go to Plugins → Order Hub Sync
   - Click "Activate"

## Configuration

1. **Go to WooCommerce → Order Hub Sync** in your WordPress admin

2. **Enter your Order Hub details**:
   - **Hub API Base URL**: `https://order-hub-production.up.railway.app`
   - **Site API Key**: Get this from your Order Hub dashboard
   - **Site API Secret**: Get this from your Order Hub dashboard

3. **Test the connection** using the "Test Connection" button

4. **Save settings**

## How It Works

- **New Orders**: Automatically syncs when customers place orders
- **Status Updates**: Syncs when order status changes
- **Real-time**: Orders appear in your Order Hub dashboard immediately
- **Secure**: Uses HMAC-SHA256 signatures for authentication

## API Endpoints Used

- `POST /api/orders` - Send new orders
- `POST /api/shipping` - Send shipping updates

## Troubleshooting

- **Check your API credentials** in the Order Hub dashboard
- **Enable debug logging** to see detailed error messages
- **Verify your Hub URL** is correct and accessible
- **Check WordPress debug log** for error details

## Support

If you encounter issues:
1. Check the debug log
2. Verify your API credentials
3. Test the connection using the test button
4. Ensure your Order Hub is running and accessible

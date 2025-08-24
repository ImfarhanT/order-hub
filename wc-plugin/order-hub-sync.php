<?php
/**
 * Plugin Name: Order Hub Sync
 * Plugin URI: https://github.com/your-org/order-hub
 * Description: Synchronize WooCommerce orders with Order Hub for centralized management
 * Version: 1.0.0
 * Author: Order Hub Team
 * License: GPL v2 or later
 * Text Domain: order-hub-sync
 * Domain Path: /languages
 * Requires at least: 5.0
 * Tested up to: 6.4
 * WC requires at least: 5.0
 * WC tested up to: 8.0
 */

// Prevent direct access
if (!defined('ABSPATH')) {
    exit;
}

// Define plugin constants
define('OHS_VERSION', '1.0.0');
define('OHS_PLUGIN_DIR', plugin_dir_path(__FILE__));
define('OHS_PLUGIN_URL', plugin_dir_url(__FILE__));

// Include required files
require_once OHS_PLUGIN_DIR . 'includes/class-ohs-admin.php';
require_once OHS_PLUGIN_DIR . 'includes/class-ohs-client.php';
require_once OHS_PLUGIN_DIR . 'includes/class-ohs-hooks.php';

/**
 * Main plugin class
 */
class OrderHubSync
{
    /**
     * Plugin instance
     */
    private static $instance = null;

    /**
     * Get plugin instance
     */
    public static function get_instance()
    {
        if (null === self::$instance) {
            self::$instance = new self();
        }
        return self::$instance;
    }

    /**
     * Constructor
     */
    private function __construct()
    {
        add_action('plugins_loaded', array($this, 'init'));
    }

    /**
     * Initialize plugin
     */
    public function init()
    {
        // Check if WooCommerce is active
        if (!class_exists('WooCommerce')) {
            add_action('admin_notices', array($this, 'woocommerce_missing_notice'));
            return;
        }

        // Initialize components
        new OHS_Admin();
        new OHS_Hooks();
    }

    /**
     * WooCommerce missing notice
     */
    public function woocommerce_missing_notice()
    {
        echo '<div class="notice notice-error"><p>';
        echo __('Order Hub Sync requires WooCommerce to be installed and activated.', 'order-hub-sync');
        echo '</p></div>';
    }
}

// Initialize plugin
OrderHubSync::get_instance();

// Activation hook
register_activation_hook(__FILE__, 'ohs_activate');
function ohs_activate()
{
    // Add default options
    add_option('ohs_hub_url', '');
    add_option('ohs_api_key', '');
    add_option('ohs_api_secret', '');
    add_option('ohs_debug_log', false);
    add_option('ohs_gateway_fees', array());
}

// Deactivation hook
register_deactivation_hook(__FILE__, 'ohs_deactivate');
function ohs_deactivate()
{
    // Clear scheduled events
    wp_clear_scheduled_hook('ohs_process_failed_orders');
}

// Uninstall hook
register_uninstall_hook(__FILE__, 'ohs_uninstall');
function ohs_uninstall()
{
    // Remove options
    delete_option('ohs_hub_url');
    delete_option('ohs_api_key');
    delete_option('ohs_api_secret');
    delete_option('ohs_debug_log');
    delete_option('ohs_gateway_fees');
    delete_option('ohs_failed_orders');
}

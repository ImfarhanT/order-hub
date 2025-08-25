<?php
/**
 * Plugin Name: Order Hub Sync
 * Plugin URI: https://github.com/ImfarhanT/order-hub
 * Description: Syncs WooCommerce orders with Order Hub for centralized order management
 * Version: 2.0.0
 * Author: Farhan
 * License: GPL v2 or later
 * Text Domain: order-hub-sync
 * Requires at least: 5.0
 * Tested up to: 6.8
 * WC requires at least: 5.0
 * WC tested up to: 8.0
 */

// Prevent direct access
if (!defined('ABSPATH')) {
    exit;
}

// Define plugin constants
define('OHS_VERSION', '2.0.0');
define('OHS_PLUGIN_URL', plugin_dir_url(__FILE__));
define('OHS_PLUGIN_PATH', plugin_dir_path(__FILE__));

// Check if WooCommerce is active
function ohs_check_woocommerce() {
    if (!class_exists('WooCommerce')) {
        add_action('admin_notices', function() {
            echo '<div class="notice notice-error"><p>Order Hub Sync requires WooCommerce to be installed and activated.</p></div>';
        });
        return false;
    }
    return true;
}

// Initialize plugin
function ohs_init() {
    if (!ohs_check_woocommerce()) {
        return;
    }
    
    // Load plugin classes
    require_once OHS_PLUGIN_PATH . 'includes/class-ohs-admin.php';
    require_once OHS_PLUGIN_PATH . 'includes/class-ohs-client.php';
    require_once OHS_PLUGIN_PATH . 'includes/class-ohs-hooks.php';
    
    // Initialize admin
    if (is_admin()) {
        new OHS_Admin();
    }
    
    // Initialize hooks
    new OHS_Hooks();
}
add_action('plugins_loaded', 'ohs_init');

// Activation hook
register_activation_hook(__FILE__, 'ohs_activate');
function ohs_activate() {
    // Create custom table for failed orders
    global $wpdb;
    
    $table_name = $wpdb->prefix . 'ohs_failed_orders';
    $charset_collate = $wpdb->get_charset_collate();
    
    $sql = "CREATE TABLE $table_name (
        id mediumint(9) NOT NULL AUTO_INCREMENT,
        order_id bigint(20) NOT NULL,
        site_id bigint(20) NOT NULL,
        payload longtext NOT NULL,
        error_message text,
        retry_count int(11) DEFAULT 0,
        next_retry datetime DEFAULT NULL,
        created_at datetime DEFAULT CURRENT_TIMESTAMP,
        PRIMARY KEY (id),
        KEY order_id (order_id),
        KEY next_retry (next_retry)
    ) $charset_collate;";
    
    require_once(ABSPATH . 'wp-admin/includes/upgrade.php');
    dbDelta($sql);
    
    // Set default options
    add_option('ohs_hub_url', '');
    add_option('ohs_api_key', '');
    add_option('ohs_api_secret', '');
    add_option('ohs_debug_log', false);
    add_option('ohs_gateway_fees', array());
}

// Deactivation hook
register_deactivation_hook(__FILE__, 'ohs_deactivate');
function ohs_deactivate() {
    // Clear any scheduled events
    wp_clear_scheduled_hook('ohs_process_failed_orders');
}

// Uninstall hook
register_uninstall_hook(__FILE__, 'ohs_uninstall');
function ohs_uninstall() {
    // Remove options
    delete_option('ohs_hub_url');
    delete_option('ohs_api_key');
    delete_option('ohs_api_secret');
    delete_option('ohs_debug_log');
    delete_option('ohs_gateway_fees');
    
    // Drop custom table
    global $wpdb;
    $wpdb->query("DROP TABLE IF EXISTS {$wpdb->prefix}ohs_failed_orders");
}

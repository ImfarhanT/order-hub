<?php
/**
 * Uninstall script for Order Hub Sync
 * 
 * This file is executed when the plugin is deleted from WordPress
 */

// If uninstall not called from WordPress, exit
if (!defined('WP_UNINSTALL_PLUGIN')) {
    exit;
}

// Remove plugin options
delete_option('ohs_hub_url');
delete_option('ohs_api_key');
delete_option('ohs_api_secret');
delete_option('ohs_debug_log');
delete_option('ohs_gateway_fees');

// Remove custom database table
global $wpdb;
$wpdb->query("DROP TABLE IF EXISTS {$wpdb->prefix}ohs_failed_orders");

// Clear any scheduled events
wp_clear_scheduled_hook('ohs_process_failed_orders');

// Log uninstall for debugging
if (defined('WP_DEBUG') && WP_DEBUG) {
    error_log('Order Hub Sync plugin uninstalled and cleaned up');
}

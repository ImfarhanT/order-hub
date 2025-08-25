<?php
/**
 * WooCommerce hooks integration for Order Hub Sync
 */

if (!defined('ABSPATH')) {
    exit;
}

class OHS_Hooks {
    
    private $client;
    
    public function __construct() {
        $this->client = new OHS_Client();
        
        // Hook into order creation and status changes
        add_action('woocommerce_checkout_order_processed', array($this, 'on_order_created'), 10, 1);
        add_action('woocommerce_order_status_changed', array($this, 'on_order_status_changed'), 10, 4);
        
        // Hook into order updates
        add_action('woocommerce_order_refunded', array($this, 'on_order_refunded'), 10, 2);
        add_action('woocommerce_order_status_completed', array($this, 'on_order_completed'), 10, 1);
        
        // Schedule failed order processing
        add_action('init', array($this, 'schedule_failed_order_processing'));
        add_action('ohs_process_failed_orders', array($this, 'process_failed_orders'));
    }
    
    /**
     * Handle new order creation
     */
    public function on_order_created($order_id) {
        $order = wc_get_order($order_id);
        if (!$order) {
            return;
        }
        
        $this->log("New order created: {$order->get_order_number()}");
        
        // Send order to Order Hub
        $result = $this->client->send_order($order);
        
        if (!$result['success']) {
            $this->log("Failed to send new order {$order->get_order_number()}: {$result['error']}");
        }
    }
    
    /**
     * Handle order status changes
     */
    public function on_order_status_changed($order_id, $old_status, $new_status, $order) {
        if (!$order) {
            return;
        }
        
        $this->log("Order {$order->get_order_number()} status changed from {$old_status} to {$new_status}");
        
        // Send updated order to Order Hub
        $result = $this->client->send_order($order);
        
        if (!$result['success']) {
            $this->log("Failed to send order status update {$order->get_order_number()}: {$result['error']}");
        }
    }
    
    /**
     * Handle order refunds
     */
    public function on_order_refunded($order_id, $refund_id) {
        $order = wc_get_order($order_id);
        if (!$order) {
            return;
        }
        
        $this->log("Order {$order->get_order_number()} was refunded");
        
        // Send updated order to Order Hub
        $result = $this->client->send_order($order);
        
        if (!$result['success']) {
            $this->log("Failed to send refunded order {$order->get_order_number()}: {$result['error']}");
        }
    }
    
    /**
     * Handle order completion
     */
    public function on_order_completed($order_id) {
        $order = wc_get_order($order_id);
        if (!$order) {
            return;
        }
        
        $this->log("Order {$order->get_order_number()} was completed");
        
        // Send updated order to Order Hub
        $result = $this->client->send_order($order);
        
        if (!$result['success']) {
            $this->log("Failed to send completed order {$order->get_order_number()}: {$result['error']}");
        }
    }
    
    /**
     * Schedule failed order processing
     */
    public function schedule_failed_order_processing() {
        if (!wp_next_scheduled('ohs_process_failed_orders')) {
            wp_schedule_event(time(), 'hourly', 'ohs_process_failed_orders');
        }
    }
    
    /**
     * Process failed orders for retry
     */
    public function process_failed_orders() {
        global $wpdb;
        
        $table_name = $wpdb->prefix . 'ohs_failed_orders';
        
        // Get failed orders that are ready for retry
        $failed_orders = $wpdb->get_results($wpdb->prepare(
            "SELECT * FROM {$table_name} 
             WHERE next_retry <= %s AND retry_count < 3
             ORDER BY next_retry ASC
             LIMIT 10",
            current_time('mysql')
        ));
        
        if (empty($failed_orders)) {
            return;
        }
        
        $this->log("Processing " . count($failed_orders) . " failed orders for retry");
        
        foreach ($failed_orders as $failed_order) {
            $this->retry_failed_order($failed_order);
        }
    }
    
    /**
     * Retry a failed order
     */
    private function retry_failed_order($failed_order) {
        global $wpdb;
        
        $table_name = $wpdb->prefix . 'ohs_failed_orders';
        $order = wc_get_order($failed_order->order_id);
        
        if (!$order) {
            // Order no longer exists, remove from failed orders
            $wpdb->delete($table_name, array('id' => $failed_order->id), array('%d'));
            return;
        }
        
        $this->log("Retrying failed order {$order->get_order_number()} (attempt " . ($failed_order->retry_count + 1) . ")");
        
        // Try to send the order again
        $result = $this->client->send_order($order);
        
        if ($result['success']) {
            // Success! Remove from failed orders
            $wpdb->delete($table_name, array('id' => $failed_order->id), array('%d'));
            $this->log("Successfully retried order {$order->get_order_number()}");
        } else {
            // Still failed, update retry count and next retry time
            $retry_count = $failed_order->retry_count + 1;
            $next_retry = date('Y-m-d H:i:s', strtotime('+' . ($retry_count * 2) . ' hours'));
            
            $wpdb->update(
                $table_name,
                array(
                    'retry_count' => $retry_count,
                    'next_retry' => $next_retry,
                    'error_message' => $result['error']
                ),
                array('id' => $failed_order->id),
                array('%d', '%s', '%s'),
                array('%d')
            );
            
            $this->log("Failed to retry order {$order->get_order_number()}: {$result['error']}");
        }
    }
    
    /**
     * Log message if debug logging is enabled
     */
    private function log($message) {
        if (get_option('ohs_debug_log')) {
            error_log('[Order Hub Sync Hooks] ' . $message);
        }
    }
}

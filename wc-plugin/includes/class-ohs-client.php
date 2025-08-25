<?php
/**
 * Client class for communicating with Order Hub API
 */

if (!defined('ABSPATH')) {
    exit;
}

class OHS_Client {
    
    private $hub_url;
    private $api_key;
    private $api_secret;
    private $debug_log;
    
    public function __construct() {
        $this->hub_url = get_option('ohs_hub_url');
        $this->api_key = get_option('ohs_api_key');
        $this->api_secret = get_option('ohs_api_secret');
        $this->debug_log = get_option('ohs_debug_log');
    }
    
    /**
     * Test connection to Order Hub
     */
    public function test_connection() {
        if (empty($this->hub_url) || empty($this->api_key)) {
            return array(
                'success' => false,
                'message' => 'Hub URL and API Key are required'
            );
        }
        
        // Test basic connectivity first - try the root URL
        $test_url = trailingslashit($this->hub_url);
        
        $response = wp_remote_get($test_url, array(
            'timeout' => 30,
            'headers' => array(
                'Content-Type' => 'application/json',
                'X-API-Key' => $this->api_key
            )
        ));
        
        if (is_wp_error($response)) {
            $this->log('Test connection failed: ' . $response->get_error_message());
            return array(
                'success' => false,
                'message' => 'Connection failed: ' . $response->get_error_message()
            );
        }
        
        $status_code = wp_remote_retrieve_response_code($response);
        $this->log("Test connection response: HTTP $status_code");
        
        if ($status_code === 200) {
            $this->log('Test connection successful - Hub is reachable');
            return array('success' => true, 'message' => 'Hub is reachable and responding');
        } elseif ($status_code === 401) {
            $this->log('Test connection failed: Authentication failed');
            return array(
                'success' => false,
                'message' => 'Authentication failed. Check your API key.'
            );
        } elseif ($status_code === 404) {
            $this->log('Test connection: Endpoint not found, but Hub is reachable');
            return array(
                'success' => true, 
                'message' => 'Hub is reachable! API endpoints may not be fully configured yet.'
            );
        } else {
            $this->log("Test connection failed with status: $status_code");
            return array(
                'success' => false,
                'message' => "Connection failed with status code: $status_code"
            );
        }
    }
    
    /**
     * Send order to Order Hub
     */
    public function send_order($order) {
        if (!$order instanceof WC_Order) {
            return array(
                'success' => false,
                'error' => 'Invalid order object'
            );
        }
        
        $payload = $this->build_order_payload($order);
        
        // Send order to Order Hub API
        $response = wp_remote_post($this->get_api_url('orders/sync'), array(
            'timeout' => 30,
            'headers' => array(
                'Content-Type' => 'application/json',
                'X-API-Key' => $this->api_key
            ),
            'body' => json_encode($payload)
        ));
        
        if (is_wp_error($response)) {
            $error_msg = $response->get_error_message();
            $this->log("Failed to send order {$order->get_order_number()}: $error_msg");
            $this->store_failed_order($order->get_id(), $payload, $error_msg);
            
            return array(
                'success' => false,
                'error' => $error_msg
            );
        }
        
        $status_code = wp_remote_retrieve_response_code($response);
        $body = wp_remote_retrieve_body($response);
        
        if ($status_code === 200) {
            $this->log("Order {$order->get_order_number()} sent successfully to Order Hub");
            return array('success' => true, 'message' => 'Order synced to Order Hub successfully');
        } else {
            $this->log("Failed to send order {$order->get_order_number()}: Status $status_code, Response: $body");
            $this->store_failed_order($order->get_id(), $payload, "HTTP $status_code: $body");
            
            return array(
                'success' => false,
                'error' => "HTTP $status_code: $body"
            );
        }
    }
    
    /**
     * Backfill existing orders
     */
    public function backfill_orders($statuses, $limit = 50) {
        $args = array(
            'status' => $statuses,
            'limit' => $limit,
            'orderby' => 'date',
            'order' => 'DESC'
        );
        
        $orders = wc_get_orders($args);
        
        if (empty($orders)) {
            return array(
                'success' => false,
                'message' => 'No orders found matching the selected criteria.'
            );
        }
        
        $success_count = 0;
        $failed_count = 0;
        $results = array();
        
        $this->log("Starting backfill for " . count($orders) . " orders with statuses: " . implode(', ', $statuses));
        
        foreach ($orders as $order) {
            $result = $this->send_order($order);
            
            if ($result['success']) {
                $success_count++;
                $results[] = array(
                    'order_id' => $order->get_order_number(),
                    'status' => 'success',
                    'message' => $result['message'] ?? 'Order processed successfully'
                );
            } else {
                $failed_count++;
                $results[] = array(
                    'order_id' => $order->get_order_number(),
                    'status' => 'failed',
                    'error' => $result['error']
                );
            }
            
            // Small delay to avoid overwhelming the system
            usleep(100000); // 0.1 second
        }
        
        $summary = array(
            'total' => count($orders),
            'success' => $success_count,
            'failed' => $failed_count,
            'statuses' => implode(', ', $statuses)
        );
        
        $this->log("Backfill completed: {$success_count} successful, {$failed_count} failed out of {$summary['total']} total");
        
        return array(
            'success' => true,
            'summary' => $summary,
            'results' => $results
        );
    }
    
    /**
     * Build order payload for API
     */
    private function build_order_payload($order) {
        $items = array();
        foreach ($order->get_items() as $item) {
            $items[] = array(
                'product_id' => $item->get_product_id(),
                'sku' => $item->get_product()->get_sku(),
                'name' => $item->get_name(),
                'qty' => $item->get_quantity(),
                'price' => $item->get_total() / $item->get_quantity(),
                'subtotal' => $item->get_subtotal(),
                'total' => $item->get_total()
            );
        }
        
        $payload = array(
            'api_key' => $this->api_key,
            'order' => array(
                'wc_order_id' => $order->get_order_number(),
                'status' => $order->get_status(),
                'currency' => $order->get_currency(),
                'order_total' => $order->get_total(),
                'subtotal' => $order->get_subtotal(),
                'discount_total' => $order->get_total_discount(),
                'shipping_total' => $order->get_shipping_total(),
                'tax_total' => $order->get_total_tax(),
                'payment_gateway_code' => $order->get_payment_method(),
                'customer_name' => $order->get_formatted_billing_full_name(),
                'customer_email' => $order->get_billing_email(),
                'customer_phone' => $order->get_billing_phone(),
                'shipping_address' => $this->format_address($order->get_address('shipping')),
                'billing_address' => $this->format_address($order->get_address('billing')),
                'placed_at' => $order->get_date_created()->format('c')
            ),
            'items' => $items
        );
        
        return $payload;
    }
    
    /**
     * Format address for API
     */
    private function format_address($address) {
        if (empty($address)) {
            return null;
        }
        
        return array(
            'first_name' => $address['first_name'] ?? '',
            'last_name' => $address['last_name'] ?? '',
            'company' => $address['company'] ?? '',
            'address_1' => $address['address_1'] ?? '',
            'address_2' => $address['address_2'] ?? '',
            'city' => $address['city'] ?? '',
            'state' => $address['state'] ?? '',
            'postcode' => $address['postcode'] ?? '',
            'country' => $address['country'] ?? ''
        );
    }
    
    /**
     * Get full API URL
     */
    private function get_api_url($endpoint) {
        return trailingslashit($this->hub_url) . 'api/v1/' . $endpoint;
    }
    
    /**
     * Store failed order for retry
     */
    private function store_failed_order($order_id, $payload, $error_message) {
        global $wpdb;
        
        $table_name = $wpdb->prefix . 'ohs_failed_orders';
        
        $wpdb->insert(
            $table_name,
            array(
                'order_id' => $order_id,
                'site_id' => get_current_blog_id(),
                'payload' => json_encode($payload),
                'error_message' => $error_message,
                'retry_count' => 0,
                'next_retry' => date('Y-m-d H:i:s', strtotime('+1 hour'))
            ),
            array('%d', '%d', '%s', '%s', '%d', '%s')
        );
    }

    /**
     * Store order data locally for future processing
     */
    private function store_order_locally($order_id, $payload) {
        global $wpdb;
        
        $table_name = $wpdb->prefix . 'ohs_local_orders';
        
        // Create table if it doesn't exist
        $this->create_local_orders_table();
        
        $wpdb->insert(
            $table_name,
            array(
                'order_id' => $order_id,
                'site_id' => get_current_blog_id(),
                'payload' => json_encode($payload),
                'created_at' => current_time('mysql')
            ),
            array('%d', '%d', '%s', '%s')
        );
        
        if ($wpdb->last_error) {
            $this->log("Failed to store order locally: " . $wpdb->last_error);
        } else {
            $this->log("Order {$order_id} stored locally successfully");
        }
    }
    
    /**
     * Create local orders table if it doesn't exist
     */
    private function create_local_orders_table() {
        global $wpdb;
        
        $table_name = $wpdb->prefix . 'ohs_local_orders';
        
        // Check if table exists
        $table_exists = $wpdb->get_var("SHOW TABLES LIKE '$table_name'") == $table_name;
        
        if (!$table_exists) {
            $charset_collate = $wpdb->get_charset_collate();
            
            $sql = "CREATE TABLE $table_name (
                id mediumint(9) NOT NULL AUTO_INCREMENT,
                order_id bigint(20) NOT NULL,
                site_id bigint(20) NOT NULL,
                payload longtext NOT NULL,
                created_at datetime DEFAULT CURRENT_TIMESTAMP,
                synced_at datetime NULL,
                PRIMARY KEY (id),
                KEY order_id (order_id),
                KEY site_id (site_id),
                KEY created_at (created_at)
            ) $charset_collate;";
            
            require_once(ABSPATH . 'wp-admin/includes/upgrade.php');
            dbDelta($sql);
            
            $this->log("Local orders table created successfully");
        }
    }
    
    /**
     * Log message if debug logging is enabled
     */
    private function log($message) {
        if ($this->debug_log) {
            // Use the new admin logging system
            if (class_exists('OHS_Admin')) {
                OHS_Admin::add_log('INFO', $message, 'client');
            }
            // Also log to WordPress debug log as backup
            error_log('[Order Hub Sync] ' . $message);
        }
    }
}

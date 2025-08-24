<?php
/**
 * Hooks class for WooCommerce integration
 */

if (!defined('ABSPATH')) {
    exit;
}

class OHS_Hooks
{
    /**
     * Client instance
     */
    private $client;

    /**
     * Constructor
     */
    public function __construct()
    {
        $this->client = new OHS_Client();

        // Hook into WooCommerce events
        add_action('woocommerce_checkout_order_processed', array($this, 'on_order_created'), 10, 1);
        add_action('woocommerce_order_status_changed', array($this, 'on_order_status_changed'), 10, 3);
        
        // AJAX handlers
        add_action('wp_ajax_ohs_backfill_orders', array($this, 'ajax_backfill_orders'));
        
        // Schedule failed order processing
        add_action('init', array($this, 'schedule_failed_order_processing'));
        add_action('ohs_process_failed_orders', array($this, 'process_failed_orders'));
    }

    /**
     * Handle new order creation
     */
    public function on_order_created($order_id)
    {
        if (!$this->client->is_configured()) {
            return;
        }

        // Add a small delay to ensure order is fully saved
        wp_schedule_single_event(time() + 5, 'ohs_send_order', array($order_id));
    }

    /**
     * Handle order status changes
     */
    public function on_order_status_changed($order_id, $old_status, $new_status)
    {
        if (!$this->client->is_configured()) {
            return;
        }

        // Send order update
        $this->client->send_order($order_id);

        // Send shipping update for certain statuses
        if (in_array($new_status, array('processing', 'completed', 'shipped'))) {
            $provider = '';
            $tracking_number = '';
            
            // Try to get tracking info from order meta
            $order = wc_get_order($order_id);
            if ($order) {
                $provider = $order->get_meta('_tracking_provider') ?: '';
                $tracking_number = $order->get_meta('_tracking_number') ?: '';
            }

            $this->client->send_shipping_update($order_id, $new_status, $provider, $tracking_number);
        }
    }

    /**
     * AJAX handler for backfill orders
     */
    public function ajax_backfill_orders()
    {
        // Verify nonce
        if (!wp_verify_nonce($_POST['nonce'], 'ohs_nonce')) {
            wp_die('Invalid nonce');
        }

        // Check permissions
        if (!current_user_can('manage_woocommerce')) {
            wp_die('Insufficient permissions');
        }

        $count = intval($_POST['count']);
        $count = min(max($count, 1), 1000); // Limit to 1-1000

        $processed = 0;
        $failed = 0;

        // Get recent orders
        $orders = wc_get_orders(array(
            'limit' => $count,
            'orderby' => 'date',
            'order' => 'DESC',
            'status' => array('processing', 'completed', 'on-hold')
        ));

        foreach ($orders as $order) {
            if ($this->client->send_order($order->get_id())) {
                $processed++;
            } else {
                $failed++;
            }

            // Small delay to avoid overwhelming the API
            usleep(100000); // 0.1 second
        }

        $message = sprintf(
            'Backfill completed. Processed: %d, Failed: %d',
            $processed,
            $failed
        );

        wp_send_json_success(array('message' => $message));
    }

    /**
     * Schedule failed order processing
     */
    public function schedule_failed_order_processing()
    {
        if (!wp_next_scheduled('ohs_process_failed_orders')) {
            wp_schedule_event(time(), 'hourly', 'ohs_process_failed_orders');
        }
    }

    /**
     * Process failed orders
     */
    public function process_failed_orders()
    {
        if (!$this->client->is_configured()) {
            return;
        }

        $this->client->process_failed_orders();
    }

    /**
     * Send order (scheduled event handler)
     */
    public function send_order($order_id)
    {
        if ($this->client->is_configured()) {
            $this->client->send_order($order_id);
        }
    }
}

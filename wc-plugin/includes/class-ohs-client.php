<?php
/**
 * Client class for Order Hub API communication
 */

if (!defined('ABSPATH')) {
    exit;
}

class OHS_Client
{
    /**
     * Hub URL
     */
    private $hub_url;

    /**
     * API Key
     */
    private $api_key;

    /**
     * API Secret
     */
    private $api_secret;

    /**
     * Constructor
     */
    public function __construct()
    {
        $this->hub_url = get_option('ohs_hub_url');
        $this->api_key = get_option('ohs_api_key');
        $this->api_secret = get_option('ohs_api_secret');
    }

    /**
     * Check if client is configured
     */
    public function is_configured()
    {
        return !empty($this->hub_url) && !empty($this->api_key) && !empty($this->api_secret);
    }

    /**
     * Send order to hub
     */
    public function send_order($order_id)
    {
        if (!$this->is_configured()) {
            $this->log('Client not configured');
            return false;
        }

        $order = wc_get_order($order_id);
        if (!$order) {
            $this->log('Order not found: ' . $order_id);
            return false;
        }

        $payload = $this->build_order_payload($order);
        $endpoint = $this->hub_url . '/api/v1/orders/sync';

        $result = $this->make_request($endpoint, $payload);
        
        if ($result) {
            $this->log('Order sent successfully: ' . $order_id);
            return true;
        } else {
            $this->log('Failed to send order: ' . $order_id);
            $this->store_failed_order($order_id, $payload);
            return false;
        }
    }

    /**
     * Send shipping update to hub
     */
    public function send_shipping_update($order_id, $status, $provider = '', $tracking_number = '', $payload = array())
    {
        if (!$this->is_configured()) {
            $this->log('Client not configured');
            return false;
        }

        $order = wc_get_order($order_id);
        if (!$order) {
            $this->log('Order not found: ' . $order_id);
            return false;
        }

        $update_data = array(
            'site_api_key' => $this->api_key,
            'nonce' => wp_generate_uuid4(),
            'timestamp' => time(),
            'wc_order_id' => $order->get_order_number(),
            'status' => $status,
            'provider' => $provider,
            'tracking_number' => $tracking_number,
            'payload' => $payload,
            'occurred_at' => gmdate('c')
        );

        // Build signature
        $signature_base = $this->api_key . '|' . $update_data['timestamp'] . '|' . $update_data['nonce'] . '|' . $update_data['wc_order_id'] . '|0';
        $update_data['signature'] = $this->compute_signature($signature_base, $this->api_secret);

        $endpoint = $this->hub_url . '/api/v1/shipping/update';

        $result = $this->make_request($endpoint, $update_data);
        
        if ($result) {
            $this->log('Shipping update sent successfully: ' . $order_id);
            return true;
        } else {
            $this->log('Failed to send shipping update: ' . $order_id);
            return false;
        }
    }

    /**
     * Build order payload
     */
    private function build_order_payload($order)
    {
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

        $order_data = array(
            'site_api_key' => $this->api_key,
            'nonce' => wp_generate_uuid4(),
            'timestamp' => time(),
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
                'placed_at' => gmdate('c', $order->get_date_created()->getTimestamp())
            ),
            'items' => $items
        );

        // Add gateway fee if configured
        $gateway_fees = get_option('ohs_gateway_fees', array());
        $payment_method = $order->get_payment_method();
        if (isset($gateway_fees[$payment_method])) {
            $order_data['gateway_fee_percent'] = $gateway_fees[$payment_method];
        }

        // Build signature
        $signature_base = $this->api_key . '|' . $order_data['timestamp'] . '|' . $order_data['nonce'] . '|' . $order_data['order']['wc_order_id'] . '|' . $order_data['order']['order_total'];
        $order_data['signature'] = $this->compute_signature($signature_base, $this->api_secret);

        return $order_data;
    }

    /**
     * Format address for JSON
     */
    private function format_address($address)
    {
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
     * Compute HMAC signature
     */
    private function compute_signature($data, $secret)
    {
        return base64_encode(hash_hmac('sha256', $data, $secret, true));
    }

    /**
     * Make HTTP request
     */
    private function make_request($url, $data)
    {
        $args = array(
            'method' => 'POST',
            'timeout' => 30,
            'redirection' => 5,
            'httpversion' => '1.1',
            'blocking' => true,
            'headers' => array(
                'Content-Type' => 'application/json',
                'User-Agent' => 'OrderHubSync/' . OHS_VERSION
            ),
            'body' => json_encode($data),
            'cookies' => array()
        );

        $response = wp_remote_post($url, $args);

        if (is_wp_error($response)) {
            $this->log('Request error: ' . $response->get_error_message());
            return false;
        }

        $status_code = wp_remote_retrieve_response_code($response);
        $body = wp_remote_retrieve_body($response);

        if ($status_code !== 200) {
            $this->log('Request failed with status ' . $status_code . ': ' . $body);
            return false;
        }

        $result = json_decode($body, true);
        if (!$result || !isset($result['ok']) || !$result['ok']) {
            $this->log('Invalid response: ' . $body);
            return false;
        }

        return true;
    }

    /**
     * Store failed order for retry
     */
    private function store_failed_order($order_id, $payload)
    {
        $failed_orders = get_option('ohs_failed_orders', array());
        $failed_orders[] = array(
            'order_id' => $order_id,
            'payload' => $payload,
            'timestamp' => time(),
            'retry_count' => 0
        );
        update_option('ohs_failed_orders', $failed_orders);
    }

    /**
     * Process failed orders
     */
    public function process_failed_orders()
    {
        $failed_orders = get_option('ohs_failed_orders', array());
        if (empty($failed_orders)) {
            return;
        }

        $remaining = array();
        foreach ($failed_orders as $failed) {
            if ($failed['retry_count'] >= 3) {
                $this->log('Order ' . $failed['order_id'] . ' failed permanently after 3 retries');
                continue;
            }

            $failed['retry_count']++;
            $endpoint = $this->hub_url . '/api/v1/orders/sync';

            if ($this->make_request($endpoint, $failed['payload'])) {
                $this->log('Failed order ' . $failed['order_id'] . ' processed successfully on retry ' . $failed['retry_count']);
            } else {
                $remaining[] = $failed;
            }
        }

        update_option('ohs_failed_orders', $remaining);
    }

    /**
     * Log message
     */
    private function log($message)
    {
        if (get_option('ohs_debug_log')) {
            error_log('Order Hub Sync: ' . $message);
        }
    }
}

<?php
/**
 * Local Test Script for Order Hub Signature
 * Run this locally to test signature calculation before deploying to WordPress
 */

// Test configuration - use your actual Order Hub credentials
$config = [
    'api_key' => 'a7bfc096b1874856aa75803dd94d0bbb',           // Your actual API key
    'api_secret' => 'ikva1f26pyZN0GTBvMhg6uPfbPYcdoR5',     // Your actual API secret
    'hub_url' => 'https://order-hub-production.up.railway.app'
];

// Test order data (simulating WooCommerce order)
$test_order = [
    'WcOrderId' => 'TEST-827',
    'Status' => 'processing',
    'Currency' => 'USD',
    'OrderTotal' => 99.99,
    'Subtotal' => 89.99,
    'DiscountTotal' => 0.00,
    'ShippingTotal' => 10.00,
    'TaxTotal' => 0.00,
    'PaymentGatewayCode' => 'stripe',
    'CustomerName' => 'Test Customer',
    'CustomerEmail' => 'test@example.com',
    'CustomerPhone' => '+1234567890',
    'ShippingAddress' => [
        'first_name' => 'Test',
        'last_name' => 'Customer',
        'address_1' => '123 Test St',
        'city' => 'Test City',
        'state' => 'TS',
        'postcode' => '12345',
        'country' => 'US'
    ],
    'BillingAddress' => [
        'first_name' => 'Test',
        'last_name' => 'Customer',
        'address_1' => '123 Test St',
        'city' => 'Test City',
        'state' => 'TS',
        'postcode' => '12345',
        'country' => 'US'
    ],
    'PlacedAt' => gmdate('c')
];

// Test order items
$test_items = [
    [
        'ProductId' => '123',
        'Sku' => 'TEST-SKU-123',
        'Name' => 'Test Product',
        'Qty' => 1.0,
        'Price' => 89.99,
        'Subtotal' => 89.99,
        'Total' => 89.99
    ]
];

echo "=== ORDER HUB SIGNATURE TEST ===\n\n";

// Build the payload exactly like your plugin
$order_data = [
    'SiteApiKey' => $config['api_key'],
    'Nonce' => generate_uuid(),
    'Timestamp' => time(),
    'Order' => $test_order,
    'Items' => $test_items
];

echo "1. PAYLOAD BUILT:\n";
echo "SiteApiKey: " . $order_data['SiteApiKey'] . "\n";
echo "Nonce: " . $order_data['Nonce'] . "\n";
echo "Timestamp: " . $order_data['Timestamp'] . " (Unix: " . time() . ")\n";
echo "WcOrderId: " . $order_data['Order']['WcOrderId'] . "\n";
echo "OrderTotal: " . $order_data['Order']['OrderTotal'] . " (Type: " . gettype($order_data['Order']['OrderTotal']) . ")\n\n";

// Build signature base exactly like your Order Hub expects
$signature_base = $order_data['SiteApiKey'] . '|' . $order_data['Timestamp'] . '|' . $order_data['Nonce'] . '|' . $order_data['Order']['WcOrderId'] . '|' . $order_data['Order']['OrderTotal'];

echo "2. SIGNATURE BASE:\n";
echo "Format: SiteApiKey|Timestamp|Nonce|WcOrderId|OrderTotal\n";
echo "Actual: " . $signature_base . "\n\n";

// Compute HMAC signature
$signature = compute_signature($signature_base, $config['api_secret']);

echo "3. SIGNATURE CALCULATION:\n";
echo "API Secret length: " . strlen($config['api_secret']) . "\n";
echo "API Secret preview: " . substr($config['api_secret'], 0, 8) . "...\n";
echo "Computed signature: " . $signature . "\n\n";

// Add signature to payload
$order_data['Signature'] = $signature;

echo "4. FINAL PAYLOAD:\n";
echo json_encode($order_data, JSON_PRETTY_PRINT) . "\n\n";

echo "5. TEST API CALL:\n";
echo "Endpoint: " . $config['hub_url'] . "/api/orders\n";
echo "Method: POST\n";
echo "Content-Type: application/json\n\n";

echo "6. VERIFICATION STEPS:\n";
echo "1. Copy the signature base above\n";
echo "2. Go to your Order Hub dashboard\n";
echo "3. Check if the API key and secret match\n";
echo "4. Try the API call manually (using Postman/curl)\n\n";

echo "=== END TEST ===\n";

/**
 * Generate UUID (simulating wp_generate_uuid4)
 */
function generate_uuid() {
    return sprintf('%04x%04x-%04x-%04x-%04x-%04x%04x%04x',
        mt_rand(0, 0xffff), mt_rand(0, 0xffff),
        mt_rand(0, 0xffff),
        mt_rand(0, 0x3fff) | 0x8000,
        mt_rand(0, 0xffff), mt_rand(0, 0xffff), mt_rand(0, 0xffff), mt_rand(0, 0xffff)
    );
}

/**
 * Compute HMAC signature (exactly like your plugin)
 */
function compute_signature($data, $secret) {
    $hash = hash_hmac('sha256', $data, $secret, true);
    return base64_encode($hash);
}
?>

<?php
/**
 * Manual Signature Test
 * Test individual components of the signature calculation
 */

echo "=== MANUAL SIGNATURE TEST ===\n\n";

// Test with known values
$test_api_key = "test-api-key-123";
$test_timestamp = 1732500000; // Example timestamp
$test_nonce = "test-nonce-456";
$test_order_id = "TEST-827";
$test_order_total = 99.99;

echo "Test Values:\n";
echo "API Key: $test_api_key\n";
echo "Timestamp: $test_timestamp\n";
echo "Nonce: $test_nonce\n";
echo "Order ID: $test_order_id\n";
echo "Order Total: $test_order_total\n\n";

// Build signature base
$signature_base = "$test_api_key|$test_timestamp|$test_nonce|$test_order_id|$test_order_total";
echo "Signature Base: $signature_base\n\n";

// Test with different secrets
$test_secrets = [
    "secret1" => "my-secret-key-123",
    "secret2" => "different-secret-456",
    "secret3" => "test-secret-789"
];

foreach ($test_secrets as $name => $secret) {
    $signature = hash_hmac('sha256', $signature_base, $secret, true);
    $base64_signature = base64_encode($signature);
    
    echo "Secret '$name': $secret\n";
    echo "HMAC-SHA256 (raw): " . bin2hex($signature) . "\n";
    echo "Base64: $base64_signature\n";
    echo "Length: " . strlen($base64_signature) . "\n\n";
}

echo "=== END MANUAL TEST ===\n";
?>

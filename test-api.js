const https = require('https');

// Test data - this simulates what your WooCommerce plugin would send
const testPayload = {
  api_key: "a7bfc096b1874856aa75803dd94d0bbb", // Your actual API key
  order: {
    wc_order_id: "TEST-001",
    status: "processing",
    currency: "USD",
    order_total: 99.99,
    customer_name: "Test Customer",
    customer_email: "test@example.com",
    payment_gateway_code: "stripe"
  },
  items: [
    {
      product_id: "123",
      sku: "TEST-SKU-001",
      name: "Test Product",
      qty: 1,
      price: 99.99,
      total: 99.99
    }
  ]
};

const postData = JSON.stringify(testPayload);

const options = {
  hostname: 'order-hub-production.up.railway.app',
  port: 443,
  path: '/api/v1/orders/sync',
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Content-Length': Buffer.byteLength(postData)
  }
};

console.log('Sending test data to Order Hub...');
console.log('Payload:', JSON.stringify(testPayload, null, 2));

const req = https.request(options, (res) => {
  console.log(`Status: ${res.statusCode}`);
  console.log(`Headers:`, res.headers);

  let data = '';
  res.on('data', (chunk) => {
    data += chunk;
  });

  res.on('end', () => {
    console.log('Response:', data);
    if (res.statusCode === 200) {
      console.log('✅ Success! Check your Order Hub dashboard at:');
      console.log('https://order-hub-production.up.railway.app/Admin/RawData');
    } else {
      console.log('❌ Failed to send data');
    }
  });
});

req.on('error', (e) => {
  console.error(`Request error: ${e.message}`);
});

req.write(postData);
req.end();


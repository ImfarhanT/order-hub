// Test script to check Payment Gateway Details API
console.log('Testing Payment Gateway Details API...');

// Test the v1 endpoint
fetch('/api/v1/paymentgatewaydetails')
    .then(response => {
        console.log('V1 Endpoint Response Status:', response.status);
        console.log('V1 Endpoint Response Headers:', response.headers);
        return response.text();
    })
    .then(data => {
        console.log('V1 Endpoint Response Data:', data);
    })
    .catch(error => {
        console.error('V1 Endpoint Error:', error);
    });

// Test the regular endpoint
fetch('/api/paymentgatewaydetails')
    .then(response => {
        console.log('Regular Endpoint Response Status:', response.status);
        console.log('Regular Endpoint Response Headers:', response.headers);
        return response.text();
    })
    .then(data => {
        console.log('Regular Endpoint Response Data:', data);
    })
    .catch(error => {
        console.error('Regular Endpoint Error:', error);
    });

// Test if we can access the page
console.log('Current page URL:', window.location.href);
console.log('Current page pathname:', window.location.pathname);


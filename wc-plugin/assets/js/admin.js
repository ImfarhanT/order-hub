/**
 * Admin JavaScript for Order Hub Sync
 */

jQuery(document).ready(function($) {
    
    // Test connection functionality
    $('#test-connection').on('click', function() {
        var $btn = $(this);
        var $result = $('#connection-result');
        
        $btn.prop('disabled', true).text('Testing...');
        $result.hide();
        
        $.ajax({
            url: ohs_ajax.ajax_url,
            type: 'POST',
            data: {
                action: 'ohs_test_connection',
                nonce: ohs_ajax.nonce
            },
            success: function(response) {
                if (response.success) {
                    $result.html('<div class="notice notice-success"><p>' + (response.data.message || response.data) + '</p></div>');
                } else {
                    $result.html('<div class="notice notice-error"><p>' + response.data + '</p></div>');
                }
                $result.show();
            },
            error: function() {
                $result.html('<div class="notice notice-error"><p>Connection test failed. Please try again.</p></div>');
                $result.show();
            },
            complete: function() {
                $btn.prop('disabled', false).text('Test Connection');
            }
        });
    });
    
    // Test AJAX functionality
    $('#test-ajax').on('click', function() {
        var $btn = $(this);
        var $result = $('#connection-result');
        
        $btn.prop('disabled', true).text('Testing AJAX...');
        $result.show().html('<div class="notice notice-info"><p>Testing AJAX...</p></div>');
        
        $.ajax({
            url: ohs_ajax.ajax_url,
            type: 'POST',
            data: {
                action: 'ohs_test_ajax',
                nonce: ohs_ajax.nonce
            },
            success: function(response) {
                console.log('Test AJAX response:', response);
                if (response.success) {
                    $result.html('<div class="notice notice-success"><p>' + response.data.message + '</p><p>User ID: ' + response.data.user_id + ', Capabilities: ' + response.data.capabilities + '</p></div>');
                } else {
                    $result.html('<div class="notice notice-error"><p>AJAX test failed: ' + response.data + '</p></div>');
                }
            },
            error: function(xhr, status, error) {
                console.log('Test AJAX error:', xhr, status, error);
                $result.html('<div class="notice notice-error"><p>AJAX test failed with status: ' + xhr.status + ' - ' + error + '</p></div>');
            },
            complete: function() {
                $btn.prop('disabled', false).text('Test AJAX');
            }
        });
    });
    
    // Backfill form submission
    $('#backfill-form').on('submit', function(e) {
        e.preventDefault();
        
        var $form = $(this);
        var $btn = $('#start-backfill');
        var $progress = $('#backfill-progress');
        var $status = $('#backfill-status');
        var $results = $('#backfill-results');
        
        var statuses = $('#backfill_status').val();
        var limit = $('#backfill_limit').val();
        
        if (!statuses || statuses.length === 0) {
            alert('Please select at least one order status.');
            return;
        }
        
        $btn.prop('disabled', true).text('Processing...');
        $progress.show();
        $status.text('Starting backfill...');
        $results.hide();
        
        $.ajax({
            url: ohs_ajax.ajax_url,
            type: 'POST',
            data: {
                action: 'ohs_backfill_orders',
                nonce: ohs_ajax.nonce,
                status: statuses,
                limit: limit
            },
            success: function(response) {
                if (response.success) {
                    var data = response.data;
                    var summary = data.summary;
                    
                    $status.html(`
                        <div class="notice notice-success">
                            <p><strong>Backfill completed!</strong></p>
                            <p>Processed ${summary.total} orders: ${summary.success} successful, ${summary.failed} failed.</p>
                            <p>Statuses: ${summary.statuses}</p>
                        </p>
                    `);
                    
                    // Show detailed results
                    if (data.results && data.results.length > 0) {
                        var resultsHtml = '<h3>Detailed Results</h3><table class="widefat"><thead><tr><th>Order ID</th><th>Status</th><th>Error (if any)</th></tr></thead><tbody>';
                        
                        data.results.forEach(function(result) {
                            var errorCell = result.error ? '<span class="error">' + result.error + '</span>' : '<span class="success">✓</span>';
                            resultsHtml += `<tr><td>${result.order_id}</td><td>${result.status}</td><td>${errorCell}</td></tr>`;
                        });
                        
                        resultsHtml += '</tbody></table>';
                        $results.html(resultsHtml).show();
                    }
                    
                } else {
                    $status.html('<div class="notice notice-error"><p>' + response.data + '</p></div>');
                }
            },
            error: function() {
                $status.html('<div class="notice notice-error"><p>Backfill failed. Please try again.</p></div>');
            },
            complete: function() {
                $btn.prop('disabled', false).text('Start Backfill');
            }
        });
    });
    
    // Form validation
    $('form').on('submit', function() {
        var $form = $(this);
        var isValid = true;
        
        $form.find('input[required], select[required]').each(function() {
            var $field = $(this);
            var value = $field.val();
            
            if (!value || (Array.isArray(value) && value.length === 0)) {
                $field.addClass('error');
                isValid = false;
            } else {
                $field.removeClass('error');
            }
        });
        
        if (!isValid) {
            alert('Please fill in all required fields.');
            return false;
        }
        
        return true;
    });
    
    // Remove error styling on input
    $('input, select').on('input change', function() {
        $(this).removeClass('error');
    });
    
    // Debug logs functionality
    var autoRefreshInterval;
    
    $('#refresh-logs').on('click', function() {
        refreshDebugLogs();
    });
    
    $('#clear-logs').on('click', function() {
        if (confirm('Are you sure you want to clear all debug logs?')) {
            clearDebugLogs();
        }
    });
    
    $('#auto-refresh').on('change', function() {
        if (this.checked) {
            startAutoRefresh();
        } else {
            stopAutoRefresh();
        }
    });
    
    function refreshDebugLogs() {
        $.ajax({
            url: ohs_ajax.ajax_url,
            type: 'POST',
            data: {
                action: 'ohs_refresh_logs',
                nonce: ohs_ajax.nonce
            },
            success: function(response) {
                if (response.success) {
                    displayDebugLogs(response.data);
                    $('#last-update').text(new Date().toLocaleString());
                }
            },
            error: function() {
                $('#debug-logs-content').html('<div class="log-entry error"><span class="message">Failed to refresh logs</span></div>');
            }
        });
    }
    
    function clearDebugLogs() {
        $.ajax({
            url: ohs_ajax.ajax_url,
            type: 'POST',
            data: {
                action: 'ohs_clear_logs',
                nonce: ohs_ajax.nonce
            },
            success: function(response) {
                if (response.success) {
                    $('#debug-logs-content').html('<div class="log-entry"><span class="message">Logs cleared successfully</span></div>');
                    $('#last-update').text(new Date().toLocaleString());
                }
            }
        });
    }
    
    function displayDebugLogs(logs) {
        var html = '';
        if (logs && logs.length > 0) {
            logs.forEach(function(log) {
                var levelClass = log.level.toLowerCase();
                html += '<div class="log-entry ' + levelClass + '">';
                html += '<span class="timestamp">' + log.timestamp + '</span>';
                html += '<span class="level ' + levelClass + '">' + log.level + '</span>';
                html += '<span class="message">' + log.message + '</span>';
                if (log.context) {
                    html += '<span class="context">(' + log.context + ')</span>';
                }
                html += '</div>';
            });
        } else {
            html = '<div class="log-entry"><span class="message">No logs found</span></div>';
        }
        $('#debug-logs-content').html(html);
    }
    
    function startAutoRefresh() {
        autoRefreshInterval = setInterval(refreshDebugLogs, 5000);
    }
    
    function stopAutoRefresh() {
        if (autoRefreshInterval) {
            clearInterval(autoRefreshInterval);
        }
    }
    
    // Start auto-refresh if enabled
    if ($('#auto-refresh').is(':checked')) {
        startAutoRefresh();
    }
    
    // Local Orders Modal functionality
    $('.view-payload').on('click', function() {
        var orderId = $(this).data('order-id');
        showPayloadModal(orderId);
    });
    
    // Close modal when clicking the X
    $('.close').on('click', function() {
        $('#payload-modal').hide();
    });
    
    // Close modal when clicking outside
    $(window).on('click', function(event) {
        if (event.target == $('#payload-modal')[0]) {
            $('#payload-modal').hide();
        }
    });
    
    function showPayloadModal(orderId) {
        // For now, we'll show a placeholder since we need to implement the AJAX call
        $('#payload-content').html('Loading order data...');
        $('#payload-modal').show();
        
        // TODO: Implement AJAX call to get order payload
        // For now, show a message
        setTimeout(function() {
            $('#payload-content').html('Order payload viewing not yet implemented.\n\nThis will show the complete order data that was stored locally for future synchronization with Order Hub.');
        }, 500);
    }
    
});

<?php
/**
 * Admin settings page for Order Hub Sync
 */

if (!defined('ABSPATH')) {
    exit;
}

class OHS_Admin {
    
    public function __construct() {
        add_action('admin_menu', array($this, 'add_admin_menu'));
        add_action('admin_init', array($this, 'init_settings'));
        add_action('wp_ajax_ohs_test_connection', array($this, 'test_connection'));
        add_action('wp_ajax_ohs_backfill_orders', array($this, 'backfill_orders'));
        add_action('wp_ajax_ohs_test_ajax', array($this, 'test_ajax')); // Debug endpoint
        add_action('wp_ajax_ohs_refresh_logs', array($this, 'refresh_logs')); // Debug logs
        add_action('wp_ajax_ohs_clear_logs', array($this, 'clear_logs')); // Clear logs
        add_action('admin_enqueue_scripts', array($this, 'enqueue_scripts'));
    }
    
    public function add_admin_menu() {
        add_submenu_page(
            'woocommerce',
            'Order Hub Sync',
            'Order Hub Sync',
            'manage_woocommerce',
            'order-hub-sync',
            array($this, 'admin_page')
        );
    }
    
    public function init_settings() {
        register_setting('ohs_settings', 'ohs_hub_url');
        register_setting('ohs_settings', 'ohs_api_key');
        register_setting('ohs_settings', 'ohs_api_secret');
        register_setting('ohs_settings', 'ohs_debug_log');
        register_setting('ohs_settings', 'ohs_gateway_fees');
    }
    
    public function enqueue_scripts($hook) {
        if ($hook !== 'woocommerce_page_order-hub-sync') {
            return;
        }
        
        wp_enqueue_script('ohs-admin', OHS_PLUGIN_URL . 'assets/js/admin.js', array('jquery'), OHS_VERSION, true);
        wp_enqueue_style('ohs-admin', OHS_PLUGIN_URL . 'assets/css/admin.css', array(), OHS_VERSION);
        
        wp_localize_script('ohs-admin', 'ohs_ajax', array(
            'ajax_url' => admin_url('admin-ajax.php'),
            'nonce' => wp_create_nonce('ohs_nonce'),
            'strings' => array(
                'testing' => 'Testing connection...',
                'success' => 'Connection successful!',
                'error' => 'Connection failed!',
                'backfilling' => 'Processing orders...',
                'backfill_complete' => 'Backfill completed!'
            )
        ));
    }
    
    public function admin_page() {
        $active_tab = isset($_GET['tab']) ? sanitize_text_field($_GET['tab']) : 'settings';
        
        ?>
        <div class="wrap">
            <h1>Order Hub Sync</h1>
            
            <nav class="nav-tab-wrapper">
                <a href="?page=order-hub-sync&tab=settings" class="nav-tab <?php echo $active_tab === 'settings' ? 'nav-tab-active' : ''; ?>">
                    Settings
                </a>
                <a href="?page=order-hub-sync&tab=backfill" class="nav-tab <?php echo $active_tab === 'backfill' ? 'nav-tab-active' : ''; ?>">
                    Backfill Orders
                </a>
                <a href="?page=order-hub-sync&tab=local-orders" class="nav-tab <?php echo $active_tab === 'local-orders' ? 'nav-tab-active' : ''; ?>">
                    Local Orders
                </a>
                <a href="?page=order-hub-sync&tab=logs" class="nav-tab <?php echo $active_tab === 'logs' ? 'nav-tab-active' : ''; ?>">
                    Debug Logs
                </a>
            </nav>
            
            <div class="tab-content">
                <?php if ($active_tab === 'settings' || $active_tab === ''): ?>
                    <div id="settings-tab" class="tab-pane <?php echo ($active_tab === 'settings' || $active_tab === '') ? 'active' : ''; ?>">
                        <?php $this->settings_tab(); ?>
                    </div>
                <?php endif; ?>
                
                <?php if ($active_tab === 'backfill'): ?>
                    <div id="backfill-tab" class="tab-pane active">
                        <?php $this->backfill_tab(); ?>
                    </div>
                <?php endif; ?>
                
                <?php if ($active_tab === 'local-orders'): ?>
                    <div id="local-orders-tab" class="tab-pane active">
                        <?php $this->local_orders_tab(); ?>
                    </div>
                <?php endif; ?>
                
                <?php if ($active_tab === 'logs'): ?>
                    <div id="logs-tab" class="tab-pane active">
                        <?php $this->logs_tab(); ?>
                    </div>
                <?php endif; ?>
            </div>
        </div>
        <?php
    }
    
    private function settings_tab() {
        ?>
        <div class="card">
            <h2>Hub Configuration</h2>
            <form method="post" action="options.php">
                <?php settings_fields('ohs_settings'); ?>
                
                <table class="form-table">
                    <tr>
                        <th scope="row">
                            <label for="ohs_hub_url">Hub URL</label>
                        </th>
                        <td>
                            <input type="url" id="ohs_hub_url" name="ohs_hub_url" 
                                   value="<?php echo esc_attr(get_option('ohs_hub_url')); ?>" 
                                   class="regular-text" required />
                            <p class="description">The base URL of your Order Hub (e.g., https://your-hub.railway.app)</p>
                        </td>
                    </tr>
                    
                    <tr>
                        <th scope="row">
                            <label for="ohs_api_key">API Key</label>
                        </th>
                        <td>
                            <input type="text" id="ohs_api_key" name="ohs_api_key" 
                                   value="<?php echo esc_attr(get_option('ohs_api_key')); ?>" 
                                   class="regular-text" required />
                            <p class="description">Your site's API key from the Order Hub dashboard</p>
                        </td>
                    </tr>
                    
                    <tr>
                        <th scope="row">
                            <label for="ohs_api_secret">API Secret</label>
                        </th>
                        <td>
                            <input type="password" id="ohs_api_secret" name="ohs_api_secret" 
                                   value="<?php echo esc_attr(get_option('ohs_api_secret')); ?>" 
                                   class="regular-text" required />
                            <p class="description">Your site's API secret from the Order Hub dashboard</p>
                        </td>
                    </tr>
                    
                    <tr>
                        <th scope="row">
                            <label for="ohs_debug_log">Debug Logging</label>
                        </th>
                        <td>
                            <input type="checkbox" id="ohs_debug_log" name="ohs_debug_log" 
                                   value="1" <?php checked(get_option('ohs_debug_log'), '1'); ?> />
                            <label for="ohs_debug_log">Enable debug logging to debug.log</label>
                        </td>
                    </tr>
                </table>
                
                <p class="submit">
                    <input type="submit" name="submit" id="submit" class="button button-primary" value="Save Settings">
                    <button type="button" id="test-connection" class="button button-secondary">Test Connection</button>
                    <button type="button" id="test-ajax" class="button button-secondary">Test AJAX</button>
                </p>
                
                <div id="connection-result" style="display: none;"></div>
            </form>
        </div>
        <?php
    }
    
    private function backfill_tab() {
        ?>
        <div class="card">
            <h2>Backfill Existing Orders</h2>
            <p>Send existing orders to Order Hub. This is useful for initial setup or after configuration changes.</p>
            
            <form id="backfill-form">
                <table class="form-table">
                    <tr>
                        <th scope="row">
                            <label for="backfill_status">Order Status</label>
                        </th>
                        <td>
                            <select id="backfill_status" name="status" multiple>
                                <option value="processing">Processing</option>
                                <option value="completed">Completed</option>
                                <option value="on-hold">On Hold</option>
                                <option value="refunded">Refunded</option>
                                <option value="shipped">Shipped</option>
                                <option value="cancelled">Cancelled</option>
                            </select>
                            <p class="description">Select which order statuses to backfill (hold Ctrl/Cmd to select multiple)</p>
                        </td>
                    </tr>
                    
                    <tr>
                        <th scope="row">
                            <label for="backfill_limit">Order Limit</label>
                        </th>
                        <td>
                            <input type="number" id="backfill_limit" name="limit" value="50" min="1" max="1000" />
                            <p class="description">Maximum number of orders to process (1-1000)</p>
                        </td>
                    </tr>
                </table>
                
                <p class="submit">
                    <button type="submit" id="start-backfill" class="button button-primary">Start Backfill</button>
                </p>
                
                <div id="backfill-progress" style="display: none;">
                    <div class="progress-bar">
                        <div class="progress-fill"></div>
                    </div>
                    <div id="backfill-status"></div>
                </div>
                
                <div id="backfill-results" style="display: none;"></div>
            </form>
        </div>
        <?php
    }
    
    private function local_orders_tab() {
        global $wpdb;
        
        $table_name = $wpdb->prefix . 'ohs_local_orders';
        
        // Get local orders
        $orders = $wpdb->get_results("
            SELECT * FROM {$table_name} 
            ORDER BY created_at DESC 
            LIMIT 100
        ");
        
        $total_count = $wpdb->get_var("SELECT COUNT(*) FROM {$table_name}");
        ?>
        <div class="card">
            <h2>Local Orders</h2>
            <p>Orders stored locally for future synchronization with Order Hub.</p>
            
            <div class="local-orders-stats">
                <p><strong>Total Orders Stored:</strong> <?php echo $total_count; ?></p>
                <p><strong>Orders Displayed:</strong> <?php echo count($orders); ?> (showing last 100)</p>
            </div>
            
            <?php if (!empty($orders)): ?>
                <div class="local-orders-table">
                    <table class="widefat">
                        <thead>
                            <tr>
                                <th>Order ID</th>
                                <th>Site ID</th>
                                <th>Created At</th>
                                <th>Synced At</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            <?php foreach ($orders as $order): ?>
                                <tr>
                                    <td><?php echo esc_html($order->order_id); ?></td>
                                    <td><?php echo esc_html($order->site_id); ?></td>
                                    <td><?php echo esc_html($order->created_at); ?></td>
                                    <td><?php echo $order->synced_at ? esc_html($order->synced_at) : '<em>Not synced</em>'; ?></td>
                                    <td>
                                        <button type="button" class="button button-small view-payload" data-order-id="<?php echo $order->id; ?>">
                                            View Data
                                        </button>
                                    </td>
                                </tr>
                            <?php endforeach; ?>
                        </tbody>
                    </table>
                </div>
                
                <!-- Modal for viewing order payload -->
                <div id="payload-modal" class="modal" style="display: none;">
                    <div class="modal-content">
                        <span class="close">&times;</span>
                        <h3>Order Data</h3>
                        <pre id="payload-content"></pre>
                    </div>
                </div>
            <?php else: ?>
                <p>No orders have been stored locally yet. Run a backfill or create some orders to see them here.</p>
            <?php endif; ?>
        </div>
        <?php
    }
    
    private function logs_tab() {
        ?>
        <div class="card">
            <h2>Debug Logs</h2>
            <p>Real-time debug information from Order Hub Sync operations.</p>
            
            <div class="debug-controls">
                <button type="button" id="refresh-logs" class="button button-secondary">Refresh Logs</button>
                <button type="button" id="clear-logs" class="button button-secondary">Clear Logs</button>
                <label>
                    <input type="checkbox" id="auto-refresh" checked> Auto-refresh every 5 seconds
                </label>
            </div>
            
            <div id="debug-logs-content">
                <div class="log-entry">
                    <span class="timestamp"><?php echo current_time('Y-m-d H:i:s'); ?></span>
                    <span class="level info">INFO</span>
                    <span class="message">Debug logs tab loaded. Click "Refresh Logs" to see recent activity.</span>
                </div>
            </div>
            
            <div class="log-stats">
                <p><strong>Debug Logging Status:</strong> <?php echo get_option('ohs_debug_log') ? 'Enabled' : 'Disabled'; ?></p>
                <p><strong>Last Updated:</strong> <span id="last-update"><?php echo current_time('Y-m-d H:i:s'); ?></span></p>
            </div>
        </div>
        <?php
    }
    
    public function test_connection() {
        // Check nonce and permissions
        if (!check_ajax_referer('ohs_nonce', 'nonce', false)) {
            wp_send_json_error('Security check failed. Please refresh the page and try again.');
        }
        
        if (!current_user_can('manage_woocommerce')) {
            wp_send_json_error('Insufficient permissions to perform this action.');
        }
        
        $hub_url = get_option('ohs_hub_url');
        $api_key = get_option('ohs_api_key');
        
        if (empty($hub_url) || empty($api_key)) {
            wp_send_json_error('Please configure Hub URL and API Key first.');
        }
        
        try {
            $client = new OHS_Client();
            $result = $client->test_connection();
            
            if ($result['success']) {
                wp_send_json_success($result);
            } else {
                wp_send_json_error($result['message']);
            }
        } catch (Exception $e) {
            wp_send_json_error('Connection test error: ' . $e->getMessage());
        }
    }
    
    public function backfill_orders() {
        // Check nonce and permissions
        if (!check_ajax_referer('ohs_nonce', 'nonce', false)) {
            wp_send_json_error('Security check failed. Please refresh the page and try again.');
        }
        
        if (!current_user_can('manage_woocommerce')) {
            wp_send_json_error('Insufficient permissions to perform this action.');
        }
        
        $statuses = isset($_POST['status']) ? array_map('sanitize_text_field', $_POST['status']) : array();
        $limit = isset($_POST['limit']) ? intval($_POST['limit']) : 50;
        
        if (empty($statuses)) {
            wp_send_json_error('Please select at least one order status.');
        }
        
        if ($limit < 1 || $limit > 1000) {
            wp_send_json_error('Order limit must be between 1 and 1000.');
        }
        
        try {
            $client = new OHS_Client();
            $result = $client->backfill_orders($statuses, $limit);
            
            if ($result['success']) {
                wp_send_json_success($result);
            } else {
                wp_send_json_error($result['message'] ?? 'Backfill failed');
            }
        } catch (Exception $e) {
            wp_send_json_error('Backfill error: ' . $e->getMessage());
        }
    }
    
    /**
     * Test AJAX endpoint for debugging
     */
    public function test_ajax() {
        wp_send_json_success(array(
            'message' => 'AJAX is working!',
            'timestamp' => current_time('mysql'),
            'user_id' => get_current_user_id(),
            'capabilities' => current_user_can('manage_woocommerce') ? 'Yes' : 'No'
        ));
    }
    
    /**
     * Refresh debug logs
     */
    public function refresh_logs() {
        if (!check_ajax_referer('ohs_nonce', 'nonce', false)) {
            wp_send_json_error('Security check failed.');
        }
        
        $logs = $this->get_debug_logs();
        wp_send_json_success($logs);
    }
    
    /**
     * Clear debug logs
     */
    public function clear_logs() {
        if (!check_ajax_referer('ohs_nonce', 'nonce', false)) {
            wp_send_json_error('Security check failed.');
        }
        
        delete_option('ohs_debug_logs');
        wp_send_json_success('Logs cleared successfully');
    }
    
    /**
     * Get debug logs
     */
    private function get_debug_logs() {
        $logs = get_option('ohs_debug_logs', array());
        
        // Add some recent activity if logs are empty
        if (empty($logs)) {
            $logs = array(
                array(
                    'timestamp' => current_time('Y-m-d H:i:s'),
                    'level' => 'INFO',
                    'message' => 'No recent debug logs found. Debug logging may be disabled.',
                    'context' => 'system'
                )
            );
        }
        
        return $logs;
    }
    
    /**
     * Add debug log entry
     */
    public static function add_log($level, $message, $context = '') {
        if (!get_option('ohs_debug_log')) {
            return;
        }
        
        $logs = get_option('ohs_debug_logs', array());
        
        $logs[] = array(
            'timestamp' => current_time('Y-m-d H:i:s'),
            'level' => strtoupper($level),
            'message' => $message,
            'context' => $context
        );
        
        // Keep only last 100 log entries
        if (count($logs) > 100) {
            $logs = array_slice($logs, -100);
        }
        
        update_option('ohs_debug_logs', $logs);
    }
}

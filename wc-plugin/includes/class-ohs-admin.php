<?php
/**
 * Admin class for Order Hub Sync
 */

if (!defined('ABSPATH')) {
    exit;
}

class OHS_Admin
{
    /**
     * Constructor
     */
    public function __construct()
    {
        add_action('admin_menu', array($this, 'add_admin_menu'));
        add_action('admin_init', array($this, 'init_settings'));
        add_action('admin_enqueue_scripts', array($this, 'enqueue_scripts'));
    }

    /**
     * Add admin menu
     */
    public function add_admin_menu()
    {
        add_submenu_page(
            'woocommerce',
            __('Order Hub Sync', 'order-hub-sync'),
            __('Order Hub Sync', 'order-hub-sync'),
            'manage_woocommerce',
            'order-hub-sync',
            array($this, 'admin_page')
        );
    }

    /**
     * Initialize settings
     */
    public function init_settings()
    {
        register_setting('ohs_settings', 'ohs_hub_url');
        register_setting('ohs_settings', 'ohs_api_key');
        register_setting('ohs_settings', 'ohs_api_secret');
        register_setting('ohs_settings', 'ohs_debug_log');
        register_setting('ohs_settings', 'ohs_gateway_fees');

        add_settings_section(
            'ohs_general_section',
            __('General Settings', 'order-hub-sync'),
            array($this, 'general_section_callback'),
            'ohs_settings'
        );

        add_settings_field(
            'ohs_hub_url',
            __('Hub API Base URL', 'order-hub-sync'),
            array($this, 'hub_url_callback'),
            'ohs_settings',
            'ohs_general_section'
        );

        add_settings_field(
            'ohs_api_key',
            __('Site API Key', 'order-hub-sync'),
            array($this, 'api_key_callback'),
            'ohs_settings',
            'ohs_general_section'
        );

        add_settings_field(
            'ohs_api_secret',
            __('Site API Secret', 'order-hub-sync'),
            array($this, 'api_secret_callback'),
            'ohs_settings',
            'ohs_general_section'
        );

        add_settings_field(
            'ohs_debug_log',
            __('Enable Debug Logging', 'order-hub-sync'),
            array($this, 'debug_log_callback'),
            'ohs_settings',
            'ohs_general_section'
        );
    }

    /**
     * Enqueue scripts
     */
    public function enqueue_scripts($hook)
    {
        if ('woocommerce_page_order-hub-sync' !== $hook) {
            return;
        }

        wp_enqueue_script('jquery');
        wp_enqueue_script('ohs-admin', OHS_PLUGIN_URL . 'assets/js/admin.js', array('jquery'), OHS_VERSION, true);
        wp_localize_script('ohs-admin', 'ohs_ajax', array(
            'ajax_url' => admin_url('admin-ajax.php'),
            'nonce' => wp_create_nonce('ohs_nonce')
        ));
    }

    /**
     * Admin page
     */
    public function admin_page()
    {
        if (!current_user_can('manage_woocommerce')) {
            wp_die(__('You do not have sufficient permissions to access this page.'));
        }

        ?>
        <div class="wrap">
            <h1><?php echo esc_html(get_admin_page_title()); ?></h1>
            
            <h2 class="nav-tab-wrapper">
                <a href="#settings" class="nav-tab nav-tab-active"><?php _e('Settings', 'order-hub-sync'); ?></a>
                <a href="#backfill" class="nav-tab"><?php _e('Backfill Orders', 'order-hub-sync'); ?></a>
                <a href="#logs" class="nav-tab"><?php _e('Logs', 'order-hub-sync'); ?></a>
            </h2>

            <div id="settings" class="tab-content">
                <form method="post" action="options.php">
                    <?php
                    settings_fields('ohs_settings');
                    do_settings_sections('ohs_settings');
                    submit_button();
                    ?>
                </form>
            </div>

            <div id="backfill" class="tab-content" style="display: none;">
                <h3><?php _e('Backfill Recent Orders', 'order-hub-sync'); ?></h3>
                <p><?php _e('Send recent orders to Order Hub. This is useful for initial setup or after configuration changes.', 'order-hub-sync'); ?></p>
                
                <table class="form-table">
                    <tr>
                        <th scope="row"><?php _e('Number of Orders', 'order-hub-sync'); ?></th>
                        <td>
                            <input type="number" id="backfill_count" name="backfill_count" value="50" min="1" max="1000" />
                            <p class="description"><?php _e('Maximum 1000 orders per backfill operation.', 'order-hub-sync'); ?></p>
                        </td>
                    </tr>
                </table>
                
                <p>
                    <button type="button" id="start_backfill" class="button button-primary">
                        <?php _e('Start Backfill', 'order-hub-sync'); ?>
                    </button>
                    <span id="backfill_status"></span>
                </p>
            </div>

            <div id="logs" class="tab-content" style="display: none;">
                <h3><?php _e('Debug Logs', 'order-hub-sync'); ?></h3>
                <?php if (get_option('ohs_debug_log')): ?>
                    <p><?php _e('Debug logging is enabled. Check your WordPress debug log for detailed information.', 'order-hub-sync'); ?></p>
                    <textarea readonly rows="20" cols="80" style="width: 100%; font-family: monospace;"><?php echo esc_textarea($this->get_debug_logs()); ?></textarea>
                <?php else: ?>
                    <p><?php _e('Debug logging is disabled. Enable it in the Settings tab to view logs.', 'order-hub-sync'); ?></p>
                <?php endif; ?>
            </div>
        </div>

        <script>
        jQuery(document).ready(function($) {
            // Tab switching
            $('.nav-tab').click(function(e) {
                e.preventDefault();
                var target = $(this).attr('href');
                
                $('.nav-tab').removeClass('nav-tab-active');
                $(this).addClass('nav-tab-active');
                
                $('.tab-content').hide();
                $(target).show();
            });

            // Backfill functionality
            $('#start_backfill').click(function() {
                var count = $('#backfill_count').val();
                var button = $(this);
                var status = $('#backfill_status');
                
                button.prop('disabled', true);
                status.html('<span style="color: blue;">Processing...</span>');
                
                $.ajax({
                    url: ohs_ajax.ajax_url,
                    type: 'POST',
                    data: {
                        action: 'ohs_backfill_orders',
                        count: count,
                        nonce: ohs_ajax.nonce
                    },
                    success: function(response) {
                        if (response.success) {
                            status.html('<span style="color: green;">' + response.data.message + '</span>');
                        } else {
                            status.html('<span style="color: red;">Error: ' + response.data + '</span>');
                        }
                    },
                    error: function() {
                        status.html('<span style="color: red;">Request failed</span>');
                    },
                    complete: function() {
                        button.prop('disabled', false);
                    }
                });
            });
        });
        </script>

        <style>
        .tab-content { margin-top: 20px; }
        .nav-tab { cursor: pointer; }
        </style>
        <?php
    }

    /**
     * General section callback
     */
    public function general_section_callback()
    {
        echo '<p>' . __('Configure your Order Hub connection settings below.', 'order-hub-sync') . '</p>';
    }

    /**
     * Hub URL callback
     */
    public function hub_url_callback()
    {
        $value = get_option('ohs_hub_url');
        echo '<input type="url" name="ohs_hub_url" value="' . esc_attr($value) . '" class="regular-text" />';
        echo '<p class="description">' . __('The base URL of your Order Hub (e.g., https://your-hub.onrender.com)', 'order-hub-sync') . '</p>';
    }

    /**
     * API Key callback
     */
    public function api_key_callback()
    {
        $value = get_option('ohs_api_key');
        echo '<input type="text" name="ohs_api_key" value="' . esc_attr($value) . '" class="regular-text" />';
        echo '<p class="description">' . __('Your site API key from Order Hub', 'order-hub-sync') . '</p>';
    }

    /**
     * API Secret callback
     */
    public function api_secret_callback()
    {
        $value = get_option('ohs_api_secret');
        echo '<input type="password" name="ohs_api_secret" value="' . esc_attr($value) . '" class="regular-text" />';
        echo '<p class="description">' . __('Your site API secret from Order Hub', 'order-hub-sync') . '</p>';
    }

    /**
     * Debug log callback
     */
    public function debug_log_callback()
    {
        $value = get_option('ohs_debug_log');
        echo '<input type="checkbox" name="ohs_debug_log" value="1" ' . checked(1, $value, false) . ' />';
        echo '<span class="description">' . __('Log detailed information to WordPress debug log', 'order-hub-sync') . '</span>';
    }

    /**
     * Get debug logs
     */
    private function get_debug_logs()
    {
        $log_file = WP_CONTENT_DIR . '/debug.log';
        if (file_exists($log_file)) {
            $logs = file_get_contents($log_file);
            // Filter for Order Hub related logs
            $lines = explode("\n", $logs);
            $filtered = array_filter($lines, function($line) {
                return strpos($line, 'Order Hub') !== false || strpos($line, 'OHS') !== false;
            });
            return implode("\n", $filtered);
        }
        return 'No debug log file found.';
    }
}

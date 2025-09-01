-- Create OrderProfits table
CREATE TABLE IF NOT EXISTS order_profits (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL,
    site_id UUID NOT NULL,
    wc_order_id VARCHAR(100) NOT NULL,
    order_total DECIMAL(18,2) NOT NULL,
    product_cost DECIMAL(18,2) NOT NULL,
    gateway_cost_percentage DECIMAL(18,2) NOT NULL,
    gateway_cost DECIMAL(18,2) NOT NULL,
    operational_cost DECIMAL(18,2) NOT NULL,
    total_costs DECIMAL(18,2) NOT NULL,
    net_profit DECIMAL(18,2) NOT NULL,
    profit_margin DECIMAL(18,2) NOT NULL,
    payout_status VARCHAR(20) NOT NULL DEFAULT 'processing',
    payout_date TIMESTAMP,
    is_calculated BOOLEAN NOT NULL DEFAULT false,
    notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_order_profits_order_id ON order_profits(order_id);
CREATE INDEX IF NOT EXISTS idx_order_profits_site_id ON order_profits(site_id);
CREATE INDEX IF NOT EXISTS idx_order_profits_wc_order_id ON order_profits(wc_order_id);
CREATE INDEX IF NOT EXISTS idx_order_profits_payout_status ON order_profits(payout_status);
CREATE INDEX IF NOT EXISTS idx_order_profits_created_at ON order_profits(created_at);

-- Add foreign key constraints
ALTER TABLE order_profits 
ADD CONSTRAINT fk_order_profits_order_id 
FOREIGN KEY (order_id) REFERENCES orders_v2(id) ON DELETE CASCADE;

ALTER TABLE order_profits 
ADD CONSTRAINT fk_order_profits_site_id 
FOREIGN KEY (site_id) REFERENCES sites(id) ON DELETE CASCADE;

-- Add comments
COMMENT ON TABLE order_profits IS 'Stores profit calculations for orders';
COMMENT ON COLUMN order_profits.payout_status IS 'Payout status: paid, processing, or refunded';
COMMENT ON COLUMN order_profits.gateway_cost_percentage IS 'Gateway cost as percentage of order total';
COMMENT ON COLUMN order_profits.gateway_cost IS 'Calculated gateway cost in dollars';
COMMENT ON COLUMN order_profits.operational_cost IS 'Fixed operational cost per order (default $5.00)';

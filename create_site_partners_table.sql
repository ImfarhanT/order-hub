-- Create site_partners table for tracking site-partner assignments
CREATE TABLE IF NOT EXISTS site_partners (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    site_id UUID NOT NULL,
    partner_id UUID NOT NULL,
    share_type VARCHAR(20) NOT NULL CHECK (share_type IN ('Profit', 'Revenue')),
    share_percentage DECIMAL(5,2) NOT NULL CHECK (share_percentage >= 0 AND share_percentage <= 100),
    is_active BOOLEAN NOT NULL DEFAULT true,
    notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    -- Foreign key constraints
    CONSTRAINT fk_site_partners_site FOREIGN KEY (site_id) REFERENCES sites("Id") ON DELETE CASCADE,
    CONSTRAINT fk_site_partners_partner FOREIGN KEY (partner_id) REFERENCES partners(id) ON DELETE CASCADE,
    
    -- Unique constraint to prevent duplicate assignments
    CONSTRAINT uk_site_partner UNIQUE (site_id, partner_id)
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_site_partners_site_id ON site_partners(site_id);
CREATE INDEX IF NOT EXISTS idx_site_partners_partner_id ON site_partners(partner_id);
CREATE INDEX IF NOT EXISTS idx_site_partners_active ON site_partners(is_active);

-- Insert sample data (optional)
INSERT INTO site_partners (site_id, partner_id, share_type, share_percentage, notes) VALUES
    (
        (SELECT "Id" FROM sites LIMIT 1), 
        (SELECT id FROM partners LIMIT 1), 
        'Profit', 
        15.00, 
        'Sample profit sharing agreement'
    )
ON CONFLICT (site_id, partner_id) DO NOTHING;

-- Add comment to table
COMMENT ON TABLE site_partners IS 'Tracks which partners are assigned to which sites and their share percentages';
COMMENT ON COLUMN site_partners.share_type IS 'Type of sharing: Profit or Revenue';
COMMENT ON COLUMN site_partners.share_percentage IS 'Percentage of profit/revenue to share with partner (0-100)';

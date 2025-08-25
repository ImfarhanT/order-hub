-- Create partners table
CREATE TABLE IF NOT EXISTS "partners" (
    "id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "name" varchar(100) NOT NULL,
    "email" varchar(100) NOT NULL UNIQUE,
    "phone" varchar(20),
    "company" varchar(100),
    "share_type" varchar(20) NOT NULL CHECK (share_type IN ('Profit', 'Revenue')),
    "share_percentage" decimal(5,2) NOT NULL CHECK (share_percentage >= 0 AND share_percentage <= 100),
    "address" text,
    "notes" text,
    "is_active" boolean NOT NULL DEFAULT true,
    "created_at" timestamp with time zone NOT NULL DEFAULT NOW(),
    "updated_at" timestamp with time zone NOT NULL DEFAULT NOW()
);

-- Create partner_orders table
CREATE TABLE IF NOT EXISTS "partner_orders" (
    "id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "partner_id" uuid NOT NULL REFERENCES "partners"("id") ON DELETE CASCADE,
    "order_id" uuid NOT NULL REFERENCES "orders_v2"("id") ON DELETE CASCADE,
    "order_total" decimal(10,2) NOT NULL,
    "share_amount" decimal(10,2) NOT NULL,
    "share_type" varchar(20) NOT NULL CHECK (share_type IN ('Profit', 'Revenue')),
    "share_percentage" decimal(5,2) NOT NULL CHECK (share_percentage >= 0 AND share_percentage <= 100),
    "is_paid" boolean NOT NULL DEFAULT false,
    "paid_at" timestamp with time zone,
    "created_at" timestamp with time zone NOT NULL DEFAULT NOW(),
    "updated_at" timestamp with time zone NOT NULL DEFAULT NOW()
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "ix_partners_email" ON "partners"("email");
CREATE INDEX IF NOT EXISTS "ix_partner_orders_partner_id" ON "partner_orders"("partner_id");
CREATE INDEX IF NOT EXISTS "ix_partner_orders_order_id" ON "partner_orders"("order_id");
CREATE INDEX IF NOT EXISTS "ix_partner_orders_is_paid" ON "partner_orders"("is_paid");

-- Insert sample partner data
INSERT INTO "partners" ("id", "name", "email", "phone", "company", "share_type", "share_percentage", "address", "notes", "is_active", "created_at", "updated_at")
VALUES 
    (gen_random_uuid(), 'John Smith', 'john@example.com', '+1234567890', 'Smith Enterprises', 'Profit', 15.00, '123 Business St, City, State', 'Key business partner', true, NOW(), NOW()),
    (gen_random_uuid(), 'Sarah Johnson', 'sarah@example.com', '+1987654321', 'Johnson Corp', 'Revenue', 10.00, '456 Corporate Ave, City, State', 'Marketing partner', true, NOW(), NOW()),
    (gen_random_uuid(), 'Mike Wilson', 'mike@example.com', '+1122334455', 'Wilson Solutions', 'Profit', 20.00, '789 Tech Blvd, City, State', 'Technology partner', true, NOW(), NOW())
ON CONFLICT ("email") DO NOTHING;

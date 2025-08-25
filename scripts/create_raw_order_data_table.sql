-- Create RawOrderData table for storing raw JSON from websites
CREATE TABLE IF NOT EXISTS "RawOrderData" (
    "Id" uuid NOT NULL,
    "SiteId" uuid NOT NULL,
    "SiteName" character varying(255) NOT NULL,
    "RawJson" text NOT NULL,
    "ReceivedAt" timestamp with time zone NOT NULL,
    "Processed" boolean NOT NULL DEFAULT false,
    "ProcessedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_RawOrderData" PRIMARY KEY ("Id")
);

-- Add foreign key constraint
ALTER TABLE "RawOrderData" 
ADD CONSTRAINT "FK_RawOrderData_Sites_SiteId" 
FOREIGN KEY ("SiteId") REFERENCES "sites"("Id") ON DELETE CASCADE;

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS "IX_RawOrderData_SiteId" ON "RawOrderData" ("SiteId");
CREATE INDEX IF NOT EXISTS "IX_RawOrderData_ReceivedAt" ON "RawOrderData" ("ReceivedAt");

-- Add comment
COMMENT ON TABLE "RawOrderData" IS 'Temporary table for storing raw JSON data from websites before parsing into structured orders';



CREATE TABLE IF NOT EXISTS sync_state (
    id                          INT         PRIMARY KEY DEFAULT 1,
    last_tender_date_modified   TIMESTAMPTZ,
    last_synced_at              TIMESTAMPTZ,
    CONSTRAINT single_row CHECK (id = 1)
);

INSERT INTO sync_state (id) VALUES (1) ON CONFLICT DO NOTHING;



CREATE TABLE IF NOT EXISTS tenders (
    id              TEXT        PRIMARY KEY,
    cpv_code        TEXT        NOT NULL,
    status          TEXT        NOT NULL,
    initial_budget  NUMERIC(18, 2),
    buyer_name      TEXT        NOT NULL,
    date_modified   TIMESTAMPTZ NOT NULL,
    extra_data      JSONB
);

CREATE INDEX IF NOT EXISTS idx_tenders_date_modified ON tenders (date_modified DESC);
CREATE INDEX IF NOT EXISTS idx_tenders_buyer_name    ON tenders (buyer_name);
CREATE INDEX IF NOT EXISTS idx_tenders_extra_data    ON tenders USING GIN (extra_data);



CREATE TABLE IF NOT EXISTS tender_contracts (
    id                  BIGSERIAL   PRIMARY KEY,
    tender_id           TEXT        NOT NULL REFERENCES tenders (id) ON DELETE CASCADE,
    prozorro_id         TEXT        NOT NULL,
    contract_amount     NUMERIC(18, 2),
    buyer_name          TEXT        NOT NULL DEFAULT '',
    initial_budget      NUMERIC(18, 2),
    UNIQUE (tender_id, prozorro_id)
);

CREATE INDEX IF NOT EXISTS idx_contracts_analytics
    ON tender_contracts (buyer_name) INCLUDE (contract_amount, initial_budget);
CREATE INDEX IF NOT EXISTS idx_contracts_tender_id
    ON tender_contracts (tender_id);



CREATE TABLE IF NOT EXISTS tender_suppliers (
    id              BIGSERIAL   PRIMARY KEY,
    tender_id       TEXT        NOT NULL REFERENCES tenders (id) ON DELETE CASCADE,
    supplier_name   TEXT        NOT NULL,
    UNIQUE (tender_id, supplier_name)
);

CREATE INDEX IF NOT EXISTS idx_suppliers_tender_id     ON tender_suppliers (tender_id) INCLUDE (supplier_name);
CREATE INDEX IF NOT EXISTS idx_suppliers_supplier_name ON tender_suppliers (supplier_name);


-- ============================================================
-- Pre-aggregated analytics tables (refreshed at end of each import)
-- ============================================================

CREATE TABLE IF NOT EXISTS analytics_summary (
    id              INT            PRIMARY KEY DEFAULT 1,
    total_savings   NUMERIC(18,2)  NOT NULL DEFAULT 0,
    last_updated_at TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    CONSTRAINT analytics_single_row CHECK (id = 1)
);
INSERT INTO analytics_summary (id) VALUES (1) ON CONFLICT DO NOTHING;

CREATE TABLE IF NOT EXISTS analytics_buyers (
    buyer_name   TEXT           PRIMARY KEY,
    total_amount NUMERIC(18,2)  NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_analytics_buyers_amount ON analytics_buyers (total_amount DESC);

CREATE TABLE IF NOT EXISTS analytics_suppliers (
    supplier_name TEXT           PRIMARY KEY,
    total_amount  NUMERIC(18,2)  NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_analytics_suppliers_amount ON analytics_suppliers (total_amount DESC);



-- ============================================================
-- Analytics queries (for reference / AnalyticsRepository)
-- ============================================================

-- Budget savings: SUM(initial_budget - contract_amount)
-- SELECT COALESCE(SUM(t.initial_budget - c.contract_amount), 0) AS total_savings
-- FROM tenders t
-- JOIN tender_contracts c ON c.tender_id = t.id;

-- Top 5 buyers by total contract amount
-- SELECT t.buyer_name, SUM(c.contract_amount) AS total_amount
-- FROM tenders t
-- JOIN tender_contracts c ON c.tender_id = t.id
-- GROUP BY t.buyer_name
-- ORDER BY total_amount DESC
-- LIMIT 5;

-- Top 5 suppliers by total contract amount
-- (each supplier gets credit for all contracts on tenders they appear in)
-- SELECT s.supplier_name, SUM(c.contract_amount) AS total_amount
-- FROM tender_suppliers s
-- JOIN tender_contracts c ON c.tender_id = s.tender_id
-- GROUP BY s.supplier_name
-- ORDER BY total_amount DESC
-- LIMIT 5;
